// This file imports lz4.h prefixing all functions with I64_

#pragma once

#define LZ4_ARCH64 1
#define LZ4_FUNC(name) I64_ ## name
//#ifdef __cplusplus_cli
	#define LZ4_MK_OPT
//#endif

#include "..\..\..\original\lz4.h"