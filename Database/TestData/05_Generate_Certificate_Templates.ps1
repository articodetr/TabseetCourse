<#
.SYNOPSIS
    Generates placeholder certificate template images for the test categories.

.DESCRIPTION
    02_Seed_TestData.sql points Categories.CertificationAR / CertificationEN at files such as
    cert_quran_ar.png that are not part of the repository (real templates are uploaded by an admin).
    Without them /Students/Certficationsstd cannot render a certificate locally.

    This script reads every category that has a template file name, and for each file that does NOT
    exist in tabseetCourse\Uploade\Images it draws a simple placeholder:
      * Arabic certificate (CertificationAR): A4 landscape 1754x1240 px
      * English ijazah     (CertificationEN): A4 portrait  1240x1754 px
    Grey guide labels mark where StudentsController.Certficationsstd draws the student name,
    appreciation, dates and serial number.

    Existing files are never overwritten, so real templates uploaded through the control panel are safe.

.EXAMPLE
    pwsh -File Database\TestData\05_Generate_Certificate_Templates.ps1
    pwsh -File Database\TestData\05_Generate_Certificate_Templates.ps1 -Server . -Database TabseetDB_Local
#>
[CmdletBinding()]
param(
    [string]$Server = ".",
    [string]$Database = "TabseetDB_Local",
    [string]$ImagesDir = (Join-Path $PSScriptRoot "..\..\tabseetCourse\Uploade\Images")
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$ImagesDir = [System.IO.Path]::GetFullPath($ImagesDir)
if (-not (Test-Path $ImagesDir)) {
    throw "Images folder not found: $ImagesDir"
}

# ---------------------------------------------------------------------------
# Read the categories that reference a template file
# ---------------------------------------------------------------------------
$connectionString = "Data Source=$Server;Initial Catalog=$Database;Integrated Security=True;TrustServerCertificate=True"
$categories = @()
$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
try {
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = @"
SELECT ID, NameAr, NameEn, CertificationAR, CertificationEN
FROM dbo.Categories
WHERE CertificationAR IS NOT NULL OR CertificationEN IS NOT NULL
ORDER BY ID
"@
    $reader = $command.ExecuteReader()
    while ($reader.Read()) {
        $categories += [pscustomobject]@{
            ID              = $reader["ID"]
            NameAr          = [string]$reader["NameAr"]
            NameEn          = [string]$reader["NameEn"]
            CertificationAR = if ($reader["CertificationAR"] -is [DBNull]) { $null } else { [string]$reader["CertificationAR"] }
            CertificationEN = if ($reader["CertificationEN"] -is [DBNull]) { $null } else { [string]$reader["CertificationEN"] }
        }
    }
    $reader.Close()
}
finally {
    $connection.Dispose()
}

# ---------------------------------------------------------------------------
# Drawing helpers
# ---------------------------------------------------------------------------
$gold  = [System.Drawing.ColorTranslator]::FromHtml("#927123")
$grey  = [System.Drawing.ColorTranslator]::FromHtml("#9E9E9E")
$ink   = [System.Drawing.ColorTranslator]::FromHtml("#2B2B2B")

function New-CenteredFormat {
    $format = New-Object System.Drawing.StringFormat
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    return $format
}

function Write-CenteredText {
    param($Graphics, [string]$Text, [string]$FontName, [float]$Size, [System.Drawing.Color]$Color, [int]$X, [int]$Y, [System.Drawing.FontStyle]$Style = [System.Drawing.FontStyle]::Regular)
    $font = New-Object System.Drawing.Font($FontName, $Size, $Style)
    $brush = New-Object System.Drawing.SolidBrush($Color)
    $format = New-CenteredFormat
    try {
        $Graphics.DrawString($Text, $font, $brush, (New-Object System.Drawing.PointF($X, $Y)), $format)
    }
    finally {
        $format.Dispose(); $brush.Dispose(); $font.Dispose()
    }
}

function Write-Frame {
    param($Graphics, [int]$Width, [int]$Height)
    $Graphics.Clear([System.Drawing.Color]::White)
    $outer = New-Object System.Drawing.Pen($gold, 10)
    $inner = New-Object System.Drawing.Pen($gold, 3)
    try {
        # Keep both frames outside the serial-number line (y=1190 on the Arabic template, y=1710 on the English one).
        $Graphics.DrawRectangle($outer, 14, 14, $Width - 28, $Height - 28)
        $Graphics.DrawRectangle($inner, 26, 26, $Width - 52, $Height - 52)
    }
    finally {
        $inner.Dispose(); $outer.Dispose()
    }
}

function New-Template {
    param([string]$Path, [int]$Width, [int]$Height, [scriptblock]$Painter)
    $bitmap = New-Object System.Drawing.Bitmap($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
            $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias
            Write-Frame $graphics $Width $Height
            & $Painter $graphics
        }
        finally {
            $graphics.Dispose()
        }
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $bitmap.Dispose()
    }
}

