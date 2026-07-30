param(
    [string]$RepositoryRoot = "",
    [int]$CanvasWidth = 960,
    [int]$CanvasHeight = 540,
    [switch]$ApproveForRuntime,
    [string]$ApprovedBy = "project-owner",
    [int]$ApprovalRevision = 1
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = [System.IO.Path]::GetFullPath(
        (Join-Path $PSScriptRoot "../.."))
}

$runtimeConnected = [bool]$ApproveForRuntime
$documentApprovalStatus = if ($ApproveForRuntime) {
    "Approved"
} else {
    "PendingUserReview"
}
$approvalMetadata = $null
if ($ApproveForRuntime) {
    $resolvedApprovedBy = $ApprovedBy.Trim()
    if ([string]::IsNullOrWhiteSpace($resolvedApprovedBy)) {
        $resolvedApprovedBy = [System.Environment]::UserName
    }
    if ([string]::IsNullOrWhiteSpace($resolvedApprovedBy)) {
        throw "ApproveForRuntime requires a non-empty approval identity."
    }
    if ($ApprovalRevision -lt 1) {
        throw "ApprovalRevision must be at least 1."
    }
    $approvalMetadata = [pscustomobject][ordered]@{
        reviewer = $resolvedApprovedBy
        revision = $ApprovalRevision
        approvedAtUtc =
            [DateTime]::UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")
    }
}

$inventoryPath = Join-Path $RepositoryRoot `
    "Documentation/CharacterStagingReview/Data/background_analysis_inventory.json"
$seedPaths = @(
    (Join-Path $PSScriptRoot "semantic_geometry_head.json"),
    (Join-Path $PSScriptRoot "semantic_geometry_tail.json"),
    (Join-Path $PSScriptRoot "semantic_geometry_base.json"))
$reviewRoot = Join-Path $RepositoryRoot `
    "Documentation/CharacterStagingReview"
$dataRoot = Join-Path $reviewRoot "Data"
$backgroundRoot = Join-Path $reviewRoot "Backgrounds"
$sceneRoot = Join-Path $reviewRoot "Scenes"
$contactRoot = Join-Path $reviewRoot "ContactSheets"

if (-not (Test-Path -LiteralPath $inventoryPath)) {
    throw "Analysis inventory is missing: $inventoryPath"
}
foreach ($seedPath in $seedPaths) {
    if (-not (Test-Path -LiteralPath $seedPath)) {
        throw "Semantic geometry seed is missing: $seedPath"
    }
}

@($dataRoot, $backgroundRoot, $sceneRoot, $contactRoot) |
    ForEach-Object {
        [System.IO.Directory]::CreateDirectory($_) | Out-Null
    }

$inventory = Get-Content -LiteralPath $inventoryPath -Raw -Encoding UTF8 |
    ConvertFrom-Json
$seedProfiles = @(
    foreach ($seedPath in $seedPaths) {
        $document = Get-Content -LiteralPath $seedPath -Raw -Encoding UTF8 |
            ConvertFrom-Json
        @($document.profiles)
    })

function Get-FileSha256 {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Source file is missing: $Path"
    }

    $sha = [System.Security.Cryptography.SHA256]::Create()
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $rawHash = [System.BitConverter]::ToString(
            $sha.ComputeHash($stream))
        return $rawHash.Replace("-", "").ToLowerInvariant()
    } finally {
        $stream.Dispose()
        $sha.Dispose()
    }
}

function Get-StringSha256 {
    param([string]$Value)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $encoding = [System.Text.UTF8Encoding]::new($false)
        $bytes = $encoding.GetBytes($Value)
        $rawHash = [System.BitConverter]::ToString(
            $sha.ComputeHash($bytes))
        return $rawHash.Replace("-", "").ToLowerInvariant()
    } finally {
        $sha.Dispose()
    }
}

function Get-StableObjectSha256 {
    param([object]$Value)
    $json = $Value | ConvertTo-Json -Depth 50 -Compress
    return Get-StringSha256 $json
}

