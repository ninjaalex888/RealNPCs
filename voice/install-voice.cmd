@echo off
setlocal
rem RealNPCs voices: sets up the voice server in this folder (2026-11-03). Everything goes under this folder and nothing
rem else on the PC changes: a Python of its own (tts-venv), PyTorch for NVIDIA cards, Chatterbox, and its voice model
rem (tools\hf). About 8 GB to download, once. Run it again after a failure: it carries on where it stopped. To remove it
rem all, delete the folder.
set VOICE_HOME=%~dp0
set VOICE_HOME=%VOICE_HOME:~0,-1%
set VENV=%VOICE_HOME%\tts-venv
set UV=%VOICE_HOME%\tools\uv\uv.exe
set UV_CACHE_DIR=%VOICE_HOME%\tools\cache
set UV_PYTHON_INSTALL_DIR=%VOICE_HOME%\tools\python
set HF_HOME=%VOICE_HOME%\tools\hf
set HF_HUB_DISABLE_SYMLINKS_WARNING=1
title RealNPCs voice setup
echo RealNPCs voices: setting up the voice server in
echo   %VOICE_HOME%
echo About 8 GB to download, once. Needs an NVIDIA card with about 4 GB of memory free beside the game.
echo.
where nvidia-smi >nul 2>&1 || echo NOTE: no NVIDIA driver found (nvidia-smi). Without an NVIDIA card the voices can't run.
if exist "%UV%" goto venv
echo [1/4] Fetching uv, which installs Python...
mkdir "%VOICE_HOME%\tools\uv" 2>nul
"%SystemRoot%\System32\curl.exe" -L -o "%VOICE_HOME%\tools\uv.zip" https://github.com/astral-sh/uv/releases/latest/download/uv-x86_64-pc-windows-msvc.zip || goto fail
"%SystemRoot%\System32\tar.exe" -xf "%VOICE_HOME%\tools\uv.zip" -C "%VOICE_HOME%\tools\uv" || goto fail
del "%VOICE_HOME%\tools\uv.zip"
:venv
echo [2/4] Python 3.12, in a folder of its own...
if not exist "%VENV%\Scripts\python.exe" "%UV%" venv --managed-python --python 3.12 "%VENV%" || goto fail
echo [3/4] PyTorch for NVIDIA cards (about 3 GB), Chatterbox and the rest...
"%UV%" pip install --python "%VENV%\Scripts\python.exe" --index-url https://download.pytorch.org/whl/cu128 torch==2.11.0 torchaudio==2.11.0 || goto fail
"%UV%" pip install --python "%VENV%\Scripts\python.exe" --no-deps chatterbox-tts==0.1.7 || goto fail
"%UV%" pip install --python "%VENV%\Scripts\python.exe" numpy==1.26.4 librosa==0.11.0 s3tokenizer==0.3.0 transformers==5.2.0 diffusers==0.29.0 resemble-perth==1.0.1 conformer==0.3.2 safetensors==0.5.3 omegaconf==2.4.0 pyloudnorm==0.2.0 pykakasi==2.3.0 spacy_pkuseg==1.0.2 sounddevice==0.5.6 soundfile==0.14.0 scipy==1.17.1 PyAudioWPatch==0.2.12.9 || goto fail
"%UV%" cache clean >nul 2>&1
echo [4/4] The voice model (about 3 GB, once), and a first line to prove the card...
"%VENV%\Scripts\python.exe" -I "%VOICE_HOME%\voice_server.py" --root "%VOICE_HOME%" --check || goto fail
echo.
echo Done. Before the game, start start-voice-server.cmd and leave its window open; in RealNPCs.ini, under [Mind], set
echo VoicedThoughts = true and VoicedDialogue = true (or switch them on in the settings panel in game).
pause
exit /b 0
:fail
echo.
echo The step above failed. Run this again to carry on from it; if it fails the same way, paste this window's text in a report.
pause
exit /b 1
