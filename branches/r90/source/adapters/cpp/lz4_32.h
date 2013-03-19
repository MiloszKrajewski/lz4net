// This file imports lz4.h prefixing all functions with I32_

#pragma once

#define LZ4_ARCH64 0
#define LZ4_FUNC(name) I32_ ## name
//#ifdef __cplusplus_cli
	#define LZ4_MK_OPT
//#endif

#include "..\..\..\original\lz4.h"