function Get-NormalizedVariantKey {
    param([string]$VariantKey)
    if ([string]::IsNullOrWhiteSpace($VariantKey)) {
        return ""
    }

    $normalized = $VariantKey.Trim().Replace("\", "/")
    if ($normalized.StartsWith(
            "serialized:",
            [StringComparison]::OrdinalIgnoreCase)) {
        $normalized = $normalized.Substring("serialized:".Length)
    }
    $slash = $normalized.LastIndexOf("/")
    if ($slash -ge 0 -and $slash -lt $normalized.Length - 1) {
        $normalized = $normalized.Substring($slash + 1)
    }
    if ($normalized.EndsWith(
            ".png",
            [StringComparison]::OrdinalIgnoreCase)) {
        $normalized = $normalized.Substring(
            0,
            $normalized.Length - 4)
    }
    return $normalized.ToLowerInvariant()
}

function Get-ProtectionVariantKey {
    param([object]$Protection)
    $property = $Protection.PSObject.Properties["variantKey"]
    if ($null -eq $property) {
        return ""
    }
    return [string]$property.Value
}

function Test-ProtectionIsPresent {
    param([object]$Protection)
    $property = $Protection.PSObject.Properties["isPresent"]
    if ($null -eq $property) {
        return $true
    }
    return [bool]$property.Value
}

function Test-ProtectionMatchesBackground {
    param(
        [object]$Protection,
        [object]$Background
    )
    $protectionVariant = Get-NormalizedVariantKey (
        Get-ProtectionVariantKey $Protection)
    if ([string]::IsNullOrWhiteSpace($protectionVariant)) {
        return $true
    }

    foreach ($backgroundVariant in @($Background.variantKeys)) {
        if ([string]::Equals(
                $protectionVariant,
                (Get-NormalizedVariantKey ([string]$backgroundVariant)),
                [StringComparison]::Ordinal)) {
            return $true
        }
    }
    return [string]::Equals(
        $protectionVariant,
        (Get-NormalizedVariantKey ([string]$Background.profileId)),
        [StringComparison]::Ordinal)
}

if ($ApproveForRuntime) {
    foreach ($background in @($inventory.backgrounds)) {
        $sourcePath = [System.IO.Path]::GetFullPath(
            (Join-Path $RepositoryRoot ([string]$background.assetPath)))
        $actualHash = Get-FileSha256 $sourcePath
        $expectedHash = [string]$background.sourceSha256
        if ([string]::IsNullOrWhiteSpace($expectedHash) -or
            -not [string]::Equals(
                $actualHash,
                $expectedHash,
                [StringComparison]::OrdinalIgnoreCase)) {
            throw (
                "Source image hash mismatch for '{0}'. " +
                "Re-export the Unity semantic inventory before approving. " +
                "Expected={1}; Actual={2}" -f
                    $background.profileId,
                    $expectedHash,
                    $actualHash)
        }
    }
}

function Clamp-Value {
    param([double]$Value, [double]$Minimum, [double]$Maximum)
    return [Math]::Max($Minimum, [Math]::Min($Maximum, $Value))
}

function New-Point {
    param([double]$X, [double]$Y)
    return [ordered]@{ x = $X; y = $Y }
}

function New-Rect {
    param(
        [double]$X,
        [double]$Y,
        [double]$Width,
        [double]$Height
    )
    return [ordered]@{
        x = $X
        y = $Y
        width = $Width
        height = $Height
    }
}

function New-ImageTransform {
    param(
        [int]$SourceWidth,
        [int]$SourceHeight,
        [ValidateSet("Fit", "Cover")]
        [string]$Mode = "Fit",
        [object]$Focus = $null,
        [double]$Zoom = 1.0
    )
    $focusX = if ($null -ne $Focus) {
        Clamp-Value ([double]$Focus.x) 0 1
    } else {
        0.5
    }
    $focusY = if ($null -ne $Focus) {
        Clamp-Value ([double]$Focus.y) 0 1
    } else {
        0.5
    }
    $fitScale = if ($Mode -eq "Cover") {
        [Math]::Max(
            $CanvasWidth / [double]$SourceWidth,
            $CanvasHeight / [double]$SourceHeight) *
            [Math]::Max(1.0, $Zoom)
    } else {
        [Math]::Min(
            $CanvasWidth / [double]$SourceWidth,
            $CanvasHeight / [double]$SourceHeight)
    }
    $drawWidth = $SourceWidth * $fitScale
    $drawHeight = $SourceHeight * $fitScale
    if ($Mode -eq "Cover") {
        $left = -$focusX * ($drawWidth - $CanvasWidth)
        $bottom = -$focusY * ($drawHeight - $CanvasHeight)
    } else {
        $left = ($CanvasWidth - $drawWidth) * 0.5
        $bottom = ($CanvasHeight - $drawHeight) * 0.5
    }

    return [pscustomobject]@{
        left = [double]$left
        bottom = [double]$bottom
        width = [double]$drawWidth
        height = [double]$drawHeight
        mode = $Mode
        focusX = $focusX
        focusY = $focusY
        zoom = [Math]::Max(1.0, $Zoom)
    }
}

function Convert-NormalizedPoint {
    param(
        [object]$Point,
        [object]$Transform
    )
    return [System.Drawing.PointF]::new(
        [single](
            [double]$Transform.left +
            [double]$Point.x * [double]$Transform.width),
        [single](
            $CanvasHeight -
            ([double]$Transform.bottom +
                [double]$Point.y * [double]$Transform.height)))
}

function Convert-NormalizedPolygon {
    param(
        [object[]]$Points,
        [object]$Transform
    )
    return [System.Drawing.PointF[]]@(
        $Points | ForEach-Object {
            Convert-NormalizedPoint $_ $Transform
        })
}

function Convert-NormalizedRect {
    param(
        [object]$Rect,
        [object]$Transform
    )
    return [System.Drawing.RectangleF]::new(
        [single](
            [double]$Transform.left +
            [double]$Rect.x * [double]$Transform.width),
        [single](
            $CanvasHeight -
            ([double]$Transform.bottom +
                ([double]$Rect.y + [double]$Rect.height) *
                [double]$Transform.height)),
        [single]([double]$Rect.width * [double]$Transform.width),
        [single]([double]$Rect.height * [double]$Transform.height))
}

function Test-PointInPolygon {
    param([object]$Point, [object[]]$Polygon)
    $inside = $false
    $j = $Polygon.Count - 1
    for ($i = 0; $i -lt $Polygon.Count; $i++) {
        $xi = [double]$Polygon[$i].x
        $yi = [double]$Polygon[$i].y
        $xj = [double]$Polygon[$j].x
        $yj = [double]$Polygon[$j].y
        $crosses = (($yi -gt [double]$Point.y) -ne
            ($yj -gt [double]$Point.y)) -and
            ([double]$Point.x -lt
                (($xj - $xi) *
                    ([double]$Point.y - $yi) /
                    (($yj - $yi) + 0.0000001) + $xi))
        if ($crosses) {
            $inside = -not $inside
        }
        $j = $i
    }
    return $inside
}

function Test-PointInZone {
    param([object]$Point, [object]$Zone)
    if ($null -ne $Zone.rect) {
        return [double]$Point.x -ge [double]$Zone.rect.x -and
            [double]$Point.x -le
                ([double]$Zone.rect.x + [double]$Zone.rect.width) -and
            [double]$Point.y -ge [double]$Zone.rect.y -and
            [double]$Point.y -le
                ([double]$Zone.rect.y + [double]$Zone.rect.height)
    }
    if ($null -ne $Zone.points -and $Zone.points.Count -ge 3) {
        return Test-PointInPolygon $Point $Zone.points
    }
    return $false
}

function Get-SourceBitmap {
    param([string]$AssetPath)
    $fullPath = Join-Path $RepositoryRoot `
        ($AssetPath.Replace("/", [System.IO.Path]::DirectorySeparatorChar))
    return [System.Drawing.Bitmap]::new($fullPath)
}

function Get-SlotGrade {
    param(
        [System.Drawing.Bitmap]$Bitmap,
        [object]$Slot
    )
    $centerX = [int](Clamp-Value (
        [double]$Slot.foot.x * $Bitmap.Width) 0 ($Bitmap.Width - 1))
    $centerY = [int](Clamp-Value (
        (1.0 - [double]$Slot.foot.y -
            [double]$Slot.normalizedHeight * 0.52) *
            $Bitmap.Height) 0 ($Bitmap.Height - 1))
    $radiusX = [Math]::Max(4, [int]($Bitmap.Width * 0.035))
    $radiusY = [Math]::Max(4, [int]($Bitmap.Height * 0.055))
    $r = 0.0
    $g = 0.0
    $b = 0.0
    $samples = 0
    for ($y = [Math]::Max(0, $centerY - $radiusY);
         $y -le [Math]::Min($Bitmap.Height - 1, $centerY + $radiusY);
         $y += 4) {
        for ($x = [Math]::Max(0, $centerX - $radiusX);
             $x -le [Math]::Min($Bitmap.Width - 1, $centerX + $radiusX);
             $x += 4) {
            $pixel = $Bitmap.GetPixel($x, $y)
            $luminance =
                (0.2126 * $pixel.R + 0.7152 * $pixel.G +
                    0.0722 * $pixel.B) / 255.0
            if ($luminance -lt 0.07 -or $luminance -gt 0.94) {
                continue
            }
            $r += $pixel.R
            $g += $pixel.G
            $b += $pixel.B
            $samples++
        }
    }

    if ($samples -eq 0) {
        $r = 205
        $g = 205
        $b = 205
        $samples = 1
    }

    $r /= $samples
    $g /= $samples
    $b /= $samples
    $luminance =
        (0.2126 * $r + 0.7152 * $g + 0.0722 * $b) / 255.0
    $maximum = [Math]::Max($r, [Math]::Max($g, $b))
    $minimum = [Math]::Min($r, [Math]::Min($g, $b))
    $chroma = ($maximum - $minimum) / 255.0

    # Blend strongly toward neutral so a local lamp or prop cannot crush skin
    # tones. This is an authoring suggestion, not a runtime color sample.
    $tintR = [int](Clamp-Value (255 * 0.72 + $r * 0.28) 0 255)
    $tintG = [int](Clamp-Value (255 * 0.72 + $g * 0.28) 0 255)
    $tintB = [int](Clamp-Value (255 * 0.72 + $b * 0.28) 0 255)
    $exposure = Clamp-Value (0.72 / [Math]::Max(0.2, $luminance)) 0.55 1.15
    $saturation = Clamp-Value (0.58 + $chroma * 0.45) 0.55 0.88

    return [ordered]@{
        tintHex = "#{0:X2}{1:X2}{2:X2}" -f $tintR, $tintG, $tintB
        sampledRgb = @(
            [Math]::Round($r / 255.0, 3),
            [Math]::Round($g / 255.0, 3),
            [Math]::Round($b / 255.0, 3))
        saturation = [Math]::Round($saturation, 3)
        exposure = [Math]::Round($exposure, 3)
        contrast = 0.86
        softness = 0.30
    }
}

function New-FallbackSeed {
    param([object]$Background)
    return [ordered]@{
        profileId = $Background.profileId
        analysisConfidence = 0.30
        approvalStatus = "NeedsReview"
        walkablePolygons = @(
            [ordered]@{
                points = @(
                    (New-Point 0.12 0.03),
                    (New-Point 0.88 0.03),
                    (New-Point 0.76 0.36),
                    (New-Point 0.24 0.36))
            })
        forbiddenZones = @()
        uncertainZones = @(
            [ordered]@{
                kind = "automatic_fallback_requires_review"
                rect = New-Rect 0.05 0.02 0.90 0.44
                points = $null
            })
        narrativeProtectedZones = @()
        perspective = [ordered]@{
            horizonY = 0.42
            vanishingPoint = New-Point 0.50 0.45
            confidence = 0.30
        }
        lighting = [ordered]@{
            direction = New-Point -0.35 0.80
            temperatureKelvin = 4300
            note = "Automatic fallback; review required."
        }
        candidateSlots = @(
            [ordered]@{
                id = "auto_near_left"
                foot = New-Point 0.26 0.07
                depth = 0.16
                normalizedHeight = 0.62
                confidence = 0.30
            },
            [ordered]@{
                id = "auto_near_right"
                foot = New-Point 0.74 0.07
                depth = 0.16
                normalizedHeight = 0.62
                confidence = 0.30
            },
            [ordered]@{
                id = "auto_mid_left"
                foot = New-Point 0.39 0.22
                depth = 0.48
                normalizedHeight = 0.50
                confidence = 0.25
            },
            [ordered]@{
                id = "auto_mid_right"
                foot = New-Point 0.61 0.22
                depth = 0.48
                normalizedHeight = 0.50
                confidence = 0.25
            })
        analysisNotes =
            "No reviewed visual seed was available; conservative fallback."
    }
}

function Draw-Zone {
    param(
        [System.Drawing.Graphics]$Graphics,
        [object]$Zone,
        [System.Drawing.Color]$FillColor,
        [System.Drawing.Color]$LineColor,
        [object]$Transform,
        [bool]$Hatched = $false
    )
    $brush = if ($Hatched) {
        [System.Drawing.Drawing2D.HatchBrush]::new(
            [System.Drawing.Drawing2D.HatchStyle]::ForwardDiagonal,
            $LineColor,
            $FillColor)
    } else {
        [System.Drawing.SolidBrush]::new($FillColor)
    }
        $pen = [System.Drawing.Pen]::new($LineColor, 2)
    try {
        if ($null -ne $Zone.points -and $Zone.points.Count -ge 3) {
            $points = Convert-NormalizedPolygon $Zone.points $Transform
            $Graphics.FillPolygon($brush, $points)
            $Graphics.DrawPolygon($pen, $points)
        } elseif ($null -ne $Zone.rect) {
            $rect = Convert-NormalizedRect $Zone.rect $Transform
            $Graphics.FillRectangle($brush, $rect)
            $Graphics.DrawRectangle(
                $pen,
                $rect.X,
                $rect.Y,
                $rect.Width,
                $rect.Height)
        }
    } finally {
        $brush.Dispose()
        $pen.Dispose()
    }
}

function Draw-ZoneLabel {
    param(
        [System.Drawing.Graphics]$Graphics,
        [object]$Zone,
        [string]$Label,
        [System.Drawing.Color]$Color,
        [object]$Transform
    )
    if ([string]::IsNullOrWhiteSpace($Label)) {
        return
    }

    $anchor = if (
        $null -ne $Zone.points -and
        $Zone.points.Count -gt 0) {
        Convert-NormalizedPoint $Zone.points[0] $Transform
    } elseif ($null -ne $Zone.rect) {
        Convert-NormalizedPoint `
            (New-Point $Zone.rect.x (
                [double]$Zone.rect.y + [double]$Zone.rect.height)) `
            $Transform
    } else {
        return
    }
    $font = [System.Drawing.Font]::new(
        "Arial",
        8,
        [System.Drawing.FontStyle]::Bold)
    $brush = [System.Drawing.SolidBrush]::new($Color)
    $back = [System.Drawing.SolidBrush]::new(
        [System.Drawing.Color]::FromArgb(185, 10, 12, 18))
    try {
        $size = $Graphics.MeasureString($Label, $font)
        $x = [single](Clamp-Value $anchor.X 1 (
            $CanvasWidth - $size.Width - 3))
        $y = [single](Clamp-Value $anchor.Y 34 (
            $CanvasHeight - 49))
        $Graphics.FillRectangle(
            $back,
            $x,
            $y,
            $size.Width + 3,
            $size.Height + 1)
        $Graphics.DrawString($Label, $font, $brush, $x + 1, $y)
    } finally {
        $font.Dispose()
        $brush.Dispose()
        $back.Dispose()
    }
}

function Draw-Silhouette {
    param(
        [System.Drawing.Graphics]$Graphics,
        [object]$Slot,
        [System.Drawing.Color]$Color,
        [string]$Label,
        [object]$Transform
    )
    $foot = Convert-NormalizedPoint $Slot.foot $Transform
    $footX = [single]$foot.X
    $footY = [single]$foot.Y
    $bodyHeight = [single](
        [double]$Slot.normalizedHeight *
        [double]$Transform.height)
    $bodyWidth = [single]($bodyHeight * 0.28)
    $head = [single]($bodyHeight * 0.115)
    $brush = [System.Drawing.SolidBrush]::new($Color)
    $outline = [System.Drawing.Pen]::new(
        [System.Drawing.Color]::FromArgb(230, 235, 248, 255),
        2)
    $font = [System.Drawing.Font]::new(
        "Arial",
        9,
        [System.Drawing.FontStyle]::Bold)
    try {
        $Graphics.FillEllipse(
            $brush,
            $footX - $head,
            $footY - $bodyHeight,
            $head * 2,
            $head * 2)
        $torsoTop = $footY - $bodyHeight + $head * 1.8
        $torsoBottom = $footY - $bodyHeight * 0.38
        $torso = [System.Drawing.PointF[]]@(
            [System.Drawing.PointF]::new(
                $footX - $bodyWidth * 0.48, $torsoTop),
            [System.Drawing.PointF]::new(
                $footX + $bodyWidth * 0.48, $torsoTop),
            [System.Drawing.PointF]::new(
                $footX + $bodyWidth * 0.30, $torsoBottom),
            [System.Drawing.PointF]::new(
                $footX + $bodyWidth * 0.12, $footY),
            [System.Drawing.PointF]::new(
                $footX - $bodyWidth * 0.12, $footY),
            [System.Drawing.PointF]::new(
                $footX - $bodyWidth * 0.30, $torsoBottom))
        $Graphics.FillPolygon($brush, $torso)
        $Graphics.DrawLine(
            $outline,
            $footX - $bodyWidth * 0.42,
            $torsoTop + $bodyHeight * 0.10,
            $footX - $bodyWidth * 0.65,
            $footY - $bodyHeight * 0.30)
        $Graphics.DrawLine(
            $outline,
            $footX + $bodyWidth * 0.42,
            $torsoTop + $bodyHeight * 0.10,
            $footX + $bodyWidth * 0.65,
            $footY - $bodyHeight * 0.30)
        $Graphics.FillEllipse(
            [System.Drawing.Brushes]::White,
            $footX - 4,
            $footY - 4,
            8,
            8)
        $Graphics.DrawString(
            $Label,
            $font,
            [System.Drawing.Brushes]::White,
            $footX - $bodyWidth * 0.55,
            [Math]::Max(2, $footY - $bodyHeight - 17))
    } finally {
        $brush.Dispose()
        $outline.Dispose()
        $font.Dispose()
    }
}

function Draw-Lighting {
    param(
        [System.Drawing.Graphics]$Graphics,
        [object]$Lighting,
        [int]$Width,
        [int]$Height
    )
    $origin = [System.Drawing.PointF]::new($Width - 72, 74)
    $scale = 44
    $end = [System.Drawing.PointF]::new(
        [single]($origin.X + [double]$Lighting.direction.x * $scale),
        [single]($origin.Y - [double]$Lighting.direction.y * $scale))
    $pen = [System.Drawing.Pen]::new(
        [System.Drawing.Color]::FromArgb(255, 255, 230, 120),
        4)
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::ArrowAnchor
    $font = [System.Drawing.Font]::new(
        "Arial",
        9,
        [System.Drawing.FontStyle]::Bold)
    try {
        $Graphics.DrawLine($pen, $origin, $end)
        $Graphics.DrawString(
            "$($Lighting.temperatureKelvin)K",
            $font,
            [System.Drawing.Brushes]::LightYellow,
            $Width - 118,
            86)
    } finally {
        $pen.Dispose()
        $font.Dispose()
    }
}

function Draw-BaseImage {
    param(
        [System.Drawing.Bitmap]$Source,
        [string]$Title,
        [ValidateSet("Fit", "Cover")]
        [string]$Mode = "Fit",
        [object]$Focus = $null,
        [double]$Zoom = 1.0
    )
    $canvas = [System.Drawing.Bitmap]::new(
        $CanvasWidth,
        $CanvasHeight,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($canvas)
    $graphics.SmoothingMode =
        [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode =
        [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.Clear([System.Drawing.Color]::FromArgb(18, 20, 26))
    $transform = New-ImageTransform `
        $Source.Width `
        $Source.Height `
        $Mode `
        $Focus `
        $Zoom
    $graphics.DrawImage(
        $Source,
        [System.Drawing.RectangleF]::new(
            [single]$transform.left,
            [single](
                $CanvasHeight -
                $transform.bottom -
                $transform.height),
            [single]$transform.width,
            [single]$transform.height))
    $headerBrush = [System.Drawing.SolidBrush]::new(
        [System.Drawing.Color]::FromArgb(205, 12, 15, 22))
    $font = [System.Drawing.Font]::new(
        "Arial",
        12,
        [System.Drawing.FontStyle]::Bold)
    $graphics.FillRectangle($headerBrush, 0, 0, $CanvasWidth, 34)
    $graphics.DrawString(
        $Title,
        $font,
        [System.Drawing.Brushes]::White,
        10,
        8)
    $headerBrush.Dispose()
    $font.Dispose()
    return [ordered]@{
        Bitmap = $canvas
        Graphics = $graphics
        Transform = $transform
    }
}

function Add-Legend {
    param(
        [System.Drawing.Graphics]$Graphics,
        [string]$Footer
    )
    $brush = [System.Drawing.SolidBrush]::new(
        [System.Drawing.Color]::FromArgb(210, 12, 15, 22))
    $font = [System.Drawing.Font]::new("Arial", 9)
    try {
        $Graphics.FillRectangle(
            $brush,
            0,
            $CanvasHeight - 30,
            $CanvasWidth,
            30)
        $legend =
            "GREEN walkable  RED forbidden  YELLOW clue/inspectable  " +
            "BLUE slots/cast  MAGENTA uncertain  |  $Footer"
        $Graphics.DrawString(
            $legend,
            $font,
            [System.Drawing.Brushes]::White,
            8,
            $CanvasHeight - 23)
    } finally {
        $brush.Dispose()
        $font.Dispose()
    }
}

function Draw-Notice {
    param(
        [System.Drawing.Graphics]$Graphics,
        [string]$Text
    )
    if ([string]::IsNullOrWhiteSpace($Text)) {
        return
    }
    $font = [System.Drawing.Font]::new(
        "Arial",
        9,
        [System.Drawing.FontStyle]::Bold)
    $brush = [System.Drawing.SolidBrush]::new(
        [System.Drawing.Color]::FromArgb(225, 80, 28, 18))
    try {
        $Graphics.FillRectangle(
            $brush,
            0,
            34,
            $CanvasWidth,
            24)
        $Graphics.DrawString(
            $Text,
            $font,
            [System.Drawing.Brushes]::LightSalmon,
            8,
            39)
    } finally {
        $font.Dispose()
        $brush.Dispose()
    }
}

function Save-Png {
    param([System.Drawing.Bitmap]$Bitmap, [string]$Path)
    $Bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
}

function New-ContactSheets {
    param(
        [object[]]$Items,
        [string]$OutputPrefix
    )
    $columns = 3
    $rows = 3
    $thumbWidth = 320
    $thumbHeight = 180
    $labelHeight = 30
    $pageSize = $columns * $rows
    $pageCount = [Math]::Ceiling($Items.Count / $pageSize)
    for ($page = 0; $page -lt $pageCount; $page++) {
        $sheet = [System.Drawing.Bitmap]::new(
            $columns * $thumbWidth,
            $rows * ($thumbHeight + $labelHeight))
        $graphics = [System.Drawing.Graphics]::FromImage($sheet)
        $graphics.Clear([System.Drawing.Color]::FromArgb(20, 23, 30))
        $font = [System.Drawing.Font]::new(
            "Arial",
            9,
            [System.Drawing.FontStyle]::Bold)
        try {
            for ($slot = 0; $slot -lt $pageSize; $slot++) {
                $index = $page * $pageSize + $slot
                if ($index -ge $Items.Count) {
                    break
                }
                $item = $Items[$index]
                $source = [System.Drawing.Image]::FromFile($item.path)
                try {
                    $x = ($slot % $columns) * $thumbWidth
                    $y = [Math]::Floor($slot / $columns) *
                        ($thumbHeight + $labelHeight)
                    $graphics.DrawImage(
                        $source,
                        $x,
                        $y,
                        $thumbWidth,
                        $thumbHeight)
                    $graphics.DrawString(
                        $item.label,
                        $font,
                        [System.Drawing.Brushes]::White,
                        $x + 6,
                        $y + $thumbHeight + 7)
                } finally {
                    $source.Dispose()
                }
            }
            $path = Join-Path $contactRoot `
                ("{0}_{1:D2}.png" -f $OutputPrefix, ($page + 1))
            Save-Png $sheet $path
        } finally {
            $font.Dispose()
            $graphics.Dispose()
            $sheet.Dispose()
        }
    }
}

function Get-SceneOrder {
    param([string]$SceneId)
    if ($SceneId -match "^P-(\d+)$") {
        return [int]$Matches[1]
    }
    if ($SceneId -match "^D(\d+)-(\d+)$") {
        return 1000 + [int]$Matches[1] * 100 + [int]$Matches[2]
    }
    return [int]::MaxValue
}

function Test-ProtectionActiveForScene {
    param([object]$Protection, [string]$SceneId)
    if ([string]$Protection.source -eq "NarrativeVisual") {
        return $true
    }
    $availableFrom = [string]$Protection.availableFromScene
    if ([string]::IsNullOrWhiteSpace($availableFrom)) {
        return $true
    }
    return (Get-SceneOrder $SceneId) -ge
        (Get-SceneOrder $availableFrom)
}

function Get-SilhouetteRect {
    param([object]$Slot)
    $height = [double]$Slot.normalizedHeight
    $width = $height * 0.28
    return [pscustomobject]@{
        x = [double]$Slot.foot.x - $width * 0.5
        y = [double]$Slot.foot.y
        width = $width
        height = $height
    }
}

function Test-RectOverlap {
    param([object]$First, [object]$Second)
    return [double]$First.x -lt
            ([double]$Second.x + [double]$Second.width) -and
        ([double]$First.x + [double]$First.width) -gt
            [double]$Second.x -and
        [double]$First.y -lt
            ([double]$Second.y + [double]$Second.height) -and
        ([double]$First.y + [double]$First.height) -gt
            [double]$Second.y
}

function Get-ProtectionOverlapPenalty {
    param([object]$Slot, [object[]]$Protections)
    $silhouette = Get-SilhouetteRect $Slot
    $penalty = 0
    foreach ($protection in $Protections) {
        if ($null -eq $protection.normalizedRect) {
            continue
        }
        if (Test-RectOverlap $silhouette $protection.normalizedRect) {
            $penalty += if (
                [string]$protection.priority -eq "Hard" -or
                [string]$protection.priority -eq "Critical") {
                100
            } else {
                25
            }
        }
    }
    return $penalty
}

function Test-SlotVisibleInTransform {
    param([object]$Slot, [object]$Transform)
    $rect = Get-SilhouetteRect $Slot
    $left =
        [double]$Transform.left +
        [double]$rect.x * [double]$Transform.width
    $right =
        [double]$Transform.left +
        ([double]$rect.x + [double]$rect.width) *
        [double]$Transform.width
    $bottom =
        [double]$Transform.bottom +
        [double]$rect.y * [double]$Transform.height
    $top =
        [double]$Transform.bottom +
        ([double]$rect.y + [double]$rect.height) *
        [double]$Transform.height
    return $left -ge 0 -and
        $right -le $CanvasWidth -and
        $bottom -ge 0 -and
        $top -le $CanvasHeight
}

$seedById = @{}
foreach ($item in $seedProfiles) {
    if ($seedById.ContainsKey([string]$item.profileId)) {
        throw "Duplicate semantic seed profile: $($item.profileId)"
    }
    $seedById[[string]$item.profileId] = $item
}
$missingSeedIds = @(
    $inventory.backgrounds |
        Where-Object {
            -not $seedById.ContainsKey([string]$_.profileId)
        } |
        ForEach-Object { [string]$_.profileId })
if ($missingSeedIds.Count -gt 0) {
    throw (
        "Semantic review requires authored geometry for every background. " +
        "Missing: " +
        ($missingSeedIds -join ", "))
}
$inventoryIds = @{}
foreach ($background in $inventory.backgrounds) {
    $inventoryIds[[string]$background.profileId] = $true
}
$extraSeedIds = @(
    $seedById.Keys |
        Where-Object { -not $inventoryIds.ContainsKey([string]$_) } |
        Sort-Object)
if ($extraSeedIds.Count -gt 0) {
    throw "Semantic seed has profiles outside the inventory: $($extraSeedIds -join ', ')"
}

$profiles = [System.Collections.Generic.List[object]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()
$errors = [System.Collections.Generic.List[string]]::new()
$backgroundImages = [System.Collections.Generic.List[object]]::new()

foreach ($background in $inventory.backgrounds) {
    $geometry = if ($seedById.ContainsKey([string]$background.profileId)) {
        $seedById[[string]$background.profileId]
    } else {
        $warnings.Add(
            "Missing reviewed seed; fallback used: $($background.profileId)")
        New-FallbackSeed $background
    }

    $protectionById = @{}
    $hiddenProtectionIds = @{}
    foreach ($protection in @(
            $inventory.protections | Where-Object {
                $background.locationCodes -contains $_.locationCode -and
                (Test-ProtectionMatchesBackground $_ $background)
            })) {
        $protectionId = [string]$protection.objectId
        if (-not (Test-ProtectionIsPresent $protection)) {
            $hiddenProtectionIds[$protectionId] = $true
            $protectionById.Remove($protectionId)
            continue
        }
        $catalogPoints = if (
            $null -ne $protection.PSObject.Properties["points"]) {
            @($protection.points)
        } else {
            @()
        }
        $protectionById[$protectionId] =
            [pscustomobject][ordered]@{
                locationCode = [string]$protection.locationCode
                objectId = [string]$protection.objectId
                kind = [string]$protection.kind
                priority = [string]$protection.priority
                displayName = [string]$protection.displayName
                description = [string]$protection.description
                normalizedRect = $protection.normalizedRect
                points = $catalogPoints
                variantKey = Get-ProtectionVariantKey $protection
                availableFromScene =
                    [string]$protection.availableFromScene
                requiredEnding = [string]$protection.requiredEnding
                argumentRole = [string]$protection.argumentRole
                coverage = [string]$protection.coverage
                sourceScenes = @($protection.sourceScenes)
                source = "Catalog"
                note = ""
            }
    }
    foreach ($protection in @($geometry.narrativeProtectedZones)) {
        $id = [string]$protection.id
        if ($hiddenProtectionIds.ContainsKey($id)) {
            continue
        }
        $existing = if ($protectionById.ContainsKey($id)) {
            $protectionById[$id]
        } else {
            $null
        }
        $hasCatalogPolygon =
            $null -ne $existing -and
            @($existing.points).Count -ge 3
        $protectionById[$id] = [pscustomobject][ordered]@{
            locationCode = @($background.locationCodes) -join ","
            objectId = $id
            kind = [string]$protection.kind
            priority = [string]$protection.priority
            displayName = if ($null -ne $existing) {
                [string]$existing.displayName
            } else {
                $id
            }
            description = if ($null -ne $existing) {
                [string]$existing.description
            } else {
                [string]$protection.note
            }
            normalizedRect = if ($hasCatalogPolygon) {
                $existing.normalizedRect
            } else {
                $protection.rect
            }
            points = if ($hasCatalogPolygon) {
                @($existing.points)
            } else {
                $protection.points
            }
            variantKey = if ($null -ne $existing) {
                [string]$existing.variantKey
            } else {
                ""
            }
            availableFromScene = if ($null -ne $existing) {
                [string]$existing.availableFromScene
            } else {
                ""
            }
            requiredEnding = if ($null -ne $existing) {
                [string]$existing.requiredEnding
            } else {
                ""
            }
            argumentRole = if ($null -ne $existing) {
                [string]$existing.argumentRole
            } else {
                ""
            }
            coverage = if ($null -ne $existing) {
                [string]$existing.coverage
            } else {
                ""
            }
            sourceScenes = if ($null -ne $existing) {
                @($existing.sourceScenes)
            } else {
                @()
            }
            source = if ($hasCatalogPolygon) {
                "CatalogShape+NarrativeVisual"
            } elseif ($null -ne $existing) {
                "Catalog+NarrativeVisual"
            } else {
                "NarrativeVisual"
            }
            note = [string]$protection.note
        }
    }
    $protections = @(
        $protectionById.Values |
            Sort-Object objectId)
    $source = Get-SourceBitmap $background.assetPath
    try {
        $slots = @()
        $slotIndex = 0
        foreach ($slot in $geometry.candidateSlots) {
            $slotIndex++
            $slotId = if ([string]::IsNullOrWhiteSpace([string]$slot.id)) {
                "slot_{0:D2}" -f $slotIndex
            } else {
                [string]$slot.id
            }
            $candidate = [ordered]@{
                id = $slotId
                foot = [ordered]@{
                    x = [double]$slot.foot.x
                    y = [double]$slot.foot.y
                }
                # Seed reviewers use 0=foreground/1=background. The canonical
                # runtime-facing convention follows BackgroundSemanticProfile:
                # 0=far/background, 1=near/foreground.
                depth01 = [Math]::Round(
                    1.0 - [double]$slot.depth,
                    3)
                normalizedHeight = [double]$slot.normalizedHeight
                confidence = [double]$slot.confidence
                grade = Get-SlotGrade $source $slot
            }
            $slots += [pscustomobject]$candidate

            $insideWalkable = $false
            foreach ($walkable in $geometry.walkablePolygons) {
                if (Test-PointInPolygon $slot.foot $walkable.points) {
                    $insideWalkable = $true
                    break
                }
            }
            if (-not $insideWalkable) {
                $warnings.Add(
                    "$($background.profileId)/$slotId is outside walkable geometry.")
            }
            foreach ($zone in $geometry.forbiddenZones) {
                if (Test-PointInZone $slot.foot $zone) {
                    $errors.Add(
                        "$($background.profileId)/$slotId enters forbidden " +
                        "zone '$($zone.kind)'.")
                }
            }
        }

        $profile = [ordered]@{
            profileId = [string]$background.profileId
            sourceKind = [string]$background.sourceKind
            assetPath = [string]$background.assetPath
            sourceSha256 = [string]$background.sourceSha256
            width = [int]$background.width
            height = [int]$background.height
            locationCodes = @($background.locationCodes)
            variantKeys = @($background.variantKeys)
            sceneIds = @($background.sceneIds)
            approvalStatus = $documentApprovalStatus
            analysisConfidence = [double]$geometry.analysisConfidence
            walkablePolygons = @($geometry.walkablePolygons)
            forbiddenZones = @($geometry.forbiddenZones)
            uncertainZones = @($geometry.uncertainZones)
            protectedZones = @($protections)
            perspective = $geometry.perspective
            lighting = $geometry.lighting
            candidateSlots = @($slots)
            analysisNotes = [string]$geometry.analysisNotes
        }
        if ($ApproveForRuntime) {
            $profile["runtimeConnected"] = $true
            $profile["reviewer"] = [string]$approvalMetadata.reviewer
            $profile["revision"] = [int]$approvalMetadata.revision
            $profile["approvedAtUtc"] =
                [string]$approvalMetadata.approvedAtUtc
        }
        $profiles.Add([pscustomobject]$profile)

        $drawing = Draw-BaseImage `
            $source `
            "$($background.profileId) | $($background.sourceKind)" `
            "Fit"
        try {
            foreach ($walkable in $geometry.walkablePolygons) {
                Draw-Zone `
                    $drawing.Graphics `
                    $walkable `
                    ([System.Drawing.Color]::FromArgb(70, 50, 220, 90)) `
                    ([System.Drawing.Color]::FromArgb(235, 70, 255, 120)) `
                    $drawing.Transform
            }
            foreach ($zone in $geometry.forbiddenZones) {
                Draw-Zone `
                    $drawing.Graphics `
                    $zone `
                    ([System.Drawing.Color]::FromArgb(85, 235, 45, 55)) `
                    ([System.Drawing.Color]::FromArgb(240, 255, 75, 85)) `
                    $drawing.Transform
                Draw-ZoneLabel `
                    $drawing.Graphics `
                    $zone `
                    "X $($zone.kind)" `
                    ([System.Drawing.Color]::FromArgb(255, 255, 120, 125)) `
                    $drawing.Transform
            }
            foreach ($zone in $geometry.uncertainZones) {
                Draw-Zone `
                    $drawing.Graphics `
                    $zone `
                    ([System.Drawing.Color]::FromArgb(60, 230, 65, 220)) `
                    ([System.Drawing.Color]::FromArgb(235, 255, 115, 245)) `
                    $drawing.Transform `
                    $true
                Draw-ZoneLabel `
                    $drawing.Graphics `
                    $zone `
                    "? $($zone.kind)" `
                    ([System.Drawing.Color]::FromArgb(255, 255, 150, 250)) `
                    $drawing.Transform
            }
            foreach ($zone in $protections) {
                $drawZone = [pscustomobject]@{
                    rect = $zone.normalizedRect
                    points = $zone.points
                }
                Draw-Zone `
                    $drawing.Graphics `
                    $drawZone `
                    ([System.Drawing.Color]::FromArgb(85, 255, 210, 35)) `
                    ([System.Drawing.Color]::FromArgb(245, 255, 235, 65)) `
                    $drawing.Transform
                Draw-ZoneLabel `
                    $drawing.Graphics `
                    $drawZone `
                    "$($zone.priority) $($zone.objectId)" `
                    ([System.Drawing.Color]::FromArgb(255, 255, 240, 115)) `
                    $drawing.Transform
            }
            foreach ($slot in $slots) {
                $slotLabel = "{0} {1} S{2:F2} E{3:F2}" -f `
                    $slot.id, `
                    $slot.grade.tintHex, `
                    [double]$slot.grade.saturation, `
                    [double]$slot.grade.exposure
                Draw-Silhouette `
                    $drawing.Graphics `
                    $slot `
                    ([System.Drawing.Color]::FromArgb(110, 40, 130, 255)) `
                    $slotLabel `
                    $drawing.Transform
            }
            Draw-Lighting `
                $drawing.Graphics `
                $geometry.lighting `
                $CanvasWidth `
                $CanvasHeight
            Add-Legend `
                $drawing.Graphics `
                ("confidence={0:F2} status={1}" -f
                    [double]$geometry.analysisConfidence,
                    [string]$geometry.approvalStatus)

            $outputPath = Join-Path $backgroundRoot `
                "$($background.profileId)_semantic.png"
            Save-Png $drawing.Bitmap $outputPath
            $backgroundImages.Add([pscustomobject]@{
                path = $outputPath
                label = [string]$background.profileId
            })
        } finally {
            $drawing.Graphics.Dispose()
            $drawing.Bitmap.Dispose()
        }
    } finally {
        $source.Dispose()
    }
}

$profileById = @{}
foreach ($profile in $profiles) {
    $profileById[[string]$profile.profileId] = $profile
}

$sceneImages = [System.Collections.Generic.List[object]]::new()
$sceneLayouts = [System.Collections.Generic.List[object]]::new()
$sceneScreenshotBaselines =
    [System.Collections.Generic.List[object]]::new()
foreach ($scene in $inventory.scenes) {
    if (-not $profileById.ContainsKey(
            [string]$scene.backgroundProfileId)) {
        $errors.Add(
            "Scene $($scene.sceneId) has no background semantic profile.")
        continue
    }
    $profile = $profileById[[string]$scene.backgroundProfileId]
    $source = Get-SourceBitmap $profile.assetPath
    try {
        $drawing = Draw-BaseImage `
            $source `
            "$($scene.sceneId) | $($scene.locationCode) | cast preview" `
            "Cover" `
            $scene.coverFocus `
            ([double]$scene.coverZoom)
        try {
            foreach ($walkable in $profile.walkablePolygons) {
                Draw-Zone `
                    $drawing.Graphics `
                    $walkable `
                    ([System.Drawing.Color]::FromArgb(38, 50, 220, 90)) `
                    ([System.Drawing.Color]::FromArgb(170, 70, 255, 120)) `
                    $drawing.Transform
            }
            foreach ($zone in $profile.forbiddenZones) {
                Draw-Zone `
                    $drawing.Graphics `
                    $zone `
                    ([System.Drawing.Color]::FromArgb(55, 235, 45, 55)) `
                    ([System.Drawing.Color]::FromArgb(190, 255, 75, 85)) `
                    $drawing.Transform
            }
            foreach ($zone in $profile.uncertainZones) {
                Draw-Zone `
                    $drawing.Graphics `
                    $zone `
                    ([System.Drawing.Color]::FromArgb(40, 230, 65, 220)) `
                    ([System.Drawing.Color]::FromArgb(180, 255, 115, 245)) `
                    $drawing.Transform `
                    $true
            }
            $sceneProtections = @(
                $profile.protectedZones |
                    Where-Object {
                        Test-ProtectionActiveForScene `
                            $_ `
                            ([string]$scene.sceneId)
                    })
            foreach ($zone in $sceneProtections) {
                $drawZone = [pscustomobject]@{
                    rect = $zone.normalizedRect
                    points = $zone.points
                }
                Draw-Zone `
                    $drawing.Graphics `
                    $drawZone `
                    ([System.Drawing.Color]::FromArgb(70, 255, 210, 35)) `
                    ([System.Drawing.Color]::FromArgb(225, 255, 235, 65)) `
                    $drawing.Transform
                Draw-ZoneLabel `
                    $drawing.Graphics `
                    $drawZone `
                    "$($zone.priority) $($zone.objectId)" `
                    ([System.Drawing.Color]::FromArgb(255, 255, 240, 115)) `
                    $drawing.Transform
            }

            $availableSlots = @(
                $profile.candidateSlots |
                    Where-Object {
                        Test-SlotVisibleInTransform `
                            $_ `
                            $drawing.Transform
                    } |
                    Sort-Object `
                        @{
                            Expression = {
                                Get-ProtectionOverlapPenalty `
                                    $_ `
                                    $sceneProtections
                            }
                            Ascending = $true
                        },
                        @{ Expression = "confidence"; Descending = $true },
                        @{ Expression = "id"; Descending = $false })
            $croppedSlotCount =
                $profile.candidateSlots.Count - $availableSlots.Count
            if ($croppedSlotCount -gt 0) {
                $warnings.Add(
                    "Scene $($scene.sceneId): $croppedSlotCount slot(s) " +
                    "were rejected by runtime cover/zoom cropping.")
            }
            $orderedCast = @(
                $scene.cast |
                    Sort-Object `
                        @{ Expression = "focus"; Descending = $true },
                        @{
                            Expression = {
                                if ($_.role -eq "ContextNpc") { 1 } else { 0 }
                            }
                            Descending = $false
                        },
                        @{
                            Expression = "characterId"
                            Descending = $false
                        })
            $castFingerprintParts = @(
                $orderedCast |
                    ForEach-Object {
                        $focusText =
                            ([bool]$_.focus).ToString().ToLowerInvariant()
                        "{0}/{1}/{2}" -f
                            [string]$_.characterId,
                            [string]$_.role,
                            $focusText
                    } |
                    Sort-Object)
            $castFingerprint = Get-StringSha256 (
                $castFingerprintParts -join "|")
            $castIndex = 0
            $offCamera = [System.Collections.Generic.List[string]]::new()
            $unsafePlacements =
                [System.Collections.Generic.List[string]]::new()
            $assignments =
                [System.Collections.Generic.List[object]]::new()
            foreach ($member in $orderedCast) {
                if ($castIndex -ge $availableSlots.Count) {
                    $warnings.Add(
                        "Scene $($scene.sceneId): $($member.characterId) " +
                        "is off-camera because semantic slot capacity is " +
                        "$($availableSlots.Count).")
                    $offCamera.Add([string]$member.characterId)
                    $assignments.Add([pscustomobject][ordered]@{
                        characterId = [string]$member.characterId
                        role = [string]$member.role
                        focus = [bool]$member.focus
                        state = [string]$member.state
                        slotId = ""
                        offCamera = $true
                        hardProtectionOverlap = $false
                    })
                    continue
                }
                $slot = $availableSlots[$castIndex]
                $overlapPenalty = Get-ProtectionOverlapPenalty `
                    $slot `
                    $sceneProtections
                if ($overlapPenalty -ge 100) {
                    $warnings.Add(
                        "Scene $($scene.sceneId): $($member.characterId) at " +
                        "$($slot.id) may occlude a hard protected clue.")
                    $unsafePlacements.Add(
                        "$($member.characterId)@$($slot.id)")
                }
                $color = if ($overlapPenalty -ge 100) {
                    [System.Drawing.Color]::FromArgb(
                        195, 255, 60, 50)
                } elseif ($member.focus) {
                    [System.Drawing.Color]::FromArgb(185, 255, 185, 45)
                } elseif ($member.role -eq "ContextNpc") {
                    [System.Drawing.Color]::FromArgb(170, 45, 220, 230)
                } else {
                    [System.Drawing.Color]::FromArgb(160, 65, 135, 255)
                }
                Draw-Silhouette `
                    $drawing.Graphics `
                    $slot `
                    $color `
                    "$(if ($overlapPenalty -ge 100) { '!' })$($member.characterId) [$($slot.id)]" `
                    $drawing.Transform
                $assignments.Add([pscustomobject][ordered]@{
                    characterId = [string]$member.characterId
                    role = [string]$member.role
                    focus = [bool]$member.focus
                    state = [string]$member.state
                    slotId = [string]$slot.id
                    offCamera = $false
                    hardProtectionOverlap =
                        [bool]($overlapPenalty -ge 100)
                })
                $castIndex++
            }
            $noticeParts = [System.Collections.Generic.List[string]]::new()
            if ($unsafePlacements.Count -gt 0) {
                $noticeParts.Add(
                    "RED=HARD CLUE OVERLAP: " +
                    ($unsafePlacements -join ", "))
            }
            if ($offCamera.Count -gt 0) {
                $noticeParts.Add(
                    "OFF-CAMERA: " +
                    ($offCamera -join ", "))
            }
            if ($noticeParts.Count -gt 0) {
                Draw-Notice `
                    $drawing.Graphics `
                    ($noticeParts -join " | ")
            }
            Add-Legend `
                $drawing.Graphics `
                ("cast={0} slots={1} off-camera={2} variant={3}" -f
                    $orderedCast.Count,
                    $availableSlots.Count,
                    $offCamera.Count,
                    $scene.variantKey)
            $sceneFileName =
                "$($scene.sceneId.Replace('-', '_'))_$($scene.locationCode).png"
            $outputPath = Join-Path $sceneRoot $sceneFileName
            Save-Png $drawing.Bitmap $outputPath
            $sceneImages.Add([pscustomobject]@{
                path = $outputPath
                label = "$($scene.sceneId) $($scene.locationCode)"
            })
            $sceneLayouts.Add([pscustomobject][ordered]@{
                sceneId = [string]$scene.sceneId
                locationCode = [string]$scene.locationCode
                backgroundProfileId =
                    [string]$scene.backgroundProfileId
                variantKey = [string]$scene.variantKey
                coverFocus = $scene.coverFocus
                coverZoom = [double]$scene.coverZoom
                castFingerprint = $castFingerprint
                availableSlotCount = $availableSlots.Count
                croppedSlotCount = $croppedSlotCount
                assignments = @($assignments)
                offCameraCharacters = @($offCamera)
            })
            if ($ApproveForRuntime) {
                $sceneScreenshotBaselines.Add(
                    [pscustomobject][ordered]@{
                        sceneId = [string]$scene.sceneId
                        path = (
                            "Documentation/CharacterStagingReview/Scenes/" +
                            $sceneFileName)
                        sha256 = Get-FileSha256 $outputPath
                    })
            }
        } finally {
            $drawing.Graphics.Dispose()
            $drawing.Bitmap.Dispose()
        }
    } finally {
        $source.Dispose()
    }
}

if ($ApproveForRuntime -and $errors.Count -gt 0) {
    throw (
        "Runtime approval was refused because {0} validation error(s) " +
        "were recorded." -f $errors.Count)
}

$profileDocument = [ordered]@{
    schemaVersion = "1.0"
    sourceInventoryGeneratedAtUtc = $inventory.generatedAtUtc
    runtimeConnected = $runtimeConnected
    approvalStatus = $documentApprovalStatus
    profiles = @($profiles)
}
if ($ApproveForRuntime) {
    $profileDocument["reviewer"] =
        [string]$approvalMetadata.reviewer
    $profileDocument["revision"] =
        [int]$approvalMetadata.revision
    $profileDocument["approvedAtUtc"] =
        [string]$approvalMetadata.approvedAtUtc
}
$profilePath = Join-Path $dataRoot "background_semantic_profiles.json"
$profileJson = $profileDocument | ConvertTo-Json -Depth 40
[System.IO.File]::WriteAllText(
    $profilePath,
    $profileJson,
    [System.Text.UTF8Encoding]::new($false))

$report = [ordered]@{
    schemaVersion = "1.0"
    runtimeConnected = $runtimeConnected
    approvalStatus = $documentApprovalStatus
    semanticSeedFiles = @(
        $seedPaths | ForEach-Object {
            [System.IO.Path]::GetFileName($_)
        })
    semanticSeedCount = $seedById.Count
    backgroundCount = $profiles.Count
    sceneReviewCount = $sceneImages.Count
    protectionCount = @(
        $profiles | ForEach-Object { @($_.protectedZones) }
    ).Count
    approvedVariantCount = @(
        $profiles | Where-Object {
            $_.sourceKind -like "ApprovedVariant*"
        }).Count
    legacyBaseCount = @(
        $profiles | Where-Object {
            $_.sourceKind -like "*LegacyBase*"
        }).Count
    excludedUnusedLocationCodes =
        @($inventory.excludedUnusedLocationCodes)
    errors = @($errors)
    warnings = @($warnings)
}
if ($ApproveForRuntime) {
    $report["reviewer"] = [string]$approvalMetadata.reviewer
    $report["revision"] = [int]$approvalMetadata.revision
    $report["approvedAtUtc"] =
        [string]$approvalMetadata.approvedAtUtc
    $report["approvedWarnings"] = $true
    $report["approvedWarningCount"] = $warnings.Count
}
$reportPath = Join-Path $dataRoot "analysis_validation_report.json"
$reportJson = $report | ConvertTo-Json -Depth 20
[System.IO.File]::WriteAllText(
    $reportPath,
    $reportJson,
    [System.Text.UTF8Encoding]::new($false))

if ($ApproveForRuntime) {
    $runtimeProfiles = @(
        foreach ($profile in $profiles) {
            $semanticContent = [ordered]@{
                walkablePolygons = @($profile.walkablePolygons)
                forbiddenZones = @($profile.forbiddenZones)
                uncertainZones = @($profile.uncertainZones)
                protectedZones = @($profile.protectedZones)
                perspective = $profile.perspective
                lighting = $profile.lighting
                candidateSlots = @($profile.candidateSlots)
            }
            [pscustomobject][ordered]@{
                profileId = [string]$profile.profileId
                assetPath = [string]$profile.assetPath
                sourceSha256 = [string]$profile.sourceSha256
                locationCodes = @($profile.locationCodes)
                variantKeys = @($profile.variantKeys)
                walkablePolygons = @($profile.walkablePolygons)
                forbiddenZones = @($profile.forbiddenZones)
                uncertainZones = @($profile.uncertainZones)
                protectedZones = @($profile.protectedZones)
                perspective = $profile.perspective
                lighting = $profile.lighting
                candidateSlots = @($profile.candidateSlots)
                semanticContentHash =
                    Get-StableObjectSha256 $semanticContent
            }
        })
    $runtimeDocument = [ordered]@{
        schemaVersion = "1.0"
        runtimeConnected = $true
        approvalStatus = "Approved"
        reviewer = [string]$approvalMetadata.reviewer
        revision = [int]$approvalMetadata.revision
        approvedAtUtc = [string]$approvalMetadata.approvedAtUtc
        approvedWarnings = $true
        approvedWarningCount = $warnings.Count
        sourceInventoryGeneratedAtUtc = $inventory.generatedAtUtc
        excludedUnusedLocationCodes =
            @($inventory.excludedUnusedLocationCodes)
        profiles = $runtimeProfiles
        sceneLayouts = $sceneLayouts.ToArray()
    }
    $runtimePath = Join-Path $dataRoot `
        "approved_background_semantic_runtime.json"
    $runtimeJson = $runtimeDocument | ConvertTo-Json -Depth 50
    [System.IO.File]::WriteAllText(
        $runtimePath,
        $runtimeJson,
        [System.Text.UTF8Encoding]::new($false))

    $baselineDocument = [ordered]@{
        schemaVersion = "1.0"
        runtimeConnected = $true
        approvalStatus = "Approved"
        reviewer = [string]$approvalMetadata.reviewer
        revision = [int]$approvalMetadata.revision
        approvedAtUtc = [string]$approvalMetadata.approvedAtUtc
        canvasWidth = $CanvasWidth
        canvasHeight = $CanvasHeight
        scenes = $sceneScreenshotBaselines.ToArray()
    }
    $baselinePath = Join-Path $dataRoot `
        "scene_screenshot_baselines.json"
    $baselineJson = $baselineDocument | ConvertTo-Json -Depth 20
    [System.IO.File]::WriteAllText(
        $baselinePath,
        $baselineJson,
        [System.Text.UTF8Encoding]::new($false))
}

New-ContactSheets $backgroundImages.ToArray() "background_semantics"
New-ContactSheets $sceneImages.ToArray() "scene_casts"

Write-Output (
    "Generated {0} background semantic reviews and {1} scene cast reviews." -f
        $profiles.Count,
        $sceneImages.Count)
Write-Output "Profiles: $profilePath"
Write-Output "Validation: $reportPath"
if ($ApproveForRuntime) {
    Write-Output "Approved runtime: $runtimePath"
    Write-Output "Scene baselines: $baselinePath"
}
if ($errors.Count -gt 0) {
    Write-Warning "$($errors.Count) validation errors were recorded."
}
if ($warnings.Count -gt 0) {
    Write-Warning "$($warnings.Count) review warnings were recorded."
}
