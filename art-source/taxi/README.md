# Blender 택시 원본

사용자 승인된 노란 소형 택시 시안을 참고해 이번 작업에서 직접 제작한 모델. 외부 모델·텍스처 구매/다운로드 없음.

- TaxiConcept.blend: 편집 가능한 Blender 원본. 현재 최종 씬 TaxiConcept_Blender.005, 이전 반복 씬과 최초 기본 씬은 보존.
- TaxiConcept.fbx: 현재 최종 씬 차량만 내보낸 교환용 모델(바퀴4개 포함).
- unity-meshes.json: Blender 평가 메시/코너 노멀을 Unity 좌표로 변환한 버전1 입력. 차체와 바퀴1개 템플릿. Unity 게임에서는 이 JSON을 읽지 않는다.
- Unity Assets/Resources/TaxiConcept: 고정 GUID의 Mesh/Material/Body·Wheel prefab. 7개 공유 재질, 차체6+바퀴3개 렌더 그룹. 차체55,456tri, 바퀴13,584tri/개, 한 차량109,792tri. 별도 LOD/저사양 성능 튜닝은 후속.
- TAXI·한글 번호판 글씨와 방향지시등은 기존 Unity 표현을 유지한다. Blender 스튜디오에 글씨가 없는 것은 런타임 결합과 구분한다.

## 재생성
Blender MCP 또는 Blender Python에서 TAXI_REPO_ROOT를 의도한 저장소 루트의 절대 경로로 지정한다. 두 스크립트는 ProjectVersion.txt 존재로 경로를 검사하며 지정하지 않으면 실패한다.

    TAXI_REPO_ROOT = r'현재 PC의 project-b 저장소 경로'
    exec(compile(open(TAXI_REPO_ROOT+'/tools/art/build_taxi_blender.py',encoding='utf-8').read(),'build_taxi_blender.py','exec'))
    exec(compile(open(TAXI_REPO_ROOT+'/tools/art/export_taxi_blender.py',encoding='utf-8').read(),'export_taxi_blender.py','exec'))

빌드는 새 씬을 생성하여 기존 씬을 보존한다. 내보내기는 현재 TaxiConcept_Blender 접두사 씬만 허용한다. 원본/교환 파일은 이 디렉터리에 갱신한다. Unity 메뉴 Taxi → Art → Import Blender Taxi 또는 TaxiBlenderImport.Import() 실행 후 TaxiArtChecks.Run()으로 검사한다. 기존 prefab/material/mesh 자산 GUID를 보존한다. 일반 플레이어나 빌드에는 Blender/MCP 설치가 필요 없다.

좌표: Blender (x,y,z)→Unity(x,z,-y). 바퀴 템플릿은 기존 WheelCollider 시각물의 +90° Z 회전을 상쇄한 좌표로 보관한다. 휠 중심/반경/물리는 변경하지 않는다.
