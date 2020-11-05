#include "pch.h"
#include "ChatData.h"
#include "CmdProcess.h"

extern "C"
_LIB_EXT_FUNC	BOOL	WINAPI	CmdCreate(HWND hWnd)
{
	return	CmdProcess::Get()->Create(hWnd);
}

extern "C"
_LIB_EXT_FUNC	LPWSTR	WINAPI	CmdPop()
{
	std::wstring	out;
	if(ChatData::Get()->PopFrontOutput(&out))
	{
		size_t	size = out.size()+1;
		LPWSTR	ret = (LPWSTR)::CoTaskMemAlloc(size*sizeof(WCHAR));
		wcscpy_s(ret, size, out.c_str());
		return	ret;
	}
	return	NULL;
}

extern "C"
_LIB_EXT_FUNC	BOOL	WINAPI	CmdRun(LPCTSTR cmd)
{
	return	CmdProcess::Get()->RunCommand(cmd);
}

extern "C"
_LIB_EXT_FUNC	void	WINAPI	CmdExit()
{
	CmdProcess::Get()->Exit();
}
