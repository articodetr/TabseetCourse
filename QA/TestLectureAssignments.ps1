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
#   [5] زر «الاختبار» يظهر دائماً للطالب والخادم يفحص الأهلية عند الضغط (Students/CheckLectureExamAccess). الاختبار مستقل عن
#       الحضور (قرار المالك 2026-10-27): يُفتح في أي وقت لمشترك فعّال ما دام للدرس اختبار بأسئلة ولم يجتزه؛ اجتيازه لا يكمل الدرس
#       وحده (الاكتمال = حضور + اجتياز). المنع فقط: بلا اختبار/بلا أسئلة، اشتراك معلّق، اجتياز سابق، والرابط المباشر محمي مع CSRF.
# يستخدم student01 (مسجل في الدورة 6 ومعلّق في الدورة 4) وينشئ محاضرات/وظائف باسم QA_ASG_* في الدورة 6 ثم يحذفها.

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

# الاختبار لا يُعدّ متاحاً بلا أسئلة (الخادم يرفضه): تُربط أسئلة من بنك الدورة بالوظائف الجاهزة
$Q_TF = [int](Sql "SELECT ISNULL(MIN(ID),0) FROM Questions WHERE Category_ID=$CAT AND Type=1;")
$Q_MC = [int](Sql "SELECT ISNULL(MIN(ID),0) FROM Questions WHERE Category_ID=$CAT AND Type=2;")
if ($Q_TF -eq 0 -or $Q_MC -eq 0) { throw "Need a true/false and a multiple-choice bank question in category $CAT" }
$null = Sql "INSERT INTO Assignment_Q (Q_ID, Assignment_ID) VALUES ($Q_TF, $A_REC), ($Q_TF, $A_LIVE);"

# محاضرات/وظائف سيناريوهات زر «الاختبار» (القسم [8]): مباشرة الآن، مباشرة انتهى موعدها قبل 4 ساعات، مسجلة، بلا وظيفة، وظيفة بلا أسئلة
$oldStr = (Get-Date).AddHours(-4).ToString("yyyy-MM-dd HH:mm:ss")
$null = Sql "INSERT INTO Lectures (Category_ID, Lecture_Name, LectureTypeOnOff, DayandTime, LectureLink_Client, LectureLink_Host, LectureMinTime) VALUES ($CAT, 'QA_ASG_EX_LIVE', '1', '$nowStr', 'https://meet.google.com/qa-asg-exlive', '', '0');"
$null = Sql "INSERT INTO Lectures (Category_ID, Lecture_Name, LectureTypeOnOff, DayandTime, LectureLink_Client, LectureLink_Host, LectureMinTime) VALUES ($CAT, 'QA_ASG_EX_LIVE_OLD', '1', '$oldStr', 'https://meet.google.com/qa-asg-exold', '', '0');"
$null = Sql "INSERT INTO Lectures (Category_ID, Lecture_Name, LectureTypeOnOff, DayandTime, LectureLink_Client, LectureLink_Host, LectureMinTime) VALUES ($CAT, 'QA_ASG_EX_REC', '0', '', 'https://www.youtube.com/watch?v=qa_asg_exrec', '', '1');"
$null = Sql "INSERT INTO Lectures (Category_ID, Lecture_Name, LectureTypeOnOff, DayandTime, LectureLink_Client, LectureLink_Host, LectureMinTime) VALUES ($CAT, 'QA_ASG_EX_NOASG', '0', '', 'https://www.youtube.com/watch?v=qa_asg_exno', '', '1');"
$null = Sql "INSERT INTO Lectures (Category_ID, Lecture_Name, LectureTypeOnOff, DayandTime, LectureLink_Client, LectureLink_Host, LectureMinTime) VALUES ($CAT, 'QA_ASG_EX_NOQ', '0', '', 'https://www.youtube.com/watch?v=qa_asg_exnoq', '', '1');"
$LEC_EX_LIVE = [int](Sql "SELECT ID FROM Lectures WHERE Lecture_Name='QA_ASG_EX_LIVE';")
$LEC_EX_LIVE_OLD = [int](Sql "SELECT ID FROM Lectures WHERE Lecture_Name='QA_ASG_EX_LIVE_OLD';")
$LEC_EX_REC = [int](Sql "SELECT ID FROM Lectures WHERE Lecture_Name='QA_ASG_EX_REC';")
$LEC_EX_NOASG = [int](Sql "SELECT ID FROM Lectures WHERE Lecture_Name='QA_ASG_EX_NOASG';")
$LEC_EX_NOQ = [int](Sql "SELECT ID FROM Lectures WHERE Lecture_Name='QA_ASG_EX_NOQ';")
if ($LEC_EX_LIVE -eq 0 -or $LEC_EX_LIVE_OLD -eq 0 -or $LEC_EX_REC -eq 0 -or $LEC_EX_NOASG -eq 0 -or $LEC_EX_NOQ -eq 0) { throw "Fixture creation failed (QA_ASG_EX_*)" }
$null = Sql "INSERT INTO Assignments (Name, Description, Lecture_ID, Assignmet_Degry, Assignmet_degry_Win) VALUES ('QA_ASG_A_EX_LIVE', 'QA', $LEC_EX_LIVE, 10, 1), ('QA_ASG_A_EX_LIVE_OLD', 'QA', $LEC_EX_LIVE_OLD, 10, 1), ('QA_ASG_A_EX_REC', 'QA', $LEC_EX_REC, 10, 1), ('QA_ASG_A_EX_NOQ', 'QA', $LEC_EX_NOQ, 10, 1);"
$A_EX_LIVE = [int](Sql "SELECT ID FROM Assignments WHERE Name='QA_ASG_A_EX_LIVE';")
$A_EX_LIVE_OLD = [int](Sql "SELECT ID FROM Assignments WHERE Name='QA_ASG_A_EX_LIVE_OLD';")
$A_EX_REC = [int](Sql "SELECT ID FROM Assignments WHERE Name='QA_ASG_A_EX_REC';")
$A_EX_NOQ = [int](Sql "SELECT ID FROM Assignments WHERE Name='QA_ASG_A_EX_NOQ';")
if ($A_EX_LIVE -eq 0 -or $A_EX_LIVE_OLD -eq 0 -or $A_EX_REC -eq 0 -or $A_EX_NOQ -eq 0) { throw "Fixture creation failed (QA_ASG_A_EX_*)" }
$null = Sql "INSERT INTO Assignment_Q (Q_ID, Assignment_ID) VALUES ($Q_TF, $A_EX_LIVE), ($Q_MC, $A_EX_LIVE), ($Q_TF, $A_EX_LIVE_OLD), ($Q_TF, $A_EX_REC);"
Write-Host "Exam fixtures: exLive=$LEC_EX_LIVE exOld=$LEC_EX_LIVE_OLD exRec=$LEC_EX_REC noAsg=$LEC_EX_NOASG noQ=$LEC_EX_NOQ"

