@echo off
CALL :CHECK_ENV

for /f %%i in ('git rev-list HEAD --count') do (
 set COMMIT_COUNT=%%i
)
for /f %%i in ('git rev-list HEAD --max-count=1') do (
 set COMMIT_HASH=%%i
)

SET GET_GIT_TAG=git for-each-ref refs/tags --sort=authordate --format=""%%(refname),%%(objectname)""
for /f "tokens=1,2 delims=," %%a in ('"%GET_GIT_TAG%"') do (
  IF "%COMMIT_HASH%" == "%%b" (
   SET TAG_NAME=%%a
   SET TAG_HASH=%%b
  )
)
IF NOT "%TAG_NAME%" == "" (
  SET RELEASE_NAME=%TAG_NAME:~10%
) ELSE (
  SET RELEASE_NAME="DEV"
)

SET MSBUILD_TOOL=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe
"%MSBUILD_TOOL%" /nologo /p:VersionRevision=%COMMIT_COUNT% /p:VersionIdentity=%COMMIT_HASH% Build.build %* 
goto :eof


:CHECK_ENV
git --help 1>NUL 2>NUL
IF %ERRORLEVEL% == 9009 (
  ECHO Git command-line program git.exe is required.
  ECHO == Get git : http://code.google.com/p/msysgit
  EXIT /B 2
)
IF NOT EXIST "%WINDIR%\Microsoft.NET\Framework\v4.0.30319" (
  ECHO Microsoft .Net Framework v4.0 is required.
  ECHO == Get Microsoft.Net Framework : http://www.microsoft.com/net
  EXIT /B 2
)
goto :eof
