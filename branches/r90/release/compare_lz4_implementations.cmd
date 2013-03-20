@echo off

setlocal ENABLEDELAYEDEXPANSION
rem set FILES=D:\Archive\Corpus
set FILES=T:\Temp\Corpus
set PROJECT=LZ4.Tests.Continuous

:cycle

call :run MixedMode64
call :run MixedMode32
call :run CppCLI64
call :run CppCLI32
call :run Unsafe64
call :run LZ4Sharp64
call :run Unsafe32
call :run LZ4Sharp32
call :run Safe64
call :run Safe32

goto :cycle

:run
call :echo %1 @ 64-bit
..\source\%PROJECT%\bin\x64\Release\%PROJECT%.exe compare_lz4_implementations.xml %FILES% 1024 %1
call :echo %1 @ 32-bit
..\source\%PROJECT%\bin\x86\Release\%PROJECT%.exe compare_lz4_implementations.xml %FILES% 1024 %1
exit /b

:echo
echo.------------------------------------------------------------------------------
echo.%*
echo.------------------------------------------------------------------------------
exit /b
