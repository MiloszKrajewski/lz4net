@echo off
setlocal
call %~dp0\paket.cmd restore
%~dp0\packages\FAKE\tools\FAKE.exe build.fsx %*
endlocal