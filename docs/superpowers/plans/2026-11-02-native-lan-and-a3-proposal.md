# Native LAN PVP + A3 map proposal Implementation Plan

사용자선택: 같은공유기/직접IP부터·최대4인·빈자리봇. 맵은시안을먼저제시하고확정전에는현재A2로시험한다. 기존WheelCollider/PassengerJobs/법규/시설/내구도와싱글을재사용한다. 새운전모터/새승객체계를만들지않는다.

Architecture: NGO1.12/UnityTransport기존패키지, 호스트권한native4택시물리/공유NPC·승객/정산. 클라이언트는현재UI와카메라를그대로쓰며명령만보내고모델·pose를복제한다. 빠른pose10Hz/자기상세상태2Hz·안정identity·크기제한·명령중복/소유자검증. 계정/중계/SDK추가없음. LAN/UDP직접IP범위이며다른인터넷/NAT자동연결/공개매칭/호스트이전은별도.

- [ ] A3전체도/시장확대/SVG·PNG/제안문서. 실제mapcode변경없음,구역별다른골목/사선/곡선/다중출구·시설진입표시.
- [ ] nativeLAN로비:호스트/참가·IP/포트·인원/시간·호스트시작·전원월드준비후경기진행,빈자리봇. 기존오프라인유지.
- [ ] 서버입력/명령권한:clientID→slot,seq/expectedtrip/candidateSeed검증,remotehost인간키입력차단·끊김시같은택시봇전환,30초ticket재접속.
- [ ] clientreplica:네택시/자기운송·경제·법규/공유호출·점유/NPC·신호·시설상태,client독립평가·정산·파일저장금지. 싱글default분기불변.
- [ ] 마지막필수모델/Editor·실제host/client두프로세스접속/입력/승객명령·상태일치/끊김봇/종료/싱글저장검사,실제다른PC·사람주행별도.
- [ ] BuildHybrid·전체폴더hash/manifest·날짜이력/LATEST·최신main보존통합·commit/push. 다른Cgit UI미커밋은보존하고섞지않음.

기존모터2인시험장네트워크의통과를현재native도시검증으로쓰지않는다. 맵hash/protocol불일치거부,서버만가격/점수/캐릭터위치결정,client는pose수정메시지제출불가. 신뢰LAN프로토·호스트우위/보간지연은고지한다. 사용자지시대로구현후마지막통합검사만수행,실패수정후관련재확인. 독립맵시안/replica/공통UI hooks에dispatching-parallel-agents사용,파일소유구분.
