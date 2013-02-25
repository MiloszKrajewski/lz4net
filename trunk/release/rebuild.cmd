@echo off
setlocal

set SOLUTION=%1
set PLATFORM=%2

if "%SOLUTION%"=="" goto :help
if "%PLATFORM%"=="" set PLATFORM=AnyCPU
if "%LOG%"=="" set LOG=%TEMP%\build.log

rem ----------------------------------------------------------------------------
rem Find Visual Studio
rem ----------------------------------------------------------------------------
set VS=UNKNOWN
if not "%VS80COMNTOOLS%"=="" set VS=%VS80COMNTOOLS%
if not "%VS90COMNTOOLS%"=="" set VS=%VS90COMNTOOLS%
if not "%VS100COMNTOOLS%"=="" set VS=%VS100COMNTOOLS%
call "%VS%\vsvars32.bat"

echo.
echo.
echo.-----------------------------------------------------------------------------
echo.
echo. Building %1
echo.
echo.-----------------------------------------------------------------------------
echo.
echo.

msbuild "%SOLUTION%" /t:Rebuild /p:Configuration=Release;Platform=%PLATFORM%
if %ERRORLEVEL% neq 0 echo %1 >> %LOG%

echo.-----------------------------------------------------------------------------
echo. Finished %1
echo.-----------------------------------------------------------------------------

goto :end

:help
echo.-----------------------------------------------------------------------------
echo.
echo. Syntax: rebuild.cmd <solution> [<platform>]
echo.
echo.-----------------------------------------------------------------------------
goto :end

:end
