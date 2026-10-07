# B58 Unity 슬라이스 — 구현 메모 & 교체 목록

작성일: 2026-10-07 · 브랜치: `feature/b58-slice` · 씬: `Assets/_Project/Scenes/B58.unity`

## 실행
- `B58.unity`를 열고 Play. 조작: ↑↓ 전진/후진, ←→ 회전, Shift 달리기, E/Space/Enter 조사, Q 180° 퀵턴.
- 씬은 `Liminal > Build B58 Scene` 메뉴로 언제든 처음부터 다시 만들 수 있음 (`Scripts/Editor/B58SceneBuilder.cs`). **씬을 손으로 고친 뒤 이 메뉴를 누르면 손댄 내용이 사라짐.**

## 목업과 다른 점
- **좌우 반전**: 좌표는 목업 그대로지만 three.js와 Unity의 좌표계 차이로 화면이 좌우 반전되어 보임. 기획서 9장의 "왼쪽/오른쪽"(마네킹 왼벽, 눈알 오른벽, -58 표지판 오른쪽)은 Unity 쪽과 일치.
- 11번 대사 끝의 "— 목업은 여기까지 —"는 뺌.
- 스캔라인·비네트·플리커는 URP 렌더러 기능 대신 화면 맨 위 UI 덮개(`Liminal/PS1ScreenOverlay` 셰이더) 1장으로 처리. 목업과 같이 HUD 위에도 스캔라인이 덮임.
- 조명 밝기는 목업 값 × 2.5 (`B58SceneBuilder.LightScale`), 가까운 조명은 셰이더에서 부드럽게 눌러 줌.
- 아직 없는 것: 타이틀 화면, 모바일 가상 패드, 음소거 버튼.

## 교체할 자리 (임시 → 실제 에셋)

| 항목 | 지금 | 위치 |
|---|---|---|
| 벽·바닥·천장 텍스처 15종 | 목업 canvas를 옮긴 단색+노이즈 PNG | `Assets/_Project/Textures/B58/T_*.png` |
| 「-58」 표지판 | 픽셀 글꼴 임시 | `T_Sign58.png` |
| 「비상 통로」 안내판 | 한글 대신 주황 블록 | `T_HangingSign.png` |
| 소녀 모델·애니메이션 | 박스/구 조합 + 절차적 흔들기 | 씬 `Girl/Body`, `Player/GirlAnimator.cs` |
| "그것" 모델·애니메이션 | 박스/구 조합 | 씬 `WeepingStalker` |
| 가구·마네킹·테디베어·눈알 | 기본 도형 | 씬 `World/Kitchen`, `World/DollRoom` |
| 한글 폰트 | 실행 시 Windows 폰트(맑은 고딕/Consolas)로 생성, 에디터에서는 □로 보임 | `UI/RuntimeFonts.cs` → Gowun Batang / Nanum Gothic Coding / VT323 에셋으로 교체 |
| 사운드 | 실행 시 합성 (드론·발소리·심장·잡힘) | `Audio/B58Audio.cs`의 clip 칸에 에셋을 넣으면 합성 대신 사용 |

## 스크립트 (`Assets/_Project/Scripts/`)
- `Core/B58Game` — 현재 방, 대사/잡힘 상태, 위험도
- `Player/TankController`, `Player/GirlAnimator`
- `Camera/CameraZone`, `Camera/RailCamera`
- `Enemy/WeepingStalker` — 8m 각성 · 내적 0.55 · 1.45m/s · 0.55m 잡힘 · 복도 입구 리스폰
- `Interaction/InspectPoint`, `UI/DialogueBox` (Text Animator, 초당 38자)
- `UI/EcgHud`, `UI/B58Hud`, `UI/ScreenFx`
- `Rendering/PS1Settings` + 셰이더 `Assets/_Project/Shaders/PS1Lit.shader`, `PS1ScreenOverlay.shader`
- `Environment/EyeWall`, `DollSway`, `LightFlicker`

## 확인한 것 (2026-10-07, 플레이 모드 자동 조종)
- 부엌 → 서쪽 통로 → 인형의 방 통과, 방마다 카메라 하드 컷
- 조사 대사 열기 → 즉시 완성 → 닫기
- 그것: 정면으로 보면 정지, 지나치면 추적, 방을 나가면 원위치, 잡히면 1.8초 뒤 복도 입구(0, 8.9)에서 재시작
