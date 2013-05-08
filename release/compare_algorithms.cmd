@echo off

setlocal ENABLEDELAYEDEXPANSION
set FILES=T:\Temp\Corpus
set PROJECT=LZ4.Tests.Continuous

:cycle

rem ---- native ----
call :run LZ4.native
call :run LZO1X.native
call :run LZO1X11.native
call :run LZO1X12.native
call :run LZO1X15.native
call :run LZO1X999.native
call :run QuickLZ.native
call :run Snappy.native

rem ---- unsafe ----
call :run LZ4Sharp.unsafe
call :run LZ4.unsafe

rem ---- safe ----
call :run LZ4.safe
call :run LZF.safe
call :run QuickLZ1.safe
call :run QuickLZ3.safe
call :run Deflate.safe

goto :cycle

:run
call :echo %1 @ 64-bit
..\source\%PROJECT%\bin\x64\Release\%PROJECT%.exe compare_algorithms.xml %FILES% 1024 %1
call :echo %1 @ 32-bit
..\source\%PROJECT%\bin\x86\Release\%PROJECT%.exe compare_algorithms.xml %FILES% 1024 %1
exit /b

:echo
echo.------------------------------------------------------------------------------
echo.%*
echo.------------------------------------------------------------------------------
exit /b
