param(
    [string]$BaseUrl = "http://localhost:50895",
    [string]$AdminPassword = "Admin@Tabseet2026!",
    [string]$TeacherPassword = "Teach@Tabseet2026!",
    [string]$StudentPassword = "Std@Tabseet2026!"
)

# اختبارات آلية (2026-10-27):
#   [1] صفحة "إشعارات المدرسين" للإدارة تعمل بلا [Authorize(Roles)] (كانت تعطي خطأ aspnetdb.mdf) مع رفض غير المصرح لهم.
#   [2] اختبار (وظيفة) المحاضرة يظهر للطالب في "برامجي التعليمية" (CategoriesStd) للمحاضرات المسجلة والمباشرة.
#   [3] اختبار المحاضرة المباشرة يُتاح بعد تسجيل الدخول إليها، واجتيازه يكمل المحاضرة.
#   [4] حفظ وظيفة بأسئلة من بنك الأسئلة على محاضرة مباشرة عبر نقطتي الإدارة والمعلم الحقيقيتين (AssingQuestionToAssignment)،
#       ثم ظهورها للطالب في "برامجي التعليمية" وإعادة أسئلتها كاملة له بعد دخول المحاضرة (2026-10-27).
# يستخدم student01 (مسجل في الدورة 6) وينشئ محاضرات/وظائف باسم QA_ASG_* في الدورة 6 ثم يحذفها.

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Continue"

$results = [System.Collections.Generic.List[PSObject]]::new()

function Record-Test {
    param([string]$TestId, [string]$Description, [bool]$Passed, [string]$Details)
    $obj = [PSCustomObject]@{ TestId = $TestId; Description = $Description; Result = $(if ($Passed) { "PASS" } else { "FAIL" }); Details = $Details }
    $results.Add($obj)
    $color = if ($Passed) { "Green" } else { "Red" }
    Write-Host "[$($obj.Result)] $TestId : $Description" -ForegroundColor $color
    if (-not $Passed) { Write-Host "       Details: $Details" -ForegroundColor Yellow }
}

function Sql {
    param([string]$Query)
    $out = & sqlcmd -S . -d TabseetDB_Local -E -h -1 -W -Q "SET NOCOUNT ON; $Query"
    return (($out | Where-Object { $_ -ne $null }) -join "`n").Trim()
}

