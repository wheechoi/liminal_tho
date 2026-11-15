# NPC 작업실(SP_NPC_workshop.html)의 NPC 블록(// <NPC-BLOCK> ~ // </NPC-BLOCK>)을
# 목업(SP_1-1_mockup.html)의 사본 자리(// <NPC-BLOCK-COPY> ~ // </NPC-BLOCK-COPY>)에 덮어쓴다.
# 작업실에서 모델을 고친 뒤 실행:  powershell -ExecutionPolicy Bypass -File Tools\NpcExport\sync_npc_block.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$utf8 = New-Object System.Text.UTF8Encoding($false)
$workshop = Join-Path $root 'Docs\SurplusPerson\SP_NPC_workshop.html'
$mockup   = Join-Path $root 'Docs\SurplusPerson\SP_1-1_mockup.html'
$w = [IO.File]::ReadAllText($workshop, $utf8)
$m = [regex]::Match($w, '(?s)// <NPC-BLOCK>(.*?)// </NPC-BLOCK>')
if (-not $m.Success) { throw "NPC 블록 표시(// <NPC-BLOCK>)를 찾지 못함: $workshop" }
$block = $m.Groups[1].Value
$h = [IO.File]::ReadAllText($mockup, $utf8)
$re = [regex]'(?s)(// <NPC-BLOCK-COPY>).*?(// </NPC-BLOCK-COPY>)'
if (-not $re.IsMatch($h)) { throw "목업에서 사본 자리(// <NPC-BLOCK-COPY>)를 찾지 못함: $mockup" }
$new = $re.Replace($h, { param($x) $x.Groups[1].Value + $block + $x.Groups[2].Value }, 1)
if ($new -eq $h) { Write-Host '이미 같음 — 바꿀 것 없음'; exit 0 }
[IO.File]::WriteAllText($mockup, $new, $utf8)
Write-Host "목업의 NPC 블록 사본을 갱신했음: $mockup"
