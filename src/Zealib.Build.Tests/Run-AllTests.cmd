@echo off
CALL :CHECK_ENV

SET MSBUILD_TOOL=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe
PUSHD "%~dp0"
  FOR /f "delims=;" %%i IN ('dir /B /AD-H') DO (
    CALL :RUN_TEST %%i
  )
POPD
ECHO.
PAUSE
goto :eof


:RUN_TEST
PUSHD "%1"
  %MSBUILD_TOOL% /nologo /v:minimal
  ECHO Test "%1" PASS.
POPD
goto :eof

:CHECK_ENV
IF NOT EXIST "%WINDIR%\Microsoft.NET\Framework\v4.0.30319" (
  ECHO Microsoft .Net Framework v4.0 is required.
  ECHO == Get Microsoft.Net Framework : http://www.microsoft.com/net
  EXIT /B 2
)
goto :eof