function Get-AuthSession {
    param([string]$Email, [string]$Password)
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $loginPage = Invoke-WebRequest -Uri "$BaseUrl/Home/LogIn" -WebSession $session -UseBasicParsing
    $token = [regex]::Match($loginPage.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value
    $null = Invoke-WebRequest -Uri "$BaseUrl/Home/LogIn" -Method POST -Body @{ "__RequestVerificationToken" = $token; "Email" = $Email; "Password" = $Password } -WebSession $session -UseBasicParsing
    return $session
}

function Get-VerificationToken {
    param($Session, [string]$Url)
    $page = Invoke-WebRequest -Uri $Url -WebSession $Session -UseBasicParsing
    return [regex]::Match($page.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value
}

function Invoke-Post {
    param($Session, [string]$Url, $Body, [string]$ContentType = "")
    $status = 0; $content = ""; $json = $null
    try {
        if ($ContentType) { $res = Invoke-WebRequest -Uri $Url -Method POST -Body $Body -ContentType $ContentType -WebSession $Session -UseBasicParsing }
        # نوع المحتوى يُمرَّر صراحةً: جلسة PowerShell تحتفظ بـ application/json بعد أي طلب JSON سابق فتُفسَّر النماذج التالية كـ JSON (خطأ 500 "Invalid JSON primitive")
        else { $res = Invoke-WebRequest -Uri $Url -Method POST -Body $Body -ContentType "application/x-www-form-urlencoded" -WebSession $Session -UseBasicParsing }
        $status = [int]$res.StatusCode; $content = $res.Content
    } catch {
        if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
        $content = $_.ErrorDetails.Message
        if (-not $content) { $content = $_.Exception.Message }
    }
    try { $json = $content | ConvertFrom-Json } catch { }
    return [PSCustomObject]@{ Status = $status; Json = $json; Raw = $content }
}

function Invoke-Get {
    param($Session, [string]$Url, [switch]$NoRedirect)
    $status = 0; $content = ""; $json = $null; $location = ""
    try {
        if ($NoRedirect) { $res = Invoke-WebRequest -Uri $Url -WebSession $Session -UseBasicParsing -MaximumRedirection 0 -ErrorAction Stop }
        else { $res = Invoke-WebRequest -Uri $Url -WebSession $Session -UseBasicParsing }
        $status = [int]$res.StatusCode; $content = $res.Content
        if ($res.Headers.Location) { $location = [string]$res.Headers.Location }
    } catch {
        if ($_.Exception.Response) {
            $status = [int]$_.Exception.Response.StatusCode
            try { $location = [string]$_.Exception.Response.Headers.Location } catch { }
        }
        $content = $_.ErrorDetails.Message
        if (-not $content) { $content = $_.Exception.Message }
    }
    try { $json = $content | ConvertFrom-Json } catch { }
    return [PSCustomObject]@{ Status = $status; Json = $json; Raw = $content; Location = $location }
}

function Get-Row {
    param([string]$StudentId, [int]$LectureId)
    $raw = Sql "SELECT ISNULL(IsFinish,-1), ISNULL(IsTimeEnd,-1), ISNULL(IsAssingmetFinish,-1), WatchedSeconds, ISNULL(Source,'NULL'), CASE WHEN CompletedAt IS NULL THEN 0 ELSE 1 END FROM std_lecture WHERE Std_ID='$StudentId' AND Lecture_ID=$LectureId;"
    if (-not $raw) { return $null }
    $p = ($raw -split '\s+')
    return [PSCustomObject]@{ IsFinish = [int]$p[0]; IsTimeEnd = [int]$p[1]; IsAssign = [int]$p[2]; Watched = [int]$p[3]; Source = $p[4]; HasCompleted = [int]$p[5] }
}

# سمات عنصر المحاضرة المخفي الذي تبني منه صفحة الطالب القائمة: <div class="sub-data-lecture" data-id=".." data-assign-id=".." ...>
function Get-LectureAttrs {
    param([string]$Html, [int]$LectureId)
    $m = [regex]::Match($Html, '<div class="sub-data-lecture"\s+data-id="' + $LectureId + '"[^>]*>')
    if (-not $m.Success) { return $null }
    $tag = $m.Value
    $get = { param($n) $x = [regex]::Match($tag, 'data-' + $n + '="([^"]*)"'); if ($x.Success) { $x.Groups[1].Value } else { "" } }
    return [PSCustomObject]@{ Type = (& $get 'type'); IsFinish = (& $get 'is-finish'); TimeEnd = (& $get 'time-end'); AssignId = (& $get 'assign-id'); AssignName = (& $get 'assign-name'); AssignDone = (& $get 'assign-done') }
}

$student = "TEST-USR-STD-01"
$CAT = 6

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Admin notifications + lecture assignments - test suite" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

Write-Host "`n[1] Admin notifications page (no [Authorize(Roles)])" -ForegroundColor Yellow
$anon = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$r = Invoke-Get -Session $anon -Url "$BaseUrl/AdminNotifications/Index" -NoRedirect
$pass = ($r.Status -eq 302) -and ($r.Location -match 'Home/LogIn')
Record-Test -TestId "AN-01" -Description "زائر غير مسجّل: Index → تحويل إلى صفحة الدخول" -Passed $pass -Details "status=$($r.Status) location=$($r.Location)"

$admin = Get-AuthSession -Email "admin.test@example.test" -Password $AdminPassword
$r = Invoke-Get -Session $admin -Url "$BaseUrl/AdminNotifications/Index"
$pass = ($r.Status -eq 200) -and ($r.Raw -match 'إشعارات المدرسين') -and ($r.Raw -notmatch 'Server Error') -and ($r.Raw -notmatch 'Failed to generate a user instance')
Record-Test -TestId "AN-02" -Description "الإدارة: صفحة إشعارات المدرسين تفتح (لا خطأ aspnetdb.mdf)" -Passed $pass -Details "status=$($r.Status) len=$($r.Raw.Length)"

$r = Invoke-Get -Session $admin -Url "$BaseUrl/AdminNotifications/GetUnreadCount"
$pass = ($r.Status -eq 200) -and $r.Json -and ($r.Json.success -eq $true)
Record-Test -TestId "AN-03" -Description "الإدارة: GetUnreadCount يعيد JSON ناجحاً (عدّاد الجرس)" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw)"

$r = Invoke-Get -Session $admin -Url "$BaseUrl/AdminNotifications/GetLatest?count=5"
$pass = ($r.Status -eq 200) -and $r.Json -and ($r.Json.success -eq $true) -and ($null -ne $r.Json.items)
Record-Test -TestId "AN-04" -Description "الإدارة: GetLatest يعيد قائمة الإشعارات" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw.Substring(0, [Math]::Min(200, $r.Raw.Length)))"

$teacher = Get-AuthSession -Email "teacher.ibrahim@example.test" -Password $TeacherPassword
$r = Invoke-Get -Session $teacher -Url "$BaseUrl/AdminNotifications/Index" -NoRedirect
$pass = ($r.Status -eq 302) -and ($r.Location -match 'Home/LogIn')
Record-Test -TestId "AN-05" -Description "مدرس (بلا دور Admin): Index → تحويل إلى صفحة الدخول وليس خطأ خادم" -Passed $pass -Details "status=$($r.Status) location=$($r.Location)"

$r = Invoke-Get -Session $teacher -Url "$BaseUrl/AdminNotifications/GetUnreadCount"
$pass = ($r.Status -eq 403) -and $r.Json -and ($r.Json.success -eq $false)
Record-Test -TestId "AN-06" -Description "مدرس: GetUnreadCount → 403 JSON" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw)"

$std = Get-AuthSession -Email "student01@example.test" -Password $StudentPassword
$r = Invoke-Get -Session $std -Url "$BaseUrl/AdminNotifications/GetLatest?count=5"
$pass = ($r.Status -eq 403)
Record-Test -TestId "AN-07" -Description "طالب: GetLatest → 403" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw)"

