Add-Type -AssemblyName System.Drawing
$p=Get-Content "$PSScriptRoot/../../../config/maps/project/eungye.project.json" -Raw | ConvertFrom-Json -Depth 100
$b=[Drawing.Bitmap]::new(1100,850);$g=[Drawing.Graphics]::FromImage($b);$g.SmoothingMode='AntiAlias';$g.Clear([Drawing.Color]::FromArgb(245,248,245))
function px($x){[single](90+($x-740)*4)}
function py($z){[single](735-($z-55)*2.7)}
$g.SetClip([Drawing.Rectangle]::new(0,100,1100,650))
foreach($r in $p.baseline.roads){if($r.points.Count -lt 2){continue};$pts=[Drawing.PointF[]]@($r.points|ForEach-Object {[Drawing.PointF]::new((px $_.x),(py $_.z))});$c=if($r.name -eq '은계로'){[Drawing.Color]::FromArgb(35,185,50)}else{[Drawing.Color]::FromArgb(215,70,160)};$pen=[Drawing.Pen]::new($c,[single][Math]::Max(4,$r.width*.9));$g.DrawLines($pen,$pts);$pen.Dispose()}
$g.ResetClip();$font=[Drawing.Font]::new('Malgun Gothic',18);$big=[Drawing.Font]::new('Malgun Gothic',25,[Drawing.FontStyle]::Bold)
$g.FillRectangle([Drawing.Brushes]::White,0,0,1100,100);$g.DrawString('32번 · 분홍색 동남쪽 골목 확대 (북 ↑)',$big,[Drawing.Brushes]::Black,25,20)
$x=px 817;$y=py 165;$g.FillEllipse([Drawing.Brushes]::White,($x-25),($y-25),50,50);$g.DrawEllipse([Drawing.Pens]::Red,($x-25),($y-25),50,50);$g.FillEllipse([Drawing.Brushes]::Red,($x-6),($y-6),12,12)
$g.DrawString('32  건물 사이 통로',$big,[Drawing.Brushes]::Crimson,($x-100),($y-75))
$g.DrawString('은계로',$font,[Drawing.Brushes]::DarkGreen,30,230)
$g.DrawString('학교 옆 은계로142번길',$font,[Drawing.Brushes]::Black,350,700)
$g.DrawString('북쪽 은계로 사거리',$font,[Drawing.Brushes]::Black,75,155)
$g.DrawString('거리뷰: 보도블록 통로로 보임 / 게임: 차량 연결 있음',$font,[Drawing.Brushes]::Black,25,770)
$b.Save("$PSScriptRoot/../images/eungye-pink-passage-question.png",[Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$b.Dispose()
