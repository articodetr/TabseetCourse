param(
    [string]$BaseUrl = "http://localhost:50895"
)

# اختبارات آلية احتساب الحضور بوقت التشغيل الفعلي (StartLecture / LectureHeartbeat / JoinLiveLecture)
# + التسجيل اليدوي من المدرس + تقرير الإدارة + مسار الواجب.
# يستخدم student01 (مسجل في الدورات 1,2,4,5,6,10) والمحاضرتين 147 و148 (الدورة 10) ويعيد حالتهما كما كانت.

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
    param($Session, [string]$Url, $Body)
    $status = 0; $content = ""; $json = $null
    try {
        $res = Invoke-WebRequest -Uri $Url -Method POST -Body $Body -WebSession $Session -UseBasicParsing
        $status = [int]$res.StatusCode; $content = $res.Content
    } catch {
        if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
        $content = $_.ErrorDetails.Message
        if (-not $content) { $content = $_.Exception.Message }
    }
    try { $json = $content | ConvertFrom-Json } catch { }
    return [PSCustomObject]@{ Status = $status; Json = $json; Raw = $content }
}

function Get-Row {
    param([string]$StudentId, [int]$LectureId)
    $raw = Sql "SELECT ISNULL(IsFinish,-1), ISNULL(IsTimeEnd,-1), ISNULL(IsAssingmetFinish,-1), WatchedSeconds, ISNULL(RequiredSeconds,-1), ISNULL(Source,'NULL'), CASE WHEN StartedAt IS NULL THEN 0 ELSE 1 END, CASE WHEN CompletedAt IS NULL THEN 0 ELSE 1 END, ISNULL(MarkedBy,'NULL') FROM std_lecture WHERE Std_ID='$StudentId' AND Lecture_ID=$LectureId;"
    if (-not $raw) { return $null }
    $p = ($raw -split '\s+')
    return [PSCustomObject]@{ IsFinish = [int]$p[0]; IsTimeEnd = [int]$p[1]; IsAssign = [int]$p[2]; Watched = [int]$p[3]; Required = [int]$p[4]; Source = $p[5]; HasStart = [int]$p[6]; HasCompleted = [int]$p[7]; MarkedBy = $p[8] }
}

function Count-Rows { param([string]$StudentId, [int]$LectureId) [int](Sql "SELECT COUNT(*) FROM std_lecture WHERE Std_ID='$StudentId' AND Lecture_ID=$LectureId;") }

$student = "TEST-USR-STD-01"
$LEC_REC = 147      # مسجلة، الدورة 10، LectureMinTime = 1 → 60 ثانية مطلوبة
$LEC_REC2 = 148     # مسجلة، الدورة 10 (للتسجيل اليدوي)
$LEC_LIVE = 21      # مباشرة، الدورة 6
$LEC_ASSIGN = 2     # مسجلة؟ لا: محاضرة 2 في الدورة 1 ولها واجب رقم 2 (لاختبار مسار الواجب)

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Attendance - test suite" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# تهيئة: إعادة ضبط صفوف student01 للمحاضرات 147 و2، وحذف 148 والمحاضرات التجريبية
$null = Sql "UPDATE std_lecture SET IsFinish=0, IsTimeEnd=0, IsAssingmetFinish=0, StartedAt=NULL, LastHeartbeatAt=NULL, WatchedSeconds=0, RequiredSeconds=NULL, CompletedAt=NULL, Source=NULL, MarkedBy=NULL, MarkedAt=NULL WHERE Std_ID='$student' AND Lecture_ID IN ($LEC_REC, $LEC_ASSIGN);"
$null = Sql "DELETE FROM std_lecture WHERE Std_ID='$student' AND Lecture_ID = $LEC_REC2;"
$null = Sql "DELETE FROM std_lecture WHERE Lecture_ID IN (SELECT ID FROM Lectures WHERE Lecture_Name LIKE 'QA_ATT_%');"
$null = Sql "DELETE FROM Lectures WHERE Lecture_Name LIKE 'QA_ATT_%';"