Write-Host "`n[2] Fixtures: QA_ASG_* lectures/assignments in category $CAT" -ForegroundColor Yellow
$null = Sql "DELETE FROM std_lecture WHERE Lecture_ID IN (SELECT ID FROM Lectures WHERE Lecture_Name LIKE 'QA_ASG_%');"
$null = Sql "DELETE FROM Assignment_Q WHERE Assignment_ID IN (SELECT ID FROM Assignments WHERE Name LIKE 'QA_ASG_%');"
$null = Sql "DELETE FROM Assignments WHERE Name LIKE 'QA_ASG_%' OR Lecture_ID IN (SELECT ID FROM Lectures WHERE Lecture_Name LIKE 'QA_ASG_%');"
$null = Sql "DELETE FROM Lectures WHERE Lecture_Name LIKE 'QA_ASG_%';"
$nowStr = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
$null = Sql "INSERT INTO Lectures (Category_ID, Lecture_Name, LectureTypeOnOff, DayandTime, LectureLink_Client, LectureLink_Host, LectureMinTime) VALUES ($CAT, 'QA_ASG_REC', '0', '', 'https://www.youtube.com/watch?v=qa_asg_rec', '', '1');"
$null = Sql "INSERT INTO Lectures (Category_ID, Lecture_Name, LectureTypeOnOff, DayandTime, LectureLink_Client, LectureLink_Host, LectureMinTime) VALUES ($CAT, 'QA_ASG_LIVE', '1', '$nowStr', 'https://meet.google.com/qa-asg-live', '', '0');"
$null = Sql "INSERT INTO Lectures (Category_ID, Lecture_Name, LectureTypeOnOff, DayandTime, LectureLink_Client, LectureLink_Host, LectureMinTime) VALUES ($CAT, 'QA_ASG_LIVE_NOASG', '1', '$nowStr', 'https://meet.google.com/qa-asg-live2', '', '0');"
$null = Sql "INSERT INTO Lectures (Category_ID, Lecture_Name, LectureTypeOnOff, DayandTime, LectureLink_Client, LectureLink_Host, LectureMinTime) VALUES ($CAT, 'QA_ASG_LIVE_SAVE', '1', '$nowStr', 'https://meet.google.com/qa-asg-save', '', '0');"
$null = Sql "INSERT INTO Lectures (Category_ID, Lecture_Name, LectureTypeOnOff, DayandTime, LectureLink_Client, LectureLink_Host, LectureMinTime) VALUES ($CAT, 'QA_ASG_LIVE_TSAVE', '1', '$nowStr', 'https://meet.google.com/qa-asg-tsave', '', '0');"
$LEC_REC = [int](Sql "SELECT ID FROM Lectures WHERE Lecture_Name='QA_ASG_REC';")
$LEC_LIVE = [int](Sql "SELECT ID FROM Lectures WHERE Lecture_Name='QA_ASG_LIVE';")
$LEC_LIVE2 = [int](Sql "SELECT ID FROM Lectures WHERE Lecture_Name='QA_ASG_LIVE_NOASG';")
$null = Sql "INSERT INTO Assignments (Name, Description, Lecture_ID, Assignmet_Degry, Assignmet_degry_Win) VALUES ('QA_ASG_A_REC', 'QA', $LEC_REC, 10, 1);"
$null = Sql "INSERT INTO Assignments (Name, Description, Lecture_ID, Assignmet_Degry, Assignmet_degry_Win) VALUES ('QA_ASG_A_LIVE', 'QA', $LEC_LIVE, 10, 1);"
$A_REC = [int](Sql "SELECT ID FROM Assignments WHERE Name='QA_ASG_A_REC';")
$A_LIVE = [int](Sql "SELECT ID FROM Assignments WHERE Name='QA_ASG_A_LIVE';")
Write-Host "Lectures: rec=$LEC_REC live=$LEC_LIVE live2=$LEC_LIVE2 | Assignments: rec=$A_REC live=$A_LIVE"
if ($LEC_REC -eq 0 -or $LEC_LIVE -eq 0 -or $A_REC -eq 0 -or $A_LIVE -eq 0) { throw "Fixture creation failed" }
$LEC_SAVE = [int](Sql "SELECT ID FROM Lectures WHERE Lecture_Name='QA_ASG_LIVE_SAVE';")
$LEC_TSAVE = [int](Sql "SELECT ID FROM Lectures WHERE Lecture_Name='QA_ASG_LIVE_TSAVE';")
if ($LEC_SAVE -eq 0 -or $LEC_TSAVE -eq 0) { throw "Fixture creation failed (QA_ASG_LIVE_SAVE / QA_ASG_LIVE_TSAVE)" }

$stdToken = Get-VerificationToken -Session $std -Url "$BaseUrl/Students/CategoriesStd"
if (-not $stdToken) { throw "Student token not found on CategoriesStd" }
$pageUrl = "$BaseUrl/Students/CategoriesStd"

