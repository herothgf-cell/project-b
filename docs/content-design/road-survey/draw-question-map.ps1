Add-Type -AssemblyName System.Drawing
$p=Get-Content config/maps/project/eungye.project.json -Raw | ConvertFrom-Json -Depth 100
$cyanIds=($p.document.lanes | Where-Object id -eq 'local:9032:10757:8270:6402').sourceId.Split(',')
$b=[Drawing.Bitmap]::new(1440,980);$g=[Drawing.Graphics]::FromImage($b);$g.SmoothingMode='AntiAlias';$g.Clear([Drawing.Color]::FromArgb(246,248,247))
$f=[Drawing.Font]::new('Malgun Gothic',16);$big=[Drawing.Font]::new('Malgun Gothic',24,[Drawing.FontStyle]::Bold);$small=[Drawing.Font]::new('Malgun Gothic',12)
$g.DrawString('은계 도로 조사 · 하늘색 구간 확인 지도',$big,[Drawing.Brushes]::Black,30,18)
$g.DrawString('①·②는 확인할 범위의 대표점입니다. 확정된 차로 전환점이 아닙니다.',$f,[Drawing.Brushes]::DimGray,30,65)
function DrawMap($left,$top,$width,$height,$xmin,$zmin,$xmax,$zmax){
 $scale=[Math]::Min($width/($xmax-$xmin),$height/($zmax-$zmin))
 $clip=$g.Save();$g.SetClip([Drawing.RectangleF]::new($left,$top,$width,$height))
 foreach($r in $p.baseline.roads){if($r.points.Count -lt 2){continue};$pts=[Drawing.PointF[]]@($r.points|ForEach-Object{[Drawing.PointF]::new(($left+($_.x-$xmin)*$scale),($top+$height-($_.z-$zmin)*$scale))});$color=if($r.id -in $cyanIds -or ($r.name -eq '은계중앙로' -and ($r.points | Measure-Object z -Minimum).Minimum -gt 640 -and ($r.points | Measure-Object z -Maximum).Maximum -lt 1530)){[Drawing.Color]::FromArgb(36,185,213)}else{[Drawing.Color]::FromArgb(196,205,205)};$pen=[Drawing.Pen]::new($color,[single][Math]::Max(2,$r.width*$scale*.65));$g.DrawLines($pen,$pts);$pen.Dispose()}
 foreach($mark in @(@{n='1';x=910;z=636},@{n='2';x=1000;z=637})){ $x=$left+($mark.x-$xmin)*$scale;$y=$top+$height-($mark.z-$zmin)*$scale;$g.FillEllipse([Drawing.Brushes]::White,($x-18),($y-18),36,36);$pen=[Drawing.Pen]::new([Drawing.Color]::Crimson,3);$g.DrawEllipse($pen,($x-18),($y-18),36,36);$pen.Dispose();$g.DrawString($mark.n,$big,[Drawing.Brushes]::Crimson,($x-12),($y-23))}
 foreach($label in @(@{s='J8066';x=806;z=636},@{s='J7973';x=797;z=374},@{s='은계호수공원';x=965;z=1283})){ $x=$left+($label.x-$xmin)*$scale;$y=$top+$height-($label.z-$zmin)*$scale;$g.DrawString($label.s,$small,[Drawing.Brushes]::Black,($x-35),($y+22))}
 $g.Restore($clip);$g.DrawRectangle([Drawing.Pens]::Silver,$left,$top,$width,$height)
}
DrawMap 25 120 700 740 0 0 1700 1750
DrawMap 755 120 650 530 750 570 1220 950
$g.DrawString('전체 위치 · 북 ↑',$f,[Drawing.Brushes]::Black,35,130)
$g.DrawString('남쪽 구간 확대 · 북 ↑',$f,[Drawing.Brushes]::Black,765,130)
$g.DrawString('① 2+1 ↔ 1+2 배분이 바뀌는 범위',$f,[Drawing.Brushes]::Black,765,680)
$g.DrawString('② 아파트 진입부 이후 1+1로 줄어드는 범위',$f,[Drawing.Brushes]::Black,765,725)
$g.DrawString('정확한 시작·끝 위치를 사용자 확인 후 확정',$f,[Drawing.Brushes]::Crimson,765,775)
$g.DrawString('정식 게임 지도 r19 기반 | 도로 출처 NGII · OSM contributors / ODbL',$small,[Drawing.Brushes]::DimGray,30,915)
$b.Save((Join-Path $PWD 'docs/content-design/images/eungye-road-survey-blue-questions.png'),[Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$b.Dispose()