Write-Host "`n[1] Authenticating..." -ForegroundColor Yellow
$std = Get-AuthSession -Email "student01@example.test" -Password "Std@Tabseet2026!"
$stdToken = Get-VerificationToken -Session $std -Url "$BaseUrl/Students/CategoriesStd"
if (-not $stdToken) { throw "Student token not found on CategoriesStd (layout token missing?)" }
Write-Host "Student token: $($stdToken.Substring(0,10))..."

$startUrl = "$BaseUrl/Students/StartLecture"
$hbUrl = "$BaseUrl/Students/LectureHeartbeat"
$joinUrl = "$BaseUrl/Students/JoinLiveLecture"

Write-Host "`n[2] Recorded lecture: start + heartbeats" -ForegroundColor Yellow
$r = Invoke-Post -Session $std -Url $startUrl -Body @{ "__RequestVerificationToken" = $stdToken; lectureId = $LEC_REC; durationSeconds = 3600 }
$row = Get-Row $student $LEC_REC
$pass = ($r.Status -eq 200) -and $r.Json.success -and ($r.Json.status.requiredSeconds -eq 60) -and ($r.Json.status.watchedSeconds -eq 0) -and (-not $r.Json.status.isFinish) -and $row -and ($row.HasStart -eq 1) -and ($row.Source -eq "Recorded") -and ($row.Required -eq 60)
Record-Test -TestId "ATT-01" -Description "StartLecture: ينشئ الصف ويحدد المطلوب = دقيقة المدرس (60 ثانية)" -Passed $pass -Details "resp=$($r.Raw) | row=$($row | ConvertTo-Json -Compress)"

$r = Invoke-Post -Session $std -Url $hbUrl -Body @{ "__RequestVerificationToken" = $stdToken; lectureId = $LEC_REC; playedSeconds = 30; durationSeconds = 3600 }
$row = Get-Row $student $LEC_REC
$pass = ($r.Status -eq 200) -and $r.Json.success -and ($row.Watched -le 5) -and (-not $r.Json.status.isTimeEnd)
Record-Test -TestId "ATT-02" -Description "Heartbeat فوري بـ 30 ثانية: يُحتسب ≤ السماحية (5 ثوانٍ) لأن الزمن الفعلي ≈ 0" -Passed $pass -Details "resp=$($r.Raw) | watched=$($row.Watched)"

$r = Invoke-Post -Session $std -Url $hbUrl -Body @{ "__RequestVerificationToken" = $stdToken; lectureId = $LEC_REC; playedSeconds = 5000 }
$row = Get-Row $student $LEC_REC
$pass = ($r.Status -eq 200) -and $r.Json.success -and ($row.Watched -le 12) -and (-not $r.Json.status.isTimeEnd)
Record-Test -TestId "ATT-03" -Description "Heartbeat بقيمة مبالغ فيها (5000 ثانية): لا تُحتسب أكثر من الزمن الفعلي" -Passed $pass -Details "watched=$($row.Watched)"

$null = Sql "UPDATE std_lecture SET LastHeartbeatAt = DATEADD(minute, -2, SYSUTCDATETIME()) WHERE Std_ID='$student' AND Lecture_ID=$LEC_REC;"
$r = Invoke-Post -Session $std -Url $hbUrl -Body @{ "__RequestVerificationToken" = $stdToken; lectureId = $LEC_REC; playedSeconds = 60; durationSeconds = 3600 }
$row = Get-Row $student $LEC_REC
$pass = ($r.Status -eq 200) -and $r.Json.success -and ($row.Watched -ge 60) -and ($row.IsTimeEnd -eq 1) -and ($row.IsFinish -eq 1) -and ($row.HasCompleted -eq 1) -and $r.Json.status.isFinish -and (Count-Rows $student $LEC_REC) -eq 1
Record-Test -TestId "ATT-04" -Description "بعد مرور دقيقتين فعلياً: نبضة 60 ثانية تُحتسب وتكتمل المحاضرة (IsTimeEnd=1, IsFinish=1)" -Passed $pass -Details "resp=$($r.Raw) | row=$($row | ConvertTo-Json -Compress)"

