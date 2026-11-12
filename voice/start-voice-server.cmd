@echo off
setlocal
rem RealNPCs' voice server: start it before the game and leave this window open. It runs on the Python that
rem install-voice.cmd put beside it (tts-venv, the model in tools\hf), or, if there's none, the one in the folder
rem REALNPCS_VOICE_ROOT names (a setup kept elsewhere, with its own export). The voices stay on a graphics card of 12 GB or more and leave a smaller one after a
rem quiet minute. Extra arguments pass through (--idle-seconds 0 to keep the voices on any card, 60 to give it back, --full
rem for the voice model in full precision, --turbo, --min-free-gb 1.0, --prefer-captured ...).
rem The device install-voice.cmd chose (device.txt: cuda, or cpu, the experimental mode for AMD and other cards; 2026-11-09)
rem is passed first, so a --device given here still wins.
set VOICE_HOME=%~dp0
set VOICE_HOME=%VOICE_HOME:~0,-1%
if exist "%VOICE_HOME%\tts-venv\Scripts\python.exe" (
  set VENV=%VOICE_HOME%\tts-venv
  set VOICE_ROOT=%VOICE_HOME%
  set HF_HOME=%VOICE_HOME%\tools\hf
set HF_HUB_DISABLE_SYMLINKS_WARNING=1
) else if defined REALNPCS_VOICE_ROOT (
  set VENV=%REALNPCS_VOICE_ROOT%\tts-venv
  set VOICE_ROOT=%REALNPCS_VOICE_ROOT%
) else (
  echo The voice server isn't set up in this folder yet: run install-voice.cmd here once ^(about 8 GB, once^), then this.
  pause
  exit /b 1
)
set DEVICE=cuda
if exist "%VOICE_HOME%\device.txt" set /p DEVICE=<"%VOICE_HOME%\device.txt"
for /f "tokens=* delims= " %%d in ("%DEVICE%") do set DEVICE=%%d
title RealNPCs voice server (%DEVICE%)
if /i "%DEVICE%"=="cpu" echo EXPERIMENTAL: the voices run on the processor ^(no NVIDIA card^). Short lines get their voice; long ones fall back to the game's.
"%VENV%\Scripts\python.exe" -I "%VOICE_HOME%\voice_server.py" --root "%VOICE_ROOT%" --device %DEVICE% %*
pause
