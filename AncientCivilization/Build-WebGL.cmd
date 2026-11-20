@echo off
setlocal EnableExtensions DisableDelayedExpansion
title PLUGIN - Unity WebGL Build

for %%P in ("%~dp0.") do set "PROJECT=%%~fP"
if not exist "%PROJECT%\Assets\Scenes\Main.unity" (
    echo Extract the whole project ZIP before opening Build-WebGL.cmd.
    goto failed
)

set "EDITOR_VERSION="
for /f "tokens=2" %%V in ('findstr /b /c:"m_EditorVersion:" "%PROJECT%\ProjectSettings\ProjectVersion.txt"') do set "EDITOR_VERSION=%%V"
if not defined EDITOR_VERSION (
    echo Could not read the Unity version for this project.
    goto failed
)

set "UNITY=%PLUGIN_UNITY_EXE%"
if not defined UNITY set "UNITY=%ProgramFiles%\Unity\Hub\Editor\%EDITOR_VERSION%\Editor\Unity.exe"
if exist "%UNITY%" goto editor_found
set "UNITY="
echo Choose Unity.exe for Unity %EDITOR_VERSION%.
for /f "usebackq delims=" %%U in (`powershell.exe -NoProfile -Sta -Command "Add-Type -AssemblyName System.Windows.Forms; $dialog = New-Object Windows.Forms.OpenFileDialog; $dialog.Filter = 'Unity Editor (Unity.exe)|Unity.exe'; $dialog.Title = 'Choose Unity %EDITOR_VERSION%'; if ($dialog.ShowDialog() -eq 'OK') { $dialog.FileName }"`) do set "UNITY=%%U"
if not defined UNITY goto failed
if not exist "%UNITY%" goto failed

:editor_found
for %%U in ("%UNITY%") do set "UNITY_FOLDER=%%~dpU"
if not exist "%UNITY_FOLDER%Data\PlaybackEngines\WebGLSupport\" (
    echo Web Build Support is missing for the selected Unity Editor.
    echo In Unity Hub, open Installs, select %EDITOR_VERSION%, and choose Add modules.
    goto failed
)

set "BUILD_TIME="
for /f %%T in ('powershell.exe -NoProfile -Command "Get-Date -Format yyyyMMdd-HHmmss-fff"') do set "BUILD_TIME=%%T"
if not defined BUILD_TIME goto failed
set "RUN_FOLDER=%PROJECT%\Builds\WebGL\%BUILD_TIME%"
set "BUILD_FOLDER=%RUN_FOLDER%\Player"
set "BUILD_LOG=%RUN_FOLDER%\Unity-WebGL.log"
set "BUILD_ZIP=%RUN_FOLDER%\PLUGIN-WebGL-upload.zip"
mkdir "%RUN_FOLDER%" || goto failed

echo Building the Unity WebGL player. This window will stay open until the build finishes.
echo Close any Unity Editor window that has this same project open.
start "" /wait "%UNITY%" -batchmode -quit -buildTarget WebGL -projectPath "%PROJECT%" -executeMethod Plugin.AncientCivilization.EditorChecks.AncientWebPlayerBuild.BuildFromCommandLine -pluginWebOutput "%BUILD_FOLDER%" -logFile "%BUILD_LOG%"
if errorlevel 1 goto build_failed
if not exist "%BUILD_FOLDER%\index.html" goto build_failed
if not exist "%BUILD_FOLDER%\Build\*.loader.js" goto build_failed
if not exist "%BUILD_FOLDER%\Build\*.wasm*" goto build_failed
if not exist "%BUILD_FOLDER%\Build\*.data*" goto build_failed

echo Creating PLUGIN-WebGL-upload.zip...
powershell.exe -NoProfile -Command "$ErrorActionPreference = 'Stop'; Add-Type -AssemblyName System.IO.Compression.FileSystem; [IO.Compression.ZipFile]::CreateFromDirectory($env:BUILD_FOLDER, $env:BUILD_ZIP, [IO.Compression.CompressionLevel]::Optimal, $false)"
if errorlevel 1 goto build_failed
if not exist "%BUILD_ZIP%" goto build_failed

echo Build complete. Upload the highlighted ZIP in this chat.
explorer.exe /select,"%BUILD_ZIP%"
pause
exit /b 0

:build_failed
echo The WebGL build or ZIP could not be completed.
if exist "%BUILD_LOG%" (
    echo Upload Unity-WebGL.log so the build error can be checked.
    explorer.exe /select,"%BUILD_LOG%"
)

:failed
pause
exit /b 1
