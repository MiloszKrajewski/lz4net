@echo off
pushd %~dp0
..\source\.nuget\NuGet.exe restore ..\source\LZ4.sln
call ..\source\packages\psake.4.3.2\tools\psake.cmd .\default.ps1 %*
popd