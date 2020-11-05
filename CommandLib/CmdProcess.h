#pragma once

class	CmdProcess
{
private:
	CmdProcess();
	~CmdProcess();

public:
	static	CmdProcess* Get();
	BOOL	Create(HWND hwnd);
	BOOL	Exit();
	BOOL	RunCommand(LPCWSTR lpszCommand);

private:
	void	NotifyExitProcess();
	void	ReadStdOut();
	static	UINT	WINAPI	ThreadCmdProcess(void* phProcess);
	static	UINT	WINAPI	ThreadReadStdOut(void*);

private:
	std::wstring	curPath_;
	HWND	hwnd_;
	HANDLE	hProcess_;
	HANDLE	readPipeStdIn_;
	HANDLE	writePipeStdIn_;
	HANDLE	readPipeStdOut_;
	HANDLE	writePipeStdOut_;
	HANDLE	threadProcess_;
	HANDLE	threadReadStdOut_;
	HANDLE	eventExit_;
};