Write-Host "`n[3] Student programs page exposes lecture assignments" -ForegroundColor Yellow
$page = Invoke-Get -Session $std -Url $pageUrl
$a = Get-LectureAttrs -Html $page.Raw -LectureId $LEC_REC
$pass = $a -and ($a.Type -eq "0") -and ($a.AssignId -eq "$A_REC") -and ($a.AssignName -eq "QA_ASG_A_REC") -and ($a.TimeEnd -eq "0") -and ($a.AssignDone -eq "0") -and ($a.IsFinish -eq "0")
Record-Test -TestId "ST-01" -Description "محاضرة مسجلة ذات وظيفة: الصفحة ترسل معرّف الوظيفة واسمها (قبل المشاهدة: الاختبار غير متاح بعد)" -Passed $pass -Details "attrs=$($a | ConvertTo-Json -Compress)"

$a = Get-LectureAttrs -Html $page.Raw -LectureId $LEC_LIVE
$pass = $a -and ($a.Type -eq "1") -and ($a.AssignId -eq "$A_LIVE") -and ($a.TimeEnd -eq "0") -and ($a.AssignDone -eq "0")
Record-Test -TestId "ST-02" -Description "محاضرة مباشرة ذات وظيفة: الصفحة ترسل معرّف الوظيفة (قبل الدخول: الاختبار غير متاح بعد)" -Passed $pass -Details "attrs=$($a | ConvertTo-Json -Compress)"

$a = Get-LectureAttrs -Html $page.Raw -LectureId $LEC_LIVE2
$pass = $a -and ($a.AssignId -eq "0")
Record-Test -TestId "ST-03" -Description "محاضرة مباشرة بلا وظيفة: assign-id = 0" -Passed $pass -Details "attrs=$($a | ConvertTo-Json -Compress)"

$pass = ($page.Raw -match 'sub-btn-lecexam') -and ($page.Raw -match 'tabseet:livejoined') -and ($page.Raw -match 'canTakeExam') -and ($page.Raw -match '/Students/AssignmentQuestion')
Record-Test -TestId "ST-04" -Description "سكربت الصفحة يحوي زر الاختبار، شرط الإتاحة، ومستمع دخول المحاضرة المباشرة" -Passed $pass -Details "len=$($page.Raw.Length)"

Write-Host "`n[4] Live lecture: exam available after joining; passing it completes the lecture" -ForegroundColor Yellow
$joinUrl = "$BaseUrl/Students/JoinLiveLecture"
$submitUrl = "$BaseUrl/Students/SubmetQuestion"
$r = Invoke-Post -Session $std -Url $joinUrl -Body @{ "__RequestVerificationToken" = $stdToken; lectureId = $LEC_LIVE }
$pass = ($r.Status -eq 200) -and $r.Json.success -and $r.Json.status.isTimeEnd -and $r.Json.status.hasAssignment -and (-not $r.Json.status.isAssignmentFinish) -and (-not $r.Json.status.isFinish)
Record-Test -TestId "LV-01" -Description "دخول محاضرة مباشرة ذات وظيفة: IsTimeEnd=1 والاكتمال معلّق على اجتياز الاختبار" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw)"

$row = Get-Row $student $LEC_LIVE
$pass = $row -and ($row.IsTimeEnd -eq 1) -and ($row.IsFinish -eq 0) -and ($row.IsAssign -eq 0) -and ($row.Source -eq "Live")
Record-Test -TestId "LV-02" -Description "قاعدة البيانات: صف الحضور Live مع IsFinish=0" -Passed $pass -Details "row=$($row | ConvertTo-Json -Compress)"

$page = Invoke-Get -Session $std -Url $pageUrl
$a = Get-LectureAttrs -Html $page.Raw -LectureId $LEC_LIVE
$pass = $a -and ($a.TimeEnd -eq "1") -and ($a.AssignDone -eq "0") -and ($a.IsFinish -eq "0") -and ($a.AssignId -eq "$A_LIVE")
Record-Test -TestId "LV-03" -Description "بعد الدخول: الصفحة ترسل time-end=1 → زر اختبار المحاضرة المباشرة يظهر" -Passed $pass -Details "attrs=$($a | ConvertTo-Json -Compress)"

$r = Invoke-Get -Session $std -Url "$BaseUrl/Students/AssignmentQuestion?A_Q=$A_LIVE"
$pass = ($r.Status -eq 200) -and ($r.Raw -match 'GetAssignmentQuestion')
Record-Test -TestId "LV-04" -Description "صفحة حل وظيفة المحاضرة المباشرة تفتح للطالب" -Passed $pass -Details "status=$($r.Status) len=$($r.Raw.Length)"

$r = Invoke-Post -Session $std -Url $submitUrl -Body "L_Degry=5&ass_ID=$A_LIVE" -ContentType "application/x-www-form-urlencoded"
$pass = ($r.Status -eq 200) -and ($r.Json.success -eq $true)
Record-Test -TestId "LV-05" -Description "تسليم وظيفة المباشرة بدرجة نجاح → success" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw)"

