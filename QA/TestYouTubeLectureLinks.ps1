param(
    [string]$BaseUrl = "http://localhost:50895",
    [int]$CategoryId = 6
)

# اختبارات آلية روابط YouTube للمحاضرات المسجلة (إضافة/تعديل من حساب المدرس والإدارة + عرض الطالب)
# تُنشئ محاضرات باسم QA_YT_% وتحذفها في النهاية.

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

function Count-QaLectures { [int](Sql "SELECT COUNT(*) FROM Lectures WHERE Lecture_Name LIKE 'QA_YT_%';") }

function Post-Lecture {
    param($Session, [string]$Token, [string]$Action, [hashtable]$Fields)
    $body = @{ "__RequestVerificationToken" = $Token }
    foreach ($k in $Fields.Keys) { $body[$k] = $Fields[$k] }
    $before = Count-QaLectures
    try {
        $res = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/$Action" -Method POST -Body $body -WebSession $Session -UseBasicParsing
        $content = $res.Content
    } catch {
        $content = $_.ErrorDetails.Message
        if (-not $content) { $content = $_.Exception.Message }
    }
    $after = Count-QaLectures
    $json = $null
    try { $json = $content | ConvertFrom-Json } catch { }
    return [PSCustomObject]@{ Json = $json; Raw = $content; Delta = ($after - $before) }
}

function Get-StoredLink { param([string]$Name) Sql "SELECT TOP 1 ISNULL(LectureLink_Client,'') FROM Lectures WHERE Lecture_Name = '$Name' ORDER BY ID DESC;" }
function Get-StoredId   { param([string]$Name) [int](Sql "SELECT TOP 1 ID FROM Lectures WHERE Lecture_Name = '$Name' ORDER BY ID DESC;") }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   YouTube lecture links - test suite (category $CategoryId)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# تنظيف بقايا سابقة
$null = Sql "DELETE FROM TeacherLectureEvents WHERE LectureId IN (SELECT ID FROM Lectures WHERE Lecture_Name LIKE 'QA_YT_%');"
$null = Sql "DELETE FROM TeacherLectureStatus WHERE LectureId IN (SELECT ID FROM Lectures WHERE Lecture_Name LIKE 'QA_YT_%');"
$null = Sql "DELETE FROM Lectures WHERE Lecture_Name LIKE 'QA_YT_%';"

Write-Host "`n[1] Authenticating..." -ForegroundColor Yellow
$teacher = Get-AuthSession -Email "teacher.ibrahim@example.test" -Password "Teach@Tabseet2026!"
$teacherToken = Get-VerificationToken -Session $teacher -Url "$BaseUrl/Teacher/LecturesByCategoryStudend?Cat_ID=$CategoryId&categoryName=QA"
$admin = Get-AuthSession -Email "admin.test@example.test" -Password "Admin@Tabseet2026!"
$adminToken = Get-VerificationToken -Session $admin -Url "$BaseUrl/Controlpanel/LecturesByCategory?Category_ID=$CategoryId&categoryName=QA"
if (-not $teacherToken) { throw "Teacher token not found (is teacher.ibrahim assigned to category ${CategoryId}? is the site running?)" }
if (-not $adminToken) { throw "Admin token not found" }
Write-Host "Teacher token: $($teacherToken.Substring(0,10))...  Admin token: $($adminToken.Substring(0,10))..."

$CANON = "https://www.youtube.com/watch?v=KOyMpdRfSHU"

