#pragma once

// パイプの監視
class	PipeMonitor
{
	HANDLE	hProcess_;
	HANDLE	eventExit_;
	HANDLE	readPipeOut_;
	HANDLE	writePipeOut_;
	HANDLE	threadReadOut_;

public:
	PipeMonitor();
	virtual	~PipeMonitor();

	BOOL	Create(HWND hwnd);
	BOOL	Start(HANDLE hProcess);
	void	Exit();
	HANDLE	GetHandle()	{	return	writePipeOut_;	}

protected:
	HWND	hwnd_;

	void	Read();
	virtual	void	Read(const std::string& str) = 0;

	static	UINT	WINAPI	ThreadReadOut(void* p);
};

// 標準メッセージの監視
class	ReadStdOut : public PipeMonitor
{
	std::wstring	curPath_;
	BOOL	commandLineErase_;

	void	Read(const std::string& str) override;

public:
	ReadStdOut();

	void	Erase()	{	commandLineErase_ = TRUE;	}
};

// エラーメッセージの監視
class	ReadErrOut : public PipeMonitor
{
	void	Read(const std::string& str) override;

public:
	ReadErrOut();
};

// コマンドプロセス
class	CmdProcess
{
private:
	CmdProcess();
	~CmdProcess();

public:
	static	CmdProcess*	Get();
	BOOL	Create(HWND hwnd);
	BOOL	Exit();
	BOOL	RunCommand(LPCWSTR lpszCommand);

private:
	void	NotifyExitProcess();

	static	UINT	WINAPI	ThreadCmdProcess(void* phProcess);

private:
	ReadStdOut	stdMonitor;
	ReadErrOut	errMonitor;

	HWND	hwnd_;
	HANDLE	hProcess_;
	HANDLE	readPipeStdIn_;
	HANDLE	writePipeStdIn_;
	HANDLE	threadProcess_;
};