$row = Get-Row $student $LEC_LIVE
$pass = $row -and ($row.IsAssign -eq 1) -and ($row.IsFinish -eq 1) -and ($row.HasCompleted -eq 1)
Record-Test -TestId "LV-06" -Description "اجتياز الوظيفة يكمل المحاضرة المباشرة (IsAssingmetFinish=1, IsFinish=1)" -Passed $pass -Details "row=$($row | ConvertTo-Json -Compress)"

$page = Invoke-Get -Session $std -Url $pageUrl
$a = Get-LectureAttrs -Html $page.Raw -LectureId $LEC_LIVE
$pass = $a -and ($a.AssignDone -eq "1") -and ($a.IsFinish -eq "1")
Record-Test -TestId "LV-07" -Description "الصفحة تعرض اجتياز الاختبار واكتمال المحاضرة المباشرة" -Passed $pass -Details "attrs=$($a | ConvertTo-Json -Compress)"

$r = Invoke-Post -Session $std -Url $joinUrl -Body @{ "__RequestVerificationToken" = $stdToken; lectureId = $LEC_LIVE2 }
$pass = ($r.Status -eq 200) -and $r.Json.success -and $r.Json.status.isFinish -and (-not $r.Json.status.hasAssignment)
Record-Test -TestId "LV-08" -Description "محاضرة مباشرة بلا وظيفة: تكتمل بمجرد الدخول (السلوك السابق بلا تغيير)" -Passed $pass -Details "resp=$($r.Raw)"

Write-Host "`n[5] Recorded lecture: exam available after required watch time" -ForegroundColor Yellow
$startUrl = "$BaseUrl/Students/StartLecture"
$hbUrl = "$BaseUrl/Students/LectureHeartbeat"
$r = Invoke-Post -Session $std -Url $startUrl -Body @{ "__RequestVerificationToken" = $stdToken; lectureId = $LEC_REC; durationSeconds = 3600 }
$null = Sql "UPDATE std_lecture SET LastHeartbeatAt = DATEADD(minute, -2, SYSUTCDATETIME()) WHERE Std_ID='$student' AND Lecture_ID=$LEC_REC;"
$r = Invoke-Post -Session $std -Url $hbUrl -Body @{ "__RequestVerificationToken" = $stdToken; lectureId = $LEC_REC; playedSeconds = 60; durationSeconds = 3600 }
$pass = ($r.Status -eq 200) -and $r.Json.success -and $r.Json.status.isTimeEnd -and $r.Json.status.hasAssignment -and (-not $r.Json.status.isFinish)
Record-Test -TestId "RC-01" -Description "إكمال مشاهدة مسجلة ذات وظيفة: IsTimeEnd=1 والاكتمال معلّق على الاختبار" -Passed $pass -Details "resp=$($r.Raw)"

$page = Invoke-Get -Session $std -Url $pageUrl
$a = Get-LectureAttrs -Html $page.Raw -LectureId $LEC_REC
$pass = $a -and ($a.TimeEnd -eq "1") -and ($a.AssignDone -eq "0") -and ($a.IsFinish -eq "0")
Record-Test -TestId "RC-02" -Description "بعد المشاهدة: الصفحة ترسل time-end=1 → زر الاختبار يظهر للمسجلة" -Passed $pass -Details "attrs=$($a | ConvertTo-Json -Compress)"

$r = Invoke-Post -Session $std -Url $submitUrl -Body "L_Degry=0&ass_ID=$A_REC" -ContentType "application/x-www-form-urlencoded"
$row = Get-Row $student $LEC_REC
$pass = ($r.Status -eq 200) -and ($r.Json.success -eq $false) -and $row -and ($row.IsAssign -eq 0) -and ($row.IsFinish -eq 0)
Record-Test -TestId "RC-03" -Description "رسوب في الوظيفة: لا يُسجَّل اجتياز ولا اكتمال" -Passed $pass -Details "resp=$($r.Raw) row=$($row | ConvertTo-Json -Compress)"

$r = Invoke-Post -Session $std -Url $submitUrl -Body "L_Degry=5&ass_ID=$A_REC" -ContentType "application/x-www-form-urlencoded"
$row = Get-Row $student $LEC_REC
$pass = ($r.Status -eq 200) -and ($r.Json.success -eq $true) -and $row -and ($row.IsAssign -eq 1) -and ($row.IsFinish -eq 1)
Record-Test -TestId "RC-04" -Description "اجتياز وظيفة المسجلة يكمل المحاضرة" -Passed $pass -Details "resp=$($r.Raw) row=$($row | ConvertTo-Json -Compress)"

$page = Invoke-Get -Session $std -Url $pageUrl
$a = Get-LectureAttrs -Html $page.Raw -LectureId $LEC_REC
$pass = $a -and ($a.AssignDone -eq "1") -and ($a.IsFinish -eq "1")
Record-Test -TestId "RC-05" -Description "الصفحة تعرض اجتياز الاختبار واكتمال المحاضرة المسجلة" -Passed $pass -Details "attrs=$($a | ConvertTo-Json -Compress)"