$createCases = @(
    @{ Id = "YT-01"; Name = "QA_YT_01_Watch";    Link = "https://www.youtube.com/watch?v=KOyMpdRfSHU&list=PL123&index=2&feature=share"; Expect = $CANON;            Desc = "رابط YouTube عادي مع معاملات إضافية" },
    @{ Id = "YT-02"; Name = "QA_YT_02_Short";    Link = "https://youtu.be/KOyMpdRfSHU?si=AbCdEf123";                                       Expect = $CANON;            Desc = "رابط YouTube مختصر youtu.be" },
    @{ Id = "YT-03"; Name = "QA_YT_03_Shorts";   Link = "https://www.youtube.com/shorts/KOyMpdRfSHU";                                      Expect = $CANON;            Desc = "رابط Shorts" },
    @{ Id = "YT-04"; Name = "QA_YT_04_Live";     Link = "https://www.youtube.com/live/KOyMpdRfSHU?feature=share";                          Expect = $CANON;            Desc = "رابط Live صالح (محاضرة مسجلة)" },
    @{ Id = "YT-05"; Name = "QA_YT_05_Embed";    Link = "https://www.youtube.com/embed/KOyMpdRfSHU?rel=0";                                 Expect = $CANON;            Desc = "رابط Embed موجود مسبقاً" },
    @{ Id = "YT-06"; Name = "QA_YT_06_Start";    Link = "https://m.youtube.com/watch?v=KOyMpdRfSHU&t=1m30s&si=xyz";                        Expect = "$CANON&t=90s";    Desc = "رابط جوال مع وقت بدء" },
    @{ Id = "YT-07"; Name = "QA_YT_07_Iframe";   Link = '<iframe width="560" height="315" src="https://www.youtube.com/embed/KOyMpdRfSHU?si=q" title="x" frameborder="0" allowfullscreen></iframe>'; Expect = $CANON; Desc = "لصق كود iframe كامل" },
    @{ Id = "YT-08"; Name = "QA_YT_08_NoScheme"; Link = "youtu.be/KOyMpdRfSHU";                                                             Expect = $CANON;            Desc = "رابط بدون https://" },
    @{ Id = "YT-09"; Name = "QA_YT_09_Http";     Link = "http://www.youtube.com/watch?v=KOyMpdRfSHU";                                      Expect = $CANON;            Desc = "رابط http يُرفع إلى https" },
    @{ Id = "YT-10"; Name = "QA_YT_10_Drive";    Link = "https://drive.google.com/file/d/1AbCdEf/view";                                     Expect = "https://drive.google.com/file/d/1AbCdEf/view"; Desc = "مصدر غير YouTube (Google Drive) يبقى كما هو" }
)

Write-Host "`n[2] Teacher: create recorded lectures" -ForegroundColor Yellow
foreach ($c in $createCases) {
    $r = Post-Lecture -Session $teacher -Token $teacherToken -Action "CreateLectuer" -Fields @{ "Category_ID" = $CategoryId; "Lecture_Name" = $c.Name; "LectureTypeOnOff" = "0"; "LectureLink_Client" = $c.Link; "LectureMinTime" = 1; "MediaLink" = ""; "Others" = "QA" }
    $stored = Get-StoredLink -Name $c.Name
    $pass = ($r.Json -and $r.Json.success -eq $true) -and ($r.Delta -eq 1) -and ($stored -eq $c.Expect)
    Record-Test -TestId $c.Id -Description $c.Desc -Passed $pass -Details "resp=$($r.Raw) | delta=$($r.Delta) | stored=$stored | expected=$($c.Expect)"
}

Write-Host "`n[3] Teacher: rejected links" -ForegroundColor Yellow
$rejectCases = @(
    @{ Id = "YT-11"; Name = "QA_YT_11_Channel";  Link = "https://www.youtube.com/@TVyemenshabab";               Desc = "رابط قناة YouTube (بدون معرّف فيديو) يُرفض" },
    @{ Id = "YT-12"; Name = "QA_YT_12_Fake";     Link = "https://www.youtube.com.evil.io/watch?v=KOyMpdRfSHU"; Desc = "نطاق مزيف يشبه YouTube يُرفض" },
    @{ Id = "YT-13"; Name = "QA_YT_13_Playlist"; Link = "https://www.youtube.com/playlist?list=PL123";          Desc = "رابط قائمة تشغيل يُرفض" },
    @{ Id = "YT-14"; Name = "QA_YT_14_BadId";    Link = "https://www.youtube.com/watch?v=short";                Desc = "معرّف فيديو غير صالح يُرفض" },
    @{ Id = "YT-15"; Name = "QA_YT_15_Garbage";  Link = "not a url at all";                                     Desc = "رابط غير صالح يُرفض" }
)
foreach ($c in $rejectCases) {
    $r = Post-Lecture -Session $teacher -Token $teacherToken -Action "CreateLectuer" -Fields @{ "Category_ID" = $CategoryId; "Lecture_Name" = $c.Name; "LectureTypeOnOff" = "0"; "LectureLink_Client" = $c.Link; "LectureMinTime" = 1 }
    $pass = ($r.Json -and $r.Json.success -eq $false) -and ($r.Delta -eq 0)
    Record-Test -TestId $c.Id -Description $c.Desc -Passed $pass -Details "resp=$($r.Raw) | delta=$($r.Delta)"
}

