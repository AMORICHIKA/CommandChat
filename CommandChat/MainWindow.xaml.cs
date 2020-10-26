using mshtml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Settings = CommandChat.Properties.Settings;
//using ControlLibrary;

namespace	CommandChat
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public	partial	class	MainWindow : Window
	{
		[DllImport("CommandLib.dll", EntryPoint = "RunCommand", CharSet = CharSet.Unicode)]
		public	static	extern	string	RunCommand32(string command);

		[DllImport("CommandLib64.dll", EntryPoint = "RunCommand", CharSet = CharSet.Unicode)]
		public	static	extern	string	RunCommand64(string command);

		[DllImport("user32.dll")]
		public	static	extern	IntPtr	PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		const	int		WM_APP = 0x8000;

		private	IHTMLDocument2	_htmlDoc2;

		private	string		_Console;
		private	string		_Command;

		private	RingBuffer<string>	_rngBuf = new RingBuffer<string>(20);

		/// <summary>
		/// This is the Win32 Interop Handle for this Window
		/// </summary>
		private	IntPtr	Handle
		{
			get
			{
				return	new WindowInteropHelper(this).Handle;
			}
		}
		private	ContextMenu	SystemMenu
		{
			get
			{
				return	Resources["systemMenu"] as ContextMenu;
			}
		}
		/// <summary>
		/// コンストラクタ
		/// </summary>
		public	MainWindow()
		{
			this.InitializeComponent();
			this.Loaded += MainWindow_Loaded;
			this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/console.png"));
			this.Title = Directory.GetCurrentDirectory();

			this.brow.Navigated += Brow_Navigated;

			// ナビゲート開始
			this.brow.NavigateToStream(System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream("CommandChat.Resources.index.html"));

			// ウィンドウを閉じる前の処理
			this.Closing += (s, e) =>
			{
			//	string pos = Win32API.GetWindowPosition(this);

				// ウィンドウ情報を保存する
			//	Settings.Default.MainWindow = pos;

				Debug.WriteLine(_htmlDoc2.body.innerHTML);
			};
		}
		/// <summary>
		/// HTMLの解析
		/// </summary>
		/// <param name="sender">送信元</param>
		/// <param name="e">イベント情報</param>
		private	void	Brow_Navigated(object sender, NavigationEventArgs e)
		{
			_htmlDoc2 = brow.Document as IHTMLDocument2;
		}
		/// <summary>
		/// キーボード押下
		/// </summary>
		/// <param name="e">イベント情報</param>
		protected	override	void	OnKeyDown(KeyEventArgs e)
		{
			// システムメニューの置き換え
			if(Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.Space)
			{
		//		ShowContextMenu();
		//		e.Handled = true;
			}
			else
			if(Key.Return == e.Key)
			{
				if(this.edit.IsFocused)
					Action();
			}
			else
			if(Key.F8 == e.Key)
			{
				if(_rngBuf.Exists)
					this.edit.Text = _rngBuf.Before();
			}
			else
			if(Key.F9 == e.Key)
			{
				if(_rngBuf.Exists)
					this.edit.Text = _rngBuf.After();
			}
			else
			{
				base.OnKeyDown(e);
			}
		}
		/// <summary>
		/// ウィンドウのロード
		/// </summary>
		/// <param name="sender">送信元</param>
		/// <param name="e">イベント情報</param>
		private	void	MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			// イベントをフック
			HwndSource	source = HwndSource.FromHwnd(this.Handle);
			source.AddHook(new HwndSourceHook(WndProc));

			this.edit.Focus();
		}
		/// <summary>
		/// ウィンドウプロシジャ
		/// </summary>
		/// <param name="hwnd">ウィンドウハンドル</param>
		/// <param name="msg">メッセージ</param>
		/// <param name="wParam">パラメータ情報1</param>
		/// <param name="lParam">パラメータ情報2</param>
		/// <param name="handled">結果</param>
		private	IntPtr	WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			// システムメニューの置き換え
		/*	if(Win32API.WM_NCRBUTTONDOWN == msg
			|| Win32API.WM_NCLBUTTONDOWN == msg)
			{
				int	nMRU = wParam.ToInt32();
				if(Win32API.WM_MOVE == nMRU)
				{
					ShowContextMenu();
					handled = true;
				}
			}*/
			switch(msg)
			{
			case	WM_APP:
					Display();
					break;
			}
			return	IntPtr.Zero;
		}
		/// <summary>
		/// 結果の表示
		/// </summary>
		private	void	Display()
		{
			string	returnhtml = _Console;
			if(!string.IsNullOrEmpty(returnhtml))
			{
				string	returnhtml2 = HtmlEncode(returnhtml);
				DateTime	dt = DateTime.Now;
				string	html = "<div class=\"result\"><div class=\"icon\"><img></div><div id=\"box1\"><div class=\"output\"><pre>";
				html += !string.IsNullOrEmpty(returnhtml2) ? returnhtml2: returnhtml;
				html += string.Format("</pre></div></div><div id=\"box2\">{0:D2}:{1:D2}</div></div>", dt.Hour, dt.Minute);

				IHTMLElement	elm2 = _htmlDoc2.body;
				elm2.insertAdjacentHTML("beforeend", html);
				ScrollBottom(_htmlDoc2);
			}
			this.brow.IsEnabled = true;
			this.edit.IsEnabled = true;
			this.edit.Focus();
			this.Title = Directory.GetCurrentDirectory();
		}
		/// <summary>
		/// コマンドの表示
		/// </summary>
		private	void	Action()
		{
			if(!this.edit.IsFocused)
				this.edit.Focus();

			_Command = this.edit.Text;
			int nTextLength = _Command.Length;
			if(0 < nTextLength)
			{
				_rngBuf.Add(_Command);

				string	szCommand2 = HtmlEncode(_Command);
				DateTime	dt = DateTime.Now;
				string	html = string.Format("<div id=\"box2\">既読<br>{0:D2}:{1:D2}</div><div id=\"box1\"class=\"input\"><pre>", dt.Hour, dt.Minute);
				html += !string.IsNullOrEmpty(szCommand2) ? szCommand2: _Command;
				html += "</pre></div>";

				IHTMLElement	elm2 = _htmlDoc2.body;
				elm2.insertAdjacentHTML("beforeend", html);
				ScrollBottom(_htmlDoc2);

				this.edit.Text = "";
				this.brow.IsEnabled = false;
				this.edit.IsEnabled = false;
				Thread	thr = new Thread(new ParameterizedThreadStart(ThreadProc));
				thr.Start(this.Handle);
			}
		}
		/// <summary>
		/// スレッドの実行
		/// </summary>
		private	void	ThreadProc(object hwnd)
		{
			_Console = Environment.Is64BitProcess ? RunCommand64(_Command): RunCommand32(_Command);
			PostMessage((IntPtr)hwnd, WM_APP, IntPtr.Zero, IntPtr.Zero);
		}
		/// <summary>
		/// HTMLエンコード
		/// </summary>
		/// <param name="lpText">変換前</param>
		/// <returns>変換後</returns>
		private	string	HtmlEncode(string lpText)
		{
			int	i, m;
			int	n = lpText.Length;
			for(i = 0, m = 0; i < n; i++)
			{
				switch(lpText[i])
				{
				case '>':
				case '<':
					m += 4;
					break;
				case '&':
					m += 5;
					break;
				default:
					m++;
					break;
				}
			}
			if(n == m)
				return	null;

			string	ptr = "";
			for(i = 0; i < n; i++)
			{
				switch (lpText[i])
				{
				case '>':
					ptr += "&gt;";
					break;
				case '<':
					ptr += "&lt;";
					break;
				case '&':
					ptr += "&amp;";
					break;
				default:
					ptr += lpText[i];
					break;
				}
			}
			return	ptr;
		}
		/// <summary>
		/// 最終行へスクロール
		/// </summary>
		/// <param name="doc">HTMLドキュメント</param>
		private	void	ScrollBottom(IHTMLDocument2 doc)
		{
			IHTMLWindow2	html = doc.parentWindow;
			if(null != html)
			{
				html.scrollTo(0, short.MaxValue);
			}
		}
	}
	public	class	RingBuffer<T> : IEnumerable<T>
	{
		private	int		_size;
		private	T[]		_buffer;
		private	int		_writeIndex = -1;	// 書き終わった位置
		private	int		_readIndex = -1;	// 読み込み位置
		private	bool	_isFull = false;

		public	RingBuffer(int size)
		{
			_size	= size;
			_buffer = new T[size];
			_writeIndex = -1;
		}

		private	int	NextIndex(int ix)
		{
			return	++ix % _size;
		}

		private	int	GetStartIndex()
		{
			return	_isFull ? NextIndex(_writeIndex): 0;
		}

		public	void	Add(T value)
		{
			if(Find(value))
			{
				return;
			}
			_writeIndex = NextIndex(_writeIndex);
			if(_writeIndex == _size - 1)
				_isFull = true;
			_buffer[_writeIndex] = value;
		}
		public	bool	Find(T value)
		{
			for(int i = 0; i < Count; i++)
			{
				if(_buffer[i].Equals(value))
					return	true;
			}
			return	false;
		}
		public	T	Before()
		{
			int	i = (_readIndex-1) % _size;
			_readIndex = i < 0 ? _writeIndex: i;
			return	_buffer[_readIndex];
		}
		public	T	After()
		{
			int	i = (_readIndex+1) % _size;
			_readIndex = _writeIndex < i ? _writeIndex: i;
			return	_buffer[_readIndex];
		}
		public	bool	Exists
		{
			get { return Count > 0; }
		}

		public	int		Count
		{
			get { return _isFull ? _size : _writeIndex + 1; }
		}

		public	void	Clear()
		{
			_writeIndex = -1;
			_readIndex = -1;
			_isFull = false;
		}

		public	IEnumerator<T>	GetEnumerator()
		{
			var	index = GetStartIndex();
			for(int i = 0; i < Count; i++)
			{
				yield return _buffer[index];
				index = NextIndex(index);
			}
		}
		IEnumerator	IEnumerable.GetEnumerator()
		{
			return	this.GetEnumerator();
		}
	}
}
