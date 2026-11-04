@echo off
setlocal
rem RealNPCs' voice server: start it before the game and leave this window open. It runs on the Python that
rem install-voice.cmd put beside it (tts-venv, the model in tools\hf), or, on the development PC, the one in
rem C:\mods\voice-test with its export. Extra arguments pass through (--idle-seconds 0 to keep the voices on the card, --full
rem for the voice model in full precision, --turbo, --min-free-gb 1.0, --prefer-captured ...).
set VOICE_HOME=%~dp0
set VOICE_HOME=%VOICE_HOME:~0,-1%
if exist "%VOICE_HOME%\tts-venv\Scripts\python.exe" (
  set VENV=%VOICE_HOME%\tts-venv
  set VOICE_ROOT=%VOICE_HOME%
  set HF_HOME=%VOICE_HOME%\tools\hf
set HF_HUB_DISABLE_SYMLINKS_WARNING=1
) else (
  set VENV=C:\mods\voice-test\tts-venv
  set VOICE_ROOT=C:\mods\voice-test
)
title RealNPCs voice server
"%VENV%\Scripts\python.exe" -I "%VOICE_HOME%\voice_server.py" --root "%VOICE_ROOT%" %*
pause
