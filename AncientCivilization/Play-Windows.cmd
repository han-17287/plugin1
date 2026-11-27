@echo off
setlocal EnableExtensions DisableDelayedExpansion
title PLUGIN - Build and play on Windows

for %%P in ("%~dp0.") do set "PROJECT=%%~fP"
if not exist "%PROJECT%\Assets\Scenes\Main.unity" goto not_extracted
if not exist "%PROJECT%\ProjectSettings\ProjectVersion.txt" goto not_extracted
set "EDITOR_VERSION="
for /f "usebackq tokens=1,2" %%V in ("%PROJECT%\ProjectSettings\ProjectVersion.txt") do if "%%V"=="m_EditorVersion:" set "EDITOR_VERSION=%%W"
if not defined EDITOR_VERSION goto not_extracted

set "UNITY=%PLUGIN_UNITY_EXE%"
if defined UNITY if exist "%UNITY%" goto editor_found
set "UNITY=%ProgramFiles%\Unity\Hub\Editor\%EDITOR_VERSION%\Editor\Unity.exe"
if exist "%UNITY%" goto editor_found
set "UNITY="
for /f "usebackq delims=" %%U in (`powershell.exe -NoProfile -Command "$p = Join-Path $env:APPDATA 'UnityHub\secondaryInstallPath.json'; if (Test-Path -LiteralPath $p) { $root = ConvertFrom-Json -InputObject ([IO.File]::ReadAllText($p)); if ($root -is [string]) { $exe = Join-Path $root ($env:EDITOR_VERSION + '\Editor\Unity.exe'); if (Test-Path -LiteralPath $exe -PathType Leaf) { $exe } } }"`) do set "UNITY=%%U"
if not defined UNITY (
    echo Unity %EDITOR_VERSION% was not found. Set PLUGIN_UNITY_EXE to that editor's Unity.exe if installed in a custom folder.
    goto failed
)

:editor_found
set "RUN_ID="
for /f "delims=" %%I in ('powershell.exe -NoProfile -Command "[Guid]::NewGuid().ToString()"') do set "RUN_ID=%%I"
if not defined RUN_ID goto failed
set "RUN_DIR=%PROJECT%\Builds\Windows\%RUN_ID%"
if exist "%RUN_DIR%" goto failed
mkdir "%RUN_DIR%" || goto failed
set "BUILD_EXE=%RUN_DIR%\Player\PLUGIN.exe"
set "BUILD_LOG=%RUN_DIR%\Unity-Windows.log"
set "BUILD_PROJECT=%PROJECT%"
set "SNAPSHOT_CREATED="

rem The Unity lock is also present while an editor is already running this project.
rem Copy only saved inputs in that case. The open editor and its unsaved scene stay open.
if not exist "%PROJECT%\Temp\UnityLockfile" goto build
if not defined TEMP goto build_failed
set "BUILD_PROJECT=%TEMP%\PLUGIN-Build-%RUN_ID%"
if exist "%BUILD_PROJECT%" goto build_failed
mkdir "%BUILD_PROJECT%" || goto failed
set "SNAPSHOT_CREATED=1"
echo The project is open. Building a separate copy of its saved files.
for %%D in (Assets Packages ProjectSettings) do (
    robocopy "%PROJECT%\%%D" "%BUILD_PROJECT%\%%D" /E /XJ /R:1 /W:1 /NFL /NDL /NJH /NJS /NP >nul
    if errorlevel 8 goto build_failed
)

:build
echo Building the full game for Windows with Unity %EDITOR_VERSION%.
echo Unity log: "%BUILD_LOG%"
start "" /wait "%UNITY%" -batchmode -quit -buildTarget Win64 -projectPath "%BUILD_PROJECT%" -executeMethod Plugin.AncientCivilization.EditorChecks.AncientWindowsPlayerBuild.BuildFromCommandLine -pluginWindowsOutput "%BUILD_EXE%" -logFile "%BUILD_LOG%"
if errorlevel 1 goto build_failed
if not exist "%BUILD_EXE%" goto build_failed
if not exist "%RUN_DIR%\Player\UnityPlayer.dll" goto build_failed
if not exist "%RUN_DIR%\Player\PLUGIN_Data\" goto build_failed

rem The snapshot directory was created by this invocation after checking it did not exist.
if defined SNAPSHOT_CREATED rmdir /s /q "%BUILD_PROJECT%"
echo Build complete. To play again, open "%BUILD_EXE%".
start "" /d "%RUN_DIR%\Player" "%BUILD_EXE%"
exit /b %ERRORLEVEL%

:build_failed
if defined SNAPSHOT_CREATED rmdir /s /q "%BUILD_PROJECT%"
echo The build did not finish. Log: "%BUILD_LOG%"
if exist "%BUILD_LOG%" start "" notepad.exe "%BUILD_LOG%"
goto failed
:not_extracted
echo Extract the entire Unity project ZIP before opening Play-Windows.cmd.
:failed
pause
exit /b 1