# Positions below mirror StudentsController.Certficationsstd; labels sit ~45 px above each drawing point.
function New-ArabicTemplate {
    param([string]$Path, [string]$CategoryName)
    New-Template -Path $Path -Width 1754 -Height 1240 -Painter {
        param($g)
        Write-CenteredText $g "شهادة إتمام دورة" "Andalus" 60 $gold 877 160 ([System.Drawing.FontStyle]::Bold)
        Write-CenteredText $g $CategoryName "Andalus" 44 $ink 877 280
        Write-CenteredText $g "تشهد أكاديمية تبسيط بأن الطالب/ة" "Andalus" 30 $ink 877 420
        Write-CenteredText $g "اسم الطالب" "Andalus" 16 $grey 850 530
        Write-CenteredText $g "قد أتم/ت متطلبات الدورة بنجاح بتقدير" "Andalus" 30 $ink 877 700
        Write-CenteredText $g "التقدير" "Andalus" 16 $grey 710 765
        Write-CenteredText $g "التاريخ الهجري" "Andalus" 16 $grey 950 915
        Write-CenteredText $g "التاريخ الميلادي" "Andalus" 16 $grey 650 915
        Write-CenteredText $g "الرقم التسلسلي" "Andalus" 16 $grey 1450 1160
        Write-CenteredText $g "Placeholder template - local testing only" "Arial" 14 $grey 877 80
        Write-CenteredText $g "قالب تجريبي للاختبار المحلي" "Andalus" 16 $grey 300 1150
    }
}

function New-EnglishTemplate {
    param([string]$Path, [string]$CategoryName)
    New-Template -Path $Path -Width 1240 -Height 1754 -Painter {
        param($g)
        Write-CenteredText $g "PLACEHOLDER TEMPLATE - local testing only" "Arial" 14 $grey 620 80
        Write-CenteredText $g "Certificate of Completion" "Georgia" 54 $gold 620 220 ([System.Drawing.FontStyle]::Bold)
        Write-CenteredText $g $CategoryName "Georgia" 34 $ink 620 340
        Write-CenteredText $g "This is to certify that" "Georgia" 26 $ink 620 480
        Write-CenteredText $g "Student name" "Arial" 14 $grey 630 560
        Write-CenteredText $g "has successfully completed the requirements of this course" "Georgia" 24 $ink 620 760
        Write-CenteredText $g "Hijri date" "Arial" 14 $grey 690 1630
        Write-CenteredText $g "Gregorian date" "Arial" 14 $grey 440 1630
        Write-CenteredText $g "Serial No." "Arial" 14 $grey 630 1684
    }
}

# ---------------------------------------------------------------------------
# Generate missing files only
# ---------------------------------------------------------------------------
$created = 0
$skipped = 0
foreach ($category in $categories) {
    $targets = @()
    if ($category.CertificationAR) { $targets += @{ File = $category.CertificationAR; Kind = "AR" } }
    if ($category.CertificationEN) { $targets += @{ File = $category.CertificationEN; Kind = "EN" } }

    foreach ($target in $targets) {
        $path = Join-Path $ImagesDir $target.File
        if (Test-Path $path) {
            Write-Host ("skip    {0} (exists)" -f $target.File)
            $skipped++
            continue
        }
        if ($target.Kind -eq "AR") {
            New-ArabicTemplate -Path $path -CategoryName $category.NameAr
        }
        else {
            New-EnglishTemplate -Path $path -CategoryName $category.NameEn
        }
        Write-Host ("created {0} (category {1}: {2})" -f $target.File, $category.ID, $category.NameAr)
        $created++
    }
}

Write-Host ""
Write-Host ("Done. created={0} skipped={1} folder={2}" -f $created, $skipped, $ImagesDir)