$stdToken = Get-VerificationToken -Session $std -Url "$BaseUrl/Students/CategoriesStd"
if (-not $stdToken) { throw "Student token not found on CategoriesStd" }
$pageUrl = "$BaseUrl/Students/CategoriesStd"

Write-Host "`n[3] Student programs page exposes lecture assignments" -ForegroundColor Yellow
$page = Invoke-Get -Session $std -Url $pageUrl
$a = Get-LectureAttrs -Html $page.Raw -LectureId $LEC_REC
$pass = $a -and ($a.Type -eq "0") -and ($a.AssignId -eq "$A_REC") -and ($a.AssignName -eq "QA_ASG_A_REC") -and ($a.TimeEnd -eq "0") -and ($a.AssignDone -eq "0") -and ($a.IsFinish -eq "0")
Record-Test -TestId "ST-01" -Description "محاضرة مسجلة ذات وظيفة: الصفحة ترسل معرّف الوظيفة واسمها وحالة الحضور (للشارات فقط)" -Passed $pass -Details "attrs=$($a | ConvertTo-Json -Compress)"

$a = Get-LectureAttrs -Html $page.Raw -LectureId $LEC_LIVE
$pass = $a -and ($a.Type -eq "1") -and ($a.AssignId -eq "$A_LIVE") -and ($a.TimeEnd -eq "0") -and ($a.AssignDone -eq "0")
Record-Test -TestId "ST-02" -Description "محاضرة مباشرة ذات وظيفة: الصفحة ترسل معرّف الوظيفة وحالة الحضور (للشارات فقط)" -Passed $pass -Details "attrs=$($a | ConvertTo-Json -Compress)"

$a = Get-LectureAttrs -Html $page.Raw -LectureId $LEC_LIVE2
$pass = $a -and ($a.AssignId -eq "0")
Record-Test -TestId "ST-03" -Description "محاضرة مباشرة بلا وظيفة: assign-id = 0" -Passed $pass -Details "attrs=$($a | ConvertTo-Json -Compress)"

$pass = ($page.Raw -match 'sub-btn-lecexam') -and ($page.Raw -match 'tabseet:livejoined') -and ($page.Raw -match 'js-lec-exam') -and ($page.Raw -match '/Students/CheckLectureExamAccess') -and ($page.Raw -match 'requestLectureExam')
Record-Test -TestId "ST-04" -Description "سكربت الصفحة يحوي زر «الاختبار» الدائم، طلب فحص الأهلية من الخادم، ومستمع دخول المحاضرة المباشرة" -Passed $pass -Details "len=$($page.Raw.Length)"

Write-Host "`n[4] Live lecture: joining marks attendance; passing the exam then completes the lecture" -ForegroundColor Yellow
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
Record-Test -TestId "LV-03" -Description "بعد الدخول: الصفحة ترسل time-end=1 (حالة الحضور للشارات)" -Passed $pass -Details "attrs=$($a | ConvertTo-Json -Compress)"

