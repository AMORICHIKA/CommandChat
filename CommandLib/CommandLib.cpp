#include "pch.h"

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
			if(NtQueryInformationProcess(hProcess, ProcessBasicInformation, &pbi, sizeof(pbi), &len) == 0 && len > 0)
			{
				SIZE_T	nRead = 0;
				PEB		peb = { 0 };
				UPP		upp = { 0 };
				if(ReadProcessMemory(hProcess, pbi.PebBaseAddress, &peb, sizeof(peb), &nRead) && nRead > 0
				&& ReadProcessMemory(hProcess, peb.ProcessParameters, &upp, sizeof(upp), &nRead) && nRead > 0)
				{
					PVOID	buffer = upp.CurrentDirectoryPath.Buffer;
					USHORT	length = upp.CurrentDirectoryPath.Length;
					lpszReturn = (LPWSTR)GlobalAlloc(0, (length / 2 + 1) * sizeof(WCHAR));
					if(!ReadProcessMemory(hProcess, buffer, lpszReturn, length, &nRead) || nRead == 0)
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
_LIB_EXT_FUNC	LPWSTR	RunCommand(LPCWSTR lpszCommand)
{
	if(!lpszCommand)
		return	0;

	LPWSTR	lpszReturn = 0;
	HANDLE	readPipe;
	HANDLE	writePipe;
	SECURITY_ATTRIBUTES	sa = { 0 };
	sa.nLength = sizeof(sa);
	sa.bInheritHandle = TRUE;
	if(CreatePipe(&readPipe, &writePipe, &sa, 0) == 0)
	{
		return	0;
	}
	STARTUPINFOW	si = { 0 };
	si.cb = sizeof(si);
	si.dwFlags = STARTF_USESTDHANDLES | STARTF_USESHOWWINDOW;
	si.hStdOutput = writePipe;
	si.hStdError  = writePipe;
	si.wShowWindow= SW_HIDE;
	LPWSTR	lpszSendBuffer = (LPWSTR)GlobalAlloc(0, (lstrlenW(lpszCommand) + 256) * sizeof(WCHAR));
	if(lpszSendBuffer)
	{
		WCHAR	szCmdExePath[MAX_PATH] = { 0 };
		GetEnvironmentVariableW(L"ComSpec", szCmdExePath, _countof(szCmdExePath));
		lstrcpyW(lpszSendBuffer, szCmdExePath);
		lstrcatW(lpszSendBuffer, L" /K ");
		lstrcatW(lpszSendBuffer, lpszCommand);
		PROCESS_INFORMATION	pi = { 0 };
		if(CreateProcessW(NULL, lpszSendBuffer, NULL, NULL, TRUE, 0, NULL, NULL, &si, &pi))
		{
			CHAR	readBuf[1025];
			std::string	str;
			BOOL	end = FALSE;
			do
			{
				WaitForSingleObject(pi.hProcess, 1000);
				DWORD	totalLen, len;
				if(PeekNamedPipe(readPipe, NULL, 0, NULL, &totalLen, NULL) && totalLen > 0)
				{
					if(ReadFile(readPipe, readBuf, sizeof(readBuf) - 1, &len, NULL) && len > 0)
					{
						readBuf[len] = 0;
						str += readBuf;
					}
				}
				else
				{
					end = TRUE;
				}
			} while(!end);
			const	int	nLength = (int)str.length();
			if(nLength)
			{
				lpszReturn = (LPWSTR)GlobalAlloc(0, (nLength + 1) * sizeof(WCHAR));
				MultiByteToWideChar(CP_THREAD_ACP, 0, str.c_str(), -1, lpszReturn, nLength + 1);
			}
			CloseHandle(pi.hThread);
			{
				LPWSTR	lpszCurrentDirectory = GetCurrentWorkingDirectory(pi.hProcess);
				SetCurrentDirectoryW(lpszCurrentDirectory);
				GlobalFree((HGLOBAL)lpszCurrentDirectory);
			}
			TerminateProcess(pi.hProcess, 0);
			CloseHandle(pi.hProcess);
		}
		GlobalFree(lpszSendBuffer);
	}
	CloseHandle(writePipe);
	CloseHandle(readPipe);
	return	lpszReturn;
}
