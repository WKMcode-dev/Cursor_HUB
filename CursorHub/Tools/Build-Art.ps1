# Rasterizes the supplied frame once, at 2x resolution. No SVG parsing at runtime.
Add-Type -AssemblyName System.Drawing
$assetDirectory = Join-Path $PSScriptRoot '../Assets'
New-Item -ItemType Directory -Path $assetDirectory -Force | Out-Null
$path = [System.Drawing.Drawing2D.GraphicsPath]::new()
$path.AddArc(0,24,20,20,180,90)
$path.AddLine(10,24,571.858,24)
$path.AddArc(561.858,24,20,20,270,45)
$path.AddLine(578.929,26.929,621.071,69.071)
$path.AddArc(618.142,52,20,20,135,-45)
$path.AddLine(628.142,72,782,72)
$path.AddArc(772,72,20,20,270,90)
$path.AddLine(792,82,792,422)
$path.AddArc(772,412,20,20,0,90)
$path.AddLine(782,432,646,432)
$path.AddArc(636,432,20,20,270,-90)
$path.AddLine(636,442,636,482)
$path.AddArc(616,472,20,20,0,90)
$path.AddLine(626,492,10,492)
$path.AddArc(0,472,20,20,90,90)
$path.CloseFigure()
$bitmap = [System.Drawing.Bitmap]::new(1600,1000)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.ScaleTransform(2,2)
$brush = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#222427'))
$pen = [System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml('#ff9b20'),2)
$graphics.FillPath($brush,$path)
$graphics.DrawPath($pen,$path)
$bitmap.Save((Join-Path $assetDirectory 'frame.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$path.Flatten($null,0.25)
$points = @($path.PathPoints | ForEach-Object { @{ X = $_.X; Y = $_.Y } })
ConvertTo-Json -InputObject $points -Compress | Set-Content (Join-Path $assetDirectory 'frame-points.json') -Encoding utf8
$pen.Dispose(); $brush.Dispose(); $graphics.Dispose(); $bitmap.Dispose(); $path.Dispose()
# Opaque centre + transparent exterior, rendered once at 4x for image scaling.
foreach ($control in @(@('close.png','#ff5f57',18,18,9), @('minimize.png','#ffbd2e',18,18,9), @('maximize.png','#28c840',18,18,9), @('play.png','#ff9b20',136,42,5))) {
  $w=[int]$control[2]; $h=[int]$control[3]; $r=[int]$control[4]
  $image=[System.Drawing.Bitmap]::new($w*4,$h*4)
  $g=[System.Drawing.Graphics]::FromImage($image)
  $g.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $g.ScaleTransform(4,4)
  $brush=[System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml($control[1]))
  if ($w -eq $h) { $g.FillEllipse($brush,0,0,$w,$h) }
  else {
    $shape=[System.Drawing.Drawing2D.GraphicsPath]::new()
    $shape.AddArc(0,0,$r*2,$r*2,180,90); $shape.AddArc($w-$r*2,0,$r*2,$r*2,270,90)
    $shape.AddArc($w-$r*2,$h-$r*2,$r*2,$r*2,0,90); $shape.AddArc(0,$h-$r*2,$r*2,$r*2,90,90)
    $shape.CloseFigure(); $g.FillPath($brush,$shape); $shape.Dispose()
  }
  $image.Save((Join-Path $assetDirectory $control[0]),[System.Drawing.Imaging.ImageFormat]::Png)
  $brush.Dispose(); $g.Dispose(); $image.Dispose()
}