Write-Host "`n[6] Admin / teacher lecture lists allow assignments on live lectures" -ForegroundColor Yellow
$r = Invoke-Post -Session $admin -Url "$BaseUrl/ControlPanel/AllLectures" -Body @{ byCategorie = $CAT }
$live = $null
if ($r.Json) { $live = @($r.Json) | Where-Object { $_.ID -eq $LEC_LIVE } | Select-Object -First 1 }
$pass = ($r.Status -eq 200) -and $live -and ([int]$live.Count -eq 1) -and ("$($live.LectureTypeOnOff)" -eq "1")
Record-Test -TestId "AD-01" -Description "قائمة محاضرات الإدارة تعيد عدد الوظائف للمحاضرة المباشرة (Count=1)" -Passed $pass -Details "status=$($r.Status) live=$($live | ConvertTo-Json -Compress)"

$root = Split-Path -Parent $PSScriptRoot
$adminView = Get-Content (Join-Path $root "tabseetCourse\Views\ControlPanel\LecturesByCategory.cshtml") -Raw
$pass = ($adminView -notmatch 'LectureTypeOnOff == 0\)\s*\{\s*if \(data\[i\]\.Count') -and ($adminView -match "addassignment\(") -and ($adminView -match "ViewAssingment\(")
Record-Test -TestId "AD-02" -Description "لوحة المسؤول: زرا الوظيفة لم يعودا مشروطين بالمحاضرة المسجلة" -Passed $pass -Details ""

$teacherView = Get-Content (Join-Path $root "tabseetCourse\Views\Teacher\LecturesByCategoryStudend.cshtml") -Raw
$pass = ($teacherView -notmatch 'LectureTypeOnOff == 0\)\s*\{\s*if \(data\[i\]\.Count') -and ($teacherView -match "addassignment\(") -and ($teacherView -match "ViewAssingment\(")
Record-Test -TestId "AD-03" -Description "لوحة المعلم: زرا الوظيفة لم يعودا مشروطين بالمحاضرة المسجلة" -Passed $pass -Details ""

$r = Invoke-Get -Session $admin -Url "$BaseUrl/ControlPanel/AddAssignment?Lecture_ID=$LEC_LIVE2&categoryName=QA"
$pass = ($r.Status -eq 200) -and ($r.Raw -match 'QA_ASG_LIVE_NOASG')
Record-Test -TestId "AD-04" -Description "الإدارة: صفحة إضافة وظيفة تفتح لمحاضرة مباشرة" -Passed $pass -Details "status=$($r.Status)"

Write-Host "`n[6b] Saving an assignment with bank questions on a live lecture (real admin/teacher endpoints) and showing it to the student" -ForegroundColor Yellow
$Q_TF = [int](Sql "SELECT ISNULL(MIN(ID),0) FROM Questions WHERE Category_ID=$CAT AND Type=1;")
$Q_MC = [int](Sql "SELECT ISNULL(MIN(ID),0) FROM Questions WHERE Category_ID=$CAT AND Type=2;")
if ($Q_TF -eq 0 -or $Q_MC -eq 0) { throw "Need a true/false and a multiple-choice bank question in category $CAT" }

# الإدارة: نفس الطلب الذي ترسله صفحة «اضافة وظيفة» (JSON إلى AssingQuestionToAssignment) لمحاضرة مباشرة
$body = @{ Name = "QA_ASG_A_SAVED"; Description = "QA"; Lecture_ID = $LEC_SAVE; Assignmet_Degry = 10; Assignmet_degry_Win = 1; L_Assignment_Q = @(@{ Q_ID = $Q_TF }, @{ Q_ID = $Q_MC }) } | ConvertTo-Json -Compress -Depth 4
$r = Invoke-Post -Session $admin -Url "$BaseUrl/ControlPanel/AssingQuestionToAssignment" -Body $body -ContentType "application/json; charset=utf-8"
$A_SAVED = [int](Sql "SELECT ISNULL(MAX(ID),0) FROM Assignments WHERE Name='QA_ASG_A_SAVED' AND Lecture_ID=$LEC_SAVE;")
$savedQ = [int](Sql "SELECT COUNT(*) FROM Assignment_Q WHERE Assignment_ID=$A_SAVED AND Q_ID IN ($Q_TF,$Q_MC);")
$pass = ($r.Status -eq 200) -and ($r.Json.success -eq $true) -and ($A_SAVED -gt 0) -and ($savedQ -eq 2)
Record-Test -TestId "SV-01" -Description "الإدارة: حفظ وظيفة بسؤالين من بنك الأسئلة على محاضرة مباشرة (AssingQuestionToAssignment) → صف Assignments + صفّا Assignment_Q" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw) asg=$A_SAVED savedQ=$savedQ"

$r = Invoke-Post -Session $admin -Url "$BaseUrl/ControlPanel/GetAssignmentByID" -Body @{ Assingment_ID = $A_SAVED }
$pass = ($r.Status -eq 200) -and ($r.Json.ID -eq $A_SAVED) -and ($r.Json.Lecture_ID -eq $LEC_SAVE) -and (@($r.Json.L_Assignment_Q).Count -eq 2)
Record-Test -TestId "SV-02" -Description "الإدارة: «عرض الوظيفة» (GetAssignmentByID) يعيد الوظيفة المحفوظة بسؤاليها" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw.Substring(0, [Math]::Min(300, $r.Raw.Length)))"

