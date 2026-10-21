# NPC 작업실(Docs/SurplusPerson/SP_NPC_workshop.html)의 인물들을 유니티용 모델(.dae, 콜라다)로 내보낸다.
# 인물 4명(소희·미희·엄마·아빠) × 차림 2가지(기본·가난) = 8개 파일.
#
# 사용:
#   powershell -ExecutionPolicy Bypass -File Tools\NpcExport\export_npc_models.ps1
#   powershell -ExecutionPolicy Bypass -File Tools\NpcExport\export_npc_models.ps1 -Settings 설정.json   # 작업실 "설정 복사" 값으로 색·키 적용
#   ... -OutDir <폴더>                                                                                 # 다른 폴더로 내보내기
#
# 방식: 작업실 HTML에서 NPC 블록(// <NPC-BLOCK> ~ // </NPC-BLOCK>)을 잘라 export_page.html에 끼워 넣고,
#       Edge(또는 Chrome)를 화면 없이 실행해 three.js ColladaExporter 결과를 받아 파일로 쓴다. (인터넷 필요: three.js를 CDN에서 받음)
param(
  [string]$OutDir = "",
  [string]$Settings = ""
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if (-not $OutDir) { $OutDir = Join-Path $root 'Assets\_Project\Meshes\SP\Characters' }
$utf8 = New-Object System.Text.UTF8Encoding $false

$workshop = Join-Path $root 'Docs\SurplusPerson\SP_NPC_workshop.html'
$html = [IO.File]::ReadAllText($workshop, $utf8)
$m = [regex]::Match($html, '(?s)// <NPC-BLOCK>(.*?)// </NPC-BLOCK>')
if (-not $m.Success) { throw "NPC 블록 표시(// <NPC-BLOCK>)를 찾지 못함: $workshop" }

$set = 'null'
if ($Settings) { $set = [IO.File]::ReadAllText((Resolve-Path $Settings).Path, $utf8) }

$tpl = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'export_page.html'), $utf8)
$page = $tpl.Replace('/*NPC_BLOCK*/', $m.Groups[1].Value).Replace('/*SETTINGS*/null', $set)

$tmp = Join-Path $env:TEMP 'sp_npc_export'
New-Item -ItemType Directory -Force $tmp | Out-Null
$pagePath = Join-Path $tmp 'export.html'
[IO.File]::WriteAllText($pagePath, $page, $utf8)

$browser = @(
  "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
  "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe",
  "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
  "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $browser) { throw 'Edge나 Chrome을 찾지 못함' }

$dump = Join-Path $tmp 'dump.html'
$argList = @('--headless=new', '--disable-gpu', '--no-first-run', "--user-data-dir=$tmp\profile",
  '--virtual-time-budget=30000', '--dump-dom', ([Uri]$pagePath).AbsoluteUri)
Start-Process -FilePath $browser -ArgumentList $argList -RedirectStandardOutput $dump -NoNewWindow -Wait | Out-Null
$dom = [IO.File]::ReadAllText($dump, $utf8)

$log = [regex]::Match($dom, '<pre id="log">([^<]*)</pre>').Groups[1].Value
if ($log -notlike 'OK*') { throw "내보내기 실패: $log" }
$items = [regex]::Matches($dom, '<textarea id="dae:([^"]+)">([^<]*)</textarea>')
if ($items.Count -eq 0) { throw '내보낸 모델이 없음' }

New-Item -ItemType Directory -Force $OutDir | Out-Null
foreach ($it in $items) {
  $file = Join-Path $OutDir ($it.Groups[1].Value + '.dae')
  [IO.File]::WriteAllBytes($file, [Convert]::FromBase64String($it.Groups[2].Value))
  $kb = [math]::Round((Get-Item $file).Length / 1KB)
  Write-Output "$file  ($kb KB)"
}
Write-Output $log
