param(
    [string]$BaseUrl = "http://localhost:50895",
    [string]$WebRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) "tabseetCourse"),
    [switch]$RevertStudent01Math
)

# ==============================================================================
# اختبارات نظام الاشتراكات بموافقة الإدارة (2026-10-24) — قاعدة البيانات المحلية فقط (TabseetDB_Local)
#   Test 1: تسجيل طالب جديد بلا دفع وبلا موافقة لدخول الموقع
#   Test 2: دورة مجانية (7 مهارات البحث العلمي) تُفتح فور الاشتراك
#   Test 3: دورة مدفوعة (3 اللغة العربية) تبقى «بانتظار موافقة الإدارة» ويُمنع محتواها (صفحات + API + ملفات)
#   Test 4: موافقة الإدارة (POST + رمز مكافحة التزوير) تفتح الدورة فوراً وتوثّق المدير والتاريخ
#   Test 5: محاولات تجاوز الصلاحيات (طالب/مدرس/مجهول/تكرار/تغيير المعرف/API)
#   Test 6: عدم التأثر: YouTube، الحضور، Zoom، الواجب، الاختبار، الشهادة، إشعارات الإدارة، الجدول، صفحات المدرس
# يُنشئ السكربت طالباً تجريبياً (qa.sub.*@example.test) وملفين مؤقتين ويحذفهما في النهاية.
# ملاحظة: Test 4 يعتمد أيضاً طلب student01 في دورة الرياضيات (الصف 27) حتى تعمل سكربتات QA القديمة التي
#          تفترض وصوله إلى الدورة 6؛ مرّر -RevertStudent01Math لإعادته إلى «معلّق» بعد الاختبار.
# ==============================================================================

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Continue"
$ProgressPreference = "SilentlyContinue"

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

function Get-Page {
    param($Session, [string]$Url, [switch]$NoRedirect)
    try {
        if ($NoRedirect) { $r = Invoke-WebRequest -Uri $Url -WebSession $Session -UseBasicParsing -MaximumRedirection 0 -SkipHttpErrorCheck -ErrorAction SilentlyContinue }
        else { $r = Invoke-WebRequest -Uri $Url -WebSession $Session -UseBasicParsing -SkipHttpErrorCheck }
        $loc = ""
        if ($r.Headers["Location"]) { $loc = [string]($r.Headers["Location"] | Select-Object -First 1) }
        return [PSCustomObject]@{ Status = [int]$r.StatusCode; Content = [string]$r.Content; Location = $loc; ContentType = [string]$r.Headers["Content-Type"] }
    } catch {
        return [PSCustomObject]@{ Status = 0; Content = $_.Exception.Message; Location = ""; ContentType = "" }
    }
}

