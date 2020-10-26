using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
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
		// 最大履歴数
		const	int		MAXPLACE = 30;

		// アドレスリスト
		private	static	List<string>	_MRU = new List<string>();

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
			System.Reflection.Assembly	asm = System.Reflection.Assembly.GetExecutingAssembly();
			string	path1 = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\";

			// AssemblyCompanyの取得
			System.Reflection.AssemblyCompanyAttribute	asmcmp = (System.Reflection.AssemblyCompanyAttribute)Attribute.GetCustomAttribute(
						asm, typeof(System.Reflection.AssemblyCompanyAttribute));
			path1 += asmcmp.Company + @"\";
			FileInfo	f1 = new FileInfo(path1);
			if(!f1.Directory.Exists)
			{
				Directory.CreateDirectory(f1.DirectoryName);
			}
			// AssemblyProductの取得
			System.Reflection.AssemblyProductAttribute	asmprd = (System.Reflection.AssemblyProductAttribute)Attribute.GetCustomAttribute(
						asm, typeof(System.Reflection.AssemblyProductAttribute));
			path1 += asmprd.Product + @"\";
			FileInfo	f2 = new FileInfo(path1);
			if(!f2.Directory.Exists)
			{
				Directory.CreateDirectory(f2.DirectoryName);
			}
			return	path1;
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
		/// 場所の登録
		/// </summary>
		/// <param name="place">場所</param>
		/// <returns>空文字:false</returns>
		public	bool	AddPlace(string place)
		{
			if(string.IsNullOrEmpty(place))
				return	false;

			if(null == _MRU.Find(n => n == place))
				_MRU.Insert(0, place);

			return	true;
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
