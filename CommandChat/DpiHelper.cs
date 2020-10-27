using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

// Per-Monitor-Dpi をAttached Propertyで実現する
// http://espresso3389.hatenablog.com/entry/2014/04/06/050948

namespace	ControlLibrary
{
	/// <summary>
	/// Handles all the Per-Monitor-DPI related tasks for a window.
	/// </summary>
	public	class	DpiHelper
	{
		// DPI変更メッセージ
		public	static	uint	DpiChanged = Win32.WM_APP+100;

		static	DpiHelper()
		{
			if(DpiHelperMethods.IsSupported)
				DpiMethods.SetProcessDpiAwareness(DpiMethods.ProcessDpiAwareness.DpiHelperAware);
		}

		/// <summary>
		/// Setup Per-Monitor-DPI configurations on a window.
		/// </summary>
		/// <param name="window">Target window.</param>
		public	DpiHelper(Window window)
		{
			this.window = window;
			this.window.Loaded += window_Loaded;
			this.window.Closed += window_Closed;
		}
		/// <summary>
		/// ロードイベント
		/// </summary>
		/// <param name="sender">送信元</param>
		/// <param name="e">イベント情報</param>
		private	void	window_Loaded(object sender, RoutedEventArgs e)
		{
			this.systemDpi  = window.GetSystemDpi();
			this.hwndSource = PresentationSource.FromVisual(window) as HwndSource;
			if(this.hwndSource != null)
			{
				this.currentDpi = this.hwndSource.GetDpi();
				this.ChangeDpi(this.currentDpi);
				this.hwndSource.AddHook(this.WndProc);
			}
		}
		/// <summary>
		/// クローズイベント
		/// </summary>
		/// <param name="sender">送信元</param>
		/// <param name="e">イベント情報</param>
		private	void	window_Closed(object sender, EventArgs e)
		{
			RemoveHook();
		}
		/// <summary>
		/// フックの解放
		/// </summary>
		public	void	RemoveHook()
		{
			if(this.hwndSource != null)
			{
				this.hwndSource.RemoveHook(this.WndProc);
				this.hwndSource = null;
			}
			if(this.window != null)
			{
				this.window.Loaded -= window_Loaded;
				this.window.Closed -= window_Closed;
				this.window = null;
			}
		}
		/// <summary>
		/// ウィンドウプロシジャ
		/// </summary>
		/// <param name="hwnd">ウィンドウハンドル</param>
		/// <param name="msg">メッセージ</param>
		/// <param name="wParam">パラメータ</param>
		/// <param name="lParam">パラメータ</param>
		/// <param name="handled">結果</param>
		/// <returns>結果</returns>
		private	IntPtr	WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if(msg == Win32.WM_DPICHANGED)
			{
				var	dpiX = wParam.ToHiWord();
				var	dpiY = wParam.ToLoWord();
				this.ChangeDpi(new Dpi(dpiX, dpiY));

				Win32.PostMessage(hwnd, DpiChanged, wParam, lParam);
				handled = true;
			}
			return	IntPtr.Zero;
		}
		/// <summary>
		/// DPIの変更
		/// </summary>
		/// <param name="dpi">DPI情報</param>
		private	void	ChangeDpi(Dpi dpi)
		{
			if(!DpiHelperMethods.IsSupported)
				return;

			var	elem = window.Content as FrameworkElement;
			if(elem != null)
			{
				elem.LayoutTransform = (dpi == this.systemDpi)
					? Transform.Identity
					: new ScaleTransform((double)dpi.X / this.systemDpi.X, (double)dpi.Y / this.systemDpi.Y);
			}

			this.window.Width  = this.window.Width * dpi.X / this.currentDpi.X;
			this.window.Height = this.window.Height* dpi.Y / this.currentDpi.Y;

			Debug.WriteLine(string.Format("DPI Change: {0} -> {1} (System: {2})",
				this.currentDpi, dpi, this.systemDpi));

			this.currentDpi = dpi;
		}

		private	Dpi			systemDpi;
		private	Dpi			currentDpi;
		private	Window		window;
		private	HwndSource	hwndSource;

		//
		// Attached property implementation
		//
		public	static	readonly	DependencyProperty IsEnabledProperty =
			DependencyProperty.RegisterAttached("IsEnabled",
			typeof(bool), typeof(DpiHelper), new PropertyMetadata(false, SetEnabled));

		public	static	void	SetIsEnabled(DependencyObject dp, bool value)
		{
			dp.SetValue(IsEnabledProperty, value);
		}

