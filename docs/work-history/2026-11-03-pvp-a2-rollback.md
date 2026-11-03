# PVP A2 복원·참가자 끊김 상태 정정

## 요청·구현
사용자 지시로 A3 이전 A2 맵을 복원했다. 복원 기준은 A3 도입2807cce의직전 main5a5df15다. PvpCityMap.cs/PvpCityRoadBuilder.cs를 해당 blob과바이트일치하게복원하고 MapCatalog표기를A2로바꿨다. A2 geometry fixture도같은버전으로복원했다. 최신참가자바퀴표시/공통교통버그수정·싱글·승객·회복·다른채팅작업은되돌리지않는다. 새 PvpMapRollbackBuild는동일eungye배포BuildAt helper다. 기존A3테스트/시안은이전단계기록이며현재지도수용에사용하지않는다.

## 최소 검사
- Unity2022.3.62f2 PvpMapRollbackBuild.Build return code0/Windows PROTOTYPE_BUILD_OK. A2연결/기하71646, 차로선택37, 출발10, 유지되는wheel8/pose11 통과.
- `pwsh -File tools/test_pvp_native_lan.ps1 -Executable artifacts/pvp/a2-rollback-candidate/EungyeTaxi.exe -MotionOnly -MotionSeconds 10 -TimeoutSeconds 240 -Port 17983`: exit0/host19+client18. 증거native-lan-20261103-143705-5a3b61df. native페달초기14m 및추가85.862m/실제이동8.608초. 추가10초관찰중정차구간은이동에서제외한다. 위치제어없음. 화면수용없이숨김실행으로접속/물리/복제정상만확인했다. 4대운송/승객/전체캠페인검사는반복하지않았다.

## 끊김 상태·구조
사용자가계속끊김을보고했으므로참가자끊김은미해결이다. 앞선짧은loopback의frame평균/정지0수치를일반해결판정으로사용하지않는다. 이번A2복원도끊김해결수용이아니다.
현재PvpNativeLan의host가이미서버역할이다. 참가자input약20Hz→hostFixedUpdate물리→snapshot약10Hz→참가자100ms지연보간이며자기차로컬예측이없다. 원인후보는hostframe/송신간격불규칙·수신지터/버퍼고갈·자기차입력왕복대기·같은PC두게임render부하다. 이사용자실행에서각기여도는아직측정하지않았다.
권장후속은실제사용조건의동시두화면/긴주행에서frame/입력왕복/패킷간격/보간버퍼를함께측정하고, 자기차로컬예측+서버검증/보정을설계하는것이다. 다른차/NPC는서버snapshot보간을유지한다. 전용server는host부하분리수단이지만왕복대기/보간지터를자동해결하지않으며이번에server구조를변경하지않았다. Unity공식설명: https://mp-docs.dl.it.unity3d.com/netcode/current/advanced-topics/client-anticipation/ 및 https://mp-docs.dl.it.unity3d.com/netcode/2.2.0/learn/dealing-with-latency/ . 현재SDK에서fullprediction구현/보정비용은별도설계대상이다.

## 배포·보존
현재실행본사용중을확인해사용자직접종료응답을받았다. 게임을강제종료하거나PC입력을조작하지않았다. 최종candidate/통합실행폴더전수hash·manifest확인후게시한다. 사용자추가지시로C:/git/project-b도mainfast-forward반영한다. 그폴더의마인드맵/목차·미추적Unity폴더/파일은사전상태/파일hash를기록하고보존한다. 실제결과는후속영수증을따른다.


## 실행 폴더 반영
실제소스 `b87424f9cc7547d7bd0186b62642bd512d70739f` 확인. 기존139파일115,322,814바이트 검증후 canonical미실행확인·기존폴더를artifacts에보관하고A2후보전체를새복사했다. 후보/배포139파일115,316,158바이트 상대경로·크기·SHA256전수일치. DoNotShip은별도보관. 맵복원과끊김미해결을분리한validation으로manifest를갱신한다.


## 게시·원래 작업 폴더 영수증
실제배포커밋 `c37ad00c94a2ec6f03b4eca396395dbb3b114dcf`, main/feature atomic push exit0·ls-remote동일해시확인. 소스b87424f, A2파일두개는5a5df15와바이트일치하며바퀴/공통후속수정은유지했다. 통합/PVP manifest각exit0(139파일115,316,158바이트).

사용자추가지시에따라 C:/git/project-b의main을5a5df15→c37ad00으로 `git merge --ff-only origin/main` exit0 반영했다. 별도stash/reset/사용자변경커밋없이성공했으며그폴더 manifest VerifyOnly도exit0이다. 사전상태의2개미커밋마인드맵/목차와2개미추적파일은크기/SHA256일치,5개미추적디렉터리는존재유지했다. 원래변경상태가그대로남아있다. 이영수증후속도동일실행본이며최종main을원래폴더에같이fast-forward한다. 참가자끊김은미해결로명시한다.
