Add-Type -AssemblyName System.Drawing
$bitmap=[Drawing.Bitmap]::new(1600,1100)
$g=[Drawing.Graphics]::FromImage($bitmap);$g.SmoothingMode='AntiAlias';$g.Clear([Drawing.ColorTranslator]::FromHtml('#f3f6fa'))
function Brush($hex){[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($hex))}
function Text($s,$x,$y,$size=20,$color='#23354d'){ $font=[Drawing.Font]::new('Malgun Gothic',$size,[Drawing.FontStyle]::Regular);$b=Brush $color;$g.DrawString($s,$font,$b,$x,$y);$font.Dispose();$b.Dispose() }
function Point($x,$z){[Drawing.PointF]::new([float](70+$x*.305),[float](980-$z*.285))}
function Road($coords,$lanes){$pts=[Drawing.PointF[]]@($coords|ForEach-Object {Point $_[0] $_[1]});$width=if($lanes -eq 8){23}elseif($lanes -eq 4){13}else{6};$color=if($lanes -eq 8){'#e88931'}elseif($lanes -eq 4){'#347caf'}else{'#7c9ca7'};$p=[Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml($color),$width);$p.StartCap='Round';$p.EndCap='Round';$g.DrawCurve($p,$pts,.25);$p.Dispose()}
function Mark($x,$z,$label,$color){$p=Point $x $z;$b=Brush $color;$g.FillEllipse($b,$p.X-13,$p.Y-13,26,26);Text $label ($p.X-8) ($p.Y-13) 13 '#ffffff';$b.Dispose()}
Text '확장 A2 | 대로·순환로·생활 골목' 55 28 32
Text '3.6 × 2.8km · 기존 시뮬레이션 재사용 · 나 1명 + 봇 3대 · 공용 호출 8개' 58 85 20
$b=Brush '#e4edde';$g.FillRectangle($b,50,160,1130,850);$b.Dispose()
Road @(@(200,400),@(250,1450),@(350,2400),@(900,2600),@(2900,2480),@(3400,2100),@(3370,1450),@(3350,400),@(2700,220),@(600,200),@(200,400)) 4
Road @(@(250,1450),@(900,1450),@(1600,1380),@(2350,1300),@(3000,1390),@(3370,1450)) 8
Road @(@(850,240),@(900,750),@(900,1450),@(800,2050),@(900,2600)) 4
Road @(@(2600,230),@(2500,750),@(2350,1300),@(2460,1900),@(2600,2500)) 4
Road @(@(350,2400),@(800,2050),@(1600,2100),@(2460,1900),@(3400,2100)) 4
Road @(@(200,400),@(850,750),@(1650,650),@(2500,750),@(3350,400)) 4
Road @(@(900,750),@(1380,1050),@(1600,1380)) 2
Road @(@(1600,1380),@(1800,1750),@(2460,1900)) 2
Road @(@(500,1450),@(510,1780),@(710,1850),@(800,2050)) 2
Road @(@(1200,1450),@(1200,1740),@(1450,1740),@(1600,2100)) 2
Road @(@(2450,1390),@(2860,1600),@(3000,1980),@(3400,2100)) 2
Road @(@(900,750),@(650,1030),@(430,1030),@(250,1450)) 2
Road @(@(1650,650),@(1910,850),@(2110,850),@(2350,1300)) 2
Road @(@(2500,750),@(2900,820),@(3020,1080),@(3370,1450)) 2
Road @(@(350,2400),@(600,2240),@(600,2050),@(800,2050)) 2
Road @(@(1600,2100),@(1820,2350),@(2150,2350),@(2200,2520)) 2
Road @(@(200,400),@(430,610),@(600,610),@(650,1030)) 2
Road @(@(1650,650),@(1650,350),@(2100,350),@(2500,750)) 2
foreach($item in @(@(900,1450),@(2350,1300),@(900,750),@(2500,750),@(800,2050),@(2460,1900),@(1600,1380),@(1600,2100))){Mark $item[0] $item[1] 'S' '#137b68'}
foreach($item in @(@(650,1450),@(1950,1330),@(2800,1350),@(2600,2250))){Mark $item[0] $item[1] 'U' '#9c55b9'}
foreach($item in @(@(520,2200),@(1500,2350),@(3000,2300),@(530,1600),@(1850,1550),@(3000,1650),@(470,470),@(1600,430),@(3000,470))){$p=Point $item[0] $item[1];$b=Brush '#123f65';$g.FillEllipse($b,$p.X-5,$p.Y-5,10,10);$g.FillEllipse($b,$p.X+18,$p.Y+10,10,10);$b.Dispose()}
foreach($item in @(@(620,2320,'서북 주거'),@(1600,2460,'병원 권역'),@(2850,2300,'대학 권역'),@(470,1730,'서부 주거'),@(1740,1660,'중앙역·상업'),@(2880,1790,'동부 업무'),@(470,520,'공원·시장'),@(1710,470,'남부 시장'),@(2870,520,'터미널'))){$p=Point $item[0] $item[1];Text $item[2] ($p.X-60) ($p.Y-35) 16}
Text 'N ↑' 1090 176 20
Text '도로 구성' 1220 175 25
Text '━ 왕복 8차로 : 중앙 대로' 1220 235 17 '#bd671b'
Text '━ 왕복 4차로 : 순환·연결' 1220 275 17 '#347caf'
Text '━ 왕복 2차로 : 생활·골목' 1220 315 17 '#607d87'
Text 'S  실제 신호등·횡단보도' 1220 390 18 '#137b68'
Text 'U  조건부 유턴 지점' 1220 430 18 '#9c55b9'
Text '●  승객 거점 총 18곳' 1220 470 18
Text '운전 선택' 1220 550 25
Text "대로: 차로 선택·추월`n순환로: 혼잡 우회`n골목: 짧지만 저속·양보`n비보호 좌회전: 표지+조건`n유턴: 지정 지점+조건" 1220 610 18
Text '기존 시설 유지' 1220 825 24
Text "주유 3 / 정비 2 / 세차 2`n주차·휴식 4" 1220 880 18
Text '개념 배치도: 신호·회전·시설의 최종 위치와 연결은 실제 차로 데이터 검사 후 확정합니다.' 60 1035 18
$bitmap.Save((Join-Path $PSScriptRoot '../docs/content-design/pvp/26-city-a2-proposal.png'),[Drawing.Imaging.ImageFormat]::Png)
$g.Dispose();$bitmap.Dispose()