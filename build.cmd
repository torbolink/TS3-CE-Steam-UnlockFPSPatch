@echo off
setlocal
set "compiler=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%compiler%" (
  echo .NET Framework C# compiler not found.
  exit /b 1
)
if not exist "%~dp0bin" mkdir "%~dp0bin"
"%compiler%" /nologo /target:winexe /platform:x64 /optimize+ /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /win32manifest:"%~dp0src\app.manifest" /out:"%~dp0bin\TS3-FPS-Patcher.exe" "%~dp0src\Program.cs"
if errorlevel 1 exit /b 1
echo Built bin\TS3-FPS-Patcher.exe
