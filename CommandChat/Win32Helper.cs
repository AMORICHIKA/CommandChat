using System;
using System.Runtime.InteropServices;

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
