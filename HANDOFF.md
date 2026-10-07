# Liminal_tho 작업 전달 문서

작성일: 2026-10-07

## 1. 지금까지 한 것

| 항목 | 상태 | 비고 |
|---|---|---|
| `.gitignore` (Unity용) | ✅ 완료 | Library/Temp/Logs/UserSettings, csproj/slnx 제외 |
| `.gitattributes` | ✅ 완료 | 이미지·모델·오디오·폰트 → Git LFS, 씬/프리팹 줄바꿈 LF |
| GitHub 저장소 | ✅ 생성됨 | `wheechoi/liminal_tho` (private) |
| Unity-MCP 패키지 등록 | ✅ manifest 반영 | `com.ivanmurzak.unity.mcp` 0.93.2 + OpenUPM 레지스트리 |
| Unity-MCP 실제 설치 | ✅ 완료 | 에디터에 설치됨 |
| Claude Code ↔ 유니티 MCP 연결 | ✅ 확인 (2026-10-07) | 씬 목록·콘솔 로그 조회 성공 |
| `CLAUDE.md` | ✅ 완료 | 프로젝트 루트. Claude Code가 자동으로 읽음 |
| Git 설치 | ✅ 완료 | Git 2.55 + LFS (winget) |
| 첫 push | ✅ 완료 (2026-10-07) | `main` 브랜치, 커밋 `966e78b` |

### 프로젝트 현황
- Unity 6000.6.4f1, URP 17.6, Input System 1.20
- URP 기본 템플릿 상태 — 씬 1개(`SampleScene`), 게임 코드 없음

## 2. 남은 할 일 (순서대로)

### 2-1. Unity-MCP 설치 확인
1. 유니티 에디터에서 `Liminal_tho` 프로젝트를 열거나 창을 클릭해 활성화
2. 패키지 다운로드가 끝나면 메뉴에 **Window > AI Game Developer** 가 생김
3. 콘솔에 빨간 에러가 있으면 메시지를 복사해 Claude에게 전달

### 2-2. GitHub에 첫 업로드 (PowerShell)
```powershell
cd C:\Users\user\Desktop\Liminal_tho
git lfs install
git init
git add .
git commit -m "Initial commit"
git branch -M main
git remote add origin https://github.com/wheechoi/liminal_tho.git
git push -u origin main
```
- Git이 없으면 https://git-scm.com 에서 설치
- 첫 push 때 뜨는 브라우저 창에서 GitHub 로그인

### 2-3. Claude Code 설치 및 연결
```powershell
npm install -g @anthropic-ai/claude-code
cd C:\Users\user\Desktop\Liminal_tho
claude
```
- 처음 실행하면 로그인 안내가 나옴
- `CLAUDE.md`를 자동으로 읽고 프로젝트 규칙을 따름

### 2-4. Claude Code ↔ 유니티 MCP 연결
`Window > AI Game Developer` 창에서 **Auto-generate (Skills)** 또는 **Configure**를 누르면 자동 설정됩니다.
수동으로 할 경우(포트는 창에 표시된 값):
```powershell
claude mcp add ai-game-developer "C:/Users/user/Desktop/Liminal_tho/Library/mcp-server/win-x64/gamedev-mcp-server.exe" port=<포트> client-transport=stdio
```
확인: Claude Code 안에서 `/mcp` 입력 → `ai-game-developer`가 connected면 성공

### 2-5. (선택) 클라우드 Claude에서 push 허용
https://github.com/apps/claude/installations/select_target 에서 Claude 앱을 `liminal_tho`에 설치하면 claude.ai에서도 커밋·PR 가능.

## 3. 주의사항
- 프로젝트 경로에 **공백이 들어가면 Unity-MCP가 동작하지 않음** — 폴더 이름 바꾸지 말 것
- `.meta` 파일은 지우지 말 것 (에셋 참조가 끊김)
- MCP 도구는 유니티 에디터가 **켜져 있을 때만** 작동