$r = Invoke-Get -Session $std -Url "$BaseUrl/Students/AssignmentQuestion?A_Q=$A_LIVE"
$pass = ($r.Status -eq 200) -and ($r.Raw -match 'GetAssignmentQuestion')
Record-Test -TestId "LV-04" -Description "صفحة حل وظيفة المحاضرة المباشرة تفتح للطالب" -Passed $pass -Details "status=$($r.Status) len=$($r.Raw.Length)"

$r = Invoke-Post -Session $std -Url $submitUrl -Body @{ "__RequestVerificationToken" = $stdToken; L_Degry = 5; ass_ID = $A_LIVE }
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

Write-Host "`n[5] Recorded lecture: required watch time marks attendance; passing the exam then completes the lecture" -ForegroundColor Yellow
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
Record-Test -TestId "RC-02" -Description "بعد المشاهدة: الصفحة ترسل time-end=1 (حالة الحضور للشارات)" -Passed $pass -Details "attrs=$($a | ConvertTo-Json -Compress)"

$r = Invoke-Post -Session $std -Url $submitUrl -Body @{ "__RequestVerificationToken" = $stdToken; L_Degry = 0; ass_ID = $A_REC }
$row = Get-Row $student $LEC_REC
$pass = ($r.Status -eq 200) -and ($r.Json.success -eq $false) -and $row -and ($row.IsAssign -eq 0) -and ($row.IsFinish -eq 0)
Record-Test -TestId "RC-03" -Description "رسوب في الوظيفة: لا يُسجَّل اجتياز ولا اكتمال" -Passed $pass -Details "resp=$($r.Raw) row=$($row | ConvertTo-Json -Compress)"

$r = Invoke-Post -Session $std -Url $submitUrl -Body @{ "__RequestVerificationToken" = $stdToken; L_Degry = 5; ass_ID = $A_REC }
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

# قبل دخول المحاضرة المباشرة: الاختبار مستقل عن الحضور فتُعاد الأسئلة كاملة
$r = Invoke-Post -Session $std -Url "$BaseUrl/Students/GetAssignmentQuestion" -Body @{ "__RequestVerificationToken" = $stdToken; A_Q = $A_SAVED }
$aqIds = @((Sql "SELECT ID FROM Assignment_Q WHERE Assignment_ID=$A_SAVED ORDER BY ID;") -split '\s+' | Where-Object { $_ } | ForEach-Object { [int]$_ })
$items = @()
if ($r.Json) { $items = @($r.Json) }
$retIds = @($items | ForEach-Object { [int]$_.ID })
$tf = $items | Where-Object { [int]$_.Type -eq 1 } | Select-Object -First 1
$mc = $items | Where-Object { [int]$_.Type -eq 2 } | Select-Object -First 1
$pass = ($r.Status -eq 200) -and ($items.Count -eq 2) -and ($aqIds.Count -eq 2) -and ($retIds -contains $aqIds[0]) -and ($retIds -contains $aqIds[1]) -and $tf -and $mc -and (-not [string]::IsNullOrWhiteSpace($tf.Q_Name)) -and ($null -ne $tf.Q_TrueOrFalse) -and (@($mc.Q_MultiChoice).Count -gt 0)
Record-Test -TestId "SV-07" -Description "الطالب: أسئلة الوظيفة المحفوظة تُعاد كاملة قبل دخول المحاضرة المباشرة (صح/خطأ + اختيار من متعدد بخياراته)" -Passed $pass -Details "status=$($r.Status) count=$($items.Count) ids=$($retIds -join ',') expected=$($aqIds -join ',')"

$r = Invoke-Post -Session $std -Url $joinUrl -Body @{ "__RequestVerificationToken" = $stdToken; lectureId = $LEC_SAVE }
$pass = ($r.Status -eq 200) -and $r.Json.success -and $r.Json.status.isTimeEnd -and $r.Json.status.hasAssignment -and (-not $r.Json.status.isAssignmentFinish) -and (-not $r.Json.status.isFinish)
Record-Test -TestId "SV-08" -Description "الطالب: بعد دخول المحاضرة المباشرة يصبح اختبارها المحفوظ متاحاً (isTimeEnd=1, hasAssignment=1) والاكتمال معلّق عليه" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw)"

$r = Invoke-Post -Session $std -Url "$BaseUrl/Students/GetAssignmentQuestion" -Body @{ "__RequestVerificationToken" = $stdToken; A_Q = $A_SAVED }
$pass = ($r.Status -eq 200) -and (@($r.Json).Count -eq 2)
Record-Test -TestId "SV-08b" -Description "الطالب: بعد الدخول ما زالت الأسئلة تُعاد كاملة من GetAssignmentQuestion" -Passed $pass -Details "status=$($r.Status) count=$(@($r.Json).Count)"

