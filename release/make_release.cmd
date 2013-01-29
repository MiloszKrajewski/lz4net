@echo off
setlocal

set SRC=..\source

rem ----------------------------------------------------------------------------
rem Run compilation
rem ----------------------------------------------------------------------------
call rebuild.cmd %SRC%\LZ4.sln x86
call rebuild.cmd %SRC%\LZ4.sln x64

rem ----------------------------------------------------------------------------
rem Copy files to target folders
rem ----------------------------------------------------------------------------
xcopy /y /d ..\source\bin\Win32\Release\*.dll x86\
xcopy /y /d ..\source\bin\x64\Release\*.dll x64\
xcopy /y /d ..\source\LZ4n\bin\Release\LZ4n.dll any\
xcopy /y /d ..\source\LZ4\bin\Release\LZ4.dll any\
echo F | xcopy /y /d ..\source\bin\Win32\Release\LZ4mm.dll any\LZ4mm.x86.dll
echo F | xcopy /y /d ..\source\bin\x64\Release\LZ4mm.dll any\LZ4mm.x64.dll
echo F | xcopy /y /d ..\source\bin\Win32\Release\LZ4cc.dll any\LZ4cc.x86.dll
echo F | xcopy /y /d ..\source\bin\x64\Release\LZ4cc.dll any\LZ4cc.x64.dll
goto :end

:end
