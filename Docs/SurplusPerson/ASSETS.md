# Surplus Person 에셋 목록

프로젝트에 넣은 에셋과 출처. 폴더 규칙은 `CLAUDE.md`의 "에셋 넣기" 참고.

## 인물 모델 — `Assets/_Project/Meshes/SP/Characters/`

NPC 작업실(`Docs/SurplusPerson/SP_NPC_workshop.html`)에서 만든 모델. 형식은 `.dae`(콜라다)로, 유니티가 별도 패키지 없이 읽는다.

| 파일 | 인물 | 차림 | 키 | 넣은 날 |
|---|---|---|---|---|
| `SP_Sohee_Basic.dae` | 소희 | 기본 (잠옷 원피스) | 145cm | 2026-10-21 |
| `SP_Sohee_Poor.dae` | 소희 | 가난 (빛바랜 원피스) | 145cm | 2026-10-21 |
| `SP_Mihee_Basic.dae` | 미희 (언니) | 기본 (가디건·주름치마) | 160cm | 2026-10-21 |
| `SP_Mihee_Poor.dae` | 미희 (언니) | 가난 (늘어난 스웨터) | 160cm | 2026-10-21 |
| `SP_Mom_Basic.dae` | 엄마 | 기본 (블라우스·진주 목걸이) | 162cm | 2026-10-21 |
| `SP_Mom_Poor.dae` | 엄마 | 가난 (앞치마·머릿수건) | 162cm | 2026-10-21 |
| `SP_Dad_Basic.dae` | 아빠 | 기본 (셔츠·조끼·넥타이) | 177cm | 2026-10-21 |
| `SP_Dad_Poor.dae` | 아빠 | 가난 (러닝셔츠·멜빵) | 177cm | 2026-10-21 |

- 모델 안 구조: `SP_<인물>_<차림>` → `Size` → `Pose` → `Body` → `Hip_L/R` → `Knee_L/R`, `Shoulder_L/R` → `Elbow_L/R` (미희는 `Ponytail` 추가). 관절 그룹을 돌리면 걷기·앉기 동작을 만들 수 있다 (뼈대·스킨 방식은 아님).
- 재질은 옷 부위마다 하나 (예: `SP_Mom_Poor_apron`). 유니티에서 재질 색을 바꾸면 그 부위 색이 바뀐다.
- 다시 뽑기: `powershell -ExecutionPolicy Bypass -File Tools\NpcExport\export_npc_models.ps1` (작업실 "설정 복사" 값을 파일로 저장해 `-Settings <파일>`로 주면 고른 색·키 반영). 같은 이름으로 덮어써서 `.meta`(GUID)는 유지된다.

## 블렌더 모델 (목업용) — `Docs/SurplusPerson/models/`

블렌더 스크립트로 만든 모델. 목업 HTML이 `.js` 파일로 읽는다. 아직 유니티에는 안 들어감 (필요하면 같은 스크립트에서 `.fbx`로 뽑을 수 있음).

| 파일 | 내용 | 만든 곳 | 넣은 날 |
|---|---|---|---|
| `sp_flesh_room.js` | 살덩이 방: 길이 35m 내장 굴 + 얼굴 문(벌린 입·이빨·눈알 두 개, 유니티 이름 `SP_Flesh_FaceDoor`) + 살에 먹힌 입구 문(`SP_Flesh_EntranceDoor`) | `Tools/Blender/sp_flesh_room.py` | 2026-11-18 |

- 다시 뽑기: `C:\Users\ADmiN\Tools\Blender-5.2.4\blender.exe -b --factory-startup --python Tools\Blender\sp_flesh_room.py` (뒤에 `-- --preview <폴더>`를 붙이면 확인용 그림도 그림)

## 그래픽 레퍼런스 — `Docs/SurplusPerson/refs/` (게임에 안 들어감)

| 파일 | 내용 | 넣은 날 |
|---|---|---|
| `1.webp` ~ `7.png` | 단면 저택 일러스트, 픽셀 어드벤처, 잉크·디더링 톤 등 첫 레퍼런스 7장 | 2026-10-10 |
| `be9554f1f7376513f51e2fcd1a345539.jpg` | PS1풍 어두운 부엌 (초록빛 실내, 창밖 붉은 숲) — 저장소 맨 위 `이미지/`에 있던 것을 옮김 | 2026-10-22 |
