@echo off
setlocal EnableDelayedExpansion

set DEFS=lz4_cs_adapter.h -undef -E -I. -CC
gcc %DEFS% > gen_unsafe32_lz4.c
gcc %DEFS% -DGEN_X64 > gen_unsafe64_lz4.c
gcc %DEFS% -DGEN_SAFE > gen_safe32_lz4.c
gcc %DEFS% -DGEN_SAFE -DGEN_X64 > gen_safe64_lz4.c

rx -p lz4_safe.xml gen_safe32_lz4.c
rx -p lz4_safe.xml gen_safe64_lz4.c
rx -p lz4hc_safe.xml gen_safe32_lz4hc.c
rx -p lz4hc_safe.xml gen_safe64_lz4hc.c


set DEFS=lz4hc_cs_adapter.h -undef -E -I. -CC
gcc %DEFS% > gen_unsafe32_lz4hc.c
gcc %DEFS% -DGEN_X64 > gen_unsafe64_lz4hc.c
gcc %DEFS% -DGEN_SAFE > gen_safe32_lz4hc.c
gcc %DEFS% -DGEN_SAFE -DGEN_X64 > gen_safe64_lz4hc.c

rx -p lz4_unsafe.xml gen_unsafe32_lz4.c
rx -p lz4_unsafe.xml gen_unsafe64_lz4.c
rx -p lz4hc_unsafe.xml gen_unsafe32_lz4hc.c
rx -p lz4hc_unsafe.xml gen_unsafe64_lz4hc.c

endlocal