$null = Sql "UPDATE std_lecture SET LastHeartbeatAt = DATEADD(minute, -2, SYSUTCDATETIME()) WHERE Std_ID='$student' AND Lecture_ID=$LEC_REC;"
$r = Invoke-Post -Session $std -Url $hbUrl -Body @{ "__RequestVerificationToken" = $stdToken; lectureId = $LEC_REC; playedSeconds = 30 }
$row = Get-Row $student $LEC_REC
$pass = ($r.Status -eq 200) -and $r.Json.success -and $r.Json.status.isFinish -and ($row.IsFinish -eq 1) -and ((Count-Rows $student $LEC_REC) -eq 1)
Record-Test -TestId "ATT-05" -Description "نبضات بعد الاكتمال لا تنشئ صفوفاً مكررة ولا تلغي الاكتمال" -Passed $pass -Details "rows=$(Count-Rows $student $LEC_REC) watched=$($row.Watched)"

Write-Host "`n[3] Authorization and validation" -ForegroundColor Yellow
$unregLecture = [int](Sql "SELECT TOP 1 ID FROM Lectures WHERE Category_ID = 3 AND LectureTypeOnOff = '0' ORDER BY ID;")
if ($unregLecture -eq 0) { $unregLecture = [int](Sql "SELECT TOP 1 ID FROM Lectures WHERE Category_ID = 3 ORDER BY ID;") }
$r = Invoke-Post -Session $std -Url $startUrl -Body @{ "__RequestVerificationToken" = $stdToken; lectureId = $unregLecture }
$pass = ($r.Status -eq 403) -and ((Count-Rows $student $unregLecture) -eq 0)
Record-Test -TestId "ATT-06" -Description "StartLecture لمحاضرة في دورة غير مسجل بها الطالب → 403 بلا صف" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw)"

$r = Invoke-Post -Session $std -Url $hbUrl -Body @{ lectureId = $LEC_REC; playedSeconds = 30 }
$pass = ($r.Status -ne 200)
Record-Test -TestId "ATT-07" -Description "Heartbeat بدون رمز الحماية من التزوير يُرفض" -Passed $pass -Details "status=$($r.Status)"

$r = Invoke-Post -Session $std -Url $startUrl -Body @{ "__RequestVerificationToken" = $stdToken; lectureId = $LEC_LIVE }
$pass = ($r.Status -eq 400) -and ((Count-Rows $student $LEC_LIVE) -eq 0)
Record-Test -TestId "ATT-08" -Description "StartLecture على محاضرة مباشرة → 400 (نوع غير مطابق)" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw)"

$r = Invoke-Post -Session $std -Url "$BaseUrl/Students/LecutreTimeEnd" -Body @{ Lect_ID = $LEC_REC2 }
$pass = ((Count-Rows $student $LEC_REC2) -eq 0)
Record-Test -TestId "ATT-09" -Description "النقطة القديمة LecutreTimeEnd لم تعد تُنشئ أو تعدّل أي صف" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw) rows=$(Count-Rows $student $LEC_REC2)"

Write-Host "`n[4] Live lectures" -ForegroundColor Yellow
$nowStr = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
$futureStr = (Get-Date).AddDays(2).ToString("yyyy-MM-dd HH:mm:ss")
$null = Sql "INSERT INTO Lectures (Category_ID, Lecture_Name, LectureTypeOnOff, DayandTime, LectureLink_Client, LectureLink_Host, LectureMinTime) VALUES (6, 'QA_ATT_LIVE_NOW', '1', '$nowStr', 'https://meet.google.com/qa-att-now', '', '0');"
$null = Sql "INSERT INTO Lectures (Category_ID, Lecture_Name, LectureTypeOnOff, DayandTime, LectureLink_Client, LectureLink_Host, LectureMinTime) VALUES (6, 'QA_ATT_LIVE_FUTURE', '1', '$futureStr', 'https://meet.google.com/qa-att-future', '', '0');"
$liveNow = [int](Sql "SELECT ID FROM Lectures WHERE Lecture_Name='QA_ATT_LIVE_NOW';")
$liveFuture = [int](Sql "SELECT ID FROM Lectures WHERE Lecture_Name='QA_ATT_LIVE_FUTURE';")

