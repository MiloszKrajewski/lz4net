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
xcopy /y /d ..\source\LZ4s\bin\Release\LZ4s.dll any\
xcopy /y /d ..\source\LZ4\bin\Release\LZ4.dll any\
echo F | xcopy /y /d ..\source\bin\Win32\Release\LZ4mm.dll any\LZ4mm.x86.dll
echo F | xcopy /y /d ..\source\bin\x64\Release\LZ4mm.dll any\LZ4mm.x64.dll
echo F | xcopy /y /d ..\source\bin\Win32\Release\LZ4cc.dll any\LZ4cc.x86.dll
echo F | xcopy /y /d ..\source\bin\x64\Release\LZ4cc.dll any\LZ4cc.x64.dll
rmdir /q /s __merge__ 2> nul
xcopy /y /d ..\source\LZ4\bin\Release\*.dll __merge__\

set LIBZ=..\external\libz.exe
del /q LZ4.libz 2> nul
%LIBZ% merge-bootstrap --exe __merge__\LZ4.dll --move
%LIBZ% add --libz __merge__\lZ4.libz --codec deflate any\LZ4s.dll any\LZ4n.dll x86\*.dll x64\*.dll
%LIBZ% inject --libz __merge__\lZ4.libz --exe __merge__\LZ4.dll --move
echo F | xcopy /y /d __merge__\LZ4.dll libz\LZ4.dll
rmdir /q /s __merge__ 2> nul

goto :end

:end
