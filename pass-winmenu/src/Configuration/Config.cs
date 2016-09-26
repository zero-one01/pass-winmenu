using System;
using System.Diagnostics;
using PassWinmenu.ExtensionMethods;
using YamlDotNet.Serialization;

namespace PassWinmenu.Configuration
{
	internal class Config
	{
		private string passwordStore = Environment.ExpandEnvironmentVariables(@"%userprofile%\.password-store");
		public string PasswordStore
		{
			get { return passwordStore; }
			set { passwordStore = Environment.ExpandEnvironmentVariables(value); }
		}

		public string PasswordFileMatch { get; set; } = ".*\\.gpg$";
		public string GpgPath { get; set; } = "gpg";
		public string GitPath { get; set; } = "git";
		public double ClipboardTimeout { get; set; } = 30;
		public string DirectorySeparator { get; set; } = "/";
		public StyleConfig Style { get; set; } = new StyleConfig();
		public OutputConfig Output { get; set; } = new OutputConfig();
		public HotkeyConfig[] Hotkeys { get; set; } =
		{
			new HotkeyConfig
			{
				Hotkey = "ctrl alt p",
				ActionString = "decrypt-password",
				Options = new HotkeyOptions
				{
					CopyToClipboard = true
				}
			},
			new HotkeyConfig
			{
				Hotkey = "ctrl alt shift p",
				ActionString = "decrypt-password",
				Options = new HotkeyOptions
				{
					CopyToClipboard = true,
					TypeUsername = true,
					TypePassword = true
				}
			}
		};
		public bool FirstLineOnly { get; set; } = true;
		public bool FollowCursor { get; set; } = true;
	}

	internal class OutputConfig
	{
		public bool DeadKeys { get; set; } = false;
	}

	internal enum HotkeyAction
	{
		DecryptPassword,
		AddPassword,
		GitPull,
		GitPush
	}

	internal class HotkeyConfig
	{
		public string Hotkey { get; set; }
		[YamlIgnore]
		public HotkeyAction Action => (HotkeyAction)Enum.Parse(typeof(HotkeyAction), ActionString.ToPascalCase(), true);
		[YamlMember(Alias = "action")]
		public string ActionString { get; set; }
		public HotkeyOptions Options { get; set; } = new HotkeyOptions();
	}

	internal class HotkeyOptions
	{
		public bool CopyToClipboard { get; set; }
		public bool TypeUsername { get; set; }
		public bool TypePassword { get; set; }
	}

	internal class StyleConfig
	{
		public LabelStyleConfig Search { get; set; } = new LabelStyleConfig { TextColour = "#FFDDDDDD", BackgroundColour = "#00FFFFFF", BorderWidth = 0, BorderColour = "#FF000000" };
		public LabelStyleConfig Options { get; set; } = new LabelStyleConfig { TextColour = "#FFDDDDDD", BackgroundColour = "#00FFFFFF", BorderWidth = 0, BorderColour = "#FF000000" };
		public LabelStyleConfig Selection { get; set; } = new LabelStyleConfig { TextColour = "#FFFFFFFF", BackgroundColour = "#FFD88900", BorderWidth = 0, BorderColour = "#FF000000" };
		public string Orientation { get; set; } = "vertical";
		public double FontSize { get; set; } = 14;
		public string FontFamily { get; set; } = "Consolas";
		public string BackgroundColour { get; set; } = "#FF202020";
		public string CaretColour { get; set; } = "#FFDDDDDD";
		// These have to be strings, because they need to support percentage values.
		public string OffsetLeft { get; set; } = "40%";
		public string OffsetTop { get; set; } = "40%";
		public string Width { get; set; } = "20%";
		public string Height { get; set; } = "20%";
	}

	internal class LabelStyleConfig
	{
		public string TextColour { get; set; }
		public string BackgroundColour { get; set; }
		public int BorderWidth { get; set; }
		public string BorderColour { get; set; }
	}
}
