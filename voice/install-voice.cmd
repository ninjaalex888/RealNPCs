@echo off
setlocal
rem RealNPCs voices: sets up the voice server in this folder (2026-11-03). Everything goes under this folder and nothing
rem else on the PC changes: a Python of its own (tts-venv), PyTorch (for NVIDIA cards, or for the processor alone), Chatterbox,
rem its voice model (tools\hf), and the microphone's transcriber, faster-whisper, with its small model (tools\stt; 2026-11-04).
rem About 8 GB to download, once. Run it again after a failure: it carries on where it stopped. To remove it all, delete the folder.
rem
rem Which device (2026-11-09, players on AMD cards: "add an option to run local voice setup on AMD cards as well"): the voice
rem model needs NVIDIA's CUDA to run on a graphics card, so on an AMD or Intel card it runs on the processor instead, in an
rem experimental CPU mode: about three times slower than real time on a fast processor (a line of a second and a half took
rem about four seconds on a Core Ultra 9), so short lines get their voice and long ones fall back to the game's own recording.
rem As it comes: NVIDIA when nvidia-smi is found, the processor otherwise. "install-voice.cmd cpu" or "install-voice.cmd cuda"
rem chooses. The choice is kept in device.txt, which start-voice-server.cmd reads.
set VOICE_HOME=%~dp0
set VOICE_HOME=%VOICE_HOME:~0,-1%
set VENV=%VOICE_HOME%\tts-venv
set UV=%VOICE_HOME%\tools\uv\uv.exe
set UV_CACHE_DIR=%VOICE_HOME%\tools\cache
set UV_PYTHON_INSTALL_DIR=%VOICE_HOME%\tools\python
set HF_HOME=%VOICE_HOME%\tools\hf
set HF_HUB_DISABLE_SYMLINKS_WARNING=1
title RealNPCs voice setup
set DEVICE=%~1
if /i "%DEVICE%"=="cpu" goto chosen
if /i "%DEVICE%"=="cuda" goto chosen
set DEVICE=cuda
where nvidia-smi >nul 2>&1 || set DEVICE=cpu
:chosen
set WAS=
if exist "%VOICE_HOME%\device.txt" set /p WAS=<"%VOICE_HOME%\device.txt"
echo RealNPCs voices: setting up the voice server in
echo   %VOICE_HOME%
if /i "%DEVICE%"=="cuda" (
  echo About 8 GB to download, once. On an NVIDIA card, with about 4 GB of its memory free beside the game.
  set TORCH_INDEX=https://download.pytorch.org/whl/cu128
) else (
  echo About 5 GB to download, once. EXPERIMENTAL: no NVIDIA card found ^(or "cpu" was asked for^), so the voices will run
  echo on the processor. Slower: short lines get their voice, long ones fall back to the game's own recordings, and while a
  echo line is being made the game can lose a few frames. For an NVIDIA card, run "install-voice.cmd cuda".
  set TORCH_INDEX=https://download.pytorch.org/whl/cpu
)
echo.
set REINSTALL=
if defined WAS if /i not "%WAS%"=="%DEVICE%" set REINSTALL=--reinstall-package torch --reinstall-package torchaudio
if exist "%UV%" goto venv
echo [1/4] Fetching uv, which installs Python...
mkdir "%VOICE_HOME%\tools\uv" 2>nul
"%SystemRoot%\System32\curl.exe" -L -o "%VOICE_HOME%\tools\uv.zip" https://github.com/astral-sh/uv/releases/latest/download/uv-x86_64-pc-windows-msvc.zip || goto fail
"%SystemRoot%\System32\tar.exe" -xf "%VOICE_HOME%\tools\uv.zip" -C "%VOICE_HOME%\tools\uv" || goto fail
del "%VOICE_HOME%\tools\uv.zip"
:venv
echo [2/4] Python 3.12, in a folder of its own...
if not exist "%VENV%\Scripts\python.exe" "%UV%" venv --managed-python --python 3.12 "%VENV%" || goto fail
echo [3/4] PyTorch for %DEVICE% (about 3 GB for NVIDIA, under 1 GB for the processor), Chatterbox, the microphone's transcriber and the rest...
"%UV%" pip install --python "%VENV%\Scripts\python.exe" --index-url %TORCH_INDEX% %REINSTALL% torch==2.11.0 torchaudio==2.11.0 || goto fail
"%UV%" pip install --python "%VENV%\Scripts\python.exe" --no-deps chatterbox-tts==0.1.7 || goto fail
"%UV%" pip install --python "%VENV%\Scripts\python.exe" numpy==1.26.4 librosa==0.11.0 s3tokenizer==0.3.0 transformers==5.2.0 diffusers==0.29.0 resemble-perth==1.0.1 conformer==0.3.2 safetensors==0.5.3 omegaconf==2.4.0 pyloudnorm==0.2.0 pykakasi==2.3.0 spacy_pkuseg==1.0.2 sounddevice==0.5.6 soundfile==0.14.0 scipy==1.17.1 PyAudioWPatch==0.2.12.9 faster-whisper==1.1.1 ctranslate2==4.8.5 onnxruntime==1.31.1 av==19.0.2 || goto fail
"%UV%" cache clean >nul 2>&1
> "%VOICE_HOME%\device.txt" echo %DEVICE%
echo [4/4] The voice model (about 3 GB, once), the transcriber's (about 150 MB), and a first line to prove it works...
"%VENV%\Scripts\python.exe" -I "%VOICE_HOME%\voice_server.py" --root "%VOICE_HOME%" --device %DEVICE% --check || goto fail
echo.
echo Done (%DEVICE%). Before the game, start start-voice-server.cmd and leave its window open; in RealNPCs.ini, under [Mind], set
echo VoicedThoughts = true and VoicedDialogue = true (or switch them on in the settings panel in game). To talk to
echo people in your own voice, TalkVoice = true as well: Talk's row "Say it out loud..." listens to your microphone.
if /i "%DEVICE%"=="cpu" echo On the processor, the line the check just made shows how long a line takes on this PC.
pause
exit /b 0
:fail
echo.
echo The step above failed. Run this again to carry on from it; if it fails the same way, paste this window's text in a report.
pause
exit /b 1
