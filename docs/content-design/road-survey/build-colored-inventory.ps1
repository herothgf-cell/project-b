param([string]$Root=(Resolve-Path "$PSScriptRoot/../../..").Path)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$p=Get-Content (Join-Path $Root 'config/maps/project/eungye.project.json') -Raw|ConvertFrom-Json -Depth 100
$groups=@($p.document.lanes|Where-Object {$_.id -like 'local:*' -and !$_.disabled -and !$_.change}|Group-Object sourceId)
$cyan=@(430,641,750,895,928,1116,1120,1193,1380,1498,1547,2059,2071,2080)|ForEach-Object {"37612023:A0020000:${_}:0"}
$orange=@(339,680,1065)|ForEach-Object {"37612023:A0020000:${_}:0"}
$selected=[Collections.Generic.List[object]]::new()
foreach($group in $groups){
 $lanes=@($group.Group);$points=@($lanes|ForEach-Object {$_.points});$name=$lanes[0].roadName
 $xmin=($points|Measure-Object x -Minimum).Minimum;$xmax=($points|Measure-Object x -Maximum).Maximum
 $zmin=($points|Measure-Object z -Minimum).Minimum;$zmax=($points|Measure-Object z -Maximum).Maximum
 $ids=@($group.Name.Split(','));$color=$null;$membership='matched';$status='unreviewed';$note='Source-group geometry matched to annotated map; no new field review.'
 if(@($ids|Where-Object {$_ -in $cyan}).Count -eq $ids.Count){$color='cyan';$status='evidence-existing'}
 elseif(@($ids|Where-Object {$_ -in $orange}).Count -eq $ids.Count){$color='orange'}
 elseif($name -eq '은계로' -or $lanes[0].id -eq 'local:663:5325:21:5358'){$color='green';$status='evidence-existing'}
 elseif(($name -eq '대은로' -and $zmin -gt 1000) -or $name -eq '계수로' -or ($name -eq '은계중앙로' -and $zmin -gt 1480) -or $name -eq '은계중앙1길' -or $lanes[0].id -eq 'local:11605:16081:10719:15876'){$color='yellow'}
 elseif(($name -eq '은행고길' -and $zmax -lt 550) -or $name -eq '은계남로' -or ($name -eq '은계중앙로' -and $zmax -lt 450) -or $name -eq '은계번영길' -or ($xmin -gt 790 -and $xmax -lt 940 -and $zmax -lt 280) -or $lanes[0].id -in @('local:6450:3862:6452:3446','local:7322:2546:7735:2544')){$color='pink';if(($xmin -gt 790 -and $xmax -lt 940) -or $name -eq '은계번영길'){$membership='uncertain';$note='Candidate: user stroke does not establish every small branch or exact endpoint. Confirm numbered junctions.'}}
 if($color){
  $allSourceLanes=@($p.document.lanes|Where-Object {$_.sourceId -eq $group.Name -and !$_.disabled -and !$_.change})
  $allPoints=@($allSourceLanes|ForEach-Object points)
  $selected.Add([ordered]@{color=$color;roadName=$name;sourceIds=$ids;representativeLane=$lanes[0].id;laneIds=@($allSourceLanes.id);localRootLaneIds=@($lanes.id);laneCountMeaning='Active same-source segments including editor/reviewed/joins; not physical lane count';bounds=@(($allPoints|Measure-Object x -Minimum).Minimum,($allPoints|Measure-Object z -Minimum).Minimum,($allPoints|Measure-Object x -Maximum).Maximum,($allPoints|Measure-Object z -Maximum).Maximum);membership=$membership;status=$status;note=$note})
 }
}
# Suin-ro uses imported NGII lanes rather than the local family prefix.
$suin=@($p.document.lanes|Where-Object {$_.roadName -eq '수인로' -and !$_.disabled -and !$_.change -and $_.id -notlike 'join:*'})
$suinPoints=@($suin|ForEach-Object {$_.points})
$selected.Add([ordered]@{color='green';roadName='수인로';sourceIds=@($p.baseline.roads|Where-Object name -eq '수인로'|ForEach-Object id);representativeLane=$suin[0].id;laneIds=@($suin.id);bounds=@(($suinPoints|Measure-Object x -Minimum).Minimum,($suinPoints|Measure-Object z -Minimum).Minimum,($suinPoints|Measure-Object x -Maximum).Maximum,($suinPoints|Measure-Object z -Maximum).Maximum);membership='matched';status='evidence-existing';note='NGII main corridor. Existing junction evidence is partial; not all sections reviewed.'})
$junctionNames=@('J0_10594','J1882_11791','J2564_12237','J3516_12881','J4396_13458','J5692_14220','J7254_14780','J10587_15883','J11723_16107','J13358_17500','J4989_17500','J5852_16963','J7292_13794','J6199_11987','J7801_9629','J8066_6364','J7973_3744','J7917_2565','J7841_867','J5057_4449','J1927_5181','J866_5285','J5074_877')
# Southern junctions remain candidates when the hand-drawn pink footprint is ambiguous.
$junctions=[Collections.Generic.List[object]]::new();$n=0
foreach($id in $junctionNames){$j=$p.baseline.junctionRules|Where-Object id -eq $id;if($j){$n++;$junctions.Add([ordered]@{number=$n;id=$j.id;x=$j.x;z=$j.z;status='unreviewed'})}}
foreach($j in @($p.baseline.junctionRules|Where-Object {$_.x -gt 790 -and $_.x -lt 940 -and $_.z -lt 280}|Sort-Object z -Descending)){if($j.id -notin $junctionNames){$n++;$junctions.Add([ordered]@{number=$n;id=$j.id;x=$j.x;z=$j.z;status='unreviewed';membership='uncertain-pink'})}}
# Append survey points so every existing user-facing number stays stable.
$northJunction=$p.baseline.junctionRules|Where-Object id -eq 'J2145_15970'
if($northJunction){$n++;$junctions.Add([ordered]@{number=$n;id=$northJunction.id;x=$northJunction.x;z=$northJunction.z;status='partial-field-review'})}
$result=[ordered]@{schema=1;revision=$p.document.revision;source='config/maps/project/eungye.project.json';coordinateSystem='world x,z metres; EPSG5186 easting=181900+x northing=537200+z';scope='Five colors from user map; membership is geometry matching, never field verification.';statusPolicy='unreviewed or evidence-existing; existing evidence is partial and not a reviewed claim';evidenceDocuments=@('docs/content-design/road-survey/2026-11-13-color-road-audit.md','docs/content-design/21-lane-movement-roadview-followup.md','docs/work-history/2026-11-10-roadview-lane-audit.md');corridors=@($selected);junctions=@($junctions)}
$jsonPath=Join-Path $PSScriptRoot 'colored-inventory.json';$result|ConvertTo-Json -Depth 20|Set-Content $jsonPath -Encoding utf8
$b=[Drawing.Bitmap]::new(1950,1500);$g=[Drawing.Graphics]::FromImage($b);$g.SmoothingMode='AntiAlias';$g.Clear([Drawing.Color]::FromArgb(248,249,245))
$title=[Drawing.Font]::new('Malgun Gothic',23,[Drawing.FontStyle]::Bold);$font=[Drawing.Font]::new('Malgun Gothic',12);$small=[Drawing.Font]::new('Malgun Gothic',10);$num=[Drawing.Font]::new('Arial',10,[Drawing.FontStyle]::Bold)
$g.DrawString('은계 · 다섯 색 도로 조사 범위 / 교차로 번호',$title,[Drawing.Brushes]::Black,25,15)
$g.DrawString('번호는 실제 교차로 좌표입니다. 차로 전환점이 아닙니다. 점선 분홍 = 범위 확인 후보. 북 ↑',$font,[Drawing.Brushes]::DimGray,25,62)
$colors=@{cyan=[Drawing.Color]::FromArgb(26,181,210);green=[Drawing.Color]::FromArgb(32,180,35);yellow=[Drawing.Color]::FromArgb(220,181,0);orange=[Drawing.Color]::FromArgb(238,100,15);pink=[Drawing.Color]::FromArgb(218,12,145)}
$roadColors=@{};$uncertain=@{};foreach($c in $selected){foreach($id in $c.sourceIds){$roadColors[$id]=$c.color;if($c.membership -eq 'uncertain'){$uncertain[$id]=$true}}}
$scale=.66;$left=28;$bottom=1300
foreach($r in $p.baseline.roads){if($r.points.Count -lt 2){continue};$pts=[Drawing.PointF[]]@($r.points|ForEach-Object {[Drawing.PointF]::new(($left+$_.x*$scale),($bottom-$_.z*$scale))});$pen=[Drawing.Pen]::new([Drawing.Color]::FromArgb(216,221,217),[single]2);$g.DrawLines($pen,$pts);$pen.Dispose()}
foreach($r in $p.baseline.roads){if(!$roadColors.ContainsKey($r.id) -or $r.points.Count -lt 2){continue};$pts=[Drawing.PointF[]]@($r.points|ForEach-Object {[Drawing.PointF]::new(($left+$_.x*$scale),($bottom-$_.z*$scale))});$pen=[Drawing.Pen]::new($colors[$roadColors[$r.id]],[single]5);if($uncertain.ContainsKey($r.id)){$pen.DashStyle='Dash'};$g.DrawLines($pen,$pts);$pen.Dispose()}
foreach($j in $junctions){$x=$left+$j.x*$scale;$y=$bottom-$j.z*$scale;$g.FillEllipse([Drawing.Brushes]::White,($x-10),($y-10),20,20);$g.DrawEllipse([Drawing.Pens]::Black,($x-10),($y-10),20,20);$s=[string]$j.number;$size=$g.MeasureString($s,$num);$g.DrawString($s,$num,[Drawing.Brushes]::Black,($x-$size.Width/2),($y-$size.Height/2))}
$row=105;foreach($key in @('cyan','green','yellow','orange','pink')){$brush=[Drawing.SolidBrush]::new($colors[$key]);$g.FillRectangle($brush,1440,$row,24,14);$g.DrawString($key,$font,[Drawing.Brushes]::Black,1475,($row-4));$brush.Dispose();$row+=27};$row+=12
$g.DrawString('번호 / 교차로 / x, z (m)',$font,[Drawing.Brushes]::Black,1435,$row);$row+=30
foreach($j in $junctions){$label='{0,2}  {1}  ({2:F0}, {3:F0})' -f $j.number,$j.id,$j.x,$j.z;$g.DrawString($label,$small,[Drawing.Brushes]::Black,1435,$row);$row+=24}
$g.DrawString('기존 근거는 부분 확인 자료입니다. 이번 지도는 실경 전수조사 완료를 뜻하지 않습니다.',$font,[Drawing.Brushes]::Black,25,1370)
$g.DrawString('정식 지도 r'+$p.document.revision+' · 원본 NGII / OSM contributors, ODbL · 상세 source/lane IDs 및 범위: colored-inventory.json',$small,[Drawing.Brushes]::DimGray,25,1410)
$out=Join-Path $Root 'docs/content-design/images/eungye-colored-road-audit-map.png';$b.Save($out,[Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$b.Dispose()
Write-Output "corridors=$($selected.Count) numberedJunctions=$($junctions.Count) json=$jsonPath image=$out"
