@echo off
rem RealNPCs' local voice server: thoughts and lines said in each person's own voice, cloned on this PC with Chatterbox
rem from your own export of the game's voice banks (refs.json + banks\ in VOICE_ROOT). Leave this window open while you play.
set VOICE_ROOT=C:\mods\voice-test
set VENV=%VOICE_ROOT%\tts-venv
"%VENV%\Scripts\python.exe" -I "%~dp0voice_server.py" --root "%VOICE_ROOT%" %*
pause
