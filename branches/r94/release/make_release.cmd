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
xcopy /y /d any\*.dll __merge__\

pushd __merge__
set LIBZ_APP=..\..\external\libz.exe
del /q LZ4.libz 2> nul
%LIBZ_APP% merge-bootstrap --main LZ4.dll --move
%LIBZ_APP% add --libz LZ4.libz --codec deflate *.dll --exclude LZ4.dll --move --overwrite
%LIBZ_APP% list --libz LZ4.libz
%LIBZ_APP% inject --libz LZ4.libz --main LZ4.dll --move
echo F | xcopy /y /d LZ4.dll ..\libz\LZ4.dll
popd
rmdir /q /s __merge__ 2> nul

goto :end

:end