$page = Invoke-Get -Session $std -Url $pageUrl
$a = Get-LectureAttrs -Html $page.Raw -LectureId $LEC_SAVE
$pass = $a -and ($a.TimeEnd -eq "1") -and ($a.AssignDone -eq "0") -and ($a.AssignId -eq "$A_SAVED")
Record-Test -TestId "SV-09" -Description "الطالب: الصفحة ترسل time-end=1 مع معرّف الوظيفة بعد الدخول (شارات الحضور)" -Passed $pass -Details "attrs=$($a | ConvertTo-Json -Compress)"

$r = Invoke-Get -Session $std -Url "$BaseUrl/Students/AssignmentQuestion?A_Q=$A_SAVED"
$pass = ($r.Status -eq 200) -and ($r.Raw -match 'GetAssignmentQuestion') -and ($r.Raw -notmatch 'Server Error')
Record-Test -TestId "SV-10" -Description "الطالب: صفحة حل الوظيفة المحفوظة تفتح" -Passed $pass -Details "status=$($r.Status) len=$($r.Raw.Length)"

$r = Invoke-Post -Session $std -Url $submitUrl -Body @{ "__RequestVerificationToken" = $stdToken; L_Degry = 5; ass_ID = $A_SAVED }
$row = Get-Row $student $LEC_SAVE
$pass = ($r.Status -eq 200) -and ($r.Json.success -eq $true) -and $row -and ($row.IsAssign -eq 1) -and ($row.IsFinish -eq 1)
Record-Test -TestId "SV-11" -Description "الطالب: اجتياز الوظيفة المحفوظة يكمل المحاضرة المباشرة" -Passed $pass -Details "resp=$($r.Raw) row=$($row | ConvertTo-Json -Compress)"

Write-Host "`n[8] Exam button: exam independent of attendance (CheckLectureExamAccess) + remaining server rules on direct URLs" -ForegroundColor Yellow
$checkUrl = "$BaseUrl/Students/CheckLectureExamAccess"
$getQUrl = "$BaseUrl/Students/GetAssignmentQuestion"
$examPageUrl = "$BaseUrl/Students/AssignmentQuestion"
function Check-Exam { param($Session, [int]$LectureId, [string]$Token) return Invoke-Post -Session $Session -Url $checkUrl -Body @{ "__RequestVerificationToken" = $Token; lectureId = $LectureId } }

$page = Invoke-Get -Session $std -Url $pageUrl
$a = Get-LectureAttrs -Html $page.Raw -LectureId $LEC_EX_NOASG
$b = Get-LectureAttrs -Html $page.Raw -LectureId $LEC_EX_NOQ
$c = Get-LectureAttrs -Html $page.Raw -LectureId $LEC_EX_LIVE
$pass = $a -and ($a.AssignId -eq "0") -and $b -and ($b.AssignId -eq "0") -and $c -and ($c.AssignId -eq "$A_EX_LIVE") -and ($c.TimeEnd -eq "0") -and ($page.Raw -match 'if \(hasExam\(lItem\)\) \{\s*actionsDiv\.appendChild\(createExamButton\(lItem, "الاختبار"\)\);') -and ($page.Raw -match 'if \(!hasExam\(lec\)\) return;\s*modalActions\.appendChild\(createExamButton\(lec') -and ($page.Raw -notmatch 'canTakeExam') -and ($page.Raw -notmatch 'sub-lec-exam-hint') -and ($page.Raw -notmatch 'يُتاح اختبار المحاضرة بعد') -and ($page.Raw -match 'closest\("\.js-lec-exam"\)')
Record-Test -TestId "EX-01" -Description "زر «الاختبار» يُرسم (بطاقة + تفاصيل) فقط للدرس الذي له اختبار بأسئلة: بلا اختبار وبلا أسئلة assign-id=0، ومع اختبار يُرسل معرّفه دون شرط حضور" -Passed $pass -Details "noAsg=$($a.AssignId) noQ=$($b.AssignId) live=$($c.AssignId)"

$r = Check-Exam $std $LEC_EX_NOASG $stdToken
$pass = ($r.Status -eq 200) -and ($r.Json.success -eq $true) -and ($r.Json.allowed -eq $false) -and ($r.Json.code -eq "NoAssignment") -and ($r.Json.title -eq "الاختبار غير متاح بعد") -and (-not $r.Json.url)
Record-Test -TestId "EX-02" -Description "درس بلا اختبار → «الاختبار غير متاح بعد» (لم يقم المدرس بإضافة اختبار)" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw)"

$r = Check-Exam $std $LEC_EX_NOQ $stdToken
$pass = ($r.Status -eq 200) -and ($r.Json.allowed -eq $false) -and ($r.Json.code -eq "NoQuestions") -and ($r.Json.title -eq "الاختبار غير متاح بعد")
Record-Test -TestId "EX-03" -Description "وظيفة محفوظة بلا أسئلة → «الاختبار غير متاح بعد» (لا تُفتح صفحة حل فارغة)" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw)"

