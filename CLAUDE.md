# CLAUDE.md — Liminal_tho

Claude Code가 이 저장소에서 작업할 때 따르는 지침입니다.

## 프로젝트 개요

- **엔진**: Unity 6000.6.4f1 (Unity 6)
- **렌더 파이프라인**: URP 17.6 (PC / Mobile 렌더러 둘 다 있음, `Assets/Settings/`)
- **입력**: New Input System 1.20 (`Assets/InputSystem_Actions.inputactions`)
- **게임**: **Surplus Person (잉여인간)** — 공포 게임. 빌드 이름도 Surplus Person. (폴더·저장소 이름 `Liminal_tho`는 MCP 경로 때문에 유지)
- **현재 상태**:
  - `Assets/_Project/Scenes/SP_1-1_Wake.unity` — 씬 1-1 목업 (소희 기상). 메뉴 `Surplus Person > Build Scene 1-1`로 생성. 메모: `Docs/SurplusPerson/SP_1-1_NOTES.md`
  - `Assets/_Project/Scenes/B58.unity` — 이전 버티컬 슬라이스 「지하 58층」. 메모: `Docs/B58/`
  - 기획 위키: `Docs/SurplusPerson/SP_Wiki.html` (Confluence 스타일, 파일 하나). 폴더 연결 시 `Docs/SurplusPerson/wiki/`에 `wiki.json`(원본)·`INDEX.md`·페이지별 `.md` 자동 저장. 기획서 읽을 땐 이 `.md`를, 밖에서 고칠 땐 `wiki.json` 수정 + `savedAt` 갱신
  - 그래픽 레퍼런스: `Docs/SurplusPerson/refs/` (단면 저택 일러스트, 픽셀 어드벤처, 잉크·디더링 톤)
  - `Assets/Scenes/SampleScene.unity`, `Assets/TutorialInfo/` — 템플릿 잔재, 삭제해도 됨
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

## 에셋 넣기 (사용자가 "이거 넣어줘" 할 때)

사용자가 **로컬 파일·폴더 경로를 주며 넣어달라고 하면**, 종류를 보고 아래 폴더로 **복사**한다 (원본은 그대로 둠). 폴더가 없으면 만든다.

| 종류 | 확장자 예 | 넣는 곳 | 파일 이름 |
|---|---|---|---|
| 인물 모델 (NPC·주인공) | `.fbx` `.dae` `.obj` `.blend` | `Assets/_Project/Meshes/SP/Characters/` | `SP_<인물>_<차림>` (예: `SP_Mom_Poor`) |
| 가구·소품 모델 | 위와 같음 | `Assets/_Project/Meshes/SP/Props/` | `SP_<물건>` |
| 건물·방 모델 | 위와 같음 | `Assets/_Project/Meshes/SP/Environment/` | `SP_<공간>` |
| 텍스처·이미지 (게임에 쓰는 것) | `.png` `.jpg` `.tga` `.psd` | `Assets/_Project/Textures/SP/` (인물용은 `Textures/SP/Characters/`) | `T_SP_<이름>` |
| UI 이미지·아이콘 | `.png` | `Assets/_Project/Textures/SP/UI/` | `UI_SP_<이름>` |
| 재질 | `.mat` | `Assets/_Project/Materials/SP/` | `M_SP_<이름>` |
| 애니메이션 | `.anim`, 애니메이션만 든 `.fbx` | `Assets/_Project/Animations/SP/` | `A_SP_<인물>_<동작>` |
| 효과음 | `.wav` `.ogg` `.mp3` | `Assets/_Project/Audio/SP/SFX/` | `SFX_<이름>` |
| 배경음 | 위와 같음 | `Assets/_Project/Audio/SP/BGM/` | `BGM_<이름>` |
| 대사 음성 | 위와 같음 | `Assets/_Project/Audio/SP/Voice/` | `VO_<인물>_<번호>` |
| 폰트 | `.ttf` `.otf` | `Assets/_Project/Fonts/` | 원래 이름 |
| 그래픽 레퍼런스 (게임에 안 들어감) | 이미지 | `Docs/SurplusPerson/refs/` | 원래 이름 |
| 에셋스토어·유니티 패키지 | `.unitypackage` | 직접 풀지 말 것 → 사용자에게 유니티에서 더블클릭해 Import 하도록 안내 | — |

- 종류가 애매하면(예: 모델+텍스처가 든 압축 파일) 풀어서 각각 위 규칙대로 나누고, 그래도 모르겠으면 넣기 전에 물어본다.
- 같은 이름이 이미 있으면 덮어쓰기 전에 물어본다.
- `.meta`는 만들지 않는다. 유니티 에디터가 켜져 있으면 MCP `assets-refresh`로 읽어 들이게 해서 유니티가 만들게 한다 (만들어진 `.meta`는 에셋과 같이 커밋).
- 넣은 뒤에는 **넣은 파일의 전체 경로 목록**과 폴더 `file:///` 링크를 사용자에게 알려주고, `Docs/SurplusPerson/ASSETS.md` 목록에 한 줄씩 추가한다.
- NPC 모델은 작업실(`Docs/SurplusPerson/SP_NPC_workshop.html`)에서 만들고 `Tools/NpcExport/export_npc_models.ps1`로 다시 뽑는다 (작업실 "설정 복사" 값을 `-Settings`로 주면 고른 색·키 반영).

## 사용자 정보
- 작업자는 서사/기획 쪽 배경. 코드 설명은 짧고 쉽게, 결과 위주로 보고할 것.
- 응답 언어: 한국어
