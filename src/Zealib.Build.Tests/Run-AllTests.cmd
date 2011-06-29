@echo off
CALL :CHECK_ENV

SET MSBUILD_TOOL=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe
PUSHD "%~dp0"
  FOR /f "delims=;" %%i IN ('dir /B /AD-H') DO (
    CALL :RUN_TEST %%i
    ECHO.
  )
POPD
ECHO.
PAUSE
goto :eof


:RUN_TEST
PUSHD "%1"
  ECHO %1 | findstr ^^[0-9][0-9][0-9]_ > NUL
  IF NOT %ERRORLEVEL% == 0 goto :eof
  SET TEST_NAME=%1
  SET TEST_NAME=%TEST_NAME:~4%
  ECHO Running test "%TEST_NAME%"...
  
  %MSBUILD_TOOL% /nologo /v:minimal "%TEST_NAME%.proj"
  
  IF NOT %ERRORLEVEL% == 0 (
    ECHO Test "%TEST_NAME%" FAILED!
    EXIT /B 1
  )
  ECHO Test "%TEST_NAME%" PASS.
POPD
goto :eof

:CHECK_ENV
IF NOT EXIST "%WINDIR%\Microsoft.NET\Framework\v4.0.30319" (
  ECHO Microsoft .Net Framework v4.0 is required.
  ECHO == Get Microsoft.Net Framework : http://www.microsoft.com/net
  EXIT /B 2
)
goto :eof
