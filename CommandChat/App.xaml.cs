using System.Windows;
using Settings = CommandChat.Properties.Settings;

namespace	CommandChat
{
	/// <summary>
	/// App.xaml の相互作用ロジック
	/// </summary>
	public	partial	class	App : Application
	{
		/// <summary>
		/// スタートアップ
		/// </summary>
		/// <param name="e">操作情報</param>
		protected	override	void	OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			Settings.Default.LoadSetting();

			MainWindow	win = new MainWindow();
			win.Show();
		}
	}
}
