param(
    [string]$BaseUrl = "http://localhost:50895"
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Testing Student Schedule & Subjects Separation        " -ForegroundColor Cyan
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
$anonSub = Invoke-WebRequest -Uri "$BaseUrl/Students/StdSubjects" -WebSession $anonSession -UseBasicParsing

Assert-Condition "Schedule redirects anonymous users to login" ($anonSched.Content -match "تسجيل الدخول|LogIn" -or $anonSched.StatusCode -eq 302) "Anonymous schedule did not redirect to login"
Assert-Condition "Subjects redirects anonymous users to login" ($anonSub.Content -match "تسجيل الدخول|LogIn" -or $anonSub.StatusCode -eq 302) "Anonymous subjects did not redirect to login"

# 2. Test student01 (Enrolled in Quran and Nooraniyah)
Write-Host "`n--- Testing student01 (Enrolled in courses) ---" -ForegroundColor Yellow
$std01Session = Get-StudentSession "student01@example.test" "Std@Tabseet2026!"

$std01Sched = Invoke-WebRequest -Uri "$BaseUrl/Students/StdonlineCourse" -WebSession $std01Session -UseBasicParsing
Assert-Condition "Student01: Schedule page loads (200 OK)" ($std01Sched.StatusCode -eq 200) "Status: $($std01Sched.StatusCode)"
Assert-Condition "Student01: Schedule has title 'جدول الحصص'" ($std01Sched.Content.Contains("جدول الحصص")) "Title missing"
Assert-Condition "Student01: Schedule has subnav tabs linking to Subjects" ($std01Sched.Content.Contains("المواد الدراسية") -and $std01Sched.Content.Contains("/Students/StdSubjects")) "Tab missing"
$hasCardBadge = $std01Sched.Content -match '(?s)<article class="tt-lecture-card[^"]*">(?:(?!</article>).)*?<span class="tt-status-badge'
Assert-Condition "Student01: Schedule cards have NO 'سابقة' or 'قادمة' badge in grid" (-not $hasCardBadge) "Found status badge in card"
Assert-Condition "Student01: Schedule has Return to Today button" ($std01Sched.Content.Contains("العودة إلى اليوم")) "Today button missing"
Assert-Condition "Student01: Schedule details modal exists" ($std01Sched.Content.Contains("id=""ttLessonModal""")) "Modal missing"

$std01Sub = Invoke-WebRequest -Uri "$BaseUrl/Students/StdSubjects" -WebSession $std01Session -UseBasicParsing
Assert-Condition "Student01: Subjects page loads (200 OK)" ($std01Sub.StatusCode -eq 200) "Status: $($std01Sub.StatusCode)"
Assert-Condition "Student01: Subjects has title 'المواد الدراسية'" ($std01Sub.Content.Contains("المواد الدراسية")) "Title missing"
Assert-Condition "Student01: Subjects has subnav tab linking to Schedule" ($std01Sub.Content.Contains("جدول الحصص") -and $std01Sub.Content.Contains("/Students/StdonlineCourse")) "Schedule tab missing"
Assert-Condition "Student01: Shows enrolled course 'تعليم القرآن الكريم والتجويد'" ($std01Sub.Content.Contains("تعليم القرآن الكريم والتجويد")) "Quran course missing"
Assert-Condition "Student01: Shows teacher name for course" ($std01Sub.Content -match "د\. إبراهيم خليل النجار|أكاديمية تبسيط") "Teacher name missing"
Assert-Condition "Student01: Shows lectures panel and list" ($std01Sub.Content.Contains("sub-lectures-panel") -and $std01Sub.Content.Contains("sub-lectures-list")) "Lectures panel missing"
Assert-Condition "Student01: Contains live lecture join link & recorded video link support" ($std01Sub.Content.Contains("data-valid-client") -and $std01Sub.Content.Contains("data-valid-media")) "Link support attributes missing"

# 3. Test student02 (Enrolled in Nooraniyah and English)
Write-Host "`n--- Testing student02 (Different enrolled courses) ---" -ForegroundColor Yellow
$std02Session = Get-StudentSession "student02@example.test" "Std@Tabseet2026!"
$std02Sub = Invoke-WebRequest -Uri "$BaseUrl/Students/StdSubjects" -WebSession $std02Session -UseBasicParsing
Assert-Condition "Student02: Subjects page loads (200 OK)" ($std02Sub.StatusCode -eq 200) "Status: $($std02Sub.StatusCode)"
Assert-Condition "Student02: Shows enrolled course 'اللغة الإنجليزية للمبتدئين'" ($std02Sub.Content.Contains("اللغة الإنجليزية للمبتدئين")) "English course missing"
Assert-Condition "Student02: Does NOT show courses not enrolled in" (-not $std02Sub.Content.Contains("أساسيات علوم الكمبيوتر")) "Found unenrolled course"

# 4. Test student21 (Student with 0 registered courses)
Write-Host "`n--- Testing student21 (No registered courses) ---" -ForegroundColor Yellow
$std21Session = Get-StudentSession "student21@example.test" "Std@Tabseet2026!"
$std21Sub = Invoke-WebRequest -Uri "$BaseUrl/Students/StdSubjects" -WebSession $std21Session -UseBasicParsing
Assert-Condition "Student21: Subjects loads without error (200 OK)" ($std21Sub.StatusCode -eq 200) "Status: $($std21Sub.StatusCode)"
Assert-Condition "Student21: Displays friendly empty state message" ($std21Sub.Content.Contains("لا توجد مواد مسجلة حاليًا")) "Empty message missing"

$std21Sched = Invoke-WebRequest -Uri "$BaseUrl/Students/StdonlineCourse" -WebSession $std21Session -UseBasicParsing
Assert-Condition "Student21: Schedule loads without error (200 OK)" ($std21Sched.StatusCode -eq 200) "Status: $($std21Sched.StatusCode)"

# 5. Test Backward Compatibility Routes
Write-Host "`n--- Testing Backward Compatibility Routes ---" -ForegroundColor Yellow
$aliasSched = Invoke-WebRequest -Uri "$BaseUrl/Students/Schedule" -WebSession $std01Session -UseBasicParsing
Assert-Condition "Alias /Students/Schedule works (200 OK)" ($aliasSched.StatusCode -eq 200) "Status: $($aliasSched.StatusCode)"

$aliasSub = Invoke-WebRequest -Uri "$BaseUrl/Students/Subjects" -WebSession $std01Session -UseBasicParsing
Assert-Condition "Alias /Students/Subjects works (200 OK)" ($aliasSub.StatusCode -eq 200) "Status: $($aliasSub.StatusCode)"

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "SUMMARY: $passed PASSED, $failed FAILED" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
Write-Host "==========================================================" -ForegroundColor Cyan
