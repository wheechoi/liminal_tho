`Docs/B58/` 폴더의 두 파일을 기준으로 「지하 58층」 버티컬 슬라이스를 Unity로 옮겨줘.

- 기획서: `Docs/B58/B58_GDD.md` (먼저 끝까지 읽기)
- 목업: `Docs/B58/B58_mockup.html` (three.js 코드. 방 좌표, 카메라 위치, 이동 속도, "그것"의 AI 규칙, 대사 같은 수치는 여기 있는 값을 그대로 가져올 것)

진행 순서 (단계마다 컴파일 에러 확인 후 짧게 보고하고 다음으로):

1. `feature/b58-slice` 브랜치 생성
2. `Assets/_Project/Scenes/B58.unity` 새 씬. 기획서 9장 좌표대로 방 3개(부엌·서쪽 통로·인형의 방)를 ProBuilder나 기본 큐브로 그레이박스, 라이트·안개 배치
3. 소녀: 일단 목업처럼 기본 도형 조합 + `Player/TankController` (Input System, CharacterController) + `Player/GirlAnimator`
4. `Camera/CameraZone` + `Camera/RailCamera`: 방마다 고정 카메라, 진입 시 하드 컷
5. `Enemy/WeepingStalker`: 기획서 6장 규칙 (8m 각성, 내적 0.55, 1.45m/s, 0.55m에서 잡힘, 복도 입구 리스폰)
6. `Interaction/InspectPoint` + `UI/DialogueBox` (Text Animator 사용) — 11개 대사 입력
7. `UI/EcgHud`: 위험도 계산과 FINE/CAUTION/DANGER 표시
8. PS1 룩: 320×240 RenderTexture 포인트 필터 출력, 정점 스냅 셰이더(URP HLSL), 스캔라인·비네트 풀스크린 패스
9. 플레이 모드로 부엌 → 복도 → 인형의 방까지 한 번 통과되는지 확인하고 커밋

규칙: CLAUDE.md를 따를 것. 씬·프리팹은 Unity-MCP 도구로 수정, `.meta` 건드리지 말 것. 이미 설치된 Feel / Text Animator / Nice Vibrations / AllIn1 셰이더는 쓸 수 있으면 쓰되, 맞지 않으면 직접 작성해도 됨. 아트 에셋은 아직 없으니 텍스처는 목업처럼 단색·노이즈로 임시 처리하고, 교체할 자리는 목록으로 남겨줘.
