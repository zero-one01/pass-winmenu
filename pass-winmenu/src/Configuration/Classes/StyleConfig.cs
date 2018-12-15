namespace PassWinmenu.Configuration
{
	internal class StyleConfig
	{
		public LabelStyleConfig Search { get; set; } = new LabelStyleConfig { TextColour = "#FFDDDDDD", BackgroundColour = "#00FFFFFF", BorderWidth = 0, BorderColour = "#FF000000" };
		public LabelStyleConfig Options { get; set; } = new LabelStyleConfig { TextColour = "#FFDDDDDD", BackgroundColour = "#00FFFFFF", BorderWidth = 0, BorderColour = "#FF000000" };
		public LabelStyleConfig Selection { get; set; } = new LabelStyleConfig { TextColour = "#FFFFFFFF", BackgroundColour = "[accent]", BorderWidth = 0, BorderColour = "#FF000000" };
		public int ScrollBoundary { get; set; } = 0;
		public string Orientation { get; set; } = "vertical";
		public double FontSize { get; set; } = 14;
		public string FontFamily { get; set; } = "Consolas";
		public string BackgroundColour { get; set; } = "#FF202020";
		public string BorderColour { get; set; } = "[accent]";
		public string CaretColour { get; set; } = "#FFDDDDDD";
		// These have to be strings, because they need to support percentage values.
		public string OffsetLeft { get; set; } = "40%";
		public string OffsetTop { get; set; } = "40%";
		public string Width { get; set; } = "20%";
		public string Height { get; set; } = "20%";
	}
}