Write-Host "`n[4] Teacher: live lectures unchanged (Zoom / Meet / YouTube Live as live)" -ForegroundColor Yellow
$liveCases = @(
    @{ Id = "YT-16"; Name = "QA_YT_16_Zoom";    Link = "https://us02web.zoom.us/j/1234567890";          Desc = "محاضرة مباشرة Zoom تُحفظ كما هي" },
    @{ Id = "YT-17"; Name = "QA_YT_17_Meet";    Link = "https://meet.google.com/abc-defg-hij";          Desc = "محاضرة مباشرة Google Meet تُحفظ كما هي" },
    @{ Id = "YT-18"; Name = "QA_YT_18_YTLive";  Link = "https://www.youtube.com/live/KOyMpdRfSHU";      Desc = "رابط YouTube Live لمحاضرة مباشرة يُحفظ كما هو (الفرع المباشر لم يتغير)" }
)
foreach ($c in $liveCases) {
    $r = Post-Lecture -Session $teacher -Token $teacherToken -Action "CreateLectuer" -Fields @{ "Category_ID" = $CategoryId; "Lecture_Name" = $c.Name; "LectureTypeOnOff" = "1"; "DayandTime" = "2026-11-10 10:00:00"; "LectureLink_Client" = $c.Link; "LectureMinTime" = 0 }
    $stored = Get-StoredLink -Name $c.Name
    $type = Sql "SELECT TOP 1 LectureTypeOnOff FROM Lectures WHERE Lecture_Name = '$($c.Name)' ORDER BY ID DESC;"
    $pass = ($r.Json -and $r.Json.success -eq $true) -and ($r.Delta -eq 1) -and ($stored -eq $c.Link) -and ($type -eq "1")
    Record-Test -TestId $c.Id -Description $c.Desc -Passed $pass -Details "resp=$($r.Raw) | delta=$($r.Delta) | stored=$stored | type=$type"
}

Write-Host "`n[5] Teacher: edit existing lectures" -ForegroundColor Yellow
# 5a: تعديل محاضرة أُنشئت للتو (تغيير الرابط إلى صيغة مختصرة مع وقت بدء) دون تكرار
$id01 = Get-StoredId -Name "QA_YT_01_Watch"
$r = Post-Lecture -Session $teacher -Token $teacherToken -Action "EditLectuer" -Fields @{ "ID" = $id01; "Category_ID" = $CategoryId; "Lecture_Name" = "QA_YT_01_Watch"; "LectureTypeOnOff" = "0"; "LectureLink_Client" = "https://youtu.be/cK8p_qxLpmA?t=45"; "LectureMinTime" = 2 }
$stored = Get-StoredLink -Name "QA_YT_01_Watch"
$idAfter = Get-StoredId -Name "QA_YT_01_Watch"
$pass = ($r.Json -and $r.Json.success -eq $true) -and ($r.Delta -eq 0) -and ($stored -eq "https://www.youtube.com/watch?v=cK8p_qxLpmA&t=45s") -and ($idAfter -eq $id01)
Record-Test -TestId "YT-19" -Description "تعديل محاضرة: رابط مختصر مع وقت بدء يُحفظ موحداً دون إنشاء سجل جديد" -Passed $pass -Details "resp=$($r.Raw) | delta=$($r.Delta) | stored=$stored | id=$id01/$idAfter"

