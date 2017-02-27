@echo off
setlocal
call %~dp0\paket.cmd restore
if not "%VS90COMNTOOLS%"=="" set VSTOOLKIT=%VS90COMNTOOLS%
if not "%VS100COMNTOOLS%"=="" set VSTOOLKIT=%VS100COMNTOOLS%
if not "%VS110COMNTOOLS%"=="" set VSTOOLKIT=%VS110COMNTOOLS%
if not "%VS120COMNTOOLS%"=="" set VSTOOLKIT=%VS120COMNTOOLS%
if not "%VS140COMNTOOLS%"=="" set VSTOOLKIT=%VS140COMNTOOLS%
call "%VSTOOLKIT%"\vsvars32.bat
set
%~dp0\packages\FAKE\tools\FAKE.exe build.fsx %*
endlocal