function Get-Token {
    param($Session, [string]$Url)
    $page = Get-Page -Session $Session -Url $Url
    return [regex]::Match($page.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value
}

function Invoke-Post {
    param($Session, [string]$Url, $Body, [switch]$NoRedirect)
    $status = 0; $content = ""; $json = $null; $loc = ""
    try {
        if ($NoRedirect) { $res = Invoke-WebRequest -Uri $Url -Method POST -Body $Body -WebSession $Session -UseBasicParsing -MaximumRedirection 0 -SkipHttpErrorCheck -ErrorAction SilentlyContinue }
        else { $res = Invoke-WebRequest -Uri $Url -Method POST -Body $Body -WebSession $Session -UseBasicParsing -SkipHttpErrorCheck }
        $status = [int]$res.StatusCode; $content = [string]$res.Content
        if ($res.Headers["Location"]) { $loc = [string]($res.Headers["Location"] | Select-Object -First 1) }
    } catch {
        $content = $_.Exception.Message
    }
    try { $json = $content | ConvertFrom-Json } catch { }
    return [PSCustomObject]@{ Status = $status; Json = $json; Raw = $content; Location = $loc }
}

function Has-ServerError { param([string]$Content) return ($Content -match "Server Error in '/'|Compilation Error|Parser Error") }

# ---------------------------------------------------------------- fixtures
$FREE_CAT = 7      # مهارات البحث العلمي (مجانية) — الاختبارات 13/14، المحاضرة المسجلة 28
$PAID_CAT = 3      # اللغة العربية (مدفوعة 200) — الاختبار 5، الواجب 5 (محاضرة 9)، المحاضرة المسجلة 12
$PAID_CAT_NAME = "اللغة العربية"
$FREE_LECTURE_RECORDED = 28
$PAID_LECTURE_RECORDED = 12
$PAID_LECTURE_LIVE = 9
$PAID_ASSIGNMENT = 5
$PAID_EXAM = 5
$FREE_EXAM = 13
$STD01 = "TEST-USR-STD-01"
$STD01_PENDING_ENGLISH = 25   # student01 / الإنجليزية (4) — يبقى معلّقاً طوال الاختبار
$STD01_PENDING_MATH = 27      # student01 / الرياضيات (6) — يُعتمد في Test 4
$STD01_FREE_CS = 26           # student01 / أساسيات الحاسوب (5) — مجانية (لا تحتاج موافقة)
$ADMIN_ID = "TEST-USR-ADMIN-01"
$stamp = (Get-Date).ToString("yyMMddHHmmss")
$qaEmail = "qa.sub.$stamp@example.test"
$qaPassword = "Qa@Tabseet2026!"
$qaName = "QA_SUB_طالب تجريبي $stamp"

$uploadDir = Join-Path $WebRoot "Uploade\Images"
$tempFiles = @()
foreach ($f in @("slides_arb_01.pdf", "slides_math_01.pdf")) {
    $p = Join-Path $uploadDir $f
    if (-not (Test-Path $p)) { Set-Content -Path $p -Value "%PDF-1.4 QA temp file" -Encoding ascii; $tempFiles += $p }
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Subscription approval - test suite" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# تنظيف بقايا تشغيل سابق
$null = Sql "DELETE FROM std_lecture WHERE Std_ID IN (SELECT Id FROM AspNetUsers WHERE Email LIKE 'qa.sub.%@example.test');"
$null = Sql "DELETE FROM Std_Reg_Category WHERE Std_ID IN (SELECT Id FROM AspNetUsers WHERE Email LIKE 'qa.sub.%@example.test');"
$null = Sql "DELETE FROM AspNetUserRoles WHERE UserId IN (SELECT Id FROM AspNetUsers WHERE Email LIKE 'qa.sub.%@example.test');"
$null = Sql "DELETE FROM AspNetUserClaims WHERE UserId IN (SELECT Id FROM AspNetUsers WHERE Email LIKE 'qa.sub.%@example.test');"
$null = Sql "DELETE FROM AspNetUsers WHERE Email LIKE 'qa.sub.%@example.test';"

$math27 = Sql "SELECT ISNULL(std_category_Ispayed,0) FROM Std_Reg_Category WHERE ID=$STD01_PENDING_MATH;"
Write-Host "Baseline: row 27 (student01/math) std_category_Ispayed=$math27"

# ============================================================== Test 1
Write-Host "`n[Test 1] تسجيل طالب جديد" -ForegroundColor Yellow
$anon = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$reg = Invoke-Post -Session $anon -Url "$BaseUrl/Home/RegesterNewStdView" -Body @{ RealName = $qaName; Gender = "1"; Email = $qaEmail; PhoneNumber = "0500000000"; Password = $qaPassword; Country = "Saudi Arabia" }
$pass = ($reg.Status -eq 200) -and ($reg.Json -ne $null) -and ($reg.Json.success -eq $true)
Record-Test -TestId "T1-01" -Description "إنشاء حساب طالب عبر RegesterNewStdView ينجح دون أي بيانات دفع" -Passed $pass -Details "status=$($reg.Status) raw=$($reg.Raw.Substring(0,[Math]::Min(120,$reg.Raw.Length)))"

$qaId = Sql "SELECT Id FROM AspNetUsers WHERE Email='$qaEmail';"
$qaRole = Sql "SELECT r.Name FROM AspNetUserRoles ur JOIN AspNetRoles r ON r.Id=ur.RoleId WHERE ur.UserId='$qaId';"
$qaRegs = [int](Sql "SELECT COUNT(*) FROM Std_Reg_Category WHERE Std_ID='$qaId';")
$pass = ($qaId -ne "") -and ($qaRole -eq "Student") -and ($qaRegs -eq 0)
Record-Test -TestId "T1-02" -Description "الحساب موجود بدور Student ولا يملك أي تسجيل في دورات" -Passed $pass -Details "id=$qaId role=$qaRole regs=$qaRegs"

$std = Get-AuthSession -Email $qaEmail -Password $qaPassword
$progPage = Get-Page -Session $std -Url "$BaseUrl/Students/CategoriesStd"
$pass = ($progPage.Status -eq 200) -and (-not (Has-ServerError $progPage.Content)) -and ($progPage.Content -match 'لا توجد برامج أو دورات مسجلة حاليًا') -and ($progPage.Content -notmatch 'id="modal-lg"') -and ($progPage.Content -notmatch 'MackPayMent|payforcategory|سداد الرسوم')
Record-Test -TestId "T1-03" -Description "تسجيل الدخول يفتح «برامجي التعليمية» مباشرة بلا طلب دفع وبلا موافقة إدارة" -Passed $pass -Details "status=$($progPage.Status) empty=$($progPage.Content -match 'لا توجد برامج') modal=$($progPage.Content -match 'modal-lg')"

$sched = Get-Page -Session $std -Url "$BaseUrl/Students/StdonlineCourse"
$pass = ($sched.Status -eq 200) -and ($sched.Content -match 'جدول الحصص') -and (-not (Has-ServerError $sched.Content))
Record-Test -TestId "T1-04" -Description "جدول الحصص متاح للطالب الجديد (دخول الموقع مجاني)" -Passed $pass -Details "status=$($sched.Status)"

# ============================================================== Test 2
Write-Host "`n[Test 2] دورة مجانية" -ForegroundColor Yellow
$pub = Get-Page -Session $anon -Url "$BaseUrl/Home/Index"
$pass = ($pub.Status -eq 200) -and ($pub.Content -match '__RequestVerificationToken') -and ($pub.Content -match 'اشترك مجانًا') -and ($pub.Content -match 'طلب الاشتراك') -and ($pub.Content -notmatch 'responseJSON\.responsetext')
Record-Test -TestId "T2-01" -Description "الصفحة الرئيسية: أزرار «اشترك مجانًا»/«طلب الاشتراك» مع رمز مكافحة التزوير" -Passed $pass -Details "status=$($pub.Status) free=$($pub.Content -match 'اشترك مجانًا') paid=$($pub.Content -match 'طلب الاشتراك')"

$stdToken = Get-Token -Session $std -Url "$BaseUrl/Students/CategoriesStd"
$r = Invoke-Post -Session $std -Url "$BaseUrl/Home/TackThisCurse" -Body @{ course_ID = $FREE_CAT; __RequestVerificationToken = $stdToken }
$pass = ($r.Status -eq 200) -and ($r.Json.success -eq $true) -and ($r.Json.state -eq "Active")
Record-Test -TestId "T2-02" -Description "الاشتراك في دورة مجانية يُفعَّل تلقائياً (state=Active)" -Passed $pass -Details "status=$($r.Status) json=$($r.Raw.Substring(0,[Math]::Min(160,$r.Raw.Length)))"

$row = Sql "SELECT CAST(ISNULL(category_Ispayed,-1) AS varchar)+'|'+CAST(ISNULL(std_category_Ispayed,-1) AS varchar)+'|'+CASE WHEN RequestedAt IS NULL THEN 'null' ELSE 'set' END FROM Std_Reg_Category WHERE Std_ID='$qaId' AND Category_ID=$FREE_CAT;"
$pass = ($row -eq "0|0|set")
Record-Test -TestId "T2-03" -Description "صف التسجيل المجاني: category_Ispayed=0, std_category_Ispayed=0, RequestedAt مسجل" -Passed $pass -Details "row=$row"

$progPage = Get-Page -Session $std -Url "$BaseUrl/Students/CategoriesStd"
$pass = ($progPage.Status -eq 200) -and ($progPage.Content -match 'data-subscription-state="Active"') -and ($progPage.Content -match "class=`"sub-data-course`"\s+data-id=`"$FREE_CAT`"") -and ($progPage.Content -match 'الاشتراك مفعّل')
Record-Test -TestId "T2-04" -Description "بطاقة الدورة المجانية تظهر «الاشتراك مفعّل» ومحاضراتها متاحة في الصفحة" -Passed $pass -Details "status=$($progPage.Status)"

$lec = Get-Page -Session $std -Url "$BaseUrl/Students/LecturesStd?cat_ID=$FREE_CAT&lectureName=QA" -NoRedirect
$pass = ($lec.Status -eq 200) -and (-not (Has-ServerError $lec.Content)) -and ($lec.Content -match 'TabseetAttendance\.track')
Record-Test -TestId "T2-05" -Description "صفحة محاضرات الدورة المجانية تُفتح مباشرة (200)" -Passed $pass -Details "status=$($lec.Status) loc=$($lec.Location)"

$r = Invoke-Post -Session $std -Url "$BaseUrl/Students/StartLecture" -Body @{ lectureId = $FREE_LECTURE_RECORDED; durationSeconds = 600; __RequestVerificationToken = $stdToken }
$pass = ($r.Status -eq 200) -and ($r.Json.success -eq $true)
Record-Test -TestId "T2-06" -Description "بدء مشاهدة محاضرة مسجلة في الدورة المجانية يُقبل (احتساب الحضور يعمل)" -Passed $pass -Details "status=$($r.Status) raw=$($r.Raw.Substring(0,[Math]::Min(120,$r.Raw.Length)))"

$r = Invoke-Post -Session $std -Url "$BaseUrl/Students/StdExams" -Body @{ Exam_ID = $FREE_EXAM; category_ID = $FREE_CAT }
$pass = ($r.Status -eq 200) -and ($r.Json -ne $null) -and ($r.Json.PSObject.Properties.Name -contains "CanStartExam")
Record-Test -TestId "T2-07" -Description "فحص متطلبات اختبار الدورة المجانية مسموح (200) ويعيد CanStartExam حسب إنجاز المحاضرات" -Passed $pass -Details "status=$($r.Status) CanStartExam=$($r.Json.CanStartExam)"

$r = Invoke-Post -Session $std -Url "$BaseUrl/Students/GetExamQuestion" -Body @{ exam_ID = $FREE_EXAM }
$pass = ($r.Status -eq 200) -and ($r.Json -is [array])
Record-Test -TestId "T2-08" -Description "أسئلة اختبار الدورة المجانية تُعاد للمشترك (200)" -Passed $pass -Details "status=$($r.Status) count=$(@($r.Json).Count)"

# ============================================================== Test 3
Write-Host "`n[Test 3] دورة مدفوعة بانتظار الموافقة" -ForegroundColor Yellow
$r = Invoke-Post -Session $std -Url "$BaseUrl/Home/TackThisCurse" -Body @{ course_ID = $PAID_CAT; __RequestVerificationToken = $stdToken }
$pass = ($r.Status -eq 200) -and ($r.Json.success -eq $true) -and ($r.Json.state -eq "PendingApproval") -and ($r.Json.response -match 'بعد تأكيد الإدارة')
Record-Test -TestId "T3-01" -Description "طلب الاشتراك في دورة مدفوعة يُسجَّل «بانتظار موافقة الإدارة» دون نموذج دفع" -Passed $pass -Details "status=$($r.Status) json=$($r.Raw.Substring(0,[Math]::Min(200,$r.Raw.Length)))"

$qaRegId = Sql "SELECT ID FROM Std_Reg_Category WHERE Std_ID='$qaId' AND Category_ID=$PAID_CAT;"
$row = Sql "SELECT CAST(ISNULL(category_Ispayed,-1) AS varchar)+'|'+CAST(ISNULL(std_category_Ispayed,-1) AS varchar)+'|'+CASE WHEN RequestedAt IS NULL THEN 'null' ELSE 'set' END+'|'+ISNULL(CAST(PayMent_Method AS varchar),'null') FROM Std_Reg_Category WHERE ID=$qaRegId;"
$pass = ($qaRegId -ne "") -and ($row -eq "1|0|set|null")
Record-Test -TestId "T3-02" -Description "صف الدورة المدفوعة: category_Ispayed=1, std_category_Ispayed=0, RequestedAt مسجل، بلا بيانات دفع" -Passed $pass -Details "regId=$qaRegId row=$row"

$progPage = Get-Page -Session $std -Url "$BaseUrl/Students/CategoriesStd"
$pass = ($progPage.Status -eq 200) -and ($progPage.Content -match 'data-subscription-state="PendingApproval"') -and ($progPage.Content -match 'بانتظار موافقة الإدارة') -and ($progPage.Content -match 'تم استلام طلب اشتراكك') -and ($progPage.Content -notmatch "class=`"sub-data-course`"\s+data-id=`"$PAID_CAT`"") -and ($progPage.Content -notmatch 'join/arb')
Record-Test -TestId "T3-03" -Description "البطاقة تعرض «بانتظار موافقة الإدارة» ولا تُرسل أي روابط محاضرات للدورة المعلّقة" -Passed $pass -Details "status=$($progPage.Status) pendingBadge=$($progPage.Content -match 'PendingApproval') leak=$($progPage.Content -match 'join/arb')"

$p = Get-Page -Session $std -Url "$BaseUrl/Students/LecturesStd?cat_ID=$PAID_CAT&lectureName=QA" -NoRedirect
$pass = ($p.Status -eq 302) -and ($p.Location -match 'CategoriesStd')
Record-Test -TestId "T3-04" -Description "الرابط المباشر لمحاضرات الدورة المعلّقة يُعاد توجيهه (302 → CategoriesStd)" -Passed $pass -Details "status=$($p.Status) loc=$($p.Location)"

$p = Get-Page -Session $std -Url "$BaseUrl/Students/Exams?examID=$PAID_EXAM" -NoRedirect
$p2 = Get-Page -Session $std -Url "$BaseUrl/Students/Courses?Deplom_ID=$PAID_CAT" -NoRedirect
$p3 = Get-Page -Session $std -Url "$BaseUrl/Students/ShowSupDeploamCourse?cat_ID=$PAID_CAT" -NoRedirect
$p4 = Get-Page -Session $std -Url "$BaseUrl/Students/AssignmentQuestion?A_Q=$PAID_ASSIGNMENT" -NoRedirect
$pass = ($p.Status -eq 302) -and ($p2.Status -eq 302) -and ($p3.Status -eq 302) -and ($p4.Status -eq 302)
Record-Test -TestId "T3-05" -Description "صفحات الاختبار/الدورات/الدبلوم/الواجب للدورة المعلّقة تُرفض بالرابط المباشر" -Passed $pass -Details "exam=$($p.Status) courses=$($p2.Status) sub=$($p3.Status) assignment=$($p4.Status)"

$r1 = Invoke-Post -Session $std -Url "$BaseUrl/Students/StdExams" -Body @{ Exam_ID = $PAID_EXAM; category_ID = $PAID_CAT }
$r2 = Invoke-Post -Session $std -Url "$BaseUrl/Students/GetExamQuestion" -Body @{ exam_ID = $PAID_EXAM }
$r3 = Invoke-Post -Session $std -Url "$BaseUrl/Students/SubmetExamQuestion" -Body @{ Exam_ID = $PAID_EXAM; "L_Degry[0]" = "100" }
$examFlag = Sql "SELECT ISNULL(std_exam_isFinish,-1) FROM Std_Reg_Category WHERE ID=$qaRegId;"
$pass = ($r1.Status -eq 403) -and ($r2.Status -eq 403) -and ($r3.Status -eq 403) -and ($examFlag -eq "-1") -and ($r2.Raw -notmatch 'Q_Text')
Record-Test -TestId "T3-06" -Description "API الاختبار (StdExams/GetExamQuestion/SubmetExamQuestion) ترفض الدورة المعلّقة بـ 403 دون تسريب أسئلة" -Passed $pass -Details "stdExams=$($r1.Status) getQ=$($r2.Status) submit=$($r3.Status) examFlag=$examFlag"

$r1 = Invoke-Post -Session $std -Url "$BaseUrl/Students/GetAssignmentQuestion" -Body @{ A_Q = $PAID_ASSIGNMENT }
$r2 = Invoke-Post -Session $std -Url "$BaseUrl/Students/SubmetQuestion" -Body @{ ass_ID = $PAID_ASSIGNMENT; "L_Degry[0]" = "100" }
$pass = ($r1.Status -eq 403) -and ($r2.Status -eq 403)
Record-Test -TestId "T3-07" -Description "API الواجبات (GetAssignmentQuestion/SubmetQuestion) ترفض الدورة المعلّقة بـ 403" -Passed $pass -Details "get=$($r1.Status) submit=$($r2.Status)"

$r1 = Invoke-Post -Session $std -Url "$BaseUrl/Students/StartLecture" -Body @{ lectureId = $PAID_LECTURE_RECORDED; durationSeconds = 600; __RequestVerificationToken = $stdToken }
$r2 = Invoke-Post -Session $std -Url "$BaseUrl/Students/JoinLiveLecture" -Body @{ lectureId = $PAID_LECTURE_LIVE; __RequestVerificationToken = $stdToken }
$r3 = Invoke-Post -Session $std -Url "$BaseUrl/Students/LectureHeartbeat" -Body @{ lectureId = $PAID_LECTURE_RECORDED; playedSeconds = 30; __RequestVerificationToken = $stdToken }
$pass = ($r1.Status -eq 403) -and ($r2.Status -eq 403) -and ($r3.Status -eq 403)
Record-Test -TestId "T3-08" -Description "نقاط الحضور (StartLecture/JoinLiveLecture/LectureHeartbeat) ترفض الدورة المعلّقة بـ 403" -Passed $pass -Details "start=$($r1.Status) join=$($r2.Status) hb=$($r3.Status)"

$f = Get-Page -Session $std -Url "$BaseUrl/Uploade/Images/slides_arb_01.pdf" -NoRedirect
$pass = ($f.Status -eq 403)
Record-Test -TestId "T3-09" -Description "مرفق محاضرة الدورة المعلّقة داخل Uploade/Images محمي على الخادم (403)" -Passed $pass -Details "status=$($f.Status)"

$sched = Get-Page -Session $std -Url "$BaseUrl/Students/StdonlineCourse"
$pass = ($sched.Status -eq 200) -and ($sched.Content -notmatch 'join/arb') -and ($sched.Content -match 'join/res')
Record-Test -TestId "T3-10" -Description "جدول الحصص يعرض الدورة المجانية المفعّلة ولا يعرض روابط الدورة المعلّقة" -Passed $pass -Details "status=$($sched.Status) arb=$($sched.Content -match 'join/arb') res=$($sched.Content -match 'join/res')"

$r = Invoke-Post -Session $std -Url "$BaseUrl/Students/payforcategory" -Body @{ Category_ID = $PAID_CAT; PayMent_Method = 3; PayMent_Value = "1" }
$row = Sql "SELECT ISNULL(CAST(PayMent_Method AS varchar),'null')+'|'+ISNULL(PayMent_Value,'null') FROM Std_Reg_Category WHERE ID=$qaRegId;"
$pass = ($r.Status -eq 410) -and ($row -eq "null|null")
Record-Test -TestId "T3-11" -Description "مسار الدفع القديم payforcategory مُعطَّل (410) ولا يغيّر أي بيانات" -Passed $pass -Details "status=$($r.Status) row=$row"

# ============================================================== Test 4
Write-Host "`n[Test 4] موافقة الإدارة" -ForegroundColor Yellow
$admin = Get-AuthSession -Email "admin.test@example.test" -Password "Admin@Tabseet2026!"
$ac = Get-Page -Session $admin -Url "$BaseUrl/Controlpanel/ActiveCourse"
$pass = ($ac.Status -eq 200) -and ($ac.Content -match 'إدارة طلبات الاشتراك') -and ($ac.Content -match "data-registration-id=`"$qaRegId`"") -and ($ac.Content -match [regex]::Escape($PAID_CAT_NAME)) -and ($ac.Content -match 'تأكيد الدفع وتفعيل الاشتراك') -and ($ac.Content -notmatch 'تفعيل طلبات الدفع')
Record-Test -TestId "T4-01" -Description "صفحة «إدارة طلبات الاشتراك» تعرض طلب الطالب الجديد مع زر التأكيد" -Passed $pass -Details "status=$($ac.Status) hasRow=$($ac.Content -match $qaRegId)"
$adminToken = [regex]::Match($ac.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value

$r = Invoke-Post -Session $admin -Url "$BaseUrl/Controlpanel/ActiveStudenCourse" -Body @{ Reg_ID = $qaRegId; confirm = 1; __RequestVerificationToken = $adminToken } -NoRedirect
$after = Get-Page -Session $admin -Url "$BaseUrl/Controlpanel/ActiveCourse"
$pass = ($r.Status -eq 302) -and ($after.Content -match 'تم تأكيد الدفع وتفعيل اشتراك الطالب')
Record-Test -TestId "T4-02" -Description "POST تأكيد الدفع وتفعيل الاشتراك ينجح ويعرض رسالة نجاح" -Passed $pass -Details "status=$($r.Status) loc=$($r.Location)"

$row = Sql "SELECT CAST(ISNULL(std_category_Ispayed,-1) AS varchar)+'|'+ISNULL(ApprovedBy,'null')+'|'+CASE WHEN ApprovedAt IS NULL THEN 'null' ELSE 'set' END FROM Std_Reg_Category WHERE ID=$qaRegId;"
$pass = ($row -eq "1|$ADMIN_ID|set")
Record-Test -TestId "T4-03" -Description "قاعدة البيانات: std_category_Ispayed=1 مع معرّف المدير وتاريخ الموافقة" -Passed $pass -Details "row=$row"

$pass = ($after.Status -eq 200) -and ($after.Content -notmatch "data-registration-id=`"$qaRegId`"")
Record-Test -TestId "T4-04" -Description "الطلب يختفي من قائمة الطلبات المعلّقة بعد التفعيل" -Passed $pass -Details "status=$($after.Status)"

$progPage = Get-Page -Session $std -Url "$BaseUrl/Students/CategoriesStd"
$pass = ($progPage.Status -eq 200) -and ($progPage.Content -match "class=`"sub-data-course`"\s+data-id=`"$PAID_CAT`"") -and ($progPage.Content -match 'join/arb')
Record-Test -TestId "T4-05" -Description "صفحة الطالب (نفس الجلسة دون إعادة دخول) تعرض الدورة مفعّلة مع محاضراتها" -Passed $pass -Details "status=$($progPage.Status)"

$lec = Get-Page -Session $std -Url "$BaseUrl/Students/LecturesStd?cat_ID=$PAID_CAT&lectureName=QA" -NoRedirect
$r = Invoke-Post -Session $std -Url "$BaseUrl/Students/StdExams" -Body @{ Exam_ID = $PAID_EXAM; category_ID = $PAID_CAT }
$f = Get-Page -Session $std -Url "$BaseUrl/Uploade/Images/slides_arb_01.pdf" -NoRedirect
$pass = ($lec.Status -eq 200) -and ($r.Status -eq 200) -and ($f.Status -eq 200)
Record-Test -TestId "T4-06" -Description "بعد الموافقة: المحاضرات (200) والاختبار API (200) والمرفق المحمي (200)" -Passed $pass -Details "lectures=$($lec.Status) exam=$($r.Status) file=$($f.Status)"

# بعد الموافقة: نقاط الحضور التي رُفضت بـ 403 في T3-08 تقبل الطالب الآن (بدء محاضرة مسجلة يُنشئ صف حضور؛ دخول المباشرة يُقيَّم بموعدها لا بالاشتراك)
$r1 = Invoke-Post -Session $std -Url "$BaseUrl/Students/StartLecture" -Body @{ lectureId = $PAID_LECTURE_RECORDED; durationSeconds = 600; __RequestVerificationToken = $stdToken }
$r2 = Invoke-Post -Session $std -Url "$BaseUrl/Students/JoinLiveLecture" -Body @{ lectureId = $PAID_LECTURE_LIVE; __RequestVerificationToken = $stdToken }
$started = Sql "SELECT COUNT(*) FROM std_lecture WHERE Std_ID='$qaId' AND Lecture_ID=$PAID_LECTURE_RECORDED AND StartedAt IS NOT NULL;"
$pass = ($r1.Status -eq 200) -and ($r1.Json.success -eq $true) -and ([int]$started -eq 1) -and ($r2.Status -eq 200) -and ($null -ne $r2.Json) -and ($r2.Json.PSObject.Properties.Name -contains 'success')
Record-Test -TestId "T4-08" -Description "بعد الموافقة: الطالب يبدأ محاضرة مسجلة في الدورة المعتمدة (StartLecture 200 + صف حضور) ودخول المباشرة لم يعد 403" -Passed $pass -Details "start=$($r1.Status) started=$started join=$($r2.Status) joinSuccess=$($r2.Json.success) joinMsg=$($r2.Json.response)"

$r = Invoke-Post -Session $admin -Url "$BaseUrl/Controlpanel/ActiveStudenCourse" -Body @{ Reg_ID = $STD01_PENDING_MATH; confirm = 1; __RequestVerificationToken = $adminToken } -NoRedirect
$row = Sql "SELECT ISNULL(std_category_Ispayed,0) FROM Std_Reg_Category WHERE ID=$STD01_PENDING_MATH;"
$pass = ($r.Status -eq 302) -and ($row -eq "1")
Record-Test -TestId "T4-07" -Description "اعتماد طلب student01 في الرياضيات (الصف 27) — يعيد توافق سكربتات QA القديمة" -Passed $pass -Details "status=$($r.Status) std=$row"

# ============================================================== Test 5
Write-Host "`n[Test 5] محاولات تجاوز الصلاحيات" -ForegroundColor Yellow
$before = Sql "SELECT ISNULL(std_category_Ispayed,0) FROM Std_Reg_Category WHERE ID=$STD01_PENDING_ENGLISH;"
$r = Invoke-Post -Session $std -Url "$BaseUrl/Controlpanel/ActiveStudenCourse" -Body @{ Reg_ID = $STD01_PENDING_ENGLISH; confirm = 1; __RequestVerificationToken = $stdToken } -NoRedirect
$afterRow = Sql "SELECT ISNULL(std_category_Ispayed,0) FROM Std_Reg_Category WHERE ID=$STD01_PENDING_ENGLISH;"
$pass = ($r.Status -eq 403) -and ($afterRow -eq $before) -and ($afterRow -eq "0")
Record-Test -TestId "T5-01" -Description "حساب طالب يحاول اعتماد طلب (طلب طالب آخر) → 403 ولا تغيير" -Passed $pass -Details "status=$($r.Status) row=$afterRow"

$teacher = Get-AuthSession -Email "teacher.ibrahim@example.test" -Password "Teach@Tabseet2026!"
$teacherToken = Get-Token -Session $teacher -Url "$BaseUrl/Home/Index"
$r = Invoke-Post -Session $teacher -Url "$BaseUrl/Controlpanel/ActiveStudenCourse" -Body @{ Reg_ID = $STD01_PENDING_ENGLISH; confirm = 1; __RequestVerificationToken = $teacherToken } -NoRedirect
$afterRow = Sql "SELECT ISNULL(std_category_Ispayed,0) FROM Std_Reg_Category WHERE ID=$STD01_PENDING_ENGLISH;"
$pass = ($r.Status -eq 403) -and ($afterRow -eq "0")
Record-Test -TestId "T5-02" -Description "حساب مدرس يحاول اعتماد طلب → 403 ولا تغيير" -Passed $pass -Details "status=$($r.Status) row=$afterRow token=$([bool]$teacherToken)"

$anonToken = [regex]::Match((Get-Page -Session $anon -Url "$BaseUrl/Home/Index").Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value
$r = Invoke-Post -Session $anon -Url "$BaseUrl/Controlpanel/ActiveStudenCourse" -Body @{ Reg_ID = $STD01_PENDING_ENGLISH; confirm = 1; __RequestVerificationToken = $anonToken } -NoRedirect
$afterRow = Sql "SELECT ISNULL(std_category_Ispayed,0) FROM Std_Reg_Category WHERE ID=$STD01_PENDING_ENGLISH;"
$pass = ($r.Status -eq 302) -and ($r.Location -match 'LogIn') -and ($afterRow -eq "0")
Record-Test -TestId "T5-03" -Description "اعتماد دون تسجيل دخول → إعادة توجيه لتسجيل الدخول ولا تغيير" -Passed $pass -Details "status=$($r.Status) loc=$($r.Location) row=$afterRow"

$countBefore = [int](Sql "SELECT COUNT(*) FROM Std_Reg_Category;")
$r = Invoke-Post -Session $anon -Url "$BaseUrl/Home/TackThisCurse" -Body @{ course_ID = $FREE_CAT; __RequestVerificationToken = $anonToken }
$countAfter = [int](Sql "SELECT COUNT(*) FROM Std_Reg_Category;")
$pass = ($r.Status -eq 200) -and ($r.Json.requiresLogin -eq $true) -and ($r.Json.success -eq $false) -and ($countAfter -eq $countBefore)
Record-Test -TestId "T5-04" -Description "طلب اشتراك دون تسجيل دخول → requiresLogin دون إنشاء أي صف" -Passed $pass -Details "status=$($r.Status) json=$($r.Raw.Substring(0,[Math]::Min(120,$r.Raw.Length)))"

$r = Invoke-Post -Session $std -Url "$BaseUrl/Home/TackThisCurse" -Body @{ course_ID = $PAID_CAT; __RequestVerificationToken = $stdToken }
$dup = [int](Sql "SELECT COUNT(*) FROM Std_Reg_Category WHERE Std_ID='$qaId' AND Category_ID=$PAID_CAT;")
$pass = ($r.Status -eq 200) -and ($r.Json.success -eq $false) -and ($r.Json.alreadyRegistered -eq $true) -and ($dup -eq 1)
Record-Test -TestId "T5-05" -Description "تكرار الاشتراك في دورة مفعّلة يُرفض (alreadyRegistered) دون صف مكرر" -Passed $pass -Details "status=$($r.Status) rows=$dup"

$std01Session = Get-AuthSession -Email "student01@example.test" -Password "Std@Tabseet2026!"
$std01Token = Get-Token -Session $std01Session -Url "$BaseUrl/Students/CategoriesStd"
$r = Invoke-Post -Session $std01Session -Url "$BaseUrl/Home/TackThisCurse" -Body @{ course_ID = 4; __RequestVerificationToken = $std01Token }
$dup = [int](Sql "SELECT COUNT(*) FROM Std_Reg_Category WHERE Std_ID='$STD01' AND Category_ID=4;")
$pass = ($r.Status -eq 200) -and ($r.Json.success -eq $false) -and ($r.Json.alreadyRegistered -eq $true) -and ($r.Json.state -eq "PendingApproval") -and ($dup -eq 1)
Record-Test -TestId "T5-06" -Description "تكرار طلب اشتراك معلّق يُرفض ويعيد الحالة PendingApproval دون صف مكرر" -Passed $pass -Details "status=$($r.Status) rows=$dup state=$($r.Json.state)"

$r = Get-Page -Session $admin -Url "$BaseUrl/Controlpanel/ActiveStudenCourse?Reg_ID=$STD01_PENDING_ENGLISH" -NoRedirect
$afterRow = Sql "SELECT ISNULL(std_category_Ispayed,0) FROM Std_Reg_Category WHERE ID=$STD01_PENDING_ENGLISH;"
$pass = ($r.Status -eq 302) -and ($afterRow -eq "0")
Record-Test -TestId "T5-07" -Description "الرابط القديم GET ActiveStudenCourse لم يعد يغيّر الحالة (حتى للمدير)" -Passed $pass -Details "status=$($r.Status) row=$afterRow"

$r = Invoke-Post -Session $admin -Url "$BaseUrl/Controlpanel/ActiveStudenCourse" -Body @{ Reg_ID = 999999; confirm = 1; __RequestVerificationToken = $adminToken } -NoRedirect
$after = Get-Page -Session $admin -Url "$BaseUrl/Controlpanel/ActiveCourse"
$pass = ($r.Status -eq 302) -and ($after.Content -match 'غير موجود')
Record-Test -TestId "T5-08" -Description "اعتماد معرف غير موجود → رسالة خطأ واضحة دون استثناء" -Passed $pass -Details "status=$($r.Status)"

$freeBefore = Sql "SELECT CAST(ISNULL(category_Ispayed,-1) AS varchar)+'|'+CAST(ISNULL(std_category_Ispayed,-1) AS varchar) FROM Std_Reg_Category WHERE ID=$STD01_FREE_CS;"
$r = Invoke-Post -Session $admin -Url "$BaseUrl/Controlpanel/ActiveStudenCourse" -Body @{ Reg_ID = $STD01_FREE_CS; confirm = 1; __RequestVerificationToken = $adminToken } -NoRedirect
$after = Get-Page -Session $admin -Url "$BaseUrl/Controlpanel/ActiveCourse"
$freeAfter = Sql "SELECT CAST(ISNULL(category_Ispayed,-1) AS varchar)+'|'+CAST(ISNULL(std_category_Ispayed,-1) AS varchar) FROM Std_Reg_Category WHERE ID=$STD01_FREE_CS;"
$pass = ($r.Status -eq 302) -and ($freeAfter -eq $freeBefore) -and ($after.Content -match 'مجانية')
Record-Test -TestId "T5-09" -Description "اعتماد صف دورة مجانية بتغيير المعرف لا يغيّر شيئاً ويوضح أنها لا تحتاج موافقة" -Passed $pass -Details "before=$freeBefore after=$freeAfter"

$r = Invoke-Post -Session $admin -Url "$BaseUrl/Controlpanel/ActiveStudenCourse" -Body @{ Reg_ID = $qaRegId; confirm = 1; __RequestVerificationToken = $adminToken } -NoRedirect
$after = Get-Page -Session $admin -Url "$BaseUrl/Controlpanel/ActiveCourse"
$row = Sql "SELECT CAST(ISNULL(std_category_Ispayed,-1) AS varchar)+'|'+ISNULL(ApprovedBy,'null') FROM Std_Reg_Category WHERE ID=$qaRegId;"
$pass = ($r.Status -eq 302) -and ($after.Content -match 'مفعّل مسبقاً') -and ($row -eq "1|$ADMIN_ID")
Record-Test -TestId "T5-10" -Description "إرسال نفس طلب الموافقة مرة ثانية آمن (مفعّل مسبقاً، لا تغيير)" -Passed $pass -Details "status=$($r.Status) row=$row"

$r = Invoke-Post -Session $admin -Url "$BaseUrl/Controlpanel/ActiveStudenCourse" -Body @{ Reg_ID = $STD01_PENDING_ENGLISH; confirm = 1 } -NoRedirect
$afterRow = Sql "SELECT ISNULL(std_category_Ispayed,0) FROM Std_Reg_Category WHERE ID=$STD01_PENDING_ENGLISH;"
$pass = ($r.Status -ne 302) -and ($r.Status -ne 200) -and ($afterRow -eq "0")
Record-Test -TestId "T5-11" -Description "اعتماد بدون رمز مكافحة التزوير يُرفض (ValidateAntiForgeryToken) ولا تغيير" -Passed $pass -Details "status=$($r.Status) row=$afterRow"

$countBefore = [int](Sql "SELECT COUNT(*) FROM Std_Reg_Category;")
$r = Invoke-Post -Session $std -Url "$BaseUrl/Home/TackThisCurse" -Body @{ course_ID = 1 }
$countAfter = [int](Sql "SELECT COUNT(*) FROM Std_Reg_Category;")
$pass = ($r.Status -ne 200) -and ($countAfter -eq $countBefore)
Record-Test -TestId "T5-12" -Description "طلب اشتراك بدون رمز مكافحة التزوير يُرفض دون إنشاء صف" -Passed $pass -Details "status=$($r.Status)"

$r = Invoke-Post -Session $teacher -Url "$BaseUrl/Home/TackThisCurse" -Body @{ course_ID = 1; __RequestVerificationToken = $teacherToken }
$tRows = [int](Sql "SELECT COUNT(*) FROM Std_Reg_Category WHERE Std_ID='TEST-USR-TCH-01';")
$pass = ($r.Status -eq 200) -and ($r.Json.success -eq $false) -and ($tRows -eq 0)
Record-Test -TestId "T5-13" -Description "حساب مدرس لا يستطيع إنشاء اشتراك طالب" -Passed $pass -Details "status=$($r.Status) rows=$tRows json=$($r.Raw.Substring(0,[Math]::Min(100,$r.Raw.Length)))"

$r1 = Invoke-Post -Session $std01Session -Url "$BaseUrl/Students/GetExamQuestion" -Body @{ exam_ID = 8 }
$r2 = Invoke-Post -Session $std01Session -Url "$BaseUrl/Students/StdExams" -Body @{ Exam_ID = 8; category_ID = 1 }
$pass = ($r1.Status -eq 403) -and ($r2.Status -eq 403)
Record-Test -TestId "T5-14" -Description "فتح اختبار دورة معلّقة عبر API (حتى مع تمرير دورة أخرى مفعّلة) → 403" -Passed $pass -Details "getQ=$($r1.Status) stdExams(spoofed cat)=$($r2.Status)"

$f = Get-Page -Session $std01Session -Url "$BaseUrl/Uploade/Images/slides_math_01.pdf" -NoRedirect
$f2 = Get-Page -Session $anon -Url "$BaseUrl/Uploade/Images/slides_math_01.pdf" -NoRedirect
$pass = ($f.Status -eq 200) -and ($f2.Status -eq 302) -and ($f2.Location -match 'LogIn')
Record-Test -TestId "T5-15" -Description "ملف دورة مفعّلة يُقدَّم للمشترك (200) ويُحوَّل المجهول لتسجيل الدخول" -Passed $pass -Details "student=$($f.Status) anon=$($f2.Status) loc=$($f2.Location)"

# ============================================================== Test 6
Write-Host "`n[Test 6] عدم التأثير على الميزات السابقة" -ForegroundColor Yellow
$lec10 = Get-Page -Session $std01Session -Url "$BaseUrl/Students/LecturesStd?cat_ID=10&lectureName=QA"
$pass = ($lec10.Status -eq 200) -and ($lec10.Content -match 'Scripts/tabseet-youtube\.js') -and ($lec10.Content -match 'TabseetAttendance\.track') -and ($lec10.Content -match 'js-open-lecture')
Record-Test -TestId "T6-01" -Description "صفحة المحاضرات: مشغّل YouTube وتتبع الحضور موجودان للدورة المفعّلة" -Passed $pass -Details "status=$($lec10.Status)"

$null = Sql "UPDATE std_lecture SET IsFinish=0, IsTimeEnd=0, StartedAt=NULL, LastHeartbeatAt=NULL, WatchedSeconds=0, RequiredSeconds=NULL, CompletedAt=NULL, Source=NULL WHERE Std_ID='$STD01' AND Lecture_ID=147;"
$r1 = Invoke-Post -Session $std01Session -Url "$BaseUrl/Students/StartLecture" -Body @{ lectureId = 147; durationSeconds = 600; __RequestVerificationToken = $std01Token }
$r2 = Invoke-Post -Session $std01Session -Url "$BaseUrl/Students/LectureHeartbeat" -Body @{ lectureId = 147; playedSeconds = 5; __RequestVerificationToken = $std01Token }
$watched = Sql "SELECT WatchedSeconds FROM std_lecture WHERE Std_ID='$STD01' AND Lecture_ID=147;"
$pass = ($r1.Status -eq 200) -and ($r1.Json.success -eq $true) -and ($r2.Status -eq 200) -and ($r2.Json.success -eq $true) -and ([int]$watched -ge 1)
Record-Test -TestId "T6-02" -Description "احتساب الحضور (StartLecture + LectureHeartbeat) يعمل للدورة المفعّلة" -Passed $pass -Details "start=$($r1.Status) hb=$($r2.Status) watched=$watched"
$null = Sql "UPDATE std_lecture SET IsFinish=1, IsTimeEnd=1, IsAssingmetFinish=NULL, StartedAt=NULL, LastHeartbeatAt=NULL, WatchedSeconds=0, RequiredSeconds=NULL, CompletedAt=NULL, Source='Legacy', MarkedBy=NULL, MarkedAt=NULL WHERE Std_ID='$STD01' AND Lecture_ID=147;"

$sched01 = Get-Page -Session $std01Session -Url "$BaseUrl/Students/StdonlineCourse"
$r = Invoke-Post -Session $std01Session -Url "$BaseUrl/Students/JoinLiveLecture" -Body @{ lectureId = 21; __RequestVerificationToken = $std01Token }
$pass = ($sched01.Status -eq 200) -and ($sched01.Content -match 'join/math') -and ($sched01.Content -match 'join/cs') -and ($sched01.Content -notmatch 'join/eng') -and ($r.Status -eq 200) -and ($r.Json -ne $null) -and ($r.Json.PSObject.Properties.Name -contains 'success')
Record-Test -TestId "T6-03" -Description "روابط Zoom للدورات المفعّلة تظهر في الجدول وتسجيل الدخول للمحاضرة المباشرة يستجيب (200)" -Passed $pass -Details "sched=$($sched01.Status) math=$($sched01.Content -match 'join/math') eng(pending)=$($sched01.Content -match 'join/eng') join=$($r.Status) joinSuccess=$($r.Json.success)"

$ap = Get-Page -Session $std01Session -Url "$BaseUrl/Students/AssignmentQuestion?A_Q=1"
$r = Invoke-Post -Session $std01Session -Url "$BaseUrl/Students/GetAssignmentQuestion" -Body @{ A_Q = 1 }
$pass = ($ap.Status -eq 200) -and (-not (Has-ServerError $ap.Content)) -and ($r.Status -eq 200) -and ($r.Json -is [array]) -and (@($r.Json).Count -gt 0)
Record-Test -TestId "T6-04" -Description "صفحة الواجب وأسئلته تعمل للدورة المفعّلة (أُصلح خطأ ترجمة قديم في الصفحة)" -Passed $pass -Details "page=$($ap.Status) api=$($r.Status) q=$(@($r.Json).Count)"

$ex = Get-Page -Session $std01Session -Url "$BaseUrl/Students/Exams?examID=2"
$r = Invoke-Post -Session $std01Session -Url "$BaseUrl/Students/GetExamQuestion" -Body @{ exam_ID = 2 }
$pass = ($ex.Status -eq 200) -and (-not (Has-ServerError $ex.Content)) -and ($r.Status -eq 200) -and ($r.Json -is [array])
Record-Test -TestId "T6-05" -Description "صفحة الاختبار وأسئلته تعمل للدورة المفعّلة" -Passed $pass -Details "page=$($ex.Status) api=$($r.Status)"

$cert = Get-Page -Session $std01Session -Url "$BaseUrl/Students/Certfications"
$certImg = Get-Page -Session $std01Session -Url "$BaseUrl/Students/Certficationsstd?Categores_ID=1&type=1" -NoRedirect
$pass = ($cert.Status -eq 200) -and ($cert.Content -match 'تعليم القرآن الكريم والتجويد') -and ($certImg.Status -eq 200) -and ($certImg.ContentType -match 'image/png')
Record-Test -TestId "T6-06" -Description "صفحة الشهادات وتحميل شهادة الطالب المؤهل يعملان" -Passed $pass -Details "page=$($cert.Status) img=$($certImg.Status) type=$($certImg.ContentType)"

$notif = Get-Page -Session $admin -Url "$BaseUrl/AdminNotifications/Index"
$cp = Get-Page -Session $admin -Url "$BaseUrl/Controlpanel/Index"
$pass = ($notif.Status -eq 200) -and (-not (Has-ServerError $notif.Content)) -and ($cp.Status -eq 200) -and (-not (Has-ServerError $cp.Content))
Record-Test -TestId "T6-07" -Description "إشعارات الإدارة ولوحة الإحصائيات تعملان" -Passed $pass -Details "notifications=$($notif.Status) index=$($cp.Status)"

$tc = Get-Page -Session $teacher -Url "$BaseUrl/Teacher/TeacherCourse"
$tp = Get-Page -Session $teacher -Url "$BaseUrl/Teacher/TeacherCoursestd?ID=10"
$pass = ($tc.Status -eq 200) -and (-not (Has-ServerError $tc.Content)) -and ($tp.Status -eq 200) -and ($tp.Content -match 'student01@example.test')
Record-Test -TestId "T6-08" -Description "صفحات المدرس (دوراته وتقدم الطلاب) تعمل" -Passed $pass -Details "courses=$($tc.Status) progress=$($tp.Status)"

$icon = Get-Page -Session $anon -Url "$BaseUrl/Uploade/Images/cert_math_ar.png" -NoRedirect
$pass = ($icon.Status -eq 200) -and ($icon.ContentType -match 'image/png')
Record-Test -TestId "T6-09" -Description "الملفات العامة في Uploade/Images (شعارات/قوالب) تُقدَّم للجميع كما كانت" -Passed $pass -Details "status=$($icon.Status) type=$($icon.ContentType)"

$huda = Get-AuthSession -Email "teacher.huda@example.test" -Password "Teach@Tabseet2026!"
$fh = Get-Page -Session $huda -Url "$BaseUrl/Uploade/Images/slides_math_01.pdf" -NoRedirect
$fi = Get-Page -Session $teacher -Url "$BaseUrl/Uploade/Images/slides_arb_01.pdf" -NoRedirect
$fa = Get-Page -Session $admin -Url "$BaseUrl/Uploade/Images/slides_arb_01.pdf" -NoRedirect
$pass = ($fh.Status -eq 200) -and ($fi.Status -eq 403) -and ($fa.Status -eq 200)
Record-Test -TestId "T6-10" -Description "ملفات الدورات: المدرس المسند (200)، مدرس غير مسند (403)، الإدارة (200)" -Passed $pass -Details "huda(cat6)=$($fh.Status) ibrahim(cat3)=$($fi.Status) admin=$($fa.Status)"

$pub2 = Get-Page -Session $anon -Url "$BaseUrl/Home/MoreCourse"
$pass = ($pub2.Status -eq 200) -and ($pub2.Content -match '__RequestVerificationToken') -and ($pub2.Content -match 'function TackThisCourse') -and ($pub2.Content -match 'requiresLogin') -and ($pub2.Content -match 'اشترك مجانًا')
Record-Test -TestId "T6-11" -Description "صفحة كل الدورات تحمّل آلية الاشتراك الجديدة مع الرمز" -Passed $pass -Details "status=$($pub2.Status)"

# ============================================================== cleanup
Write-Host "`n[Cleanup]" -ForegroundColor Yellow
$null = Sql "DELETE FROM std_lecture WHERE Std_ID='$qaId';"
$null = Sql "DELETE FROM Std_Reg_Category WHERE Std_ID='$qaId';"
$null = Sql "DELETE FROM AspNetUserRoles WHERE UserId='$qaId';"
$null = Sql "DELETE FROM AspNetUserClaims WHERE UserId='$qaId';"
$null = Sql "DELETE FROM AspNetUsers WHERE Id='$qaId';"
foreach ($p in $tempFiles) { Remove-Item -Path $p -Force -ErrorAction SilentlyContinue }
if ($RevertStudent01Math) {
    $null = Sql "UPDATE Std_Reg_Category SET std_category_Ispayed=0, ApprovedAt=NULL, ApprovedBy=NULL WHERE ID=$STD01_PENDING_MATH;"
    Write-Host "Row 27 (student01/math) reverted to pending."
} else {
    Write-Host "Row 27 (student01/math) left ACTIVE (approved by admin in T4-07); pass -RevertStudent01Math to undo."
}
Write-Host "QA student removed: $qaEmail"

Write-Host "`n==========================================================" -ForegroundColor Cyan
$passed = ($results | Where-Object { $_.Result -eq "PASS" }).Count
$failed = ($results | Where-Object { $_.Result -eq "FAIL" }).Count
Write-Host "TOTAL: $($results.Count)   PASS: $passed   FAIL: $failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
$results | Format-Table -AutoSize -Wrap | Out-String -Width 220 | Write-Host
