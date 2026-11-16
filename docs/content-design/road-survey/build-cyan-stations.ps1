$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=(Resolve-Path "$PSScriptRoot/../../..").Path
$p=Get-Content "$root/config/maps/project/eungye.project.json" -Raw|ConvertFrom-Json -Depth 100
$inventory=Get-Content "$PSScriptRoot/colored-inventory.json" -Raw|ConvertFrom-Json
$sourceIds=@($inventory.corridors|Where-Object color -eq cyan|ForEach-Object sourceIds)
$lanes=@($p.document.lanes|Where-Object {!$_.disabled -and !$_.change -and @($_.sourceId -split ','|Where-Object {$_ -in $sourceIds}).Count -gt 0})
function Length($pts){$v=0.;for($k=1;$k -lt $pts.Count;$k++){$v+=[math]::Sqrt([math]::Pow($pts[$k].x-$pts[$k-1].x,2)+[math]::Pow($pts[$k].z-$pts[$k-1].z,2))};return $v}
function Sample($pts,$at){for($k=1;$k -lt $pts.Count;$k++){$a=$pts[$k-1];$b=$pts[$k];$len=[math]::Sqrt([math]::Pow($b.x-$a.x,2)+[math]::Pow($b.z-$a.z,2));if($at -le $len){$t=if($len -gt 0){$at/$len}else{0};return @{x=$a.x+($b.x-$a.x)*$t;z=$a.z+($b.z-$a.z)*$t}};$at-=$len};return @{x=$pts[-1].x;z=$pts[-1].z}}
$rows=@($lanes|ForEach-Object {[ordered]@{id=$_.id;sourceId=$_.sourceId;length=(Length $_.points);number=$_.number;turn=$_.turn;start=$_.points[0];end=$_.points[-1];next=$_.next;fromNode=$_.fromNode;toNode=$_.toNode}})
$eastIds=@('local:8265:6305:9056:10851','editor:segment:bG9jYWw6ODI2NTo2MzA1OjkwNTY6MTA4NTE:ed2c13ae72fd47778c56a1615168fbfa','editor:segment:bG9jYWw6ODI2NTo2MzA1OjkwNTY6MTA4NTE:c1b0d0a926574cd3992859201dc10032')
$east=@(foreach($id in $eastIds){($lanes|Where-Object id -eq $id).points})
$stations=@(for($at=0.;$at -le (Length $east);$at+=50){$q=Sample $east $at;[ordered]@{station=$at;x=$q.x;z=$q.z;meaning='distance along current eastbound/northbound lane from its J8066 departure; not field measurement'}})
$junctions=@($p.baseline.junctionRules|Where-Object id -in @('J8066_6364','J8921_10835','J7355_12821','J7292_13794','J7254_14780')|Select-Object id,x,z)
$out=[ordered]@{mapRevision=$p.document.revision;reference='Current authored geometry; no pano coordinates inferred';stationOrigin='local:8265:6305:9056:10851 start at x826.463 z630.479, after J8066';laneRows=$rows;eastboundArcStations=$stations;junctions=$junctions}
$out|ConvertTo-Json -Depth 20|Set-Content "$PSScriptRoot/cyan-stations.json" -Encoding utf8
$b=[Drawing.Bitmap]::new(1600,1400);$g=[Drawing.Graphics]::FromImage($b);$g.SmoothingMode='AntiAlias';$g.Clear([Drawing.Color]::FromArgb(248,249,245));$f=[Drawing.Font]::new('Malgun Gothic',12);$sm=[Drawing.Font]::new('Malgun Gothic',10);$big=[Drawing.Font]::new('Malgun Gothic',22,[Drawing.FontStyle]::Bold)
$g.DrawString('하늘색 도로 · 현재 차로/거리 기준점',$big,[Drawing.Brushes]::Black,25,20)
$g.DrawString('s = J8066 출구 차로 시작부터의 게임 경로 거리(m). 실제 전환점·거리뷰 촬영점이 아닙니다.',$f,[Drawing.Brushes]::DimGray,25,65)
$scale=1.1;$xmin=650;$zmin=550;$bottom=1200;$left=40
function Pts($points){return ,([Drawing.PointF[]]@($points|ForEach-Object {[Drawing.PointF]::new(($left+($_.x-$xmin)*$scale),($bottom-($_.z-$zmin)*$scale))}))}
$clip=$g.Save();$g.SetClip([Drawing.RectangleF]::new(0,105,840,1180))
foreach($r in $p.baseline.roads){if($r.points.Count -lt 2){continue};$pen=[Drawing.Pen]::new([Drawing.Color]::FromArgb(217,222,218),2);$g.DrawLines($pen,(Pts $r.points));$pen.Dispose()}
foreach($l in $lanes){if($l.points.Count -lt 2){continue};$color=if($l.id -like 'reviewed:*' -or $l.id -like 'join:reviewed:*'){[Drawing.Color]::Crimson}elseif($l.id -like 'join:*'){[Drawing.Color]::DarkOrange}else{[Drawing.Color]::FromArgb(20,175,204)};$pen=[Drawing.Pen]::new($color,3);$g.DrawLines($pen,(Pts $l.points));$pen.Dispose()}
foreach($s in $stations){$x=$left+($s.x-$xmin)*$scale;$y=$bottom-($s.z-$zmin)*$scale;$g.FillEllipse([Drawing.Brushes]::White,($x-4),($y-4),8,8);$g.DrawEllipse([Drawing.Pens]::Navy,($x-4),($y-4),8,8);$g.DrawString(('s'+$s.station),$sm,[Drawing.Brushes]::Navy,($x+7),($y+6))}
foreach($j in $junctions){$x=$left+($j.x-$xmin)*$scale;$y=$bottom-($j.z-$zmin)*$scale;$g.FillEllipse([Drawing.Brushes]::White,($x-7),($y-7),14,14);$g.DrawEllipse([Drawing.Pens]::Black,($x-7),($y-7),14,14);$g.DrawString($j.id,$f,[Drawing.Brushes]::Black,([math]::Max(5,$x-140)),($y-25))}
$g.Restore($clip)
# Clear sidebar after background lines to preserve text readability.
$g.FillRectangle([Drawing.Brushes]::White,850,105,750,1180)
$y=120;$g.DrawString('차로 단면 그룹 (실경 미확정)',$f,[Drawing.Brushes]::Black,880,$y);$y+=40
foreach($j in $junctions){$g.DrawString(('{0}  x{1:F1} z{2:F1}' -f $j.id,$j.x,$j.z),$f,[Drawing.Brushes]::Black,880,$y);$y+=30}
$y+=20;foreach($s in $stations){$g.DrawString(('s{0,3}  x{1:F1} z{2:F1}' -f $s.station,$s.x,$s.z),$sm,[Drawing.Brushes]::Navy,880,$y);$y+=25}
$g.DrawString('빨강: r19 포켓/임시 사유지 진입. 주황: 교차로 연결.',$f,[Drawing.Brushes]::Black,880,1050)
$g.DrawString('J8066~J8921 호 내부에는 원본 교차로가 없습니다.',$f,[Drawing.Brushes]::Black,880,1085)
$g.DrawString('출입구 실경 정렬 후 새 분기/전환 범위 결정 필요.',$f,[Drawing.Brushes]::Black,880,1120)
$g.DrawString('원본 NGII / OSM contributors, ODbL · 기존 노면 좌표만 사용 · 상세 JSON에 모든 source/next 연결 포함',$sm,[Drawing.Brushes]::DimGray,25,1320)
$b.Save("$root/docs/content-design/images/cyan-stations.png",[Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$b.Dispose()
"CYAN_STATIONS_OK lanes=$($rows.Count) stations=$($stations.Count) arcLength=$([math]::Round((Length $east),2))"