$r = Invoke-Post -Session $std -Url $joinUrl -Body @{ "__RequestVerificationToken" = $stdToken; lectureId = $liveNow }
$row = Get-Row $student $liveNow
$pass = ($r.Status -eq 200) -and $r.Json.success -and $row -and ($row.Source -eq "Live") -and ($row.IsTimeEnd -eq 1) -and ($row.IsFinish -eq 1)
Record-Test -TestId "ATT-10" -Description "JoinLiveLecture ضمن موعد المحاضرة يسجل الحضور (Source=Live)" -Passed $pass -Details "resp=$($r.Raw) | row=$($row | ConvertTo-Json -Compress)"

$r = Invoke-Post -Session $std -Url $joinUrl -Body @{ "__RequestVerificationToken" = $stdToken; lectureId = $liveFuture }
$pass = ($r.Status -eq 200) -and ($r.Json.success -eq $false) -and ((Count-Rows $student $liveFuture) -eq 0)
Record-Test -TestId "ATT-11" -Description "JoinLiveLecture خارج الموعد (بعد يومين) يُرفض بلا صف" -Passed $pass -Details "resp=$($r.Raw)"

Write-Host "`n[5] Assignment path (exam independent of attendance: passing alone must not complete the lecture)" -ForegroundColor Yellow
# الاختبار مستقل عن الحضور (2026-10-27): الأسئلة والتسليم متاحان دون حضور، لكن الاجتياز وحده لا يكمل المحاضرة (الاكتمال = حضور + اجتياز)
$r0 = Invoke-Post -Session $std -Url "$BaseUrl/Students/GetAssignmentQuestion" -Body @{ A_Q = 2; "__RequestVerificationToken" = $stdToken }
$r = Invoke-Post -Session $std -Url "$BaseUrl/Students/SubmetQuestion" -Body "L_Degry=10&L_Degry=10&ass_ID=2&__RequestVerificationToken=$([uri]::EscapeDataString($stdToken))"
$row = Get-Row $student $LEC_ASSIGN
$pass = ($r0.Status -eq 200) -and ($r.Status -eq 200) -and $r.Json.success -and $row -and ($row.IsAssign -eq 1) -and ($row.IsTimeEnd -eq 0) -and ($row.IsFinish -eq 0)
Record-Test -TestId "ATT-12" -Description "اجتياز الواجب دون حضور يضبط IsAssingmetFinish فقط ولا يكمل المحاضرة" -Passed $pass -Details "get=$($r0.Status) resp=$($r.Raw) | row=$($row | ConvertTo-Json -Compress)"

$r = Invoke-Post -Session $std -Url "$BaseUrl/Students/SubmetQuestion" -Body @{ ass_ID = 2; L_Degry = 10; "__RequestVerificationToken" = $stdToken }
$pass = ($r.Status -eq 403) -and ($r.Json.code -eq "AlreadyPassed")
Record-Test -TestId "ATT-12b" -Description "إعادة تسليم واجب مُجتاز تُرفض (403 AlreadyPassed)" -Passed $pass -Details "status=$($r.Status) resp=$($r.Raw)"

Write-Host "`n[6] Teacher: attendance list + manual marking" -ForegroundColor Yellow
$teacher = Get-AuthSession -Email "teacher.ibrahim@example.test" -Password "Teach@Tabseet2026!"
$teacherToken = Get-VerificationToken -Session $teacher -Url "$BaseUrl/Teacher/LecturesByCategoryStudend?Cat_ID=10&categoryName=QA"
$teacherId = Sql "SELECT Id FROM AspNetUsers WHERE Email='teacher.ibrahim@example.test';"