# 5b: محاضرة قديمة (مدخلة مباشرة في قاعدة البيانات بصيغة قديمة) ثم تعديل اسمها فقط من واجهة المدرس
$null = Sql "INSERT INTO Lectures (Category_ID, Lecture_Name, LectureTypeOnOff, LectureLink_Client, LectureMinTime, DayandTime, LectureLink_Host) VALUES ($CategoryId, 'QA_YT_OLD_Legacy', '0', 'https://www.youtube.com/watch?v=Lr8L1bM0o0M&feature=youtu.be', '3', '', '');"
$idOld = Get-StoredId -Name "QA_YT_OLD_Legacy"
$editForm = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/EditLecture" -Method POST -Body @{ id = $idOld } -WebSession $teacher -UseBasicParsing
$editJson = $editForm.Content | ConvertFrom-Json
$pass = ($editJson.LectureLink_Client -eq "https://www.youtube.com/watch?v=Lr8L1bM0o0M&feature=youtu.be") -and ($editJson.ID -eq $idOld)
Record-Test -TestId "YT-20" -Description "نموذج التعديل يعرض الرابط القديم كما هو مخزن" -Passed $pass -Details "resp=$($editForm.Content)"

$r = Post-Lecture -Session $teacher -Token $teacherToken -Action "EditLectuer" -Fields @{ "ID" = $idOld; "Category_ID" = $CategoryId; "Lecture_Name" = "QA_YT_OLD_Legacy_Renamed"; "LectureTypeOnOff" = "0"; "LectureLink_Client" = $editJson.LectureLink_Client; "LectureMinTime" = 3 }
$stored = Get-StoredLink -Name "QA_YT_OLD_Legacy_Renamed"
$cnt = [int](Sql "SELECT COUNT(*) FROM Lectures WHERE Lecture_Name LIKE 'QA_YT_OLD_Legacy%';")
$pass = ($r.Json -and $r.Json.success -eq $true) -and ($stored -eq "https://www.youtube.com/watch?v=Lr8L1bM0o0M") -and ($cnt -eq 1)
Record-Test -TestId "YT-21" -Description "تعديل محاضرة قديمة: الرابط القديم يُوحَّد ولا يُنشأ سجل مكرر" -Passed $pass -Details "resp=$($r.Raw) | stored=$stored | count=$cnt"

Write-Host "`n[6] Admin: create + edit" -ForegroundColor Yellow
$r = Post-Lecture -Session $admin -Token $adminToken -Action "CreateLectuer" -Fields @{ "Category_ID" = $CategoryId; "Lecture_Name" = "QA_YT_ADM_01"; "LectureTypeOnOff" = "0"; "LectureLink_Client" = "https://www.youtube.com/shorts/KOyMpdRfSHU?feature=share"; "LectureMinTime" = 1 }
$stored = Get-StoredLink -Name "QA_YT_ADM_01"
$pass = ($r.Json -and $r.Json.success -eq $true) -and ($r.Delta -eq 1) -and ($stored -eq $CANON)
Record-Test -TestId "YT-22" -Description "الإدارة: إضافة محاضرة مسجلة برابط Shorts تُحفظ موحدة" -Passed $pass -Details "resp=$($r.Raw) | delta=$($r.Delta) | stored=$stored"

$idAdm = Get-StoredId -Name "QA_YT_ADM_01"
$r = Post-Lecture -Session $admin -Token $adminToken -Action "EditLectuer" -Fields @{ "ID" = $idAdm; "Category_ID" = $CategoryId; "Lecture_Name" = "QA_YT_ADM_01"; "LectureTypeOnOff" = "0"; "LectureLink_Client" = "https://www.youtube.com/@SomeChannel"; "LectureMinTime" = 1 }
$stored = Get-StoredLink -Name "QA_YT_ADM_01"
$pass = ($r.Json -and $r.Json.success -eq $false) -and ($r.Delta -eq 0) -and ($stored -eq $CANON)
Record-Test -TestId "YT-23" -Description "الإدارة: تعديل برابط قناة يُرفض ويبقى الرابط السابق" -Passed $pass -Details "resp=$($r.Raw) | stored=$stored"

