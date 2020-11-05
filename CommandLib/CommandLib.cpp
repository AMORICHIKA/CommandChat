#include "pch.h"
#include "ChatData.h"
#include "CmdProcess.h"

LPWSTR	GetCurrentWorkingDirectory(HANDLE hProcess)
{
	struct	UPP
	{
		long	MaximumLength;
		long	Length;
		long	Flags;
		long	DebugFlags;
		HANDLE	ConsoleHandle;
		long	ConsoleFlags;
		HANDLE	StdInputHandle;
		HANDLE	StdOuputHandle;
		HANDLE	StdErrorHandle;
		UNICODE_STRING	CurrentDirectoryPath;
		HANDLE	CurrentDirectoryHandle;
		UNICODE_STRING	ImagePathName;
		UNICODE_STRING	CommandLine;
	};
	LPWSTR	lpszReturn = 0;
	HMODULE	hModule = GetModuleHandleW(L"ntdll");
	if(hModule)
	{
		typedef	NTSTATUS(__stdcall* fnNtQueryInformationProcess)(HANDLE, PROCESSINFOCLASS, PVOID, ULONG, PULONG);
		fnNtQueryInformationProcess	NtQueryInformationProcess = fnNtQueryInformationProcess(GetProcAddress(hModule, "NtQueryInformationProcess"));
		if(NtQueryInformationProcess)
		{
			PROCESS_BASIC_INFORMATION	pbi = { 0 };
			ULONG	len = 0;
			if(0 == NtQueryInformationProcess(hProcess, ProcessBasicInformation, &pbi, sizeof(pbi), &len) && 0 < len)
			{
				SIZE_T	nRead = 0;
				PEB		peb = { 0 };
				UPP		upp = { 0 };
				if(ReadProcessMemory(hProcess, pbi.PebBaseAddress,    &peb, sizeof(peb), &nRead) && 0 < nRead
				&& ReadProcessMemory(hProcess, peb.ProcessParameters, &upp, sizeof(upp), &nRead) && 0 < nRead)
				{
					PVOID	buffer = upp.CurrentDirectoryPath.Buffer;
					USHORT	length = upp.CurrentDirectoryPath.Length;
					lpszReturn = (LPWSTR)GlobalAlloc(0, (length / 2 + 1) * sizeof(WCHAR));
					if(!ReadProcessMemory(hProcess, buffer, lpszReturn, length, &nRead) || 0 == nRead)
					{
						GlobalFree(lpszReturn);
						lpszReturn = 0;
					}
					lpszReturn[length / 2] = 0;
				}
			}
		}
	}
	return	lpszReturn;
}

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
		// 改行だけの場合はカレントのフォルダを表示
		if(L"\n\r" == out)
		{
			TCHAR	sz[_MAX_PATH];
			GetCurrentDirectory(_MAX_PATH, sz);
			out = sz;
			out+= L"\r\n";
		}
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
