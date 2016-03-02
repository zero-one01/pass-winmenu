using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassWinmenu
{
	internal class Config
	{
		private string passwordStore = Environment.ExpandEnvironmentVariables(@"%userprofile%\.password-store");
		public string PasswordStore
		{
			get { return passwordStore; }
			set { passwordStore = Environment.ExpandEnvironmentVariables(value); }
		}
		public string PasswordFileMatch { get; set; } = "*.gpg";
		public string GpgPath { get; set; } = "gpg";
		public double ClipboardTimeout { get; set; } = 30;
		public string DirectorySeparator { get; set; } = "/";
		public StyleConfig Style { get; set; } = new StyleConfig();
		public bool FirstLineOnly { get; set; } = true;
	}

	internal class StyleConfig
	{
		public LabelStyleConfig Search { get; set; } = new LabelStyleConfig { TextColour = "#FFDDDDDD", BackgroundColour = "#00FFFFFF", BorderWidth = 0, BorderColour = "#FF000000" };
		public LabelStyleConfig Options { get; set; } = new LabelStyleConfig { TextColour = "#FFDDDDDD", BackgroundColour = "#00FFFFFF", BorderWidth = 0, BorderColour = "#FF000000" };
		public LabelStyleConfig Selection { get; set; } = new LabelStyleConfig { TextColour = "#FFFFFFFF", BackgroundColour = "#FFD88900", BorderWidth = 0, BorderColour = "#FF000000" };

		public double FontSize { get; set; } = 14;
		public string FontFamily { get; set; } = "Consolas";
		public string BackgroundColour { get; set; } = "#FF202020";
		public double OffsetLeft { get; set; } = 0;
		public double OffsetTop { get; set; } = 0;
		public double Width { get; set; } = 1920;
		public double Height { get; set; } = 18;
	}

	internal class LabelStyleConfig
	{
		public string TextColour { get; set; }
		public string BackgroundColour { get; set; }
		public int BorderWidth { get; set; }
		public string BorderColour { get; set; }
	}
}
