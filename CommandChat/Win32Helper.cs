using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Windows;

namespace	ControlLibrary
{
	public	class	Win32
	{
		public	const	UInt32	WM_DPICHANGED	= 0x02E0;
		public	const	UInt32	WM_APP			= 0x8000;

		[DllImport("user32.dll")]
		public	static	extern	IntPtr	PostMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.U1)]
		public	static	extern	bool	SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.U1)]
		public	static	extern	bool	GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);

		/// <summary>
		/// ウィンドウ位置の設定
		/// </summary>
		/// <param name="str">カンマ区切り文字列</param>
		/// <param name="hwnd">対象ウィンドウハンドル</param>
		public	static	void	SetWindowPosition(string str, IntPtr hwnd)
		{
			// 無効なデータはスキップ
			if(string.IsNullOrWhiteSpace(str))
				return;

			try
			{
				WINDOWPLACEMENT	wp = WINDOWPLACEMENT.Convert(str);
				int	length	= wp.length;
				wp.length	= Marshal.SizeOf(typeof(WINDOWPLACEMENT));
				// 無効なデータはスキップ
				if(wp.length != length)
					return;

				wp.flags = 0;

				// 最小化を解除
				if(wp.showCmd == (int)SW.SHOWMINIMIZED)
					wp.showCmd = (int)SW.SHOWNORMAL;

				CheckScreen(ref wp);

				SetWindowPlacement(hwnd, ref wp);
			}
			catch(SerializationException e)
			{
				Debug.WriteLine(string.Format("WindowPlacement.SetWindowPosition : {0}", e.Message));
			}
		}
		/// <summary>
		/// スクリーン内の補正
		/// </summary>
		/// <param name="wp">WINDOWPLACEMENT</param>
		/// <returns>結果(true:補正なし,false:補正あり)</returns>
		public	static	bool	CheckScreen(ref WINDOWPLACEMENT wp)
		{
			System.Drawing.Rectangle	rect = new System.Drawing.Rectangle(
						wp.rcNormalPosition_left,
						wp.rcNormalPosition_top,
						wp.rcNormalPosition_right - wp.rcNormalPosition_left,
						wp.rcNormalPosition_bottom- wp.rcNormalPosition_top);
			// スクリーン内に存在するか検証
			System.Windows.Forms.Screen	scr = System.Windows.Forms.Screen.FromRectangle(rect);

			// タスクバーを除いた領域内で検証する
			System.Drawing.Rectangle	area = scr.WorkingArea;
			if(area.Contains(rect))
				return	true;

			// 表示位置を補正する
			int	l = rect.Left, t = rect.Top;
			if(area.Left  > rect.Left  )	l = area.Left;
			if(area.Right < rect.Right )	l = rect.Left- (rect.Right - area.Right);
			if(area.Top   > rect.Top   )	t = area.Top;
			if(area.Bottom< rect.Bottom)	t = rect.Top - (rect.Bottom- area.Bottom);

			System.Drawing.Rectangle	tmp = new System.Drawing.Rectangle(l, t, rect.Width, rect.Height);
			if(area.Contains(tmp))
			{
				wp.rcNormalPosition_left  = l;
				wp.rcNormalPosition_top	  = t;
				wp.rcNormalPosition_right = wp.rcNormalPosition_left+ rect.Width;
				wp.rcNormalPosition_bottom= wp.rcNormalPosition_top + rect.Height;
				return	false;
			}
			// 中央に補正する
			Rect	work = SystemParameters.WorkArea;
			rect.X = (int)((work.Width - rect.Width )/ 2.0);
			rect.Y = (int)((work.Height- rect.Height)/ 2.0);
			if(rect.Left < 0 || rect.Top < 0)
			{
				// 納まらない場合はサイズを調整する
				wp.rcNormalPosition_left  = 0;
				wp.rcNormalPosition_top	  = 0;
				wp.rcNormalPosition_right = (int)work.Width;
				wp.rcNormalPosition_bottom= (int)work.Height;
			}
			else
			{
				wp.rcNormalPosition_left  = rect.X;
				wp.rcNormalPosition_top	  = rect.Y;
				wp.rcNormalPosition_right = wp.rcNormalPosition_left+ rect.Width;
				wp.rcNormalPosition_bottom= wp.rcNormalPosition_top + rect.Height;
			}
			return	false;
		}
		/// <summary>
		/// ウィンドウ位置の取得
		/// </summary>
		/// <param name="hwnd">対象ウィンドウハンドル</param>
		/// <returns>位置情報の文字列</returns>
		public	static	string	GetWindowPosition(IntPtr hwnd)
		{
			WINDOWPLACEMENT	wp;
			wp.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));

			if(!GetWindowPlacement(hwnd, out wp))
				return	"";

			return	wp.ToString();
		}
	}

	[System.Reflection.Obfuscation(Exclude = true)]
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public	struct	WINDOWPLACEMENT
	{
		public	int	length;
		public	int	flags;
		public	int	showCmd;
		public	int	ptMinPosition_x;
		public	int	ptMinPosition_y;
		public	int	ptMaxPosition_x;
		public	int	ptMaxPosition_y;
		public	int	rcNormalPosition_left;
		public	int	rcNormalPosition_top;
		public	int	rcNormalPosition_right;
		public	int	rcNormalPosition_bottom;

		/// <summary>
		/// 位置情報を文字列に変換
		/// </summary>
		/// <returns>カンマ区切りの文字列</returns>
		public	override	string	ToString()
		{
			string	str = length.ToString()+
			"," + flags.ToString()+
			"," + showCmd.ToString()+
			"," + ptMinPosition_x.ToString()+
			"," + ptMinPosition_y.ToString()+
			"," + ptMaxPosition_x.ToString()+
			"," + ptMaxPosition_y.ToString()+
			"," + rcNormalPosition_left.ToString()+
			"," + rcNormalPosition_top.ToString()+
			"," + rcNormalPosition_right.ToString()+
			"," + rcNormalPosition_bottom.ToString();
			return	str;
		}
		/// <summary>
		/// 文字列から位置情報に復元
		/// </summary>
		/// <param name="str">カンマ区切り文字列</param>
		/// <returns>位置情報</returns>
		public	static	WINDOWPLACEMENT	Convert(string str)
		{
			WINDOWPLACEMENT	wp = new WINDOWPLACEMENT();
			int	value;
			string[]	split = str.Split(',');
			for(int i = 0; i < split.Length; ++i)
			{
				switch(i)
				{
				case  0:	wp.length	= int.TryParse(split[i], out value) ? value:0;	break;
				case  1:	wp.flags	= int.TryParse(split[i], out value) ? value:0;	break;
				case  2:	wp.showCmd	= int.TryParse(split[i], out value) ? value:0;	break;
				case  3:	wp.ptMinPosition_x = int.TryParse(split[i], out value) ? value:0;	break;
				case  4:	wp.ptMinPosition_y = int.TryParse(split[i], out value) ? value:0;	break;
				case  5:	wp.ptMaxPosition_x = int.TryParse(split[i], out value) ? value:0;	break;
				case  6:	wp.ptMaxPosition_y = int.TryParse(split[i], out value) ? value:0;	break;
				case  7:	wp.rcNormalPosition_left	= int.TryParse(split[i], out value) ? value:0;	break;
				case  8:	wp.rcNormalPosition_top		= int.TryParse(split[i], out value) ? value:0;	break;
				case  9:	wp.rcNormalPosition_right	= int.TryParse(split[i], out value) ? value:0;	break;
				case 10:	wp.rcNormalPosition_bottom	= int.TryParse(split[i], out value) ? value:0;	break;
				}
			}
			return	wp;
		}
	}
	internal	enum	SW
	{
		SHOWNORMAL		= 1,
		SHOWMINIMIZED	= 2,
		SHOWMAXIMIZED	= 3,
	}
}
