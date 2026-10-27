param(
    [string]$BaseUrl = "http://localhost:50895",
    [string]$AdminPassword = "Admin@Tabseet2026!",
    [string]$TeacherPassword = "Teach@Tabseet2026!",
    [int]$CAT = 6
)

# اختبارات قاعدة «الأحدث أولاً» (Newest First) - 2026-10-27:
#   [1] المحاضرات: إنشاء ثلاث محاضرات بالتتابع ثم التحقق أن قوائم الإدارة والمعلم تعرضها (الثالثة، الثانية، الأولى)،
#       وأن الترتيب يبقى بعد التحديث ومع التصفية، وأن تعديل محاضرة قديمة لا ينقلها إلى الأعلى.
#   [2] الطلاب: إنشاء ثلاثة طلاب عبر لوحة الإدارة ثم التحقق أن صفحة الطلاب تعرضهم في الأعلى بترتيب (3، 2، 1)
#       اعتماداً على العمود الجديد AspNetUsers.CreatedAt (ترحيل 20261027_AspNetUsers_CreatedAt.sql).
#   [3] بنك الأسئلة والاختبارات والوظائف: إنشاء ثلاثة أسئلة وثلاثة اختبارات ثم التحقق من ترتيب القوائم (إدارة + معلم).
#   [4] الدورات والاشتراكات وقوائم إدارية أخرى على البيانات الموجودة (الدورات، دورات المعلم، طلاب الدورة،
#       طلبات الاشتراك، أكواد التفعيل، إشعارات الإدارة، دورات الطالب).
#   [5] فحوص ثابتة: كل جداول DataTables في القوائم تحمل "order": [] حتى لا تقلب ترتيب الخادم، مع بقاء
#       الاستثناءات (جدول الحصص بالموعد، تسلسل دروس الطالب، ترتيب الأسئلة داخل الاختبار) دون تغيير.
# يستخدم الدورة 6 ويُنشئ سجلات باسم QA_NF_* / qa_nf_std*@example.test ثم يحذفها.

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
    param($Session, [string]$Url)
    $status = 0; $content = ""; $json = $null
    try {
        $res = Invoke-WebRequest -Uri $Url -WebSession $Session -UseBasicParsing
        $status = [int]$res.StatusCode; $content = $res.Content
    } catch {
        if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
        $content = $_.ErrorDetails.Message
        if (-not $content) { $content = $_.Exception.Message }
    }
    try { $json = $content | ConvertFrom-Json } catch { }
    return [PSCustomObject]@{ Status = $status; Json = $json; Raw = $content }
}

# هل التسلسل تنازلي تماماً (كل عنصر أصغر من سابقه)؟
function Test-Descending {
    param([int[]]$Ids)
    if (-not $Ids -or $Ids.Count -lt 2) { return $true }
    for ($i = 1; $i -lt $Ids.Count; $i++) { if ($Ids[$i] -ge $Ids[$i - 1]) { return $false } }
    return $true
}

# معرّفات العمود الأول (<td>ID</td>) من صفوف جدول مُصيَّر في الخادم، بترتيب ظهورها في HTML
function Get-FirstCellIds {
    param([string]$Html)
    $tbody = [regex]::Match($Html, '<tbody[^>]*>([\s\S]*?)</tbody>')
    if (-not $tbody.Success) { return @() }
    $ids = [regex]::Matches($tbody.Groups[1].Value, '<tr[^>]*>\s*<td[^>]*>\s*(\d+)\s*</td>') | ForEach-Object { [int]$_.Groups[1].Value }
    return @($ids)
}