$r = Invoke-Post -Session $admin -Url "$BaseUrl/ControlPanel/AllLectures" -Body @{ byCategorie = $CAT }
$saved = $null
if ($r.Json) { $saved = @($r.Json) | Where-Object { $_.ID -eq $LEC_SAVE } | Select-Object -First 1 }
$pass = ($r.Status -eq 200) -and $saved -and ([int]$saved.Count -eq 1) -and ("$($saved.LectureTypeOnOff)" -eq "1")
Record-Test -TestId "SV-03" -Description "قائمة محاضرات الإدارة: المحاضرة المباشرة أصبحت تحمل وظيفة (Count=1 → زر «عرض الوظيفة»)" -Passed $pass -Details "status=$($r.Status) lec=$($saved | ConvertTo-Json -Compress)"

# المعلم: بنك أسئلة دورته ثم الحفظ عبر نقطة المعلم
$r = Invoke-Post -Session $teacher -Url "$BaseUrl/Teacher/GetAllQuesinBankTeacher" -Body @{ type = 0; cat_ID = $CAT }
$bankIds = @()
if ($r.Json) { $bankIds = @(@($r.Json) | ForEach-Object { [int]$_.ID }) }
$pass = ($r.Status -eq 200) -and ($bankIds -contains $Q_TF) -and ($bankIds -contains $Q_MC)
Record-Test -TestId "SV-04" -Description "المعلم: بنك أسئلة الدورة (GetAllQuesinBankTeacher) يعرض الأسئلة التي ستُضاف للوظيفة" -Passed $pass -Details "status=$($r.Status) count=$($bankIds.Count)"

$body = @{ Name = "QA_ASG_A_TSAVED"; Description = "QA"; Lecture_ID = $LEC_TSAVE; Assignmet_Degry = 10; Assignmet_degry_Win = 1; L_Assignment_Q = @(@{ Q_ID = $Q_MC }) } | ConvertTo-Json -Compress -Depth 4
$r = Invoke-Post -Session $teacher -Url "$BaseUrl/Teacher/AssingQuestionToAssignment" -Body $body -ContentType "application/json; charset=utf-8"
$A_TSAVED = [int](Sql "SELECT ISNULL(MAX(ID),0) FROM Assignments WHERE Name='QA_ASG_A_TSAVED' AND Lecture_ID=$LEC_TSAVE;")
$tq = [int](Sql "SELECT COUNT(*) FROM Assignment_Q WHERE Assignment_ID=$A_TSAVED AND Q_ID=$Q_MC;")
$pass = ($r.Status -eq 200) -and ($r.Json.success -eq $true) -and ($A_TSAVED -gt 0) -and ($tq -eq 1)
Record-Test -TestId "SV-05" -Description "المعلم: حفظ وظيفة بسؤال على محاضرة مباشرة عبر Teacher/AssingQuestionToAssignment" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw) asg=$A_TSAVED q=$tq"

# الطالب: الوظيفتان تظهران في "برامجي التعليمية" (قبل الدخول: الاختبار مقفل)، وأسئلة الوظيفة تُعاد كاملة
$page = Invoke-Get -Session $std -Url $pageUrl
$a = Get-LectureAttrs -Html $page.Raw -LectureId $LEC_SAVE
$b = Get-LectureAttrs -Html $page.Raw -LectureId $LEC_TSAVE
$pass = $a -and ($a.Type -eq "1") -and ($a.AssignId -eq "$A_SAVED") -and ($a.AssignName -eq "QA_ASG_A_SAVED") -and ($a.TimeEnd -eq "0") -and ($a.AssignDone -eq "0") -and $b -and ($b.AssignId -eq "$A_TSAVED") -and ($b.AssignName -eq "QA_ASG_A_TSAVED")
Record-Test -TestId "SV-06" -Description "الطالب: الصفحة تعرض الوظيفتين المحفوظتين على المحاضرتين المباشرتين (قبل الدخول: الاختبار غير متاح بعد)" -Passed $pass -Details "a=$($a | ConvertTo-Json -Compress) b=$($b | ConvertTo-Json -Compress)"

