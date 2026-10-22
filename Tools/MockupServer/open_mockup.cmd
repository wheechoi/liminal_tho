@echo off
rem 목업을 크롬 새 창으로 연다. 로컬 서버(serve.ps1)가 꺼져 있으면 먼저 켠다 (최소화 창으로).
rem 더블클릭하거나, 바탕화면에 바로가기를 만들어 두고 쓰면 됨. (프로젝트 경로에 공백이 없다는 전제 — CLAUDE.md 규칙)
powershell -NoProfile -Command "if(-not (Get-NetTCPConnection -LocalPort 8765 -State Listen -ErrorAction SilentlyContinue)){Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-WindowStyle','Minimized','-File','%~dp0serve.ps1'; Start-Sleep -Seconds 1}"
start "" chrome --new-window "http://localhost:8765/SurplusPerson/SP_1-1_mockup.html"
