# CLAUDE.md — Liminal_tho

Claude Code가 이 저장소에서 작업할 때 따르는 지침입니다.

## 프로젝트 개요

- **엔진**: Unity 6000.6.4f1 (Unity 6)
- **렌더 파이프라인**: URP 17.6 (PC / Mobile 렌더러 둘 다 있음, `Assets/Settings/`)
- **입력**: New Input System 1.20 (`Assets/InputSystem_Actions.inputactions`)
- **현재 상태**: URP 기본 템플릿 직후. 게임 코드는 아직 없음.
  - `Assets/Scenes/SampleScene.unity` — 유일한 씬
  - `Assets/TutorialInfo/` — 템플릿 안내용, 삭제해도 됨
- **저장소**: https://github.com/wheechoi/liminal_tho (private)
- **로컬 경로**: `C:\Users\user\Desktop\Liminal_tho` (경로에 공백 금지 — Unity-MCP 요구사항)

## Unity-MCP (AI Game Developer)

에디터 조작은 IvanMurzak/Unity-MCP 플러그인을 통해 합니다.

- 패키지: `com.ivanmurzak.unity.mcp` 0.93.2 (OpenUPM, `Packages/manifest.json`에 scopedRegistry 등록됨)
- 에디터 메뉴: **Window > AI Game Developer**
- Claude Code 연결:
  1. 유니티 에디터에서 프로젝트를 열고 `Window > AI Game Developer`에서 포트 확인
  2. 프로젝트 루트에서:
     ```bash
     claude mcp add ai-game-developer "C:/Users/user/Desktop/Liminal_tho/Library/mcp-server/win-x64/gamedev-mcp-server.exe" port=<포트> client-transport=stdio
     ```
     또는 같은 창에서 `Auto-generate` Skills / `Configure`를 눌러 자동 설정
- MCP 도구는 **유니티 에디터가 켜져 있을 때만** 동작합니다. 연결이 안 되면 먼저 에디터가 열려 있는지, 컴파일 에러가 없는지 확인할 것.

## 작업 규칙

### 파일
- **`.meta` 파일을 절대 지우거나 따로 만들지 말 것.** 에셋을 옮기거나 이름을 바꿀 때는 `.meta`도 같이 옮긴다(GUID가 깨지면 참조가 전부 끊김). 가능하면 MCP 도구나 에디터 API로 옮긴다.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.csproj`, `*.slnx`는 자동 생성물 — 수정·커밋 금지 (`.gitignore`에 등록됨).
- 씬(`.unity`)·프리팹(`.prefab`)은 YAML이지만 손으로 편집하지 말고 MCP 도구로 수정한다.
- 큰 바이너리(png, fbx, wav 등)는 Git LFS로 관리된다 (`.gitattributes`).

### 코드
- 스크립트 위치: `Assets/_Project/Scripts/` 아래에 기능별 폴더 (예: `Player/`, `UI/`, `Systems/`)
- 네임스페이스: `Liminal.<폴더명>`
- 입력은 구 `Input.GetKey` 말고 **Input System**(`InputSystem_Actions`)을 쓴다.
- 에디터 전용 코드는 `Editor/` 폴더 또는 `#if UNITY_EDITOR`로 분리.
- 스크립트를 추가·수정한 뒤에는 유니티가 리컴파일하므로, MCP로 콘솔 에러를 확인하고 다음 단계로 넘어간다.

### Git
- 기본 브랜치: `main`
- 기능 단위로 브랜치를 따서 작업 (`feature/<이름>`), 커밋 메시지는 한국어 가능.
- 커밋 전 확인: 컴파일 에러 없음, 불필요한 `Library/` 변경 없음.

## 사용자 정보
- 작업자는 서사/기획 쪽 배경. 코드 설명은 짧고 쉽게, 결과 위주로 보고할 것.
- 응답 언어: 한국어