		public	static	bool	GetIsEnabled(DependencyObject dp)
		{
			return	(bool)dp.GetValue(IsEnabledProperty);
		}

		private	static	void	SetEnabled(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			var	window = sender as Window;
			if(window == null)
				return;

			if((bool)e.OldValue)
			{
				GetDpiHelper(window).RemoveHook();
			}

			if((bool)e.NewValue)
			{
				SetDpiHelper(window, new DpiHelper(window));
			}
		}

		private	static	readonly	DependencyProperty	DpiHelperProperty =
			DependencyProperty.RegisterAttached("DpiHelper", typeof(DpiHelper), typeof(DpiHelper));

		private	static	void	SetDpiHelper(DependencyObject dp, DpiHelper value)
		{
			dp.SetValue(DpiHelperProperty, value);
		}

		private	static	DpiHelper	GetDpiHelper(DependencyObject dp)
		{
			return	(DpiHelper)dp.GetValue(DpiHelperProperty);
		}
		/// <summary>
		/// モニターごとのDPI情報を取得する
		/// </summary>
		/// <param name="mon">モニタハンドル</param>
		/// <returns>DpiScale</returns>
		internal	static	DpiScale	GetDpiForMonitor(IntPtr mon)
		{
			uint	dpiX = 96;
			uint	dpiY = 96;
			if(null != mon && DpiHelperMethods.IsSupported)
				DpiMethods.GetDpiForMonitor(mon, MonitorDpiType.Default, ref dpiX, ref dpiY);

			return	new DpiScale(dpiX/96.0, dpiY/96.0);
		}
		/// <summary>
		/// 指定スクリーンにおけるDPI情報を取得する
		/// </summary>
		/// <param name="screen">スクリーン</param>
		/// <returns>DpiScale</returns>
		public	static	DpiScale	GetDpi(System.Windows.Forms.Screen screen)
		{
			var	point = new System.Drawing.Point(screen.Bounds.Left + 1, screen.Bounds.Top + 1);
			return	GetDpiForMonitor(DpiMethods.MonitorFromPoint(point, DpiMethods.MonitorDefaultTo.Nearest));
		}
		/// <summary>
		/// 指定ウィンドウにおけるDPI情報を取得する
		/// </summary>
		/// <param name="hwnd">ウィンドウハンドル</param>
		/// <returns>DpiScale</returns>
		public	static	DpiScale	GetDpi(IntPtr hwnd)
		{
			return	GetDpi(System.Windows.Forms.Screen.FromHandle(hwnd));
		}
		/// <summary>
		/// 指定スクリーンにおけるDPIを取得する
		/// </summary>
		/// <param name="screen">スクリーン</param>
		/// <param name="dpiX">X値</param>
		/// <param name="dpiY">Y値</param>
		public	static	void	GetDpi(System.Windows.Forms.Screen screen, out float dpiX, out float dpiY)
		{
			DpiScale	dpi = GetDpi(screen);
			dpiX = (float)dpi.PixelsPerInchX;
			dpiY = (float)dpi.PixelsPerInchY;
		}
		/// <summary>
		/// DPIスケールを取得する
		/// </summary>
		/// <param name="hwnd">ウィンドウハンドル</param>
		/// <returns>スケール</returns>
		public	static	float	GetDisplayScaleFactor(IntPtr hwnd)
		{
			return	(float)GetDpiForMonitor(DpiMethods.MonitorFromWindow(hwnd, DpiMethods.MonitorDefaultTo.Nearest)).DpiScaleX;
		}
	}

	internal	static	class	DpiHelperMethods
	{

		public	static	bool	IsSupported
		{
			get
			{
				// Windows 8.1: 6.3 以降
				return	Environment.OSVersion.Version >= new Version(6, 3, 0);
			}
		}

		public	static	Dpi	GetSystemDpi(this Visual visual)
		{
			var	source = PresentationSource.FromVisual(visual);
			if(source != null && source.CompositionTarget != null)
			{
				return	new Dpi(
					(uint)(Dpi.Default.X * source.CompositionTarget.TransformToDevice.M11),
					(uint)(Dpi.Default.Y * source.CompositionTarget.TransformToDevice.M22));
			}
			return	Dpi.Default;
		}

		public	static	Dpi	GetDpi(this HwndSource hwndSource, MonitorDpiType dpiType = MonitorDpiType.Default)
		{
			if(!IsSupported)
				return	Dpi.Default;

			var	hMonitor = DpiMethods.MonitorFromWindow(hwndSource.Handle, DpiMethods.MonitorDefaultTo.Nearest);
			uint	dpiX = 1, dpiY = 1;
			DpiMethods.GetDpiForMonitor(hMonitor, dpiType, ref dpiX, ref dpiY);

			return	new Dpi(dpiX, dpiY);
		}
	}

	internal	struct	Dpi
	{
		public	static	readonly	Dpi	Default = new Dpi(96, 96);

		public	uint	X	{ get; set; }
		public	uint	Y	{ get; set; }

		public	Dpi(uint x, uint y)
			: this()
		{
			this.X = x;
			this.Y = y;
		}

		public	static	bool	operator ==(Dpi dpi1, Dpi dpi2)
		{
			return	dpi1.X == dpi2.X && dpi1.Y == dpi2.Y;
		}

		public	static	bool	operator !=(Dpi dpi1, Dpi dpi2)
		{
			return	!(dpi1 == dpi2);
		}

		public	bool	Equals(Dpi other)
		{
			return	this.X == other.X && this.Y == other.Y;
		}

		public	override	bool	Equals(object obj)
		{
			if(ReferenceEquals(null, obj))
				return	false;

			return	obj is Dpi && Equals((Dpi)obj);
		}

		public	override	int		GetHashCode()
		{
			unchecked
			{
				return	((int)this.X * 397) ^ (int)this.Y;
			}
		}

		public	override	string	ToString()
		{
			return	string.Format("[X={0},Y={1}]", X, Y);
		}
	}

	/// <summary>
	/// Identifies dots per inch (dpi) type.
	/// </summary>
	internal	enum	MonitorDpiType
	{
		/// <summary>
		/// MDT_Effective_DPI
		/// <para>Effective DPI that incorporates accessibility overrides and matches what Desktop Window Manage (DWM) uses to scale desktop applications.</para>
		/// </summary>
		EffectiveDpi = 0,

		/// <summary>
		/// MDT_Angular_DPI
		/// <para>DPI that ensures rendering at a compliant angular resolution on the screen, without incorporating accessibility overrides.</para>
		/// </summary>
		AngularDpi = 1,

		/// <summary>
		/// MDT_Raw_DPI
		/// <para>Linear DPI of the screen as measures on the screen itself.</para>
		/// </summary>
		RawDpi = 2,

		/// <summary>
		/// MDT_Default
		/// </summary>
		Default = EffectiveDpi,
	}

	internal	static	class	DpiMethods
	{
		// ウィンドウハンドルから、そのウィンドウが乗っているディスプレイハンドルを取得
		[DllImport("user32.dll", SetLastError = true)]
		public	static	extern	IntPtr	MonitorFromWindow(IntPtr hwnd, MonitorDefaultTo dwFlags);

		// ポイント位置からディスプレイハンドルを取得
		[DllImport("User32.dll", SetLastError = true)]
		public	static	extern	IntPtr	MonitorFromPoint(System.Drawing.Point pt, MonitorDefaultTo dwFlags);

		// ディスプレイの1インチあたりのドット数(DPI)を取得
		[DllImport("shcore.dll")]
		public	static	extern	void	GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, ref uint dpiX, ref uint dpiY);

		[DllImport("shcore.dll")]
		public	static	extern	int		SetProcessDpiAwareness(ProcessDpiAwareness awareness);

		[DllImport("shcore.dll")]
		private	static	extern	int		GetProcessDpiAwareness(IntPtr handle, ref ProcessDpiAwareness awareness);

		public	static	ProcessDpiAwareness	GetProcessDpiAwareness(IntPtr handle)
		{
			ProcessDpiAwareness	pda = ProcessDpiAwareness.Unaware;
			if(GetProcessDpiAwareness(handle, ref pda) == 0)
				return	pda;

			return	ProcessDpiAwareness.Unaware;
		}

		public	enum	MonitorDefaultTo
		{
			Null = 0,
			Primary = 1,
			Nearest = 2,
		}

		public	enum	ProcessDpiAwareness
		{
			Unaware = 0,
			SystemDpiAware = 1,
			DpiHelperAware = 2
		}
	}

	internal	static	class	IntPtrExtensions
	{
		public	static	ushort	ToLoWord(this IntPtr dword)
		{
			return	(ushort)((uint)dword & 0xffff);
		}

		public	static	ushort	ToHiWord(this IntPtr dword)
		{
			return	(ushort)((uint)dword >> 16);
		}
	}
}