$r = Invoke-Post -Session $teacher -Url "$BaseUrl/Teacher/LectureAttendance" -Body @{ lectureId = $LEC_REC }
$me = $null; if ($r.Json -and $r.Json.rows) { $me = $r.Json.rows | Where-Object { $_.StudentId -eq $student } }
$pass = ($r.Status -eq 200) -and $r.Json.success -and $me -and ($me.IsFinish -eq $true) -and ($me.StatusKey -eq "Finished") -and ($me.Source -eq "Recorded") -and ($me.WatchedSeconds -ge 60)
Record-Test -TestId "ATT-13" -Description "قائمة حضور المحاضرة للمدرس تعرض الطالب مكتملاً بمصدر المشاهدة" -Passed $pass -Details "status=$($r.Status) me=$($me | ConvertTo-Json -Compress)"

$r = Invoke-Post -Session $teacher -Url "$BaseUrl/Teacher/SetLectureAttendance" -Body @{ "__RequestVerificationToken" = $teacherToken; lectureId = $LEC_REC2; studentId = $student; attended = "true" }
$row = Get-Row $student $LEC_REC2
$pass = ($r.Status -eq 200) -and $r.Json.success -and $row -and ($row.Source -eq "Manual") -and ($row.IsTimeEnd -eq 1) -and ($row.IsFinish -eq 1) -and ($row.MarkedBy -eq $teacherId)
Record-Test -TestId "ATT-14" -Description "تسجيل حضور يدوي من المدرس (Source=Manual, MarkedBy=المدرس)" -Passed $pass -Details "resp=$($r.Raw) | row=$($row | ConvertTo-Json -Compress)"

$r = Invoke-Post -Session $teacher -Url "$BaseUrl/Teacher/SetLectureAttendance" -Body @{ "__RequestVerificationToken" = $teacherToken; lectureId = $LEC_REC2; studentId = $student; attended = "false" }
$row = Get-Row $student $LEC_REC2
$pass = ($r.Status -eq 200) -and $r.Json.success -and $row -and ($row.IsTimeEnd -eq 0) -and ($row.IsFinish -eq 0)
Record-Test -TestId "ATT-15" -Description "إلغاء الحضور اليدوي يعيد الصف إلى غير مكتمل" -Passed $pass -Details "resp=$($r.Raw) | row=$($row | ConvertTo-Json -Compress)"

$r = Invoke-Post -Session $teacher -Url "$BaseUrl/Teacher/LectureAttendance" -Body @{ lectureId = 1 }
$pass = ($r.Status -eq 403)
Record-Test -TestId "ATT-16" -Description "المدرس لا يرى حضور محاضرة في دورة غير مسندة إليه → 403" -Passed $pass -Details "status=$($r.Status)"

$tp = Invoke-WebRequest -Uri "$BaseUrl/Teacher/TeacherCoursestd?ID=10" -WebSession $teacher -UseBasicParsing
$pass = ($tp.StatusCode -eq 200) -and ($tp.Content -match 'student01@example.test') -and ($tp.Content -match 'progress-bar')
Record-Test -TestId "ATT-17" -Description "صفحة تقدم الطلاب للمدرس تعمل وتعرض الطالب" -Passed $pass -Details "status=$($tp.StatusCode) hasStudent=$($tp.Content -match 'student01@example.test')"