$r = Check-Exam $std $LEC_EX_LIVE $stdToken
$row = Get-Row $student $LEC_EX_LIVE
$pass = ($r.Status -eq 200) -and ($r.Json.allowed -eq $true) -and ($r.Json.code -eq "Allowed") -and ($r.Json.assignmentId -eq $A_EX_LIVE) -and ($r.Json.url -match "AssignmentQuestion\?A_Q=$A_EX_LIVE$") -and (-not $row)
Record-Test -TestId "EX-04" -Description "درس مباشر له اختبار ولم يحضره الطالب (لا صف حضور) → مسموح بالدخول: الاختبار مستقل عن الحضور" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw) row=$($row | ConvertTo-Json -Compress)"

$r = Invoke-Get -Session $std -Url "$examPageUrl`?A_Q=$A_EX_LIVE"
$r2 = Invoke-Post -Session $std -Url $getQUrl -Body @{ "__RequestVerificationToken" = $stdToken; A_Q = $A_EX_LIVE }
$pass = ($r.Status -eq 200) -and ($r.Raw -match 'GetAssignmentQuestion') -and ($r.Raw -notmatch 'Server Error') -and ($r2.Status -eq 200) -and (@($r2.Json).Count -eq 2)
Record-Test -TestId "EX-05" -Description "صفحة الحل تفتح وتُعاد الأسئلة دون أي حضور" -Passed $pass -Details "page=$($r.Status) api=$($r2.Status) q=$(@($r2.Json).Count)"

$p = Invoke-Get -Session $std -Url "$examPageUrl`?A_Q=$A_EX_NOQ" -NoRedirect
$r2 = Invoke-Post -Session $std -Url $getQUrl -Body @{ "__RequestVerificationToken" = $stdToken; A_Q = $A_EX_NOQ }
$r3 = Invoke-Post -Session $std -Url $submitUrl -Body @{ "__RequestVerificationToken" = $stdToken; L_Degry = 5; ass_ID = $A_EX_NOQ }
$row = Get-Row $student $LEC_EX_NOQ
$pass = ($p.Status -eq 302) -and ($p.Location -match 'CategoriesStd') -and ($r2.Status -eq 403) -and ($r2.Json.code -eq "NoQuestions") -and ($r2.Raw -notmatch 'Q_Text') -and ($r3.Status -eq 403) -and ((-not $row) -or ($row.IsAssign -eq 0))
Record-Test -TestId "EX-06" -Description "اختبار بلا أسئلة بالرابط المباشر: صفحة الحل تُحوَّل (302) وجلب الأسئلة والتسليم مرفوضان (403) بلا تسجيل اجتياز" -Passed $pass -Details "page=$($p.Status) get=$($r2.Status) submit=$($r3.Status)"

# كما يفعل المتصفح: طلب الرابط المباشر (302) ثم فتح الصفحة المحوَّل إليها في الجلسة نفسها (PowerShell لا يتابع هذا التحويل تلقائياً)
$null = Invoke-Get -Session $std -Url "$examPageUrl`?A_Q=$A_EX_NOQ" -NoRedirect
$page = Invoke-Get -Session $std -Url $pageUrl
$pass = ($page.Status -eq 200) -and ($page.Raw -match 'الاختبار غير متاح بعد') -and ($page.Raw -match 'أسئلة لهذا الاختبار')
Record-Test -TestId "EX-07" -Description "بعد التحويل تعرض صفحة برامجي التعليمية سبب المنع للطالب (رسالة SweetAlert)" -Passed $pass -Details "status=$($page.Status)"

$r = Invoke-Post -Session $std -Url $submitUrl -Body @{ "__RequestVerificationToken" = $stdToken; L_Degry = 5; ass_ID = $A_EX_LIVE }
$row = Get-Row $student $LEC_EX_LIVE
$pass = ($r.Status -eq 200) -and ($r.Json.success -eq $true) -and $row -and ($row.IsAssign -eq 1) -and ($row.IsTimeEnd -eq 0) -and ($row.IsFinish -eq 0)
Record-Test -TestId "EX-08" -Description "اجتياز اختبار المباشرة دون حضور → يُسجَّل الاجتياز (IsAssingmetFinish=1) ويبقى الدرس غير مكتمل (IsFinish=0)" -Passed $pass -Details "resp=$($r.Raw) row=$($row | ConvertTo-Json -Compress)"

$r2 = Check-Exam $std $LEC_EX_LIVE $stdToken
$pass = ($r2.Status -eq 200) -and ($r2.Json.allowed -eq $false) -and ($r2.Json.code -eq "AlreadyPassed") -and ($r2.Json.title -eq "تم اجتياز الاختبار بنجاح") -and ($r2.Json.icon -eq "success")
Record-Test -TestId "EX-09" -Description "بعد الاجتياز → حالة النجاح «تم اجتياز الاختبار بنجاح»" -Passed $pass -Details "check=$($r2.Raw)"