function Clean-TestData {
    $null = Sql "DELETE FROM std_lecture WHERE Lecture_ID IN (SELECT ID FROM Lectures WHERE Lecture_Name LIKE 'QA_NF_%');"
    $null = Sql "DELETE FROM Assignment_Q WHERE Assignment_ID IN (SELECT ID FROM Assignments WHERE Lecture_ID IN (SELECT ID FROM Lectures WHERE Lecture_Name LIKE 'QA_NF_%'));"
    $null = Sql "DELETE FROM Assignments WHERE Lecture_ID IN (SELECT ID FROM Lectures WHERE Lecture_Name LIKE 'QA_NF_%');"
    $null = Sql "DELETE FROM Lectures WHERE Lecture_Name LIKE 'QA_NF_%';"
    $null = Sql "DELETE FROM Exam_Q WHERE Exam_ID IN (SELECT ID FROM Exams WHERE Name LIKE 'QA_NF_%');"
    $null = Sql "DELETE FROM Exams WHERE Name LIKE 'QA_NF_%';"
    $null = Sql "DELETE FROM Q_TrueOrFolse WHERE Question_ID IN (SELECT ID FROM Questions WHERE Q_Name LIKE 'QA_NF_%');"
    $null = Sql "DELETE FROM Exam_Q WHERE Q_ID IN (SELECT ID FROM Questions WHERE Q_Name LIKE 'QA_NF_%');"
    $null = Sql "DELETE FROM Assignment_Q WHERE Q_ID IN (SELECT ID FROM Questions WHERE Q_Name LIKE 'QA_NF_%');"
    $null = Sql "DELETE FROM Questions WHERE Q_Name LIKE 'QA_NF_%';"
    $null = Sql "DELETE FROM AspNetUserRoles WHERE UserId IN (SELECT Id FROM AspNetUsers WHERE Email LIKE 'qa_nf_std%@example.test');"
    $null = Sql "DELETE FROM AspNetUsers WHERE Email LIKE 'qa_nf_std%@example.test';"
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Newest First (الأحدث أولاً) - default ordering test suite" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$hasCreatedAt = Sql "SELECT CASE WHEN COL_LENGTH('dbo.AspNetUsers','CreatedAt') IS NULL THEN 0 ELSE 1 END;"
Record-Test "NF-00" "AspNetUsers.CreatedAt column exists on the test database (migration applied)" ($hasCreatedAt -eq "1") "COL_LENGTH=$hasCreatedAt"

Clean-TestData
$admin = Get-AuthSession -Email "admin.test@example.test" -Password $AdminPassword
$teacher = Get-AuthSession -Email "teacher.ibrahim@example.test" -Password $TeacherPassword
$lecToken = Get-VerificationToken -Session $admin -Url "$BaseUrl/ControlPanel/LecturesByCategory?Category_ID=$CAT&categoryName=QA"
Record-Test "NF-00b" "Admin + teacher sessions and lecture CSRF token" ([bool]$lecToken) "token length=$($lecToken.Length)"

# ---------------------------------------------------------------- [1] Lectures
Write-Host "`n[1] Lectures: create 1 -> 2 -> 3, expect 3, 2, 1" -ForegroundColor Yellow
$createUrl = "$BaseUrl/ControlPanel/CreateLectuer"
$LEC = @{}
foreach ($n in 1..3) {
    $body = @{ "__RequestVerificationToken" = $lecToken; Lecture_Name = "QA_NF_LEC_$n"; Category_ID = $CAT; LectureTypeOnOff = "1"; DayandTime = "2026-11-0$n 10:00"; LectureLink = "https://meet.google.com/qa-nf-$n"; LectureLink_Client = "https://meet.google.com/qa-nf-$n"; MediaLink = ""; Others = "QA"; LectureMinTime = "0" }
    $r = Invoke-Post -Session $admin -Url $createUrl -Body $body
    $LEC[$n] = [int](Sql "SELECT ISNULL(MAX(ID),0) FROM Lectures WHERE Lecture_Name='QA_NF_LEC_$n';")
    Record-Test "NF-01$([char](96+$n))" "Create lecture QA_NF_LEC_$n through the real endpoint" ($r.Status -eq 200 -and $r.Json.success -eq $true -and $LEC[$n] -gt 0) "status=$($r.Status) id=$($LEC[$n]) $($r.Json.responseText)"
}
$expected = @($LEC[3], $LEC[2], $LEC[1])
Record-Test "NF-01d" "Lecture IDs grow with creation order (ID reflects creation)" ($LEC[1] -lt $LEC[2] -and $LEC[2] -lt $LEC[3]) "$($LEC[1]) < $($LEC[2]) < $($LEC[3])"

$r = Invoke-Post -Session $admin -Url "$BaseUrl/ControlPanel/AllLectures" -Body @{ byCategorie = $CAT }
$ids = @($r.Json | ForEach-Object { [int]$_.ID })
$top3 = @($ids | Select-Object -First 3)
Record-Test "NF-02" "Admin AllLectures (category filter): newest first = 3, 2, 1 and whole list descending" (($top3 -join ",") -eq ($expected -join ",") -and (Test-Descending $ids)) "top=$($top3 -join ',') expected=$($expected -join ',') count=$($ids.Count)"
$first = $ids -join ","

$r = Invoke-Post -Session $admin -Url "$BaseUrl/ControlPanel/AllLectures" -Body @{ byCategorieType = 0; byCategorie = 0 }
$idsAll = @($r.Json | ForEach-Object { [int]$_.ID })
Record-Test "NF-03" "Admin AllLectures (no filter): descending and the three new lectures on top" ((Test-Descending $idsAll) -and (($idsAll | Select-Object -First 3) -join ",") -eq ($expected -join ",")) "top=$(($idsAll | Select-Object -First 3) -join ',') count=$($idsAll.Count)"

$r = Invoke-Post -Session $admin -Url "$BaseUrl/ControlPanel/AllLectures" -Body @{ byCategorie = $CAT }
$again = (@($r.Json | ForEach-Object { [int]$_.ID })) -join ","
Record-Test "NF-04" "Refresh (second request) returns the identical order" ($again -eq $first) "first=$first again=$again"

$r = Invoke-Post -Session $teacher -Url "$BaseUrl/Teacher/LectureByCategorysStd" -Body @{ Cat_ID = $CAT }
$tids = @($r.Json | ForEach-Object { [int]$_.ID })
Record-Test "NF-05" "Teacher LectureByCategorysStd: newest first = 3, 2, 1 and descending" ((($tids | Select-Object -First 3) -join ",") -eq ($expected -join ",") -and (Test-Descending $tids)) "top=$(($tids | Select-Object -First 3) -join ',') status=$($r.Status)"

$r = Invoke-Get -Session $teacher -Url "$BaseUrl/Teacher/StdonlineCourse"
$pageIds = Get-FirstCellIds $r.Raw
$qaOnPage = @($pageIds | Where-Object { $expected -contains $_ })
Record-Test "NF-06" "Teacher live-lectures page (server rendered): rows descending, new lectures ordered 3, 2, 1" (($qaOnPage -join ",") -eq ($expected -join ",") -and (Test-Descending $pageIds)) "qa=$($qaOnPage -join ',') rows=$($pageIds.Count) status=$($r.Status)"

$edit = @{ "__RequestVerificationToken" = $lecToken; ID = $LEC[1]; Lecture_Name = "QA_NF_LEC_1 (edited)"; Category_ID = $CAT; LectureTypeOnOff = "1"; DayandTime = "2026-11-09 10:00"; LectureLink = "https://meet.google.com/qa-nf-1e"; LectureLink_Client = "https://meet.google.com/qa-nf-1e"; MediaLink = ""; Others = "QA edited"; LectureMinTime = "0" }
$r = Invoke-Post -Session $admin -Url "$BaseUrl/ControlPanel/EditLectuer" -Body $edit
$r2 = Invoke-Post -Session $admin -Url "$BaseUrl/ControlPanel/AllLectures" -Body @{ byCategorie = $CAT }
$afterEdit = (@($r2.Json | ForEach-Object { [int]$_.ID })) -join ","
$editedName = ($r2.Json | Where-Object { [int]$_.ID -eq $LEC[1] }).Lecture_Name
Record-Test "NF-07" "Editing the oldest lecture does not move it up (order by creation, not last edit)" ($r.Json.success -eq $true -and $afterEdit -eq $first -and $editedName -like "*edited*") "edit=$($r.Json.success) order=$afterEdit name=$editedName"

# ---------------------------------------------------------------- [2] Students
Write-Host "`n[2] Students: create 1 -> 2 -> 3, expect 3, 2, 1 at the top of the admin list" -ForegroundColor Yellow
$STD = @{}
foreach ($n in 1..3) {
    $body = @{ RealName = "QA_NF_STD_$n"; Email = "qa_nf_std$n@example.test"; PhoneNumber = "05000000$n"; Password = "Qa@Tabseet2026!" } | ConvertTo-Json -Compress
    $r = Invoke-Post -Session $admin -Url "$BaseUrl/ControlPanel/AddNewStd" -Body $body -ContentType "application/json; charset=utf-8"
    $row = Sql "SELECT Id + '|' + ISNULL(CONVERT(varchar(27), CreatedAt, 126), 'NULL') FROM AspNetUsers WHERE Email='qa_nf_std$n@example.test';"
    $STD[$n] = $row
    Record-Test "NF-08$([char](96+$n))" "Create student qa_nf_std$n through AddNewStd; CreatedAt stamped by the DB default" ($r.Status -eq 200 -and $r.Json.success -eq $true -and $row -and $row -notlike "*|NULL") "status=$($r.Status) row=$row"
}
$c1 = ($STD[1] -split '\|')[1]; $c2 = ($STD[2] -split '\|')[1]; $c3 = ($STD[3] -split '\|')[1]
Record-Test "NF-08d" "CreatedAt grows with creation order" ($c1 -lt $c2 -and $c2 -lt $c3) "$c1 < $c2 < $c3"

function Get-StudentOrder {
    param([string]$Html)
    $tbody = [regex]::Match($Html, '<tbody[^>]*>([\s\S]*?)</tbody>').Groups[1].Value
    $p3 = $tbody.IndexOf("qa_nf_std3@example.test"); $p2 = $tbody.IndexOf("qa_nf_std2@example.test"); $p1 = $tbody.IndexOf("qa_nf_std1@example.test")
    $pLegacy = $tbody.IndexOf("student01@example.test")
    return [PSCustomObject]@{ P3 = $p3; P2 = $p2; P1 = $p1; Legacy = $pLegacy }
}
$r = Invoke-Get -Session $admin -Url "$BaseUrl/ControlPanel/Student"
$o = Get-StudentOrder $r.Raw
$okOrder = ($o.P3 -ge 0 -and $o.P3 -lt $o.P2 -and $o.P2 -lt $o.P1)
$okTop = ($o.Legacy -lt 0 -or $o.P1 -lt $o.Legacy)
Record-Test "NF-09" "Admin Student page: 3, 2, 1 in that order and above legacy accounts (NULL CreatedAt last)" ($okOrder -and $okTop) "p3=$($o.P3) p2=$($o.P2) p1=$($o.P1) legacy=$($o.Legacy) status=$($r.Status)"
$r = Invoke-Get -Session $admin -Url "$BaseUrl/ControlPanel/Student"
$o2 = Get-StudentOrder $r.Raw
Record-Test "NF-10" "Admin Student page after refresh: same order" ($o2.P3 -ge 0 -and $o2.P3 -lt $o2.P2 -and $o2.P2 -lt $o2.P1) "p3=$($o2.P3) p2=$($o2.P2) p1=$($o2.P1)"
$r = Invoke-Post -Session $admin -Url "$BaseUrl/ControlPanel/GetUsers" -Body @{}
$rt = Invoke-Post -Session $admin -Url "$BaseUrl/ControlPanel/GetTeachers" -Body @{}
Record-Test "NF-11" "GetUsers / GetTeachers still return their lists through the ordering helper" ($r.Status -eq 200 -and @($r.Json).Count -gt 0 -and $rt.Status -eq 200 -and @($rt.Json).Count -gt 0) "users=$(@($r.Json).Count) teachers=$(@($rt.Json).Count)"

# ---------------------------------------------------------------- [3] Questions / exams / assignments
Write-Host "`n[3] Question bank, exams, assignments" -ForegroundColor Yellow
$Q = @{}
foreach ($n in 1..3) {
    $body = @{ Category_ID = $CAT; Q_Name = "QA_NF_Q_$n"; Q_Text = "QA newest first $n"; Q_TrueOrFalse = @{ TrueValue = 1; Degry = 1 } } | ConvertTo-Json -Compress
    $r = Invoke-Post -Session $admin -Url "$BaseUrl/ControlPanel/CreateTruOrFalseQuestion" -Body $body -ContentType "application/json; charset=utf-8"
    $Q[$n] = [int](Sql "SELECT ISNULL(MAX(ID),0) FROM Questions WHERE Q_Name='QA_NF_Q_$n';")
    Record-Test "NF-12$([char](96+$n))" "Create true/false question QA_NF_Q_$n" ($r.Status -eq 200 -and $r.Json.success -eq $true -and $Q[$n] -gt 0) "status=$($r.Status) id=$($Q[$n]) $($r.Json.response)"
}
$expQ = @($Q[3], $Q[2], $Q[1])
$r = Invoke-Post -Session $admin -Url "$BaseUrl/ControlPanel/GetAllQuesinBank" -Body @{ type = 0; category_ID = $CAT }
$qids = @($r.Json | ForEach-Object { [int]$_.ID })
Record-Test "NF-13" "Admin question bank (category filter): 3, 2, 1 on top and descending" ((($qids | Select-Object -First 3) -join ",") -eq ($expQ -join ",") -and (Test-Descending $qids)) "top=$(($qids | Select-Object -First 3) -join ',') count=$($qids.Count)"
$r = Invoke-Post -Session $admin -Url "$BaseUrl/ControlPanel/GetAllQuesinBank" -Body @{ type = 0; category_ID = 0 }
$qall = @($r.Json | ForEach-Object { [int]$_.ID })
Record-Test "NF-14" "Admin question bank (no filter): descending with the new questions on top" ((Test-Descending $qall) -and (($qall | Select-Object -First 3) -join ",") -eq ($expQ -join ",")) "top=$(($qall | Select-Object -First 3) -join ',') count=$($qall.Count)"
$r = Invoke-Post -Session $teacher -Url "$BaseUrl/Teacher/GetAllQuesinBankTeacher" -Body @{ cat_ID = $CAT }
$tq = @($r.Json | ForEach-Object { [int]$_.ID })
$r2 = Invoke-Post -Session $teacher -Url "$BaseUrl/Teacher/GetQuesinBankTeacher" -Body @{}
$tq2 = @($r2.Json | ForEach-Object { [int]$_.ID })
Record-Test "NF-15" "Teacher question bank endpoints: 3, 2, 1 on top (by category) and descending (all teacher categories)" ((($tq | Select-Object -First 3) -join ",") -eq ($expQ -join ",") -and (Test-Descending $tq) -and (Test-Descending $tq2) -and (($tq2 | Select-Object -First 3) -join ",") -eq ($expQ -join ",")) "byCat=$(($tq | Select-Object -First 3) -join ',') all=$(($tq2 | Select-Object -First 3) -join ',')"

$E = @{}
foreach ($n in 1..3) {
    $body = @{ Name = "QA_NF_EXAM_$n"; Description = "QA"; Category_ID = $CAT; Duration = 10; Exam_Degry = 10; Exam_win_Degry = 5; Exam_Q = @() } | ConvertTo-Json -Compress
    $r = Invoke-Post -Session $admin -Url "$BaseUrl/ControlPanel/CreateExams" -Body $body -ContentType "application/json; charset=utf-8"
    $E[$n] = [int](Sql "SELECT ISNULL(MAX(ID),0) FROM Exams WHERE Name='QA_NF_EXAM_$n';")
    Record-Test "NF-16$([char](96+$n))" "Create exam QA_NF_EXAM_$n" ($r.Status -eq 200 -and $r.Json.success -eq $true -and $E[$n] -gt 0) "status=$($r.Status) id=$($E[$n]) $($r.Json.response)"
}
$expE = @($E[3], $E[2], $E[1])
$r = Invoke-Post -Session $admin -Url "$BaseUrl/ControlPanel/getAllExams" -Body @{ Duration = 0; category = $CAT }
$eids = @($r.Json | ForEach-Object { [int]$_.ID })
$r2 = Invoke-Post -Session $admin -Url "$BaseUrl/ControlPanel/getAllExams" -Body @{ Duration = 0; category = 0 }
$eall = @($r2.Json | ForEach-Object { [int]$_.ID })
$r3 = Invoke-Post -Session $teacher -Url "$BaseUrl/Teacher/getAllExams" -Body @{ Duration = 0; category = $CAT }
$teids = @($r3.Json | ForEach-Object { [int]$_.ID })
Record-Test "NF-17" "Exams lists (admin filtered / admin all / teacher): 3, 2, 1 on top and descending" ((($eids | Select-Object -First 3) -join ",") -eq ($expE -join ",") -and (Test-Descending $eids) -and (Test-Descending $eall) -and (($teids | Select-Object -First 3) -join ",") -eq ($expE -join ",")) "admin=$(($eids | Select-Object -First 3) -join ',') all=$(($eall | Select-Object -First 3) -join ',') teacher=$(($teids | Select-Object -First 3) -join ',')"

$r = Invoke-Post -Session $admin -Url "$BaseUrl/ControlPanel/GetAllAssinment" -Body @{}
$aids = @($r.Json | ForEach-Object { [int]$_.ID })
$r2 = Invoke-Post -Session $teacher -Url "$BaseUrl/Teacher/GetAllAssinment" -Body @{}
$taids = @($r2.Json | ForEach-Object { [int]$_.ID })
$maxA = [int](Sql "SELECT ISNULL(MAX(ID),0) FROM Assignments;")
Record-Test "NF-18" "Assignments lists (admin + teacher): descending, newest assignment first" ((Test-Descending $aids) -and (Test-Descending $taids) -and ($aids.Count -eq 0 -or $aids[0] -eq $maxA)) "admin=$($aids -join ',') teacher=$($taids -join ',') max=$maxA"

# ---------------------------------------------------------------- [4] Courses, subscriptions, other admin lists (existing data)
Write-Host "`n[4] Courses, subscriptions and other admin lists" -ForegroundColor Yellow
$r = Invoke-Get -Session $admin -Url "$BaseUrl/ControlPanel/Categories"
$cids = Get-FirstCellIds $r.Raw
$maxC = [int](Sql "SELECT ISNULL(MAX(ID),0) FROM Categories;")
$hasOrder = $r.Raw -match '"ordering":\s*true,\s*"order":\s*\[\]'
Record-Test "NF-19" "Admin Categories page: rows descending, newest course first, DataTable keeps server order" ((Test-Descending $cids) -and $cids.Count -gt 1 -and $cids[0] -eq $maxC -and $hasOrder) "first=$($cids[0]) max=$maxC rows=$($cids.Count) order[]=$hasOrder"
$r = Invoke-Post -Session $admin -Url "$BaseUrl/Home/GetAllMoreCourse" -Body @{}
$mids = @($r.Json | ForEach-Object { [int]$_.ID })
Record-Test "NF-20" "Public course catalogue (GetAllMoreCourse): descending" ((Test-Descending $mids) -and $mids.Count -gt 0) "ids=$($mids -join ',')"
$r = Invoke-Get -Session $teacher -Url "$BaseUrl/Teacher/TeacherCourse"
$tcids = Get-FirstCellIds $r.Raw
Record-Test "NF-21" "Teacher courses page: descending (newest course first)" ((Test-Descending $tcids) -and $tcids.Count -gt 1) "ids=$($tcids -join ',')"

$r = Invoke-Get -Session $teacher -Url "$BaseUrl/Teacher/TeacherCoursestd?ID=$CAT"
$tbody = [regex]::Match($r.Raw, '<tbody[^>]*>([\s\S]*?)</tbody>').Groups[1].Value
$firstEmail = [regex]::Match($tbody, '<td>\s*([^<\s]+@[^<\s]+)\s*</td>').Groups[1].Value
$expectedEmail = Sql "SELECT TOP 1 u.Email FROM Std_Reg_Category r JOIN AspNetUsers u ON u.Id = r.Std_ID WHERE r.Category_ID = $CAT AND r.Std_ID IS NOT NULL GROUP BY u.Email ORDER BY MAX(r.ID) DESC;"
Record-Test "NF-22" "Teacher course students: latest subscriber first (not alphabetical)" ($firstEmail -and $firstEmail -eq $expectedEmail) "first=$firstEmail expected=$expectedEmail"

$r = Invoke-Get -Session $admin -Url "$BaseUrl/ControlPanel/ActiveCourse"
$pids = Get-FirstCellIds $r.Raw
$okPending = $true
if ($pids.Count -gt 1) {
    $stamps = @()
    foreach ($id in $pids) { $stamps += (Sql "SELECT ISNULL(CONVERT(varchar(27), RequestedAt, 126), '') FROM Std_Reg_Category WHERE ID=$id;") }
    for ($i = 1; $i -lt $stamps.Count; $i++) {
        if ($stamps[$i] -ne "" -and ($stamps[$i - 1] -eq "" -or $stamps[$i] -gt $stamps[$i - 1])) { $okPending = $false }
        if ($stamps[$i] -eq $stamps[$i - 1] -and $pids[$i] -gt $pids[$i - 1]) { $okPending = $false }
    }
}
Record-Test "NF-23" "Pending subscriptions page: newest request first (RequestedAt desc, ID desc, no DataTable re-sort)" ($okPending -and ($r.Raw -match '"order":\s*\[\]')) "ids=$($pids -join ',')"

$r = Invoke-Get -Session $admin -Url "$BaseUrl/ControlPanel/CodeGenerated"
$gids = Get-FirstCellIds $r.Raw
Record-Test "NF-24" "Activation codes page: descending" (Test-Descending $gids) "ids=$($gids -join ',')"
$r = Invoke-Get -Session $admin -Url "$BaseUrl/AdminNotifications/GetLatest?count=5"
$nids = @($r.Json.items | ForEach-Object { [int]$_.NotificationId })
if ($nids.Count -eq 0) { $nids = @($r.Json.data | ForEach-Object { [int]$_.NotificationId }) }
Record-Test "NF-25" "Admin notifications (latest): newest first by CreatedAt then NotificationId" ($r.Status -eq 200 -and (Test-Descending $nids)) "ids=$($nids -join ',') status=$($r.Status)"
$r = Invoke-Get -Session $admin -Url "$BaseUrl/ControlPanel/GetStdCourse?std_ID=TEST-USR-STD-01"
$tbody = [regex]::Match($r.Raw, '<tbody[^>]*>([\s\S]*?)</tbody>').Groups[1].Value
$firstCourse = [regex]::Match($tbody, '<td>\*</td>\s*<td>([^<]+)</td>').Groups[1].Value.Trim()
$expectedCourse = Sql "SELECT TOP 1 c.NameAr FROM Std_Reg_Category r JOIN Categories c ON c.ID = r.Category_ID WHERE r.Std_ID = 'TEST-USR-STD-01' ORDER BY r.ID DESC;"
Record-Test "NF-26" "Student's courses (admin GetStdCourse): latest registration first" ($firstCourse -and $firstCourse -eq $expectedCourse) "first=$firstCourse expected=$expectedCourse"

# ---------------------------------------------------------------- [5] Static checks on the views / exceptions
Write-Host "`n[5] Static checks: DataTables keep server order; exceptions untouched" -ForegroundColor Yellow
$root = Split-Path -Parent $PSScriptRoot
$views = @(
    "Controlpanel\Lectures", "Controlpanel\LecturesByCategory", "Controlpanel\Student", "Controlpanel\Users", "Controlpanel\Categories",
    "Controlpanel\categoriesUIUX", "Controlpanel\Exams", "Controlpanel\QuestionBank", "Controlpanel\CodeGenerated", "Controlpanel\ActiveCourse",
    "Controlpanel\GetStdCourse", "Controlpanel\AddAssignment", "Controlpanel\ViewAssingment", "Controlpanel\ViewExam",
    "Teacher\TeacherCourse", "Teacher\TeacherCoursestd", "Teacher\LecturesByCategoryStudend", "Teacher\StdonlineCourse", "Teacher\ExamsTeacher",
    "Teacher\QuestionBankTeacher", "Teacher\AddAssignment", "Teacher\ViewAssingment", "Teacher\ViewExamTeacher")
$missing = @()
$total = 0
foreach ($v in $views) {
    $text = Get-Content (Join-Path $root "tabseetCourse\Views\$v.cshtml") -Raw
    $inits = [regex]::Matches($text, '\.DataTable\(\{[\s\S]*?\}\)')
    foreach ($m in $inits) {
        $total++
        if ($m.Value -notmatch '"order":\s*\[\]') { $missing += $v }
    }
}
Record-Test "NF-27" "Every list DataTable init in the $($views.Count) list views carries ""order"": [] (keeps server order, manual sort still allowed)" ($missing.Count -eq 0 -and $total -ge 24) "inits=$total missing=$($missing -join ',')"

$viewExam = Get-Content (Join-Path $root "tabseetCourse\Views\Controlpanel\ViewExam.cshtml") -Raw
$examQuestionsNotDataTable = ($viewExam -notmatch "#Table_Options'?\)\.DataTable")
$students = Get-Content (Join-Path $root "tabseetCourse\Controllers\StudentsController.cs") -Raw
$scheduleKept = ($students -match '\.OrderBy\(x => x\.Item3\)') -and ($students -match '\.OrderByDescending\(x => x\.Item3\)')
$lecturesStdUntouched = ($students -match 'dbEntity\.Lectures\.Where\(e => e\.Category_ID == cat_ID\)\.ToList\(\);')
Record-Test "NF-28" "Exceptions kept: exam question order (Table_Options) not a DataTable, student schedule by date, student lecture sequence untouched" ($examQuestionsNotDataTable -and $scheduleKept -and $lecturesStdUntouched) "examQ=$examQuestionsNotDataTable schedule=$scheduleKept lecturesStd=$lecturesStdUntouched"

# ---------------------------------------------------------------- Cleanup
Write-Host "`n[6] Cleanup" -ForegroundColor Yellow
Clean-TestData
$left = Sql "SELECT (SELECT COUNT(*) FROM Lectures WHERE Lecture_Name LIKE 'QA_NF_%') + (SELECT COUNT(*) FROM Questions WHERE Q_Name LIKE 'QA_NF_%') + (SELECT COUNT(*) FROM Exams WHERE Name LIKE 'QA_NF_%') + (SELECT COUNT(*) FROM AspNetUsers WHERE Email LIKE 'qa_nf_std%@example.test');"
Record-Test "NF-29" "All QA_NF_* test records removed" ($left -eq "0") "left=$left"

Write-Host "`n==========================================================" -ForegroundColor Cyan
$pass = ($results | Where-Object { $_.Result -eq "PASS" }).Count
$fail = ($results | Where-Object { $_.Result -eq "FAIL" }).Count
Write-Host "   Total: $($results.Count)   PASS: $pass   FAIL: $fail" -ForegroundColor $(if ($fail -eq 0) { "Green" } else { "Red" })
Write-Host "==========================================================" -ForegroundColor Cyan
if ($fail -gt 0) { $results | Where-Object { $_.Result -eq "FAIL" } | Format-Table -AutoSize | Out-String | Write-Host }
exit $(if ($fail -eq 0) { 0 } else { 1 })