$r = Invoke-Post -Session $std -Url "$BaseUrl/Students/GetAssignmentQuestion" -Body @{ A_Q = $A_SAVED }
$aqIds = @((Sql "SELECT ID FROM Assignment_Q WHERE Assignment_ID=$A_SAVED ORDER BY ID;") -split '\s+' | Where-Object { $_ } | ForEach-Object { [int]$_ })
$items = @()
if ($r.Json) { $items = @($r.Json) }
$retIds = @($items | ForEach-Object { [int]$_.ID })
$tf = $items | Where-Object { [int]$_.Type -eq 1 } | Select-Object -First 1
$mc = $items | Where-Object { [int]$_.Type -eq 2 } | Select-Object -First 1
$pass = ($r.Status -eq 200) -and ($items.Count -eq 2) -and ($aqIds.Count -eq 2) -and ($retIds -contains $aqIds[0]) -and ($retIds -contains $aqIds[1]) -and $tf -and $mc -and (-not [string]::IsNullOrWhiteSpace($tf.Q_Name)) -and ($null -ne $tf.Q_TrueOrFalse) -and (@($mc.Q_MultiChoice).Count -gt 0)
Record-Test -TestId "SV-07" -Description "الطالب: أسئلة الوظيفة المحفوظة تُعاد كاملة (صح/خطأ + اختيار من متعدد بخياراته) من GetAssignmentQuestion" -Passed $pass -Details "status=$($r.Status) count=$($items.Count) ids=$($retIds -join ',') expected=$($aqIds -join ',')"

$r = Invoke-Post -Session $std -Url $joinUrl -Body @{ "__RequestVerificationToken" = $stdToken; lectureId = $LEC_SAVE }
$pass = ($r.Status -eq 200) -and $r.Json.success -and $r.Json.status.isTimeEnd -and $r.Json.status.hasAssignment -and (-not $r.Json.status.isAssignmentFinish) -and (-not $r.Json.status.isFinish)
Record-Test -TestId "SV-08" -Description "الطالب: بعد دخول المحاضرة المباشرة يصبح اختبارها المحفوظ متاحاً (isTimeEnd=1, hasAssignment=1) والاكتمال معلّق عليه" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw)"

$page = Invoke-Get -Session $std -Url $pageUrl
$a = Get-LectureAttrs -Html $page.Raw -LectureId $LEC_SAVE
$pass = $a -and ($a.TimeEnd -eq "1") -and ($a.AssignDone -eq "0") -and ($a.AssignId -eq "$A_SAVED")
Record-Test -TestId "SV-09" -Description "الطالب: الصفحة ترسل time-end=1 مع معرّف الوظيفة → زر «الاختبار» يظهر للمحاضرة المباشرة" -Passed $pass -Details "attrs=$($a | ConvertTo-Json -Compress)"

$r = Invoke-Get -Session $std -Url "$BaseUrl/Students/AssignmentQuestion?A_Q=$A_SAVED"
$pass = ($r.Status -eq 200) -and ($r.Raw -match 'GetAssignmentQuestion') -and ($r.Raw -notmatch 'Server Error')
Record-Test -TestId "SV-10" -Description "الطالب: صفحة حل الوظيفة المحفوظة تفتح" -Passed $pass -Details "status=$($r.Status) len=$($r.Raw.Length)"

$r = Invoke-Post -Session $std -Url $submitUrl -Body "L_Degry=5&ass_ID=$A_SAVED" -ContentType "application/x-www-form-urlencoded"
$row = Get-Row $student $LEC_SAVE
$pass = ($r.Status -eq 200) -and ($r.Json.success -eq $true) -and $row -and ($row.IsAssign -eq 1) -and ($row.IsFinish -eq 1)
Record-Test -TestId "SV-11" -Description "الطالب: اجتياز الوظيفة المحفوظة يكمل المحاضرة المباشرة" -Passed $pass -Details "resp=$($r.Raw) row=$($row | ConvertTo-Json -Compress)"

Write-Host "`n[7] Cleanup" -ForegroundColor Yellow
$null = Sql "DELETE FROM std_lecture WHERE Lecture_ID IN (SELECT ID FROM Lectures WHERE Lecture_Name LIKE 'QA_ASG_%');"
$null = Sql "DELETE FROM Assignment_Q WHERE Assignment_ID IN (SELECT ID FROM Assignments WHERE Name LIKE 'QA_ASG_%');"
$null = Sql "DELETE FROM Assignments WHERE Name LIKE 'QA_ASG_%' OR Lecture_ID IN (SELECT ID FROM Lectures WHERE Lecture_Name LIKE 'QA_ASG_%');"
$null = Sql "DELETE FROM Lectures WHERE Lecture_Name LIKE 'QA_ASG_%';"
$leftover = [int](Sql "SELECT COUNT(*) FROM Lectures WHERE Lecture_Name LIKE 'QA_ASG_%';") + [int](Sql "SELECT COUNT(*) FROM Assignments WHERE Name LIKE 'QA_ASG_%';")
Record-Test -TestId "CL-01" -Description "حذف بيانات الاختبار QA_ASG_*" -Passed ($leftover -eq 0) -Details "leftover=$leftover"

Write-Host "`n==========================================================" -ForegroundColor Cyan
$passed = @($results | Where-Object { $_.Result -eq "PASS" }).Count
$failed = @($results | Where-Object { $_.Result -eq "FAIL" }).Count
Write-Host "   Total: $($results.Count)   PASS: $passed   FAIL: $failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
Write-Host "==========================================================" -ForegroundColor Cyan
if ($failed -gt 0) { $results | Where-Object { $_.Result -eq "FAIL" } | Format-Table -AutoSize | Out-String -Width 200 | Write-Host }
exit $(if ($failed -eq 0) { 0 } else { 1 })