$r = Invoke-Get -Session $std -Url "$examPageUrl`?A_Q=$A_EX_LIVE" -NoRedirect
$r2 = Invoke-Post -Session $std -Url $submitUrl -Body @{ "__RequestVerificationToken" = $stdToken; L_Degry = 5; ass_ID = $A_EX_LIVE }
$pass = ($r.Status -eq 302) -and ($r2.Status -eq 403) -and ($r2.Json.code -eq "AlreadyPassed")
Record-Test -TestId "EX-10" -Description "بعد الاجتياز: الرابط المباشر يُحوَّل وإعادة التسليم تُرفض (403)" -Passed $pass -Details "page=$($r.Status) submit=$($r2.Status)"

$r = Invoke-Post -Session $std -Url $joinUrl -Body @{ "__RequestVerificationToken" = $stdToken; lectureId = $LEC_EX_LIVE }
$row = Get-Row $student $LEC_EX_LIVE
$pass = ($r.Status -eq 200) -and ($r.Json.success -eq $true) -and $row -and ($row.IsTimeEnd -eq 1) -and ($row.IsAssign -eq 1) -and ($row.IsFinish -eq 1)
Record-Test -TestId "EX-11" -Description "تسجيل الحضور بعد الاجتياز يكمل الدرس (قاعدة الاكتمال: حضور + اجتياز لم تتغير)" -Passed $pass -Details "resp=$($r.Raw) row=$($row | ConvertTo-Json -Compress)"

$r = Check-Exam $std $LEC_EX_LIVE_OLD $stdToken
$pass = ($r.Status -eq 200) -and ($r.Json.allowed -eq $true) -and ($r.Json.code -eq "Allowed") -and ($r.Json.assignmentId -eq $A_EX_LIVE_OLD)
Record-Test -TestId "EX-12" -Description "درس مباشر انتهى موعده (قبل 4 ساعات) ولم يحضره الطالب → الاختبار متاح" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw)"

$r = Invoke-Post -Session $std -Url $joinUrl -Body @{ "__RequestVerificationToken" = $stdToken; lectureId = $LEC_EX_LIVE_OLD }
$r2 = Check-Exam $std $LEC_EX_LIVE_OLD $stdToken
$pass = ($r.Status -eq 200) -and ($r.Json.success -eq $false) -and ($r2.Json.allowed -eq $true)
Record-Test -TestId "EX-13" -Description "تسجيل الحضور خارج الموعد ما زال مرفوضاً (نظام الحضور لم يتغير) والاختبار متاح رغم ذلك" -Passed $pass -Details "join=$($r.Raw) check=$($r2.Raw)"

$teacherToken = Get-VerificationToken -Session $teacher -Url "$BaseUrl/Teacher/LecturesByCategoryStudend?Cat_ID=$CAT&categoryName=QA"
$m1 = Invoke-Post -Session $teacher -Url "$BaseUrl/Teacher/SetLectureAttendance" -Body @{ "__RequestVerificationToken" = $teacherToken; lectureId = $LEC_EX_LIVE_OLD; studentId = $student; attended = "true" }
$c1 = Check-Exam $std $LEC_EX_LIVE_OLD $stdToken
$m2 = Invoke-Post -Session $teacher -Url "$BaseUrl/Teacher/SetLectureAttendance" -Body @{ "__RequestVerificationToken" = $teacherToken; lectureId = $LEC_EX_LIVE_OLD; studentId = $student; attended = "false" }
$c2 = Check-Exam $std $LEC_EX_LIVE_OLD $stdToken
$pass = ($m1.Status -eq 200) -and ($m1.Json.success -eq $true) -and ($c1.Json.allowed -eq $true) -and ($m2.Status -eq 200) -and ($c2.Json.allowed -eq $true)
Record-Test -TestId "EX-14" -Description "تسجيل المدرس للحضور أو إلغاؤه لا يغيّر إتاحة الاختبار (مستقل عن الحضور)" -Passed $pass -Details "mark=$($m1.Status)/$($m2.Status) check=$($c1.Json.code)/$($c2.Json.code)"

$r = Check-Exam $std $LEC_EX_REC $stdToken
$p = Invoke-Get -Session $std -Url "$examPageUrl`?A_Q=$A_EX_REC"
$row = Get-Row $student $LEC_EX_REC
$pass = ($r.Status -eq 200) -and ($r.Json.allowed -eq $true) -and ($r.Json.assignmentId -eq $A_EX_REC) -and ($p.Status -eq 200) -and ($p.Raw -match 'GetAssignmentQuestion') -and (-not $row)
Record-Test -TestId "EX-15" -Description "درس مسجل لم تبدأ مشاهدته → الاختبار متاح وصفحة الحل تفتح" -Passed $pass -Details "check=$($r.Raw) page=$($p.Status)"

