using System;
using System.IO;
using System.Xml;
using System.Diagnostics;

namespace	CommandChat.Properties
{
	// このクラスでは設定クラスでの特定のイベントを処理することができます:
	//	SettingChanging イベントは、設定値が変更される前に発生します。
	//	PropertyChanged イベントは、設定値が変更された後に発生します。
	//	SettingsLoaded イベントは、設定値が読み込まれた後に発生します。
	//	SettingsSaving イベントは、設定値が保存される前に発生します。
	internal	sealed	partial	class	Settings
	{
		/// <summary>
		/// コンストラクタ
		/// </summary>
		public	Settings()
		{
			// // 設定の保存と変更のイベント ハンドラーを追加するには、以下の行のコメントを解除します:
			//
			// this.SettingChanging += this.SettingChangingEventHandler;
			//
			// this.SettingsSaving += this.SettingsSavingEventHandler;
			//
		}
		/// <summary>
		/// 設定値の変更イベント
		/// </summary>
		/// <param name="sender">送信元</param>
		/// <param name="e">イベント情報</param>
		private	void	SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e)
		{
			// SettingChangingEvent イベントを処理するコードをここに追加してください。
		}
		/// <summary>
		/// 設定値の保存イベント
		/// </summary>
		/// <param name="sender">送信元</param>
		/// <param name="e">イベント情報</param>
		private	void	SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// SettingsSaving イベントを処理するコードをここに追加してください。
		}

		/// <summary>
		/// ファイルパスの取得
		/// </summary>
		public	string	GetFilePath()
		{
			// 設定ファイルを保存するフォルダを指定します
			return	Path.GetTempPath();
		}

		// 設定ファイル名
		private	const	string	_strFileName = "Setting.xml";

		/// <summary>
		/// 設定値の保存
		/// </summary>
		public	void	SaveSetting()
		{
			string	path = GetFilePath();

			// XML設定
			XmlWriterSettings	setting = new XmlWriterSettings();
			setting.Indent = true;
			setting.IndentChars = "    ";	// インデント（スペース４つ）

			try
			{
				using(XmlWriter w = XmlWriter.Create(path+_strFileName, setting))
				{
					w.WriteStartElement("Settings");

					w.WriteElementString("MainWindow",	Default.MainWindow);			// ウィンドウ情報
					//
					// 要素が増えた場合はここに記述
					//
					w.WriteEndElement();	// </Settings>
				}
			}
			catch
			{
				// 失敗
				Debug.WriteLine("SaveSetting error!!\n");
			}
		}
		/// <summary>
		/// 設定値の復元
		/// </summary>
		public	void	LoadSetting()
		{
			string	path = GetFilePath();
			try
			{
				using(XmlReader r = XmlReader.Create(path+_strFileName))
				{
					while(r.Read())
					{
						if(r.NodeType == XmlNodeType.Element)
						{
							GetSettings(r);
						}
					}
				}
			}
			catch
			{
				// 失敗（初回起動時）
				Debug.WriteLine("LoadSetting error!!\n");
			}
		}
		/// <summary>
		/// 値の検証（文字列）
		/// </summary>
		/// <param name="str">検証値</param>
		/// <param name="def">基準値</param>
		/// <returns>設定値</returns>
		private	string	CheckValue(string str, string def)
		{
			return	0 == str.Length ? def:str;
		}
		/// <summary>
		/// 値の検証（整数値）
		/// </summary>
		/// <param name="str">検証値</param>
		/// <param name="def">基準値</param>
		/// <returns>設定値</returns>
		private	int		CheckValue(string str, int def)
		{
			int	value;
			return	!int.TryParse(str, out value) ? def:value;
		}
		/// <summary>
		/// 値の検証（BOOL値）
		/// </summary>
		/// <param name="str">検証値</param>
		/// <param name="def">基準値</param>
		/// <returns>設定値</returns>
		private	bool	CheckValue(string str, bool def)
		{
			bool	value;
			if(!bool.TryParse(str, out value))
				return	def;

			return	value;
		}
		/// <summary>
		/// データの復元
		/// </summary>
		/// <param name="reader">Xmlリーダー</param>
		private	void	GetSettings(XmlReader reader)
		{
			while(reader.Read())
			{
				if((reader.NodeType == XmlNodeType.EndElement)
				&& (reader.Name == "Settings"))
				{
					break;
				}
				else
				if(reader.NodeType == XmlNodeType.Element)
				{
					switch(reader.Name)
					{
					case "MainWindow":
						Default.MainWindow = CheckValue(reader.ReadString(), Default.MainWindow);
						break;
					//
					// 要素が増えた場合はここに記述
					//
					}
				}
			}
		}
	}
}
