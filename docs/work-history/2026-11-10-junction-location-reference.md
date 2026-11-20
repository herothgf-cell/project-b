# J866·J5057 위치 안내

- 요구: 두 내부 교차로 ID의 위치를 지도에 표시.
- 실제 작업: 정식 지도 baseline.roads와 junctionRules로 위치 안내 PNG 생성. J866_5285=(86.61,528.49), J5057_4449=(505.70,444.87). 동쪽419.09m·남쪽83.62m, 직선 약427m.
- 결과: `artifacts/location-reference/j866-j5057.png`(Git 제외), 생성 스크립트 같은 폴더 `draw.ps1`. 재현: `pwsh -File artifacts/location-reference/draw.ps1`.
- 검증: PowerShell 생성 exit0, PNG 시각 확인. Python 기본 별칭은 실행 불가하여 System.Drawing 사용.
- 게임/지도/빌드 수정 없음. 플레이·물리·캠페인 검사는 수행하지 않음. 기존 FirstDrive.unity 변경 및 미추적 PackageManagerSettings.asset 보존. 커밋 미수행.
