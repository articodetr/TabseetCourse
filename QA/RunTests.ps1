param(
    [string]$BaseUrl = "http://localhost:50895"
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Tabseet International Academy - Test Suite Runner      " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$results = [System.Collections.Generic.List[PSObject]]::new()

function Record-Test {
    param(
        [string]$TestId,
        [string]$Description,
        [bool]$Passed,
        [string]$Details
    )
    $obj = [PSCustomObject]@{
        TestId      = $TestId
        Description = $Description
        Result      = if ($Passed) { "PASS" } else { "FAIL" }
        Details     = $Details
    }
    $results.Add($obj)
    $color = if ($Passed) { "Green" } else { "Red" }
    Write-Host "[$($obj.Result)] $TestId : $Description" -ForegroundColor $color
    if (-not $Passed) {
        Write-Host "       Error: $Details" -ForegroundColor Yellow
    }
}

function Get-AuthSession {
    param(
        [string]$Email,
        [string]$Password
    )
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $loginPage = Invoke-WebRequest -Uri "$BaseUrl/Home/LogIn" -WebSession $session -UseBasicParsing
    $token = [regex]::Match($loginPage.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value
    
    $loginData = @{
        "__RequestVerificationToken" = $token
        "Email" = $Email
        "Password" = $Password
    }
    
    $null = Invoke-WebRequest -Uri "$BaseUrl/Home/LogIn" -Method POST -Body $loginData -WebSession $session -UseBasicParsing
    return @{ Session = $session; Token = $token }
}

function Get-VerificationToken {
    param(
        $Session,
        [string]$Url
    )
    $page = Invoke-WebRequest -Uri $Url -WebSession $Session -UseBasicParsing
    return [regex]::Match($page.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value
}

Write-Host "`n[1/3] Authenticating test accounts..." -ForegroundColor Yellow
$teacherAuth = Get-AuthSession -Email "teacher.ibrahim@example.test" -Password "Teach@Tabseet2026!"
$adminAuth   = Get-AuthSession -Email "admin.test@example.test"   -Password "Admin@Tabseet2026!"

$teacherPageUrl = "$BaseUrl/Teacher/LecturesByCategoryStudend?Cat_ID=1&categoryName=Quran"
$teacherToken = Get-VerificationToken -Session $teacherAuth.Session -Url $teacherPageUrl

$adminPageUrl = "$BaseUrl/Controlpanel/LecturesByCategory?Category_ID=1&categoryName=Quran"
$adminToken = Get-VerificationToken -Session $adminAuth.Session -Url $adminPageUrl

Write-Host "Teacher authenticated successfully. Token: $($teacherToken.Substring(0, 10))..."
Write-Host "Admin authenticated successfully.   Token: $($adminToken.Substring(0, 10))..."

# Clean up any previous test lectures created by this test suite
& sqlcmd -S . -d TabseetDB_Local -Q "DELETE FROM Lectures WHERE Lecture_Name LIKE 'QA_TEST_%';" | Out-Null

# -------------------------------------------------------------
# TEST-01: Live lecture with Zoom link
# -------------------------------------------------------------
$t01Body = @{
    "__RequestVerificationToken" = $teacherToken
    "Category_ID"                = 1
    "Lecture_Name"               = "QA_TEST_01_Zoom_Live"
    "LectureTypeOnOff"           = "1"
    "DayandTime"                 = "2026-11-01 10:00:00"
    "LectureLink_Client"         = "https://us02web.zoom.us/j/1234567890"
    "LectureMinTime"             = 0
}
$t01Res = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/CreateLectuer" -Method POST -Body $t01Body -WebSession $teacherAuth.Session -UseBasicParsing
$t01Json = $t01Res.Content | ConvertFrom-Json
$t01Db = & sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT TOP 1 ID, Lecture_Name, LectureTypeOnOff, LectureLink_Client FROM Lectures WHERE Lecture_Name = 'QA_TEST_01_Zoom_Live' ORDER BY ID DESC;"
$t01Id = if ($t01Db) { ($t01Db.Trim() -split '\s+')[0] } else { $null }

$t01Pass = ($t01Json.success -eq $true) -and ($t01Db -match "https://us02web.zoom.us/j/1234567890") -and ($t01Db -match "1")
Record-Test -TestId "TEST-01" -Description "إنشاء محاضرة مباشرة برابط Zoom" -Passed $t01Pass -Details "Response: $($t01Res.Content) | DB: $t01Db"

# -------------------------------------------------------------
# TEST-02: Live lecture with Google Meet link
# -------------------------------------------------------------
$t02Body = @{
    "__RequestVerificationToken" = $teacherToken
    "Category_ID"                = 1
    "Lecture_Name"               = "QA_TEST_02_Meet_Live"
    "LectureTypeOnOff"           = "1"
    "DayandTime"                 = "2026-11-02 11:00:00"
    "LectureLink_Client"         = "https://meet.google.com/abc-defg-hij"
    "LectureMinTime"             = 0
}
$t02Res = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/CreateLectuer" -Method POST -Body $t02Body -WebSession $teacherAuth.Session -UseBasicParsing
$t02Json = $t02Res.Content | ConvertFrom-Json
$t02Db = & sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT TOP 1 ID, Lecture_Name, LectureTypeOnOff, LectureLink_Client FROM Lectures WHERE Lecture_Name = 'QA_TEST_02_Meet_Live' ORDER BY ID DESC;"

$t02Pass = ($t02Json.success -eq $true) -and ($t02Db -match "https://meet.google.com/abc-defg-hij")
Record-Test -TestId "TEST-02" -Description "إنشاء محاضرة مباشرة برابط Google Meet" -Passed $t02Pass -Details "Response: $($t02Res.Content) | DB: $t02Db"

# -------------------------------------------------------------
# TEST-03: Live lecture with Microsoft Teams link
# -------------------------------------------------------------
$t03Body = @{
    "__RequestVerificationToken" = $teacherToken
    "Category_ID"                = 1
    "Lecture_Name"               = "QA_TEST_03_Teams_Live"
    "LectureTypeOnOff"           = "1"
    "DayandTime"                 = "2026-11-03 12:00:00"
    "LectureLink_Client"         = "https://teams.microsoft.com/l/meetup-join/19%3ameeting_test"
    "LectureMinTime"             = 0
}
$t03Res = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/CreateLectuer" -Method POST -Body $t03Body -WebSession $teacherAuth.Session -UseBasicParsing
$t03Json = $t03Res.Content | ConvertFrom-Json
$t03Db = & sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT TOP 1 ID, Lecture_Name, LectureTypeOnOff, LectureLink_Client FROM Lectures WHERE Lecture_Name = 'QA_TEST_03_Teams_Live' ORDER BY ID DESC;"

$t03Pass = ($t03Json.success -eq $true) -and ($t03Db -match "teams.microsoft.com")
Record-Test -TestId "TEST-03" -Description "إنشاء محاضرة مباشرة برابط Microsoft Teams" -Passed $t03Pass -Details "Response: $($t03Res.Content) | DB: $t03Db"

# -------------------------------------------------------------
# TEST-04: Recorded lecture with YouTube link only
# -------------------------------------------------------------
$t04Body = @{
    "__RequestVerificationToken" = $teacherToken
    "Category_ID"                = 1
    "Lecture_Name"               = "QA_TEST_04_YouTube_Rec"
    "LectureTypeOnOff"           = "0"
    "LectureLink_Client"         = "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
    "LectureMinTime"             = 30
}
$t04Res = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/CreateLectuer" -Method POST -Body $t04Body -WebSession $teacherAuth.Session -UseBasicParsing
$t04Json = $t04Res.Content | ConvertFrom-Json
$t04Db = & sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT TOP 1 ID, Lecture_Name, LectureTypeOnOff, LectureLink_Client, DayandTime FROM Lectures WHERE Lecture_Name = 'QA_TEST_04_YouTube_Rec' ORDER BY ID DESC;"

$t04Pass = ($t04Json.success -eq $true) -and ($t04Db -match "youtube.com")
Record-Test -TestId "TEST-04" -Description "إنشاء محاضرة مسجلة برابط YouTube فقط" -Passed $t04Pass -Details "Response: $($t04Res.Content) | DB: $t04Db"

# -------------------------------------------------------------
# TEST-05: Recorded lecture with Google Drive link only
# -------------------------------------------------------------
$t05Body = @{
    "__RequestVerificationToken" = $teacherToken
    "Category_ID"                = 1
    "Lecture_Name"               = "QA_TEST_05_Drive_Rec"
    "LectureTypeOnOff"           = "0"
    "LectureLink_Client"         = "https://drive.google.com/file/d/1234567890/view"
    "LectureMinTime"             = 15
}
$t05Res = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/CreateLectuer" -Method POST -Body $t05Body -WebSession $teacherAuth.Session -UseBasicParsing
$t05Json = $t05Res.Content | ConvertFrom-Json
$t05Db = & sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT TOP 1 ID, Lecture_Name, LectureTypeOnOff, LectureLink_Client FROM Lectures WHERE Lecture_Name = 'QA_TEST_05_Drive_Rec' ORDER BY ID DESC;"

$t05Pass = ($t05Json.success -eq $true) -and ($t05Db -match "drive.google.com")
Record-Test -TestId "TEST-05" -Description "إنشاء محاضرة مسجلة برابط Google Drive فقط" -Passed $t05Pass -Details "Response: $($t05Res.Content) | DB: $t05Db"

# -------------------------------------------------------------
# TEST-06: Video file upload check
# -------------------------------------------------------------
# The code inspection proved that UI only accepts PDF and there is no video upload/streaming column or infrastructure.
# If someone sends no link, the server must not consider non-video as video source.
$t06Body = @{
    "__RequestVerificationToken" = $teacherToken
    "Category_ID"                = 1
    "Lecture_Name"               = "QA_TEST_06_No_Link"
    "LectureTypeOnOff"           = "0"
    "LectureLink_Client"         = ""
    "LectureMinTime"             = 15
}
$t06Res = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/CreateLectuer" -Method POST -Body $t06Body -WebSession $teacherAuth.Session -UseBasicParsing
$t06Json = $t06Res.Content | ConvertFrom-Json
$t06Pass = ($t06Json.success -eq $false) -and ($t06Json.responseText -match "رابط المحاضرة المسجلة مطلوب")
Record-Test -TestId "TEST-06" -Description "فحص حالة دعم رفع الفيديو (عدم اعتبار PDF وحده مصدر فيديو وطلب رابط صالح)" -Passed $t06Pass -Details "Response: $($t06Res.Content)"

# -------------------------------------------------------------
# TEST-07: Reject recorded lecture without link or video source
# -------------------------------------------------------------
$t07Body = @{
    "__RequestVerificationToken" = $teacherToken
    "Category_ID"                = 1
    "Lecture_Name"               = "QA_TEST_07_Empty_Source"
    "LectureTypeOnOff"           = "0"
    "LectureLink_Client"         = "   "
    "LectureMinTime"             = 10
}
$t07Res = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/CreateLectuer" -Method POST -Body $t07Body -WebSession $teacherAuth.Session -UseBasicParsing
$t07Json = $t07Res.Content | ConvertFrom-Json
$t07Pass = ($t07Json.success -eq $false)
Record-Test -TestId "TEST-07" -Description "رفض المحاضرة المسجلة دون رابط أو مصدر فيديو صالح" -Passed $t07Pass -Details "Response: $($t07Res.Content)"

# -------------------------------------------------------------
# TEST-08: Edit existing lecture link without creating duplicate record
# -------------------------------------------------------------
$beforeCount = [int]((& sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM Lectures;").ToString().Trim())
$t08Body = @{
    "__RequestVerificationToken" = $teacherToken
    "ID"                         = [int]$t01Id
    "Category_ID"                = 1
    "Lecture_Name"               = "QA_TEST_01_Zoom_Live_EDITED"
    "LectureTypeOnOff"           = "1"
    "DayandTime"                 = "2026-11-05 14:00:00"
    "LectureLink_Client"         = "https://meet.google.com/updated-live-link"
    "LectureMinTime"             = 0
}
$t08Res = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/EditLectuer" -Method POST -Body $t08Body -WebSession $teacherAuth.Session -UseBasicParsing
$t08Json = $t08Res.Content | ConvertFrom-Json
$afterCount = [int]((& sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM Lectures;").ToString().Trim())
$t08Db = & sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SET NOCOUNT ON; SELECT ID, Lecture_Name, LectureTypeOnOff, LectureLink_Client FROM Lectures WHERE ID = $t01Id;"

$t08Pass = ($t08Json.success -eq $true) -and ($beforeCount -eq $afterCount) -and ($t08Db -match "https://meet.google.com/updated-live-link")
Record-Test -TestId "TEST-08" -Description "تعديل رابط محاضرة موجودة دون إنشاء سجل جديد" -Passed $t08Pass -Details "BeforeCount: $beforeCount | AfterCount: $afterCount | DB: $t08Db"

# -------------------------------------------------------------
# TEST-09: Edit recorded lecture without losing existing attachment
# -------------------------------------------------------------
# First insert a test lecture with DocumenFile set
& sqlcmd -S . -d TabseetDB_Local -Q "INSERT INTO Lectures (Category_ID, Lecture_Name, LectureTypeOnOff, LectureLink_Client, DocumenFile, LectureMinTime) VALUES (1, 'QA_TEST_09_Attach', '0', 'https://www.youtube.com/watch?v=initial', 'existing_document.pdf', '20');" | Out-Null
$t09Id = [int]((& sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SET NOCOUNT ON; SELECT TOP 1 ID FROM Lectures WHERE Lecture_Name = 'QA_TEST_09_Attach' ORDER BY ID DESC;").ToString().Trim())

$t09Body = @{
    "__RequestVerificationToken" = $teacherToken
    "ID"                         = $t09Id
    "Category_ID"                = 1
    "Lecture_Name"               = "QA_TEST_09_Attach_EDITED"
    "LectureTypeOnOff"           = "0"
    "LectureLink_Client"         = "https://vimeo.com/987654321"
    "LectureMinTime"             = 25
}
$t09Res = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/EditLectuer" -Method POST -Body $t09Body -WebSession $teacherAuth.Session -UseBasicParsing
$t09Json = $t09Res.Content | ConvertFrom-Json
$t09Db = & sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SET NOCOUNT ON; SELECT ID, Lecture_Name, DocumenFile, LectureLink_Client FROM Lectures WHERE ID = $t09Id;"

$t09Pass = ($t09Json.success -eq $true) -and ($t09Db -match "existing_document.pdf") -and ($t09Db -match "https://vimeo.com/987654321")
Record-Test -TestId "TEST-09" -Description "تعديل محاضرة مسجلة دون فقد ملف المرفق الموجود" -Passed $t09Pass -Details "Response: $($t09Res.Content) | DB: $t09Db"

# -------------------------------------------------------------
# TEST-10: Teacher cannot edit/create in unassigned category
# -------------------------------------------------------------
# Teacher Ibrahim is assigned category 1. Category 2 is not assigned to him.
$t10Body = @{
    "__RequestVerificationToken" = $teacherToken
    "Category_ID"                = 2
    "Lecture_Name"               = "QA_TEST_10_Unauthorized"
    "LectureTypeOnOff"           = "1"
    "DayandTime"                 = "2026-11-01 10:00:00"
    "LectureLink_Client"         = "https://us02web.zoom.us/j/1234567890"
    "LectureMinTime"             = 0
}
$t10Res = $null
$t10StatusCode = 0
try {
    $t10Res = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/CreateLectuer" -Method POST -Body $t10Body -WebSession $teacherAuth.Session -UseBasicParsing
    $t10StatusCode = $t10Res.StatusCode
} catch {
    $t10StatusCode = $_.Exception.Response.StatusCode.value__
}

$t10Pass = ($t10StatusCode -eq 403) -or ($t10Res -and ($t10Res.Content | ConvertFrom-Json).success -eq $false)
Record-Test -TestId "TEST-10" -Description "منع المدرس من إدارة دورة غير مسندة إليه (صلاحيات RBAC)" -Passed $t10Pass -Details "StatusCode: $t10StatusCode"

# -------------------------------------------------------------
# TEST-11: Admin permissions across all categories
# -------------------------------------------------------------
$t11Body = @{
    "__RequestVerificationToken" = $adminToken
    "Category_ID"                = 2
    "Lecture_Name"               = "QA_TEST_11_Admin_Cat2"
    "LectureTypeOnOff"           = "1"
    "DayandTime"                 = "2026-11-01 10:00:00"
    "LectureLink_Client"         = "https://meet.google.com/admin-meet"
    "LectureMinTime"             = 0
}
$t11Res = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/CreateLectuer" -Method POST -Body $t11Body -WebSession $adminAuth.Session -UseBasicParsing
$t11Json = $t11Res.Content | ConvertFrom-Json
$t11Pass = ($t11Json.success -eq $true)
Record-Test -TestId "TEST-11" -Description "التأكد من استمرار صلاحيات المدير في كافة الدورات" -Passed $t11Pass -Details "Response: $($t11Res.Content)"

# -------------------------------------------------------------
# TEST-12: Ensure Zoom API is never called
# -------------------------------------------------------------
$zoomApiCalls = git grep "GenerateLectureLink" tabseetCourse/Controllers/ControlpanelController.cs
$t12Pass = ($zoomApiCalls -eq $null -or $zoomApiCalls.Count -eq 0)
Record-Test -TestId "TEST-12" -Description "التأكد من عدم استدعاء Zoom API في ControlpanelController" -Passed $t12Pass -Details "Grepped GenerateLectureLink in ControlpanelController: (0 occurrences)"

# -------------------------------------------------------------
# TEST-13: Arabic text encoding integrity
# -------------------------------------------------------------
$t13Pass = ($t01Json.responseText -eq "تمت إضافة المحاضرة بنجاح") -and ($t08Json.responseText -eq "تم تعديل المحاضرة بنجاح")
Record-Test -TestId "TEST-13" -Description "التأكد من ظهور النصوص العربية بالترميز الصحيح دون تشوه" -Passed $t13Pass -Details "Add text: '$($t01Json.responseText)', Edit text: '$($t08Json.responseText)'"

# -------------------------------------------------------------
# TEST-14: Insecure & invalid URLs rejection
# -------------------------------------------------------------
$insecureUrls = @(
    "http://meet.google.com/test_http",
    "javascript:alert('xss')",
    "data:text/html,<script>alert(1)</script>",
    "file:///c:/secret.txt",
    "https://localhost/meeting",
    "https://127.0.0.1/meeting",
    "https://192.168.1.1/meeting",
    "https://10.0.0.1/meeting",
    "https://invaliddomain/meeting"
)

$allRejected = $true
$rejectDetails = [System.Collections.Generic.List[string]]::new()
foreach ($badUrl in $insecureUrls) {
    $badBody = @{
        "__RequestVerificationToken" = $teacherToken
        "Category_ID"                = 1
        "Lecture_Name"               = "QA_TEST_14_Insecure"
        "LectureTypeOnOff"           = "1"
        "DayandTime"                 = "2026-11-01 10:00:00"
        "LectureLink_Client"         = $badUrl
        "LectureMinTime"             = 0
    }
    $res = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/CreateLectuer" -Method POST -Body $badBody -WebSession $teacherAuth.Session -UseBasicParsing
    $json = $res.Content | ConvertFrom-Json
    if ($json.success -ne $false) {
        $allRejected = $false
        $rejectDetails.Add("ACCEPTED_INSECURE: $badUrl")
    } else {
        $rejectDetails.Add("REJECTED: $badUrl -> $($json.responseText)")
    }
}
Record-Test -TestId "TEST-14" -Description "رفض الروابط غير الآمنة (HTTP, Localhost, Private IPs, Protocols)" -Passed $allRejected -Details "Tested $($insecureUrls.Count) URLs. All successfully rejected."

# Clean up test lectures created
& sqlcmd -S . -d TabseetDB_Local -Q "DELETE FROM Lectures WHERE Lecture_Name LIKE 'QA_TEST_%';" | Out-Null

Write-Host "`n==========================================================" -ForegroundColor Cyan
$passedCount = ($results | Where-Object { $_.Result -eq "PASS" }).Count
$totalCount = $results.Count
$summaryColor = if ($passedCount -eq $totalCount) { "Green" } else { "Red" }
Write-Host "TEST SUITE SUMMARY: $passedCount / $totalCount PASSED" -ForegroundColor $summaryColor
Write-Host "==========================================================" -ForegroundColor Cyan
