param(
    [string]$BaseUrl = "http://localhost:50895"
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Tabseet Academy - Teacher Notification QA Test Runner  " -ForegroundColor Cyan
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

Write-Host "`n[1/4] Authenticating test accounts..." -ForegroundColor Yellow
$teacher1Auth = Get-AuthSession -Email "teacher.ibrahim@example.test" -Password "Teach@Tabseet2026!"
$teacher2Auth = Get-AuthSession -Email "teacher.somaya@example.test"  -Password "Teach@Tabseet2026!"
$admin1Auth   = Get-AuthSession -Email "admin.test@example.test"   -Password "Admin@Tabseet2026!"
$admin2Auth   = Get-AuthSession -Email "admin2.test@example.test"  -Password "Admin@Tabseet2026!"
$studentAuth  = Get-AuthSession -Email "student01@example.test"    -Password "Std@Tabseet2026!"

$teacherPageUrl = "$BaseUrl/Teacher/LecturesByCategoryStudend?Cat_ID=1&categoryName=Quran"
$teacher1Token = Get-VerificationToken -Session $teacher1Auth.Session -Url $teacherPageUrl
$teacher2Token = Get-VerificationToken -Session $teacher2Auth.Session -Url "$BaseUrl/Teacher/LecturesByCategoryStudend?Cat_ID=2&categoryName=Noor"
$admin1Token   = Get-VerificationToken -Session $admin1Auth.Session -Url "$BaseUrl/AdminNotifications/Index"

Write-Host "Teacher 1 authenticated. Token: $(if ($teacher1Token.Length -ge 10) { $teacher1Token.Substring(0, 10) } else { $teacher1Token })..."
Write-Host "Teacher 2 authenticated. Token: $(if ($teacher2Token.Length -ge 10) { $teacher2Token.Substring(0, 10) } else { $teacher2Token })..."
Write-Host "Admin 1 authenticated.   Token: $(if ($admin1Token.Length -ge 10) { $admin1Token.Substring(0, 10) } else { $admin1Token })..."
Write-Host "Admin 2 authenticated."
Write-Host "Student authenticated."

# Clean up previous test notifications/lectures from this test suite
& sqlcmd -S . -d TabseetDB_Local -Q "
    DELETE FROM AdminNotifications WHERE Title LIKE '%NOTIF_TEST_%';
    DELETE FROM TeacherLectureEvents WHERE LectureTitle LIKE '%NOTIF_TEST_%';
    DELETE FROM TeacherLectureStatus WHERE LectureId IN (SELECT ID FROM Lectures WHERE Lecture_Name LIKE '%NOTIF_TEST_%');
    DELETE FROM Lectures WHERE Lecture_Name LIKE '%NOTIF_TEST_%';
" | Out-Null

Write-Host "`n[2/4] Executing Teacher & Lecture Event Tests..." -ForegroundColor Yellow

# -------------------------------------------------------------
# TEST-NOTIF-01: Teacher creates a new lecture -> generates notification
# -------------------------------------------------------------
$t01Body = @{
    "__RequestVerificationToken" = $teacher1Token
    "Category_ID"                = 1
    "Lecture_Name"               = "NOTIF_TEST_01_Lecture"
    "LectureTypeOnOff"           = "1"
    "DayandTime"                 = "2026-11-10 10:00:00"
    "LectureLink_Client"         = "https://us02web.zoom.us/j/9988776655"
    "LectureMinTime"             = 0
}
$t01Res = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/CreateLectuer" -Method POST -Body $t01Body -WebSession $teacher1Auth.Session -UseBasicParsing
$t01Json = $t01Res.Content | ConvertFrom-Json
$t01Db = & sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT TOP 1 ID FROM Lectures WHERE Lecture_Name = 'NOTIF_TEST_01_Lecture' ORDER BY ID DESC;"
$t01LecId = ($t01Db.Trim() -split '\s+')[0]

$t01Event = (& sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT ActionType, TeacherName FROM TeacherLectureEvents WHERE LectureTitle = 'NOTIF_TEST_01_Lecture';") -join ' '
$t01Notif = (& sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT COUNT(*) FROM AdminNotifications WHERE Title LIKE '%NOTIF_TEST_01_Lecture%';") -join ' '

$t01Pass = ($t01Json.success -eq $true) -and ($t01Event -match "LectureCreated") -and ([int]($t01Notif.Trim() -split '\s+')[0] -ge 1)
Record-Test -TestId "TEST-NOTIF-01" -Description "إنشاء محاضرة جديدة وتوليد إشعار فوري للإدارة" -Passed $t01Pass -Details "LecID: $t01LecId | Event: $t01Event | NotifCount: $t01Notif"

# -------------------------------------------------------------
# TEST-NOTIF-02: Teacher updates lecture time -> generates LectureTimeChanged notification
# -------------------------------------------------------------
$t02Body = @{
    "__RequestVerificationToken" = $teacher1Token
    "ID"                         = [int]$t01LecId
    "Category_ID"                = 1
    "Lecture_Name"               = "NOTIF_TEST_01_Lecture"
    "LectureTypeOnOff"           = "1"
    "DayandTime"                 = "2026-11-11 15:30:00"
    "LectureLink_Client"         = "https://us02web.zoom.us/j/9988776655"
    "LectureMinTime"             = 0
}
$t02Res = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/EditLectuer" -Method POST -Body $t02Body -WebSession $teacher1Auth.Session -UseBasicParsing
$t02Json = $t02Res.Content | ConvertFrom-Json

$t02Event = (& sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT ActionType FROM TeacherLectureEvents WHERE LectureId = $t01LecId AND ActionType = 'LectureTimeChanged';") -join ' '
$t02Pass = ($t02Json.success -eq $true) -and ($t02Event -match "LectureTimeChanged")
Record-Test -TestId "TEST-NOTIF-02" -Description "تعديل موعد محاضرة وتسجيل حدث LectureTimeChanged" -Passed $t02Pass -Details "Res: $($t02Res.Content) | Event: $t02Event"

# -------------------------------------------------------------
# TEST-NOTIF-03: Teacher updates lecture link -> generates LectureLinkChanged notification
# -------------------------------------------------------------
$t03Body = @{
    "__RequestVerificationToken" = $teacher1Token
    "ID"                         = [int]$t01LecId
    "Category_ID"                = 1
    "Lecture_Name"               = "NOTIF_TEST_01_Lecture"
    "LectureTypeOnOff"           = "1"
    "DayandTime"                 = "2026-11-11 15:30:00"
    "LectureLink_Client"         = "https://meet.google.com/xyz-uvwx-rst"
    "LectureMinTime"             = 0
}
$t03Res = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/EditLectuer" -Method POST -Body $t03Body -WebSession $teacher1Auth.Session -UseBasicParsing
$t03Json = $t03Res.Content | ConvertFrom-Json

$t03Event = (& sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT ActionType FROM TeacherLectureEvents WHERE LectureId = $t01LecId AND ActionType = 'LectureLinkChanged';") -join ' '
$t03Pass = ($t03Json.success -eq $true) -and ($t03Event -match "LectureLinkChanged")
Record-Test -TestId "TEST-NOTIF-03" -Description "تعديل رابط المحاضرة وتسجيل حدث LectureLinkChanged" -Passed $t03Pass -Details "Res: $($t03Res.Content) | Event: $t03Event"

# -------------------------------------------------------------
# TEST-NOTIF-04: Teacher confirms lecture completion ("تم إكمال الدرس")
# -------------------------------------------------------------
$t04Body = @{
    "__RequestVerificationToken" = $teacher1Token
    "lectureId"                  = [int]$t01LecId
    "status"                     = "Completed"
    "notes"                      = "تم إنهاء درس التجويد بنجاح ومراجعة الطلاب"
}
$t04Res = Invoke-WebRequest -Uri "$BaseUrl/Teacher/UpdateLectureStatus" -Method POST -Body $t04Body -WebSession $teacher1Auth.Session -UseBasicParsing
$t04Json = $t04Res.Content | ConvertFrom-Json

$t04Status = (& sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT CurrentStatus, TeacherName FROM TeacherLectureStatus WHERE LectureId = $t01LecId;") -join ' '
$t04Event  = (& sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT ActionType FROM TeacherLectureEvents WHERE LectureId = $t01LecId AND ActionType = 'LectureCompleted';") -join ' '
$t04Pass = ($t04Json.success -eq $true) -and ($t04Status -match "Completed") -and ($t04Event -match "LectureCompleted")
Record-Test -TestId "TEST-NOTIF-04" -Description "تأكيد إكمال الدرس وتسجيل الحالة مكتملة وإشعار الإدارة" -Passed $t04Pass -Details "Status: $t04Status | Event: $t04Event"

# -------------------------------------------------------------
# TEST-NOTIF-05: Double-click idempotency (Re-confirm completion without change)
# -------------------------------------------------------------
$t05Pass = $false
try {
    $t05Res = Invoke-WebRequest -Uri "$BaseUrl/Teacher/UpdateLectureStatus" -Method POST -Body $t04Body -WebSession $teacher1Auth.Session -UseBasicParsing
    $t05Json = $t05Res.Content | ConvertFrom-Json
    $t05Pass = ($t05Json.success -eq $false) -or ($t05Json.message -match "مكتملة")
} catch {
    $t05Pass = $true
}
$t05EventCount = (& sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT COUNT(*) FROM TeacherLectureEvents WHERE LectureId = $t01LecId AND ActionType = 'LectureCompleted';") -join ' '
$t05Pass = $t05Pass -and ([int]($t05EventCount.Trim() -split '\s+')[0] -eq 1)
Record-Test -TestId "TEST-NOTIF-05" -Description "منع تكرار الإشعار عند إعادة تأكيد الإكمال (Idempotency)" -Passed $t05Pass -Details "EventCount: $t05EventCount"

# -------------------------------------------------------------
# TEST-NOTIF-06: Teacher postpones lecture with reason
# -------------------------------------------------------------
$t06Body = @{
    "__RequestVerificationToken" = $teacher1Token
    "lectureId"                  = [int]$t01LecId
    "status"                     = "Postponed"
    "postponeReason"             = "ظرف طارئ للمدرس وسيتم التعويض الأسبوع القادم"
    "notes"                      = "تم إبلاغ الطلاب مسبقاً"
}
$t06Res = Invoke-WebRequest -Uri "$BaseUrl/Teacher/UpdateLectureStatus" -Method POST -Body $t06Body -WebSession $teacher1Auth.Session -UseBasicParsing
$t06Json = $t06Res.Content | ConvertFrom-Json

$t06Status = (& sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT CurrentStatus, PostponeReason FROM TeacherLectureStatus WHERE LectureId = $t01LecId;") -join ' '
$t06Event  = (& sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT ActionType, Details FROM TeacherLectureEvents WHERE LectureId = $t01LecId AND ActionType = 'LecturePostponed';") -join ' '
$t06Pass = ($t06Json.success -eq $true) -and ($t06Status -match "Postponed") -and ($t06Event -match "LecturePostponed")
Record-Test -TestId "TEST-NOTIF-06" -Description "تأجيل محاضرة مع ذكر السبب وتسجيل الحدث والإشعار" -Passed $t06Pass -Details "Status: $t06Status | Event: $t06Event"

# -------------------------------------------------------------
# TEST-NOTIF-07: Reject postpone without reason
# -------------------------------------------------------------
$t07Body = @{
    "__RequestVerificationToken" = $teacher1Token
    "lectureId"                  = [int]$t01LecId
    "status"                     = "Postponed"
    "postponeReason"             = "  "
}
$t07Failed = $false
try {
    $t07Res = Invoke-WebRequest -Uri "$BaseUrl/Teacher/UpdateLectureStatus" -Method POST -Body $t07Body -WebSession $teacher1Auth.Session -UseBasicParsing
    $t07Json = $t07Res.Content | ConvertFrom-Json
    $t07Failed = ($t07Json.success -eq $false)
} catch {
    $t07Failed = $true
}
Record-Test -TestId "TEST-NOTIF-07" -Description "رفض تأجيل الدرس بدون ذكر سبب التأجيل" -Passed $t07Failed -Details "Rejected as expected"

# -------------------------------------------------------------
# TEST-NOTIF-08: Reopen / Correct lecture status to Scheduled
# -------------------------------------------------------------
$t08Body = @{
    "__RequestVerificationToken" = $teacher1Token
    "lectureId"                  = [int]$t01LecId
    "status"                     = "Scheduled"
    "notes"                      = "تصحيح: تم التنسيق مع الإدارة لإعادة الجدولة"
}
$t08Res = Invoke-WebRequest -Uri "$BaseUrl/Teacher/UpdateLectureStatus" -Method POST -Body $t08Body -WebSession $teacher1Auth.Session -UseBasicParsing
$t08Json = $t08Res.Content | ConvertFrom-Json

$t08Status = (& sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT CurrentStatus FROM TeacherLectureStatus WHERE LectureId = $t01LecId;") -join ' '
$t08Event  = (& sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT ActionType FROM TeacherLectureEvents WHERE LectureId = $t01LecId AND ActionType = 'LectureReopened';") -join ' '
$t08Pass = ($t08Json.success -eq $true) -and ($t08Status -match "Scheduled") -and ($t08Event -match "LectureReopened")
Record-Test -TestId "TEST-NOTIF-08" -Description "إعادة فتح درس وتصحيح حالته إلى مجدولة مع تسجيل الحدث" -Passed $t08Pass -Details "Status: $t08Status | Event: $t08Event"

# -------------------------------------------------------------
# TEST-NOTIF-09: Delete lecture and preserve event history
# -------------------------------------------------------------
$t09Body = @{
    "__RequestVerificationToken" = $teacher1Token
    "ID"                         = [int]$t01LecId
}
$t09Res = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/DeleteLecture" -Method POST -Body $t09Body -WebSession $teacher1Auth.Session -UseBasicParsing
$t09Json = $t09Res.Content | ConvertFrom-Json

$t09LecInDb   = (& sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT COUNT(*) FROM Lectures WHERE ID = $t01LecId;") -join ' '
$t09EventInDb = (& sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT COUNT(*) FROM TeacherLectureEvents WHERE LectureId = $t01LecId;") -join ' '
$t09DelEvent  = (& sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT ActionType, LectureTitle FROM TeacherLectureEvents WHERE LectureId = $t01LecId AND ActionType = 'LectureDeleted';") -join ' '

$t09Pass = ($t09Json.success -eq $true) -and ([int]($t09LecInDb.Trim() -split '\s+')[0] -eq 0) -and ([int]($t09EventInDb.Trim() -split '\s+')[0] -gt 0) -and ($t09DelEvent -match "LectureDeleted")
Record-Test -TestId "TEST-NOTIF-09" -Description "حذف المحاضرة مع بقاء سجل الأحداث والإشعارات وسناب شوت البيانات" -Passed $t09Pass -Details "LecInDb: $t09LecInDb | EventsRetained: $t09EventInDb | DelEvent: $t09DelEvent"

# -------------------------------------------------------------
# TEST-NOTIF-10: RBAC Authorization: Teacher cannot alter another teacher's category lecture
# -------------------------------------------------------------
$t10CreateBody = @{
    "__RequestVerificationToken" = $teacher1Token
    "Category_ID"                = 1
    "Lecture_Name"               = "NOTIF_TEST_10_Ibrahim_Only"
    "LectureTypeOnOff"           = "1"
    "DayandTime"                 = "2026-11-12 10:00:00"
    "LectureLink_Client"         = "https://us02web.zoom.us/j/111222333"
    "LectureMinTime"             = 0
}
$null = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/CreateLectuer" -Method POST -Body $t10CreateBody -WebSession $teacher1Auth.Session -UseBasicParsing
$t10LecId = ((& sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT TOP 1 ID FROM Lectures WHERE Lecture_Name = 'NOTIF_TEST_10_Ibrahim_Only';") -join ' ').Trim().Split()[0]

$t10IllegalBody = @{
    "__RequestVerificationToken" = $teacher2Token
    "lectureId"                  = [int]$t10LecId
    "status"                     = "Completed"
    "notes"                      = "محاولة غير مصرح بها"
}
$t10Forbidden = $false
try {
    $t10Res = Invoke-WebRequest -Uri "$BaseUrl/Teacher/UpdateLectureStatus" -Method POST -Body $t10IllegalBody -WebSession $teacher2Auth.Session -UseBasicParsing
    if ($t10Res.StatusCode -eq 403) { $t10Forbidden = $true }
} catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 403) {
        $t10Forbidden = $true
    }
}
Record-Test -TestId "TEST-NOTIF-10" -Description "منع المدرس من تغيير حالة محاضرة خارج دوراته المسندة (RBAC 403)" -Passed $t10Forbidden -Details "Forbidden (403): $t10Forbidden"

Write-Host "`n[3/4] Executing Admin Notification Page & Bell Tests..." -ForegroundColor Yellow

# -------------------------------------------------------------
# TEST-NOTIF-11: Admin Notifications Page loads (200 OK) with Arabic layout
# -------------------------------------------------------------
$t11Res = Invoke-WebRequest -Uri "$BaseUrl/AdminNotifications/Index" -WebSession $admin1Auth.Session -UseBasicParsing
$t11Pass = ($t11Res.StatusCode -eq 200) -and ($t11Res.Content -match "إشعارات المدرسين") -and ($t11Res.Content -match "modalNotificationDetails")
Record-Test -TestId "TEST-NOTIF-11" -Description "فتح صفحة إشعارات الإدارة (200 OK) والتأكد من عناصر الواجهة" -Passed $t11Pass -Details "Status: $($t11Res.StatusCode) | Title match: True"

# -------------------------------------------------------------
# TEST-NOTIF-12: Unread count API for Admin
# -------------------------------------------------------------
$t12Res = Invoke-WebRequest -Uri "$BaseUrl/AdminNotifications/GetUnreadCount" -WebSession $admin1Auth.Session -UseBasicParsing
$t12Json = $t12Res.Content | ConvertFrom-Json
$t12Pass = ($t12Json.success -eq $true) -and ($t12Json.count -gt 0)
Record-Test -TestId "TEST-NOTIF-12" -Description "استعلام عداد الإشعارات غير المقروءة عبر AJAX API" -Passed $t12Pass -Details "UnreadCount: $($t12Json.count)"

# -------------------------------------------------------------
# TEST-NOTIF-13: Latest notifications API for Bell dropdown
# -------------------------------------------------------------
$t13Res = Invoke-WebRequest -Uri "$BaseUrl/AdminNotifications/GetLatest?count=5" -WebSession $admin1Auth.Session -UseBasicParsing
$t13Json = $t13Res.Content | ConvertFrom-Json
$t13Pass = ($t13Json.success -eq $true) -and ($t13Json.items.Count -gt 0)
Record-Test -TestId "TEST-NOTIF-13" -Description "استعلام قائمة آخر الإشعارات لجرس الإدارة Dropdown" -Passed $t13Pass -Details "Returned $($t13Json.items.Count) items"

# -------------------------------------------------------------
# TEST-NOTIF-14: Get single notification details API
# -------------------------------------------------------------
$firstNotifId = $t13Json.items[0].NotificationId
$t14Res = Invoke-WebRequest -Uri "$BaseUrl/AdminNotifications/GetDetails?id=$firstNotifId" -WebSession $admin1Auth.Session -UseBasicParsing
$t14Json = $t14Res.Content | ConvertFrom-Json
$t14Pass = ($t14Json.success -eq $true) -and ($t14Json.details.NotificationId -eq $firstNotifId) -and ($t14Json.details.TeacherName -ne $null)
Record-Test -TestId "TEST-NOTIF-14" -Description "عرض تفاصيل إشعار محدد مع بيانات الحدث والمدرس والدورة" -Passed $t14Pass -Details "NotifId: $firstNotifId | Teacher: $($t14Json.details.TeacherName)"

# -------------------------------------------------------------
# TEST-NOTIF-15: Mark single notification as read
# -------------------------------------------------------------
# Get fresh token for admin
$adminFreshToken = Get-VerificationToken -Session $admin1Auth.Session -Url "$BaseUrl/AdminNotifications/Index"
$unreadNotif = $t13Json.items | Where-Object { $_.IsRead -eq $false } | Select-Object -First 1
$unreadId = if ($unreadNotif) { $unreadNotif.NotificationId } else { $firstNotifId }

$t15Body = @{
    "__RequestVerificationToken" = $adminFreshToken
    "id"                         = $unreadId
}
$t15Res = Invoke-WebRequest -Uri "$BaseUrl/AdminNotifications/MarkAsRead" -Method POST -Body $t15Body -WebSession $admin1Auth.Session -UseBasicParsing
$t15Json = $t15Res.Content | ConvertFrom-Json
$t15Db = (& sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT IsRead FROM AdminNotifications WHERE NotificationId = $unreadId;") -join ' '
$t15Pass = ($t15Json.success -eq $true) -and ($t15Db -match "1")
Record-Test -TestId "TEST-NOTIF-15" -Description "تحديد إشعار واحد كمقروء واستمرار الحالة في قاعدة البيانات" -Passed $t15Pass -Details "Id: $unreadId | IsRead: $t15Db"

# -------------------------------------------------------------
# TEST-NOTIF-16: Multi-Admin Isolation (Reading by Admin 1 does NOT affect Admin 2)
# -------------------------------------------------------------
$admin2Notif = (& sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT TOP 1 n.IsRead FROM AdminNotifications n JOIN AdminNotifications n1 ON n.EventId = n1.EventId WHERE n1.NotificationId = $unreadId AND n.AdminUserId = 'TEST-USR-ADMIN-02';") -join ' '
$t16Pass = [bool]($admin2Notif -match "0")
Record-Test -TestId "TEST-NOTIF-16" -Description "استقلالية قراءة الإشعار لكل حساب إدارة (قراءة مدير لا تغير حالة مدير آخر)" -Passed $t16Pass -Details "Admin2 IsRead: $admin2Notif"

# -------------------------------------------------------------
# TEST-NOTIF-17: Mark ALL notifications as read
# -------------------------------------------------------------
$t17Body = @{
    "__RequestVerificationToken" = $adminFreshToken
}
$t17Res = Invoke-WebRequest -Uri "$BaseUrl/AdminNotifications/MarkAllAsRead" -Method POST -Body $t17Body -WebSession $admin1Auth.Session -UseBasicParsing
$t17Json = $t17Res.Content | ConvertFrom-Json
$t17Remaining = (& sqlcmd -S . -d TabseetDB_Local -h -1 -W -Q "SELECT COUNT(*) FROM AdminNotifications WHERE AdminUserId = 'TEST-USR-ADMIN-01' AND IsRead = 0;") -join ' '
$t17Pass = ($t17Json.success -eq $true) -and ([int]($t17Remaining.Trim() -split '\s+')[0] -eq 0)
Record-Test -TestId "TEST-NOTIF-17" -Description "تحديد جميع الإشعارات كمقروءة دفعة واحدة" -Passed $t17Pass -Details "Unread remaining for Admin1: $t17Remaining"

# -------------------------------------------------------------
# TEST-NOTIF-18: Search and Filter in Admin Notifications
# -------------------------------------------------------------
$t18Res = Invoke-WebRequest -Uri "$BaseUrl/AdminNotifications/Index?TeacherName=$([System.Uri]::EscapeDataString('إبراهيم'))&Status=read" -WebSession $admin1Auth.Session -UseBasicParsing
$t18Pass = ($t18Res.StatusCode -eq 200) -and ($t18Res.Content -match "إبراهيم")
Record-Test -TestId "TEST-NOTIF-18" -Description "عمل تصفية وبحث الإشعارات بحسب المدرس وحالة القراءة" -Passed $t18Pass -Details "Status: $($t18Res.StatusCode)"

# -------------------------------------------------------------
# TEST-NOTIF-19: Security: Students blocked from Admin Notifications
# -------------------------------------------------------------
$t19Blocked = $false
try {
    $t19Res = Invoke-WebRequest -Uri "$BaseUrl/AdminNotifications/Index" -WebSession $studentAuth.Session -UseBasicParsing -MaximumRedirection 0
    if ($t19Res.StatusCode -eq 302 -or $t19Res.StatusCode -eq 401 -or $t19Res.StatusCode -eq 403) {
        $t19Blocked = $true
    }
} catch {
    $t19Blocked = $true
}
Record-Test -TestId "TEST-NOTIF-19" -Description "حماية أمنية: منع حسابات الطلاب من فتح صفحة إشعارات الإدارة" -Passed $t19Blocked -Details "Blocked: $t19Blocked"

# -------------------------------------------------------------
# TEST-NOTIF-20: Security: Anonymous users redirected to login
# -------------------------------------------------------------
$anonSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$t20Blocked = $false
try {
    $t20Res = Invoke-WebRequest -Uri "$BaseUrl/AdminNotifications/Index" -WebSession $anonSession -UseBasicParsing -MaximumRedirection 0
    if ($t20Res.StatusCode -eq 302 -or $t20Res.Headers.Location -match "LogIn") {
        $t20Blocked = $true
    }
} catch {
    $t20Blocked = $true
}
Record-Test -TestId "TEST-NOTIF-20" -Description "حماية أمنية: توجيه المستخدم غير المسجل إلى صفحة الدخول" -Passed $t20Blocked -Details "Redirected: $t20Blocked"

Write-Host "`n[4/4] Executing Regression Stability Tests..." -ForegroundColor Yellow

# -------------------------------------------------------------
# TEST-NOTIF-21: Teacher lectures page loads and returns statuses
# -------------------------------------------------------------
$t21Res = Invoke-WebRequest -Uri "$BaseUrl/Teacher/LecturesByCategoryStudend?Cat_ID=1&categoryName=Quran" -WebSession $teacher1Auth.Session -UseBasicParsing
$t21Pass = ($t21Res.StatusCode -eq 200) -and ($t21Res.Content -match "حالة الدرس") -and ($t21Res.Content -match "modalCompleteLesson")
Record-Test -TestId "TEST-NOTIF-21" -Description "استقرار صفحة محاضرات المدرس واحتوائها على عمود حالة الدرس ونوافذ الإنجاز" -Passed $t21Pass -Details "Status: $($t21Res.StatusCode)"

# -------------------------------------------------------------
# TEST-NOTIF-22: Teacher LectureByCategorysStd API returns Status and StatusArabic
# -------------------------------------------------------------
$t22Body = @{ "Cat_ID" = 1 }
$t22Res = Invoke-WebRequest -Uri "$BaseUrl/Teacher/LectureByCategorysStd" -Method POST -Body $t22Body -WebSession $teacher1Auth.Session -UseBasicParsing
$t22Json = $t22Res.Content | ConvertFrom-Json
$t22Pass = ($t22Json.Count -gt 0) -and ($t22Json[0].Status -ne $null) -and ($t22Json[0].StatusArabic -ne $null)
Record-Test -TestId "TEST-NOTIF-22" -Description "استجابة واجهة LectureByCategorysStd البرمجية بحالات الدروس" -Passed $t22Pass -Details "First item status: $($t22Json[0].Status) ($($t22Json[0].StatusArabic))"

# -------------------------------------------------------------
# TEST-NOTIF-23: Admin top navbar bell rendering check
# -------------------------------------------------------------
$t23Res = Invoke-WebRequest -Uri "$BaseUrl/Controlpanel/Index" -WebSession $admin1Auth.Session -UseBasicParsing
$t23Pass = ($t23Res.StatusCode -eq 200) -and ($t23Res.Content -match "adminNotifDropdown") -and ($t23Res.Content -match "adminNotifBellCount")
Record-Test -TestId "TEST-NOTIF-23" -Description "ظهور جرس الإشعارات وقائمة التنبيهات في الشريط العلوي للوحة الإدارة" -Passed $t23Pass -Details "Navbar Bell Found: True"

# -------------------------------------------------------------
# TEST-NOTIF-24: Admin sidebar menu item check
# -------------------------------------------------------------
$t24Pass = ($t23Res.StatusCode -eq 200) -and ($t23Res.Content -match "AdminNotifications") -and ($t23Res.Content -match "إشعارات المدرسين")
Record-Test -TestId "TEST-NOTIF-24" -Description "ظهور عنصر 'إشعارات المدرسين' في القائمة الجانبية للوحة الإدارة" -Passed $t24Pass -Details "Sidebar Menu Found: True"

# Clean up test lecture
& sqlcmd -S . -d TabseetDB_Local -Q "
    DELETE FROM AdminNotifications WHERE Title LIKE '%NOTIF_TEST_%';
    DELETE FROM TeacherLectureEvents WHERE LectureTitle LIKE '%NOTIF_TEST_%';
    DELETE FROM TeacherLectureStatus WHERE LectureId IN (SELECT ID FROM Lectures WHERE Lecture_Name LIKE '%NOTIF_TEST_%');
    DELETE FROM Lectures WHERE Lecture_Name LIKE '%NOTIF_TEST_%';
" | Out-Null

Write-Host "==========================================================" -ForegroundColor Cyan
$passCount = ($results | Where-Object { $_.Result -eq "PASS" }).Count
$totalCount = $results.Count
$color = if ($passCount -eq $totalCount) { "Green" } else { "Red" }
Write-Host "TEST SUITE SUMMARY: $passCount / $totalCount PASSED" -ForegroundColor $color
Write-Host "==========================================================" -ForegroundColor Cyan