$r = Invoke-Post -Session $std -Url $submitUrl -Body @{ "__RequestVerificationToken" = $stdToken; L_Degry = 5; ass_ID = $A_EX_REC }
$row = Get-Row $student $LEC_EX_REC
$pass = ($r.Status -eq 200) -and ($r.Json.success -eq $true) -and $row -and ($row.IsAssign -eq 1) -and ($row.IsTimeEnd -eq 0) -and ($row.IsFinish -eq 0)
Record-Test -TestId "EX-16" -Description "اجتياز اختبار المسجلة دون مشاهدة → اجتياز مسجَّل والدرس غير مكتمل" -Passed $pass -Details "resp=$($r.Raw) row=$($row | ConvertTo-Json -Compress)"

$r = Invoke-Post -Session $std -Url $startUrl -Body @{ "__RequestVerificationToken" = $stdToken; lectureId = $LEC_EX_REC; durationSeconds = 3600 }
$null = Sql "UPDATE std_lecture SET LastHeartbeatAt = DATEADD(minute, -2, SYSUTCDATETIME()) WHERE Std_ID='$student' AND Lecture_ID=$LEC_EX_REC;"
$r = Invoke-Post -Session $std -Url $hbUrl -Body @{ "__RequestVerificationToken" = $stdToken; lectureId = $LEC_EX_REC; playedSeconds = 60; durationSeconds = 3600 }
$row = Get-Row $student $LEC_EX_REC
$pass = ($r.Status -eq 200) -and ($r.Json.status.isTimeEnd -eq $true) -and ($r.Json.status.isFinish -eq $true) -and $row -and ($row.IsTimeEnd -eq 1) -and ($row.IsFinish -eq 1)
Record-Test -TestId "EX-17" -Description "إكمال المشاهدة بعد الاجتياز يكمل الدرس المسجّل (IsFinish=1)" -Passed $pass -Details "hb=$($r.Raw) row=$($row | ConvertTo-Json -Compress)"

$r = Invoke-Post -Session $std -Url $submitUrl -Body @{ "__RequestVerificationToken" = $stdToken; L_Degry = 5; ass_ID = $A_EX_REC }
$r2 = Check-Exam $std $LEC_EX_REC $stdToken
$pass = ($r.Status -eq 403) -and ($r.Json.code -eq "AlreadyPassed") -and ($r2.Json.allowed -eq $false) -and ($r2.Json.code -eq "AlreadyPassed")
Record-Test -TestId "EX-18" -Description "درس مسجّل مُجتاز: إعادة التسليم مرفوضة (403 AlreadyPassed) والزر يعرض حالة النجاح" -Passed $pass -Details "submit=$($r.Status) check=$($r2.Json.code)"

$PEND_CAT = 4
$PEND_LEC = [int](Sql "SELECT ISNULL(MIN(ID),0) FROM Lectures WHERE Category_ID=$PEND_CAT;")
$PEND_ASG = [int](Sql "SELECT ISNULL(MIN(ID),0) FROM Assignments WHERE Lecture_ID=$PEND_LEC;")
$pendState = Sql "SELECT CAST(ISNULL(category_Ispayed,0) AS varchar) + '/' + CAST(ISNULL(std_category_Ispayed,0) AS varchar) FROM Std_Reg_Category WHERE Std_ID='$student' AND Category_ID=$PEND_CAT;"
if ($PEND_LEC -gt 0 -and $PEND_ASG -gt 0 -and $pendState -eq "1/0") {
    $r = Check-Exam $std $PEND_LEC $stdToken
    $pass = ($r.Status -eq 200) -and ($r.Json.allowed -eq $false) -and ($r.Json.code -eq "SubscriptionPending") -and ($r.Json.title -eq "الاشتراك بانتظار الموافقة") -and ($r.Json.assignmentId -eq 0)
    Record-Test -TestId "EX-19" -Description "اشتراك بانتظار موافقة الإدارة → رسالة واضحة دون كشف معرّف الاختبار" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw)"
    $p = Invoke-Get -Session $std -Url "$examPageUrl`?A_Q=$PEND_ASG" -NoRedirect
    $r2 = Invoke-Post -Session $std -Url $getQUrl -Body @{ "__RequestVerificationToken" = $stdToken; A_Q = $PEND_ASG }
    $pass = ($p.Status -eq 302) -and ($r2.Status -eq 403) -and ($r2.Raw -notmatch 'Q_Text')
    Record-Test -TestId "EX-20" -Description "الرابط المباشر وجلب الأسئلة للدورة المعلّقة مرفوضان" -Passed $pass -Details "page=$($p.Status) api=$($r2.Status)"
} else {
    Record-Test -TestId "EX-19" -Description "اشتراك معلّق (يحتاج student01 معلّقاً في الدورة 4 محلياً)" -Passed $false -Details "lec=$PEND_LEC asg=$PEND_ASG state=$pendState"
    Record-Test -TestId "EX-20" -Description "الرابط المباشر للدورة المعلّقة (تخطّي: لا بيانات)" -Passed $false -Details "no fixture"
}

