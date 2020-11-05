using mshtml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Settings = CommandChat.Properties.Settings;
using ControlLibrary;

namespace	CommandChat
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public	partial	class	MainWindow : Window
	{
		// ----------------------------------------------------------------------------------------
		// 32bit版
		[DllImport("CommandLib.dll", EntryPoint = "CmdCreate")]
		public	static	extern	bool	CmdCreate32(IntPtr hWnd);

		[DllImport("CommandLib.dll", EntryPoint = "CmdPop", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.LPWStr)]
		public	static	extern	string	CmdPop32();

		[DllImport("CommandLib.dll", EntryPoint = "CmdRun", CharSet = CharSet.Unicode)]
		public	static	extern	bool	CmdRun32(string cmd);

		[DllImport("CommandLib.dll", EntryPoint = "CmdExit")]
		public	static	extern	void	CmdExit32();
		// ----------------------------------------------------------------------------------------
		// 64bit版
		[DllImport("CommandLib64.dll", EntryPoint = "CmdCreate")]
		public	static	extern	bool	CmdCreate64(IntPtr hWnd);

		[DllImport("CommandLib64.dll", EntryPoint = "CmdPop", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.LPWStr)]
		public	static	extern	string	CmdPop64();

		[DllImport("CommandLib64.dll", EntryPoint = "CmdRun", CharSet = CharSet.Unicode)]
		public	static	extern	bool	CmdRun64(string cmd);

		[DllImport("CommandLib64.dll", EntryPoint = "CmdExit")]
		public	static	extern	void	CmdExit64();

		// HTMLドキュメント
		private IHTMLDocument2	_htmlDoc2;

		// コマンド履歴バッファ(Max.20)
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
		/// <summary>
		/// コンストラクタ
		/// </summary>
		public	MainWindow()
		{
			this.InitializeComponent();
			this.Loaded += MainWindow_Loaded;

			// タイトルバーアイコン
			this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/console.png"));
			this.Title = Directory.GetCurrentDirectory();

			this.scrl.Maximum = 20;
			this.scrl.Scroll += Scrl_Scroll;

			this.brow.Navigated += Brow_Navigated;

			// ナビゲート開始
			this.brow.NavigateToStream(System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream("CommandChat.Resources.index.html"));

			// ウィンドウスタイルの設定
			this.SourceInitialized += (s, e) =>
			{
				// 位置データがある場合は復元
				Win32.SetWindowPosition(Settings.Default.MainWindow, this.Handle);
			};
			// ウィンドウを閉じる前の処理
			this.Closing += (s, e) =>
			{
				// ウィンドウ情報を保存する
				Settings.Default.MainWindow = Win32.GetWindowPosition(this.Handle);

				Settings.Default.SaveSetting();
				Debug.WriteLine(_htmlDoc2.body.innerHTML);

				if(Environment.Is64BitProcess)
					CmdExit64();
				else
					CmdExit32();
			};
		}
		/// <summary>
		/// コマンド履歴
		/// </summary>
		/// <param name="sender">送信元</param>
		/// <param name="e">イベント情報</param>
		private	void	Scrl_Scroll(object sender, ScrollEventArgs e)
		{
			if(!_rngBuf.Exists)
				return;

			if(ScrollEventType.SmallIncrement == e.ScrollEventType)
			{
				this.edit.Text = _rngBuf.After();
			}
			else
			if(ScrollEventType.SmallDecrement == e.ScrollEventType)
			{
				this.edit.Text = _rngBuf.Before();
			}
			Debug.WriteLine(e.NewValue.ToString());
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
			switch(e.Key)
			{
			case	Key.Escape:
				Close();
				return;

			case	Key.Return:
				if(this.edit.IsFocused)
				{
					Send();
					return;
				}
				break;

			case	Key.F8:
				Scrl_Scroll(this, new ScrollEventArgs(ScrollEventType.SmallDecrement, 1));
				return;

			case	Key.F9:
				Scrl_Scroll(this, new ScrollEventArgs(ScrollEventType.SmallIncrement, 1));
				return;
			}
			base.OnKeyDown(e);
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

			// コマンドプロセスを起動
			bool	rc = Environment.Is64BitProcess ? CmdCreate64(this.Handle): CmdCreate32(this.Handle);
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
			switch((uint)msg)
			{
			case	Win32.WM_APP:
					Receive();
					break;
			}
			return	IntPtr.Zero;
		}
		/// <summary>
		/// 受信
		/// </summary>
		private	void	Receive()
		{
			string	returnhtml;
			while(!string.IsNullOrEmpty(returnhtml = Environment.Is64BitProcess ? CmdPop64(): CmdPop32()))
			{
				string	returnhtml2 = HtmlEncode(returnhtml);
				DateTime	dt = DateTime.Now;
				string	html = "<div class=\"result\"><div class=\"icon\"><img></div><div id=\"box1\"><div class=\"output\"><pre>";
				html += returnhtml2;
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
		/// 送信
		/// </summary>
		private	void	Send()
		{
			if(!this.edit.IsFocused)
				this.edit.Focus();

			string	command = this.edit.Text;
			int	nTextLength = command.Length;
			if(0 < nTextLength)
			{
				_rngBuf.Add(command);

				string	szCommand2 = HtmlEncode(command);
				DateTime	dt = DateTime.Now;
				string	html = string.Format("<div id=\"box2\">既読<br>{0:D2}:{1:D2}</div><div id=\"box1\"class=\"input\"><pre>", dt.Hour, dt.Minute);
				html += szCommand2;
				html += "</pre></div>";

				IHTMLElement	elm2 = _htmlDoc2.body;
				elm2.insertAdjacentHTML("beforeend", html);
				ScrollBottom(_htmlDoc2);

				this.edit.Text = "";
				this.brow.IsEnabled = false;
				this.edit.IsEnabled = false;
				bool	rc = Environment.Is64BitProcess ? CmdRun64(command): CmdRun32(command);
			}
		}
		/// <summary>
		/// HTMLエンコード
		/// </summary>
		/// <param name="lpText">変換前</param>
		/// <returns>変換後</returns>
		private	string	HtmlEncode(string lpText)
		{
			int	n = lpText.Length;
			string	html = "";
			for(int i = 0; i < n; i++)
			{
				switch (lpText[i])
				{
				case '>':
					html += "&gt;";
					break;
				case '<':
					html += "&lt;";
					break;
				case '&':
					html += "&amp;";
					break;
				default:
					html += lpText[i];
					break;
				}
			}
			return	html;
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
	// リングバッファ
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
			_readIndex = i < 0 ? 0: i;
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