Write-Host "`n[7] Admin report + student pages" -ForegroundColor Yellow
$admin = Get-AuthSession -Email "admin.test@example.test" -Password "Admin@Tabseet2026!"
$rep = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/AttendanceReport?categoryId=10" -WebSession $admin -UseBasicParsing
$pass = ($rep.StatusCode -eq 200) -and ($rep.Content -match 'id="attendanceTable"') -and ($rep.Content -match 'student01@example.test')
Record-Test -TestId "ATT-18" -Description "تقرير الإدارة لدورة يعرض جدول الحضور والطالب" -Passed $pass -Details "status=$($rep.StatusCode) table=$($rep.Content -match 'attendanceTable')"
$rep0 = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/AttendanceReport" -WebSession $admin -UseBasicParsing
$pass = ($rep0.StatusCode -eq 200) -and ($rep0.Content -match 'name="categoryId"')
Record-Test -TestId "ATT-19" -Description "تقرير الإدارة بدون دورة يعرض قائمة الاختيار" -Passed $pass -Details "status=$($rep0.StatusCode)"

$cats = Invoke-WebRequest -Uri "$BaseUrl/Students/CategoriesStd" -WebSession $std -UseBasicParsing
$c = $cats.Content
$pass = ($c -match 'Scripts/tabseet-attendance\.js') -and ($c -match '__RequestVerificationToken') -and ($c -match 'id="subModalAttendance"') -and ($c -match 'TabseetAttendance\.configure')
Record-Test -TestId "ATT-20" -Description "واجهة الطالب الجديدة تحمّل سكربت الحضور والرمز وشريط الحالة" -Passed $pass -Details "script=$($c -match 'tabseet-attendance') token=$($c -match '__RequestVerificationToken') box=$($c -match 'subModalAttendance')"
$lec = Invoke-WebRequest -Uri "$BaseUrl/Students/LecturesStd?cat_ID=10&lectureName=QA" -WebSession $std -UseBasicParsing
$l = $lec.Content
$pass = ($l -match 'id="attendanceProgress"') -and (-not ($l -match 'function btnStartLecture')) -and ($l -match 'TabseetAttendance\.track')
Record-Test -TestId "ATT-21" -Description "واجهة الطالب القديمة: أُزيل المؤقّت القديم وأُضيف تتبع التشغيل الفعلي" -Passed $pass -Details "progress=$($l -match 'attendanceProgress') oldTimer=$($l -match 'function btnStartLecture')"

Write-Host "`n[8] Cleanup / restore fixtures" -ForegroundColor Yellow
$null = Sql "DELETE FROM std_lecture WHERE Lecture_ID IN (SELECT ID FROM Lectures WHERE Lecture_Name LIKE 'QA_ATT_%');"
$null = Sql "DELETE FROM Lectures WHERE Lecture_Name LIKE 'QA_ATT_%';"
$null = Sql "DELETE FROM std_lecture WHERE Std_ID='$student' AND Lecture_ID = $LEC_REC2;"
$null = Sql "UPDATE std_lecture SET IsFinish=1, IsTimeEnd=1, IsAssingmetFinish=NULL, StartedAt=NULL, LastHeartbeatAt=NULL, WatchedSeconds=0, RequiredSeconds=NULL, CompletedAt=NULL, Source='Legacy', MarkedBy=NULL, MarkedAt=NULL WHERE Std_ID='$student' AND Lecture_ID = $LEC_REC;"
$null = Sql "UPDATE std_lecture SET IsFinish=1, IsTimeEnd=1, IsAssingmetFinish=1, StartedAt=NULL, LastHeartbeatAt=NULL, WatchedSeconds=0, RequiredSeconds=NULL, CompletedAt=NULL, Source='Legacy', MarkedBy=NULL, MarkedAt=NULL WHERE Std_ID='$student' AND Lecture_ID = $LEC_ASSIGN;"
Write-Host "Fixtures restored."

Write-Host "`n==========================================================" -ForegroundColor Cyan
$passed = ($results | Where-Object { $_.Result -eq "PASS" }).Count
$failed = ($results | Where-Object { $_.Result -eq "FAIL" }).Count
Write-Host "TOTAL: $($results.Count)   PASS: $passed   FAIL: $failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
$results | Format-Table -AutoSize -Wrap | Out-String -Width 220 | Write-Host
