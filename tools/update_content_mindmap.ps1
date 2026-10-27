param([switch]$ValidateOnly)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskPath=Join-Path $taskRoot 'docs/content-design/택시 시뮬레이션.mmst'
$taskArchive=Join-Path $taskRoot 'docs/design-history/content-design/2026-10-27-mindmap-before-update.mmst'
$taskBase='https://github.com/herothgf-cell/project-b/blob/main/'
$taskMap=Get-Content -LiteralPath $taskPath -Raw | ConvertFrom-Json
function Put($n,$key,$value){$n|Add-Member -NotePropertyName $key -NotePropertyValue $value -Force}
function New-Topic($topic){[pscustomobject]@{id=[guid]::NewGuid().ToString();topic=$topic;children=@();createdAt=[DateTimeOffset]::Now.ToUnixTimeMilliseconds();modifiedAt=[DateTimeOffset]::Now.ToUnixTimeMilliseconds()}}
function Child($n,$topic){$c=@($n.children|Where-Object topic -eq $topic);if($c.Count){return $c[0]};$c=New-Topic $topic;Put $n children @(@($n.children)+$c);return $c}
function Find-Topic($topic){$script:found=$null;function Visit($n){if($n.topic -eq $topic){$script:found=$n;return};foreach($c in $n.children){Visit $c}};Visit $taskMap.root;return $script:found}
function Move-Topic($topic,$target){$n=Find-Topic $topic;if(!$n){return};function Remove-Topic($p,$id){Put $p children @($p.children|Where-Object id -ne $id);foreach($c in $p.children){Remove-Topic $c $id}};Remove-Topic $taskMap.root $n.id;Put $target children @(@($target.children)+$n)}
function Rename($old,$new){$n=Find-Topic $old;if($n){Put $n topic $new}}
if(!$ValidateOnly){
 # Preserve the exact starting document, including the user's uncommitted edits.
 if(!(Test-Path -LiteralPath $taskArchive)){[IO.File]::Copy($taskPath,$taskArchive)}
 $future=Child $taskMap.root '후속 구상 (현재 Proto 제외)'
 $reference=Child $taskMap.root '제작·검토 참고 (플레이 기획과 분리)'
 foreach($topic in @('PVP 모드','이벤트 요소','차량 구매','차량 용품점','화장실','너무 배부름','대출 및 카드 대금 처리 방식','보험 시스템','차량 내부 조작','차량 정비 소모품','차량 내부 인테리어 아이템 구매 시스템','세차 용품 ','비 / 눈 ','내구도?','운전 험하게 하는 차량','버스 같은 차량 추가','단골·개인 예약 (후속 기획)','추가 아이디어','보험료 할인','PVP 모드 승패에 영향','보행자 사고 패널티 (보험, 치료비 등 지불됨)','연료 타입 및 주유비 변동성','실제 주유처럼 하는 UI')){Move-Topic $topic $future}
 foreach($topic in @('개발 검증','Unity Scene 3D 지도 편집','좌우로 많이 흔들림')){Move-Topic $topic $reference}
 $goal=Find-Topic '게임의 목표';Move-Topic '기획 메모' $goal
 $requests=Find-Topic '요청 17종·선택 및 평가'
 foreach($topic in @('운송 변경 요청','분실물 후속 안내·텍스트 기록','불법 요구 안전 거절')){Move-Topic $topic $requests}
 $care=Find-Topic '차량 관리';Move-Topic '청결도' $care
 $names=@{
 'Poroto : 3일간 운용'='Proto: 3일간 영업';'생계 안정??'='안전 운송·생활 선택 후 남긴 수익과 상태 비교';
 '돈을 벌어서 어디에 사용하고 무엇을 하나?'='연료·식사·세차·주차 지출과 남긴 돈·상태 비교';
 '택시 시야가 좁아지거나, 졸음 운전으로 조작이 제대로 되지 않음'='빠른 눈감김·재개방 반복 → 최종 서서히 암전';
 '피로 한계의 신규 호출·승차 제한'='피로100 초기 영업 가능·누적120초부터 신규 승차 차단';
 '탑승 중 피로 한계에도 운송 유지'='초기 운송 유지·누적120초 안전 정차 후0원 취소';
 '수면 후 영업 재개'='정상 수면은 계속 영업·완전 암전 귀가 휴식은 당일 마감';
 '배고픔'='포만감10~25미만 피로1.5배·10미만2배 (중첩 없음)';
 '음식별 효과 정리'='음식3종의 가격·시간·포만감 회복 동일';
 '생활 거점 (집)'='휴식 거점 (집 실내 콘텐츠 아님)';'수면 (피로도 회복)'='주차장·휴식 구역 수면/영업 종료 참조';
 '남은 영업시간 확인'='예정시간 후에도 이용 가능·실제 이용 시간 반영';
 '안전 승하차 후보·호출 다양화'='승하차 장소24후보·실제 대기자/접근 가능한 호출';
 '일반 영업용 안전 승하차 후보 확대'='기존12·추가 게임용12후보 (현장 허가와 구분)';
 '시간·연료·피로를 고려한 일반 호출 구성'='운송·생활 상태는 담당 기획 참조';
 '식사·주차·필수 지원의 잔액 부족 처리'='식사·세차·주차·필수 지원 부족금 미납·주유는 현금 필요';
 'H 조향 보조'='H 조향만 보조·가속/제동/양보는 직접';
 '출구 정체 상황'='일반 영업은 팝업 없이 판단·별도 연습에서 선택 비교';
 '승/하차 도달 시간 및 실패에 따른 처리'='픽업 만료와 탑승 후 지각 구분·지각 운송 유지/0원';
 '청결도'='청결·세차 (상세 기준)';
 '마감·마지막 승객 하차 후 정산'='정상 마감은 하차 후 거점·졸음 안전 취소/귀가 마감은 예외';
 '다음 날 현금·연료·컨디션·운행 점수 이월'='현금·미납·연료·포만감·청결·운전 점수 이월·야간 피로 회복';
 '하루 시작·정산 경계 저장/복원'='운송·위치·요청·시간·생활 상태 이어하기';
 '운행 점수 사건 즉시 저널'='운행 기록 유지';'저장 실패 보존·재시도'='저장 오류 시 원본 기록 보존';
 '법규 위반 사례 히스토리 및 근거 벙렵 사례'='법규 사건·근거·보류 사유·회고';
 '초기 만족도 범위를 랜덤하게 설정'='요구치보다 낮음/같음/높음 가능·재수락/이어하기 시작값 유지';
 '운임 요금에 보너스'='요구 미달 감액·충족 전액/보너스·0점/지각0원 (운임 규칙 참조)';
 '운임·기한·요구 만족도 확인 후 수락'='목록은 출발/도착·예상 운임·선택 미리보기와 수락 별도';
 '주행 HUD·게이지·두 점수 분리'='현재 승객 만족도/요구치와 누적 운전 점수 구분';
 '난이도?'='승객별 시간 난도 (운송 제한 시간 참조)';
 '우회전 시 서행 의무를 지키지 않음'='서행 판단 안내·자동 감점 기준 미확정';
 '지도 오류 검사·저장·복원'='개인 지도 편집·기록 보존';'별도 시험 주행·영업 상태 분리'='개인 지도 시험은 본 영업과 분리'
 }
 foreach($old in $names.Keys){Rename $old $names[$old]}
 $fatigue=Find-Topic '피로도'
 foreach($topic in @('첫 위험 안내는 짧게1회·이후 택시 위 Zzzz','깊은 눈감김 중 가속·조향 일시 불응·제동 가능·눈 뜨면 복귀','완전 암전180초·긴급 귀가100초/피로-20·당일 마감')){[void](Child $fatigue $topic)}
 $region=Find-Topic '지역';[void](Child $region '초반 주거·중반 업무·후반 상업 상대 수요3:나머지1');[void](Child $region '수입 보장·운임 할증 없음')
 foreach($r in $region.children|Where-Object topic -in @('상업 지역','주거 지역','업무 지역')){foreach($c in $r.children){if($c.topic -eq '승객 유형 및 등장 시간, 승하차 지역 등에 영향'){Put $c topic '게임 상대 수요의 해당 지역 분류 (대표 수요 규칙 참조)'}}}
 $clean=Find-Topic '청결·세차 (상세 기준)';foreach($topic in @('활성 영업120초당 청결-1·일반 하차-2·구토 시 최대10','청결80이상 승차 만족도+3·30미만 승차 거부/호출 잠금','세차100 회복·구토/거부 잠금 해제')){[void](Child $clean $topic)}
 $account=Find-Topic '수익 관리를 볼 수 있는 가계부';[void](Child $account '공차/탑승 거리·미탑승 이동/대기·서비스 시간')
 $limit=Find-Topic '운송 제한 시간';[void](Child $limit '여유·보통·급함 승객별 기한')
 $stop=Find-Topic '안전 승차·하차 판정';[void](Child $stop '공간 일시 점유→해소 대기·기한 보호1회 (대체 이동은 후속)')
 [void](Child $requests '운송별0~2건·승차/중간 안전 정차·상충 제외·개별 결과 합계');[void](Child $requests '분실물은 하차 후 별도 사건')
 $walkers=Find-Topic '승객 외 보행자 추가';[void](Child $walkers '일반 녹색 출발·횡단 중 적색 완주·지정1명 드문 무단횡단')
 $proto=Find-Topic 'Proto';if($proto){[void](Child $proto '역주행');[void](Child $proto '적색 정지선 침범 후 정차·운전 점수-1')}
 $effect=Find-Topic '점수에 따른 효과';[void](Child $effect '다음 승차 운임 보정·차량 성능/피로는 변경 없음')
 $ux=Find-Topic '도움말·패널 가독성';foreach($topic in @('점유 안내/대기 버튼·생활 안내를 TIP 하나로 통합','TIP 왼쪽 슬라이드 숨김·가장자리 탭으로 복귀','신호등 상부 표지·충분한 크기/고대비 한글','시설 한글 줄바꿈·짧은 경고 후 상태 연출')){[void](Child $ux $topic)}
 $route=Find-Topic '내비게이션·경로 선택';[void](Child $route '좌회전 금지 유지·연결된 서쪽 유턴 후 복귀/우회전 안내')
 # Refresh native Detail-Links on every node, not merely Markdown note links.
 function Refresh($n,$path,$doc,$state){
  $p=if($path){$path+' > '+$n.topic}else{$n.topic}
  if($n.topic -eq '후속 구상 (현재 Proto 제외)'){$doc='docs/future-design/README.md';$state='후속 구상·현재 Proto 제외'}
  elseif($n.topic -eq '제작·검토 참고 (플레이 기획과 분리)'){$doc='docs/technical-reference/README.md';$state='제작/검토 참고·플레이 규칙 아님'}
  elseif(!$path -or $path -eq '택시 시뮬레이션'){
   $doc=switch($n.topic){'맵'{'docs/content-design/05-interface-and-navigation.md'} '운전자'{'docs/content-design/03-life-and-economy.md'} '경제'{'docs/content-design/03-life-and-economy.md'} '차량'{'docs/content-design/03-life-and-economy.md'} '주행 & 운송'{'docs/content-design/01-driving-and-traffic.md'} '운전 점수'{'docs/content-design/01-driving-and-traffic.md'} '승객&보행자'{'docs/content-design/02-passengers-and-fares.md'} '캠페인'{'docs/content-design/04-campaign-and-feedback.md'} 'UI/UX'{'docs/content-design/05-interface-and-navigation.md'} default{'docs/content-design/00-game-overview.md'}}
  }
  if($state -eq '현재 플레이 기획'){
   if($p -match '맵 > 주요 시설|맵 > 지역'){$doc='docs/content-design/03-life-and-economy.md'}
   elseif($p -match '운임 요금 규칙|運送|안전 승차·하차 판정|운송 제한 시간'){$doc='docs/content-design/02-passengers-and-fares.md'}
   elseif($p -match '내비게이션·경로 선택|운송 후 생활 목적지 추천'){$doc='docs/content-design/05-interface-and-navigation.md'}
  }
  $external=@(@($n.hyperlink)+@($n.hyperlinks)|Where-Object {$_ -and $_ -notmatch 'github.com/herothgf-cell/project-b/'})
  Put $n hyperlink ($taskBase+$doc);Put $n hyperlinks $external
  Put $n note ("## $state`n`n"+$n.topic+"`n`n상세 행동·결과·영향은 Detail → Links의 최신 기획서를 기준으로 합니다. 현재/후속 구분은 상위 가지를 따릅니다. 변경 전 원안과 과거 검토 메모는 별도 기획 이력의 마인드맵 보존본에 남겼습니다.")
  Put $n modifiedAt ([DateTimeOffset]::Now.ToUnixTimeMilliseconds())
  if($state -ne '현재 플레이 기획'){Put $n tags @('후속/참고');if($n.task){Put $n.task progress 0}}
  foreach($c in $n.children){Refresh $c $p $doc $state}
 }
 Refresh $taskMap.root '' 'docs/content-design/README.md' '현재 플레이 기획'
 [IO.File]::WriteAllText($taskPath,($taskMap|ConvertTo-Json -Depth 100)+"`n",[Text.UTF8Encoding]::new($false))
}
$taskIds=[Collections.Generic.HashSet[string]]::new();$script:taskCount=0;$script:taskLinks=0
function Check($n){if(!$taskIds.Add($n.id)){throw "Duplicate node ID: $($n.id)"};$script:taskCount++;foreach($url in @($n.hyperlink)+@($n.hyperlinks)){if(!$url){continue};$script:taskLinks++;if($url.StartsWith($taskBase)){$relative=[Uri]::UnescapeDataString($url.Substring($taskBase.Length).Split('#')[0]);if(!(Test-Path -LiteralPath (Join-Path $taskRoot $relative))){throw "Missing Detail-Link: $url"}}};foreach($c in $n.children){Check $c}}
Check $taskMap.root
if(Test-Path -LiteralPath $taskArchive){$before=Get-Content -LiteralPath $taskArchive -Raw|ConvertFrom-Json;function OldIds($n){if(!$taskIds.Contains($n.id)){throw "Original user node lost: $($n.topic)"};foreach($c in $n.children){OldIds $c}};OldIds $before.root}
Write-Output "MINDMAP_OK nodes=$script:taskCount links=$script:taskLinks original IDs preserved"
