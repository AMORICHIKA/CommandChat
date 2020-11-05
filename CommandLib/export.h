#pragma once

#if defined( COMMANDLIB_EXPORTS )
#define _LIB_EXT_CLASS	__declspec( dllexport )
#define _LIB_EXT_FUNC	__declspec( dllexport )
#else
#define _LIB_EXT_CLASS	__declspec( dllimport )
#define _LIB_EXT_FUNC	__declspec( dllimport )
#endif

// åˆäJä÷êî
extern "C"
{
_LIB_EXT_FUNC	BOOL	WINAPI	CmdCreate(HWND hWnd);

_LIB_EXT_FUNC	LPWSTR	WINAPI	CmdPop();

_LIB_EXT_FUNC	BOOL	WINAPI	CmdRun(LPCTSTR cmd);

_LIB_EXT_FUNC	void	WINAPI	CmdExit();
}
