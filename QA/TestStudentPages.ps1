param(
    [string]$BaseUrl = "http://localhost:50895"
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Testing 'برامجي التعليمية' & 'جدول الحصص' Integration  " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$passed = 0
$failed = 0

function Assert-Condition($testName, $condition, $details) {
    if ($condition) {
        Write-Host "[PASS] $testName" -ForegroundColor Green
        $script:passed++
    } else {
        Write-Host "[FAIL] $testName - $details" -ForegroundColor Red
        $script:failed++
    }
}

function Get-StudentSession($email, $pwd) {
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $loginPage = Invoke-WebRequest -Uri "$BaseUrl/Home/LogIn" -WebSession $session -UseBasicParsing
    $token = [regex]::Match($loginPage.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value
    $loginData = @{
        "__RequestVerificationToken" = $token
        "Email" = $email
        "Password" = $pwd
    }
    $res = Invoke-WebRequest -Uri "$BaseUrl/Home/LogIn" -Method POST -Body $loginData -WebSession $session -UseBasicParsing
    return $session
}

# 1. Test unauthenticated redirect
Write-Host "`n--- Testing Unauthenticated Access ---" -ForegroundColor Yellow
$anonSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$anonSched = Invoke-WebRequest -Uri "$BaseUrl/Students/StdonlineCourse" -WebSession $anonSession -UseBasicParsing
$anonProg = Invoke-WebRequest -Uri "$BaseUrl/Students/CategoriesStd" -WebSession $anonSession -UseBasicParsing
$anonSub = Invoke-WebRequest -Uri "$BaseUrl/Students/StdSubjects" -WebSession $anonSession -UseBasicParsing

Assert-Condition "Schedule redirects anonymous users to login" ($anonSched.Content -match "تسجيل الدخول|LogIn" -or $anonSched.StatusCode -eq 302) "Anonymous schedule did not redirect"
Assert-Condition "CategoriesStd redirects anonymous users to login" ($anonProg.Content -match "تسجيل الدخول|LogIn" -or $anonProg.StatusCode -eq 302) "Anonymous categories did not redirect"
Assert-Condition "StdSubjects redirects anonymous users to login" ($anonSub.Content -match "تسجيل الدخول|LogIn" -or $anonSub.StatusCode -eq 302) "Anonymous subjects did not redirect"

# 2. Test student01 (Enrolled in Quran and Nooraniyah)
Write-Host "`n--- Testing student01 (Enrolled in courses) ---" -ForegroundColor Yellow
$std01Session = Get-StudentSession "student01@example.test" "Std@Tabseet2026!"

# A. Testing CategoriesStd (برامجي التعليمية)
$std01Prog = Invoke-WebRequest -Uri "$BaseUrl/Students/CategoriesStd" -WebSession $std01Session -UseBasicParsing
Assert-Condition "Student01: 'برامجي التعليمية' page loads (200 OK)" ($std01Prog.StatusCode -eq 200) "Status: $($std01Prog.StatusCode)"
Assert-Condition "Student01: Page title is 'برامجي التعليمية'" ($std01Prog.Content.Contains("برامجي التعليمية")) "Title missing"
Assert-Condition "Student01: Subnav tabs link to 'جدول الحصص'" ($std01Prog.Content.Contains("/Students/StdonlineCourse") -and $std01Prog.Content.Contains("جدول الحصص")) "Schedule tab missing"
Assert-Condition "Student01: Shows enrolled course 'تعليم القرآن الكريم والتجويد'" ($std01Prog.Content.Contains("تعليم القرآن الكريم والتجويد")) "Quran course missing"
Assert-Condition "Student01: Shows teacher name for course" ($std01Prog.Content -match "د\. إبراهيم خليل النجار|أكاديمية تبسيط") "Teacher name missing"
Assert-Condition "Student01: Shows interactive lectures panel and list" ($std01Prog.Content.Contains("sub-lectures-panel") -and $std01Prog.Content.Contains("sub-lectures-list")) "Lectures panel missing"
Assert-Condition "Student01: Contains live lecture join & recorded video support" ($std01Prog.Content.Contains("data-valid-client") -and $std01Prog.Content.Contains("data-valid-media")) "Link support attributes missing"

# B. Preserved Functions & Modals in CategoriesStd
Assert-Condition "Student01: Payment modal exists (#modal-lg)" ($std01Prog.Content.Contains('id="modal-lg"')) "Payment modal missing"
Assert-Condition "Student01: Exam modal exists (#Exams_ID)" ($std01Prog.Content.Contains('id="Exams_ID"')) "Exam modal missing"
Assert-Condition "Student01: Lecture details modal exists (#subLectureModal)" ($std01Prog.Content.Contains('id="subLectureModal"')) "Lecture modal missing"
Assert-Condition "Student01: Required hidden inputs exist (PayMent_Method, examID, Categories_ID, PaymentValue)" (
    $std01Prog.Content.Contains('id="PayMent_Method"') -and
    $std01Prog.Content.Contains('id="examID"') -and
    $std01Prog.Content.Contains('id="Categories_ID"') -and
    $std01Prog.Content.Contains('id="PaymentValue"')
) "Hidden inputs missing"
Assert-Condition "Student01: Core JS functions exist (getExam, MackPayMent, GetDocmntation)" (
    $std01Prog.Content.Contains('function getExam(') -and
    $std01Prog.Content.Contains('function MackPayMent(') -and
    $std01Prog.Content.Contains('function GetDocmntation(')
) "Core JS functions missing"

# C. Testing StdonlineCourse (جدول الحصص)
$std01Sched = Invoke-WebRequest -Uri "$BaseUrl/Students/StdonlineCourse" -WebSession $std01Session -UseBasicParsing
Assert-Condition "Student01: Schedule page loads (200 OK)" ($std01Sched.StatusCode -eq 200) "Status: $($std01Sched.StatusCode)"
Assert-Condition "Student01: Schedule has title 'جدول الحصص'" ($std01Sched.Content.Contains("جدول الحصص")) "Title missing"
Assert-Condition "Student01: Schedule subnav tab links to 'برامجي التعليمية'" ($std01Sched.Content.Contains("برامجي التعليمية") -and $std01Sched.Content.Contains("/Students/CategoriesStd")) "Programs tab missing"
$hasCardBadge = $std01Sched.Content -match '(?s)<article class="tt-lecture-card[^"]*">(?:(?!</article>).)*?<span class="tt-status-badge'
Assert-Condition "Student01: Schedule cards have NO 'سابقة' or 'قادمة' badge in grid" (-not $hasCardBadge) "Found status badge in card"
Assert-Condition "Student01: Schedule has Return to Today button" ($std01Sched.Content.Contains("العودة إلى اليوم")) "Today button missing"
Assert-Condition "Student01: Schedule details modal exists" ($std01Sched.Content.Contains('id="ttLessonModal"')) "Modal missing"

# 3. Test student02 (Enrolled in Nooraniyah and English)
Write-Host "`n--- Testing student02 (Different enrolled courses) ---" -ForegroundColor Yellow
$std02Session = Get-StudentSession "student02@example.test" "Std@Tabseet2026!"
$std02Prog = Invoke-WebRequest -Uri "$BaseUrl/Students/CategoriesStd" -WebSession $std02Session -UseBasicParsing
Assert-Condition "Student02: CategoriesStd page loads (200 OK)" ($std02Prog.StatusCode -eq 200) "Status: $($std02Prog.StatusCode)"
Assert-Condition "Student02: Shows enrolled course 'اللغة الإنجليزية للمبتدئين'" ($std02Prog.Content.Contains("اللغة الإنجليزية للمبتدئين")) "English course missing"
Assert-Condition "Student02: Does NOT show courses not enrolled in" (-not $std02Prog.Content.Contains("أساسيات علوم الكمبيوتر")) "Found unenrolled course"

# 4. Test student21 (Student with 0 registered courses)
Write-Host "`n--- Testing student21 (No registered courses) ---" -ForegroundColor Yellow
$std21Session = Get-StudentSession "student21@example.test" "Std@Tabseet2026!"
$std21Prog = Invoke-WebRequest -Uri "$BaseUrl/Students/CategoriesStd" -WebSession $std21Session -UseBasicParsing
Assert-Condition "Student21: CategoriesStd loads without error (200 OK)" ($std21Prog.StatusCode -eq 200) "Status: $($std21Prog.StatusCode)"
Assert-Condition "Student21: Displays friendly empty state message" ($std21Prog.Content.Contains("لا توجد برامج أو دورات مسجلة حاليًا")) "Empty message missing"

$std21Sched = Invoke-WebRequest -Uri "$BaseUrl/Students/StdonlineCourse" -WebSession $std21Session -UseBasicParsing
Assert-Condition "Student21: Schedule loads without error (200 OK)" ($std21Sched.StatusCode -eq 200) "Status: $($std21Sched.StatusCode)"

# 5. Test Backward Compatibility & Redirect Routes
Write-Host "`n--- Testing Backward Compatibility Routes ---" -ForegroundColor Yellow
$aliasSched = Invoke-WebRequest -Uri "$BaseUrl/Students/Schedule" -WebSession $std01Session -UseBasicParsing
Assert-Condition "Alias /Students/Schedule works (200 OK)" ($aliasSched.StatusCode -eq 200) "Status: $($aliasSched.StatusCode)"

$aliasSub = Invoke-WebRequest -Uri "$BaseUrl/Students/Subjects" -WebSession $std01Session -UseBasicParsing
Assert-Condition "Alias /Students/Subjects redirects to CategoriesStd (200 OK)" ($aliasSub.StatusCode -eq 200 -and $aliasSub.Content.Contains("برامجي التعليمية")) "Status: $($aliasSub.StatusCode)"

$aliasStdSub = Invoke-WebRequest -Uri "$BaseUrl/Students/StdSubjects" -WebSession $std01Session -UseBasicParsing
Assert-Condition "Alias /Students/StdSubjects redirects to CategoriesStd (200 OK)" ($aliasStdSub.StatusCode -eq 200 -and $aliasStdSub.Content.Contains("برامجي التعليمية")) "Status: $($aliasStdSub.StatusCode)"

# 6. Test Navigation Layout Dropdown
Write-Host "`n--- Testing Layout Student Dropdown Navigation ---" -ForegroundColor Yellow
Assert-Condition "Student menu contains 'برامجي التعليمية'" ($std01Prog.Content -match 'href="[^"]*CategoriesStd[^"]*"[^>]*>\s*<svg[\s\S]*?برامجي التعليمية') "Missing 'برامجي التعليمية' in dropdown"
Assert-Condition "Student menu contains 'جدول الحصص'" ($std01Prog.Content -match 'href="[^"]*StdonlineCourse[^"]*"[^>]*>[\s\S]*?جدول الحصص') "Missing 'جدول الحصص' in dropdown"
Assert-Condition "Student menu does NOT contain 'المواد الدراسية'" (-not ($std01Prog.Content -match 'class="dropdown-item"[^>]*>[\s\S]*?المواد الدراسية')) "Found redundant 'المواد الدراسية' in dropdown"

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "SUMMARY: $passed PASSED, $failed FAILED" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
Write-Host "==========================================================" -ForegroundColor Cyan
