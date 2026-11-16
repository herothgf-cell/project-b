Add-Type -AssemblyName System.Drawing
$p=Get-Content "$PSScriptRoot/../../../config/maps/project/eungye.project.json" -Raw | ConvertFrom-Json -Depth 100
$b=[Drawing.Bitmap]::new(1100,850);$g=[Drawing.Graphics]::FromImage($b);$g.SmoothingMode='AntiAlias';$g.Clear([Drawing.Color]::FromArgb(245,248,245))
function px($x){[single](75+($x-470)*2)}
function py($z){[single](730-($z-40)*1.4)}
$g.SetClip([Drawing.Rectangle]::new(0,95,1100,650))
foreach($r in $p.baseline.roads){if($r.points.Count -lt 2){continue};$pts=[Drawing.PointF[]]@($r.points|ForEach-Object {[Drawing.PointF]::new((px $_.x),(py $_.z))});$c=if($r.name -eq '은계로'){[Drawing.Color]::FromArgb(35,185,50)}else{[Drawing.Color]::FromArgb(215,70,160)};$pen=[Drawing.Pen]::new($c,[single][Math]::Max(3,$r.width*.45));$g.DrawLines($pen,$pts);$pen.Dispose()}
$g.ResetClip();$font=[Drawing.Font]::new('Malgun Gothic',17);$big=[Drawing.Font]::new('Malgun Gothic',24,[Drawing.FontStyle]::Bold)
$g.FillRectangle([Drawing.Brushes]::White,0,0,1100,95);$g.DrawString('33번 · 상가 골목에서 은계로 진입 (북 ↑)',$big,[Drawing.Brushes]::Black,25,20)
$x=px 765;$y=py 255;$g.FillEllipse([Drawing.Brushes]::White,($x-23),($y-23),46,46);$g.DrawEllipse([Drawing.Pens]::Red,($x-23),($y-23),46,46);$g.FillEllipse([Drawing.Brushes]::Red,($x-6),($y-6),12,12)
$pen=[Drawing.Pen]::new([Drawing.Color]::Blue,5);$pen.EndCap=[Drawing.Drawing2D.LineCap]::ArrowAnchor;$g.DrawLine($pen,(px 690),($y+45),(px 775),($y+45));$pen.Dispose()
$g.DrawString('33 · 동행 2차로의 진행 방향 확인',$font,[Drawing.Brushes]::Crimson,270,($y-65))
$g.DrawString('은계로',$font,[Drawing.Brushes]::DarkGreen,715,330)
$g.DrawString('은계로142번길 · 학교 방향',$font,[Drawing.Brushes]::Black,640,690)
$g.DrawString('빨간 점: 은계로 서쪽 정지선 / 파란 화살표 방향으로 진입',$font,[Drawing.Brushes]::Black,25,770)
$b.Save("$PSScriptRoot/../images/eungye-p29-question.png",[Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$b.Dispose()
