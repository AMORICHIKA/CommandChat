using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace	CommandChat
{
	// TextBoxにWatermarkを表示する
	// http://blog.livedoor.jp/pso2_cgi-kokoroyoku_pso2/archives/20480158.html
	public	static	class	TextBoxAssist
	{
		#region Watermark

		/// <summary>
		/// Watermark用の添付プロパティを定義します。
		/// </summary>
		public	static	readonly	DependencyProperty	WatermarkProperty = DependencyProperty.RegisterAttached(
				"Watermark", typeof(string), typeof(TextBoxAssist), new PropertyMetadata(string.Empty, RaiseWatermarkPropertyChanged));

		public	static	void	SetWatermark(TextBox textBox, string watermark)
		{
			textBox.SetValue(WatermarkProperty, watermark);
		}

		public	static	string	GetWatermark(TextBox textBox)
		{
			return	(string)textBox.GetValue(WatermarkProperty);
		}

		/// <summary>
		/// ウォーターマークを表示する前に、以下のデフォルト値を保持します。
		/// </summary>
		private	static	string	_watermark;
		private	static	Brush	_watermarkDefaultBackground;
		private	static	Brush	_watermarkDefaultForeground;

		/// <summary>
		/// 添付プロパティが変更されたときに発生します。各種イベントの登録と、ウォーターマークを保持します。
		/// </summary>
		private	static	void	RaiseWatermarkPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var	textBox = d as TextBox;
			_watermark = e.NewValue as string;
			if(string.IsNullOrEmpty(_watermark))
			{
				textBox.Loaded -= TextBox_Loaded;
				textBox.SizeChanged -= TextBox_SizeChanged;
				textBox.TextChanged -= TextBox_TextChanged;
			}
			else
			{
				textBox.Loaded += TextBox_Loaded;
				textBox.SizeChanged += TextBox_SizeChanged;
				textBox.TextChanged += TextBox_TextChanged;
			}
		}
		/// <summary>
		/// 背景色と前景色のデフォルト値を格納します。またテキストが入力されていない場合は、ウォーターマークを表示します。
		/// </summary>
		private	static	void	TextBox_Loaded(object sender, RoutedEventArgs e)
		{
			var	textBox = sender as TextBox;
			_watermarkDefaultForeground = textBox.Foreground;
			_watermarkDefaultBackground = textBox.Background;
			if(string.IsNullOrEmpty(textBox.Text))
			{
				textBox.Background =
					CreateWatermark(_watermark, textBox.Background, textBox.Foreground, textBox);
			}
		}
		/// <summary>
		/// <para>イベントの発生順序としては「TextBox_Loaded」よりも先に発生するため。「IsLoaded」で先に要素が読み込まれているか判定します。</para>
		/// <para>テキストボックスのサイズが変更されるたびに、ウォーターマークサイズを更新します。</para>
		/// </summary>
		private	static	void	TextBox_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var	textBox = sender as TextBox;
			if(string.IsNullOrEmpty(textBox.Text) && textBox.IsLoaded)
			{
				textBox.Background =
					CreateWatermark(_watermark, _watermarkDefaultBackground, _watermarkDefaultForeground, textBox);
			}
		}
		/// <summary>
		/// 変更されたテキストに応じてウォーターマークまたはデフォルト値を表示します。
		/// </summary>
		private	static	void	TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var	textBox = sender as TextBox;
			if(string.IsNullOrEmpty(textBox.Text))
			{
				textBox.Background =
					CreateWatermark(_watermark, _watermarkDefaultBackground, _watermarkDefaultForeground, textBox);
			}
			else
			{
				textBox.Background = _watermarkDefaultBackground;
			}
		}
		/// <summary>
		/// ウォーターマークを作成します。
		/// </summary>
		private	static	VisualBrush	CreateWatermark(string watermark, Brush background, Brush foreground, TextBox textBox)
		{
			var	border = new Border
			{
				Child = new TextBlock
				{
					Text = watermark,
					Foreground = foreground,
					Opacity = 0.5,
				},
				Background = background,
				Padding = new Thickness(4,8,0,0),
				Height = textBox.RenderSize.Height,
				Width = textBox.RenderSize.Width,
			};
			var	visualBrush = new VisualBrush
			{
				Visual = border,
				Stretch = Stretch.None,
				TileMode = TileMode.None,
				AlignmentY = AlignmentY.Center,
				AlignmentX = AlignmentX.Left,
			};
			return	visualBrush;
		}
		#endregion
	}
}
