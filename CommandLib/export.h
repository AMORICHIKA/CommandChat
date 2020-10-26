#pragma once

#if defined( COMMANDLIB_EXPORTS )
#define _LIB_EXT_CLASS	__declspec( dllexport )
#define _LIB_EXT_FUNC	__declspec( dllexport )
#else
#define _LIB_EXT_CLASS	__declspec( dllimport )
#define _LIB_EXT_FUNC	__declspec( dllimport )
#endif

extern "C"
_LIB_EXT_FUNC	LPWSTR	RunCommand(LPCWSTR lpszCommand);