$r = Invoke-Post -Session $std -Url $checkUrl -Body @{ lectureId = $LEC_EX_REC }
$pass = ($r.Status -ne 200) -and ($r.Raw -notmatch '"allowed"')
Record-Test -TestId "EX-21" -Description "CheckLectureExamAccess بلا رمز الحماية من التزوير → مرفوض" -Passed $pass -Details "status=$($r.Status)"

$anon2 = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$anonToken = Get-VerificationToken -Session $anon2 -Url "$BaseUrl/Home/LogIn"
$r = Invoke-Post -Session $anon2 -Url $checkUrl -Body @{ "__RequestVerificationToken" = $anonToken; lectureId = $LEC_EX_REC }
$pass = ($r.Status -eq 403) -and ($r.Json.requiresLogin -eq $true)
Record-Test -TestId "EX-22" -Description "زائر غير مسجّل الدخول → 403 requiresLogin" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw)"

$r = Check-Exam $std 999999 $stdToken
$pass = ($r.Status -eq 404) -and ($r.Json.success -eq $false)
Record-Test -TestId "EX-23" -Description "درس غير موجود → 404 دون خطأ خادم" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw)"

Write-Host "`n[9] Saving an assignment: lecture link and questions are validated (Teacher/ControlPanel AssingQuestionToAssignment)" -ForegroundColor Yellow
$body = @{ Name = "QA_ASG_BAD_NOLEC"; Description = "QA"; Lecture_ID = 0; Assignmet_Degry = 10; Assignmet_degry_Win = 1; L_Assignment_Q = @(@{ Q_ID = $Q_TF }) } | ConvertTo-Json -Compress -Depth 4
$r = Invoke-Post -Session $teacher -Url "$BaseUrl/Teacher/AssingQuestionToAssignment" -Body $body -ContentType "application/json; charset=utf-8"
$cnt = [int](Sql "SELECT COUNT(*) FROM Assignments WHERE Name='QA_ASG_BAD_NOLEC';")
$pass = ($r.Status -eq 400) -and ($r.Json.success -eq $false) -and ($r.Json.response -match 'المحاضرة المرتبطة') -and ($cnt -eq 0)
Record-Test -TestId "TS-01" -Description "المعلم: حفظ وظيفة بلا محاضرة (Lecture_ID=0) → 400 برسالة واضحة ولا يُحفظ شيء" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw) rows=$cnt"

$body = @{ Name = "QA_ASG_BAD_NOLEC2"; Description = "QA"; Lecture_ID = 999999; Assignmet_Degry = 10; Assignmet_degry_Win = 1; L_Assignment_Q = @(@{ Q_ID = $Q_TF }) } | ConvertTo-Json -Compress -Depth 4
$r = Invoke-Post -Session $admin -Url "$BaseUrl/ControlPanel/AssingQuestionToAssignment" -Body $body -ContentType "application/json; charset=utf-8"
$cnt = [int](Sql "SELECT COUNT(*) FROM Assignments WHERE Name='QA_ASG_BAD_NOLEC2';")
$pass = ($r.Status -eq 400) -and ($r.Json.success -eq $false) -and ($r.Json.response -match 'المحاضرة المرتبطة') -and ($cnt -eq 0)
Record-Test -TestId "TS-02" -Description "الإدارة: حفظ وظيفة على محاضرة غير موجودة → 400 ولا يُحفظ شيء" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw) rows=$cnt"

$body = @{ Name = "QA_ASG_BAD_NOQ"; Description = "QA"; Lecture_ID = $LEC_EX_NOASG; Assignmet_Degry = 10; Assignmet_degry_Win = 1; L_Assignment_Q = @() } | ConvertTo-Json -Compress -Depth 4
$r = Invoke-Post -Session $teacher -Url "$BaseUrl/Teacher/AssingQuestionToAssignment" -Body $body -ContentType "application/json; charset=utf-8"
$cnt = [int](Sql "SELECT COUNT(*) FROM Assignments WHERE Name='QA_ASG_BAD_NOQ';")
$pass = ($r.Status -eq 400) -and ($r.Json.success -eq $false) -and ($r.Json.response -match 'سؤال واحد على الأقل') -and ($cnt -eq 0)
Record-Test -TestId "TS-03" -Description "المعلم: حفظ وظيفة بلا أسئلة → 400 «يجب إضافة سؤال واحد على الأقل»" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw) rows=$cnt"

Write-Host "`n[10] Cleanup" -ForegroundColor Yellow
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
