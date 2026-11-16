Add-Type -AssemblyName System.Drawing
$p=Get-Content "$PSScriptRoot/../../../config/maps/project/eungye.project.json" -Raw | ConvertFrom-Json -Depth 100
$b=[Drawing.Bitmap]::new(1100,1000);$g=[Drawing.Graphics]::FromImage($b);$g.SmoothingMode='AntiAlias';$g.Clear([Drawing.Color]::FromArgb(246,248,245))
function px($x){[single](90+$x*1.6)}
function py($z){[single](900-($z-1280)*1.6)}
$g.SetClip([Drawing.Rectangle]::new(0,130,1100,750))
foreach($r in $p.baseline.roads){if($r.points.Count -lt 2){continue};$pts=[Drawing.PointF[]]@($r.points|ForEach-Object {[Drawing.PointF]::new((px $_.x),(py $_.z))});$c=[Drawing.Color]::FromArgb(204,212,210);if($r.name -eq '은계로'){$c=[Drawing.Color]::FromArgb(45,176,60)}elseif($r.name -eq '계수로' -or $r.name -eq '대은로'){$c=[Drawing.Color]::FromArgb(225,185,20)};$pen=[Drawing.Pen]::new($c,[single][Math]::Max(4,$r.width*.6));$g.DrawLines($pen,$pts);$pen.Dispose()}
$g.ResetClip()
$title=[Drawing.Font]::new('Malgun Gothic',25,[Drawing.FontStyle]::Bold);$font=[Drawing.Font]::new('Malgun Gothic',18);$small=[Drawing.Font]::new('Malgun Gothic',14)
$g.FillRectangle([Drawing.Brushes]::White,0,0,1100,130)
$g.DrawString('31번 = 은계로 × 은행로 사거리',$title,[Drawing.Brushes]::Black,30,20)
$g.DrawString('북 ↑  ·  초록색 은계로의 북서쪽 사거리 확대',$font,[Drawing.Brushes]::DimGray,30,75)
$j=$p.baseline.junctionRules|Where-Object id -eq 'J2145_15970';$x=px $j.x;$y=py $j.z
$g.FillEllipse([Drawing.Brushes]::White,($x-28),($y-28),56,56);$g.DrawEllipse([Drawing.Pens]::Crimson,($x-28),($y-28),56,56);$g.FillEllipse([Drawing.Brushes]::Crimson,($x-8),($y-8),16,16)
$g.FillRectangle([Drawing.Brushes]::White,($x+40),($y-50),490,90);$g.DrawString('31  여기: 은행로 사거리',$title,[Drawing.Brushes]::Crimson,($x+45),($y-45));$g.DrawString('J2145 · x215, z1597',$small,[Drawing.Brushes]::DimGray,($x+48),($y+1))
$j5=$p.baseline.junctionRules|Where-Object id -eq 'J4396_13458';$x5=px $j5.x;$y5=py $j5.z;$g.FillEllipse([Drawing.Brushes]::White,($x5-20),($y5-20),40,40);$g.DrawString('⑤',$title,[Drawing.Brushes]::Black,($x5-18),($y5-25));$g.DrawString('⑤ 은계로 × 계수로',$font,[Drawing.Brushes]::Black,($x5-230),($y5+30))
function Arrow($ax,$az,$bx,$bz,$color){$pen=[Drawing.Pen]::new($color,9);$pen.CustomEndCap=[Drawing.Drawing2D.AdjustableArrowCap]::new(6,7);$g.DrawLine($pen,(px $ax),(py $az),(px $bx),(py $bz));$pen.Dispose()}
Arrow 350 1410 245 1530 ([Drawing.Color]::RoyalBlue)
Arrow 90 1730 170 1640 ([Drawing.Color]::DarkOrange)
$g.DrawString('A  ⑤에서 31로 올라오는 방향',$font,[Drawing.Brushes]::RoyalBlue,90,695)
$g.DrawString('B  지도 북서쪽에서 31로 내려오는 방향',$font,[Drawing.Brushes]::DarkOrange,30,170)
$g.DrawString('은계로',$font,[Drawing.Brushes]::DarkGreen,620,620)
$g.DrawString('은행로',$font,[Drawing.Brushes]::DimGray,180,490)
$g.FillRectangle([Drawing.Brushes]::White,0,900,1100,100);$g.DrawString('질문 대상은 빨간 점 사거리의 A / B 접근입니다.',$font,[Drawing.Brushes]::Black,30,915);$g.DrawString('도로 선형: 게임 원본 NGII 지도 · 번호는 기존 조사 지도와 동일',$small,[Drawing.Brushes]::DimGray,30,955)
$out="$PSScriptRoot/../images/eungye-j2145-question.png";$b.Save($out,[Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$b.Dispose();Write-Output $out
