@echo off
setlocal EnableDelayedExpansion
set DEFS=lz4_cs_adapter.h -undef
gcc %DEFS% -E -I. > gen_unsafe32_lz4.c
gcc %DEFS% -DGEN_X64 -E -I. > gen_unsafe64_lz4.c
gcc %DEFS% -DGEN_SAFE -E -I. > gen_safe32_lz4.c
gcc %DEFS% -DGEN_SAFE -DGEN_X64 -E -I. > gen_safe64_lz4.c
endlocal