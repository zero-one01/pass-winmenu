namespace PassWinmenu.Configuration
{
	internal class InterfaceConfig
	{
		public bool FollowCursor { get; set; } = true;

		public string DirectorySeparator { get; set; } = "/";

		public double ClipboardTimeout { get; set; } = 30;

		public PasswordEditorConfig PasswordEditor { get; set; } = new PasswordEditorConfig();

		public StyleConfig Style { get; set; } = new StyleConfig();
	}
}