Write-Host "`n[7] Student pages" -ForegroundColor Yellow
$student = Get-AuthSession -Email "student01@example.test" -Password "Std@Tabseet2026!"
$cats = Invoke-WebRequest -Uri "$BaseUrl/Students/CategoriesStd" -WebSession $student -UseBasicParsing
$c = $cats.Content
$pass = ($c -match 'Scripts/tabseet-youtube\.js') -and ($c -match 'id="subModalPlayer"') -and ($c -match 'sub-btn-play') -and ($c -match ('data-client-url="' + [regex]::Escape($CANON) + '"')) -and ($c -match 'data-type="0"')
Record-Test -TestId "YT-24" -Description "واجهة الطالب الجديدة (CategoriesStd) تحمّل المشغّل وتعرض رابط YouTube الموحد للمحاضرة المسجلة" -Passed $pass -Details "len=$($c.Length) script=$($c -match 'tabseet-youtube') player=$($c -match 'subModalPlayer') url=$($c -match [regex]::Escape($CANON))"

$lec = Invoke-WebRequest -Uri "$BaseUrl/Students/LecturesStd?cat_ID=10&lectureName=QA" -WebSession $student -UseBasicParsing
$l = $lec.Content
$pass = ($l -match 'Scripts/tabseet-youtube\.js') -and ($l -match 'id="lecturePlayer"') -and ($l -match 'js-open-lecture') -and ($l -match 'data-link="https://www\.youtube\.com/watch\?v=Lr8L1bM0o0M"') -and (-not ($l -match 'id="Video_ID"'))
Record-Test -TestId "YT-25" -Description "واجهة الطالب القديمة (LecturesStd) تعرض محاضرة قديمة بصيغة watch داخل المشغّل الجديد بدل iframe الخام" -Passed $pass -Details "len=$($l.Length) script=$($l -match 'tabseet-youtube') host=$($l -match 'lecturePlayer') btn=$($l -match 'js-open-lecture') oldIframe=$($l -match 'id=\"Video_ID\"')"

$js = Invoke-WebRequest -Uri "$BaseUrl/Scripts/tabseet-youtube.js" -UseBasicParsing
$pass = ($js.StatusCode -eq 200) -and ($js.Content -match 'TabseetYouTube')
Record-Test -TestId "YT-26" -Description "ملف Scripts/tabseet-youtube.js يُقدَّم من الخادم" -Passed $pass -Details "status=$($js.StatusCode) len=$($js.Content.Length)"

Write-Host "`n[8] No duplicates" -ForegroundColor Yellow
$dups = Sql "SELECT COUNT(*) FROM (SELECT Lecture_Name FROM Lectures WHERE Lecture_Name LIKE 'QA_YT_%' GROUP BY Lecture_Name HAVING COUNT(*) > 1) d;"
$total = Count-QaLectures
$pass = ([int]$dups -eq 0) -and ($total -eq 15)
Record-Test -TestId "YT-27" -Description "لا توجد أي محاضرة مكررة بعد كل عمليات الحفظ والتعديل" -Passed $pass -Details "duplicates=$dups total=$total (expected 15)"

Write-Host "`n[9] Cleanup" -ForegroundColor Yellow
$null = Sql "DELETE FROM TeacherLectureEvents WHERE LectureId IN (SELECT ID FROM Lectures WHERE Lecture_Name LIKE 'QA_YT_%');"
$null = Sql "DELETE FROM TeacherLectureStatus WHERE LectureId IN (SELECT ID FROM Lectures WHERE Lecture_Name LIKE 'QA_YT_%');"
$null = Sql "DELETE FROM Lectures WHERE Lecture_Name LIKE 'QA_YT_%';"
Write-Host "Remaining QA_YT_ lectures: $(Count-QaLectures)"

Write-Host "`n==========================================================" -ForegroundColor Cyan
$passed = ($results | Where-Object { $_.Result -eq "PASS" }).Count
$failed = ($results | Where-Object { $_.Result -eq "FAIL" }).Count
Write-Host "TOTAL: $($results.Count)   PASS: $passed   FAIL: $failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
$results | Format-Table -AutoSize -Wrap | Out-String -Width 220 | Write-Host
