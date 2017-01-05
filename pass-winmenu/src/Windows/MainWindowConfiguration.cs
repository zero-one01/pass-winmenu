using System;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using PassWinmenu.Configuration;
using PassWinmenu.Utilities.ExtensionMethods;
using Orientation = System.Windows.Controls.Orientation;

namespace PassWinmenu.Windows
{
	internal class MainWindowConfiguration
	{
		public Vector Position { get; set; }
		public Vector Dimensions { get; set; }
		public Orientation Orientation { get; set; }

		/// <summary>
		/// Builds a MainWindowConfiguration object according to the config settings.
		/// </summary>
		/// <returns></returns>
		public static MainWindowConfiguration ParseMainWindowConfiguration(Config config)
		{
			var activeScreen = Screen.AllScreens.First(screen => screen.Bounds.Contains(Cursor.Position));
			var selectedScreen = config.FollowCursor ? activeScreen : Screen.PrimaryScreen;

			double left, top, width, height;
			try
			{
				// The menu position may either be specified in pixels or percentage values.
				// ParseSize takes care of parsing both into a double (representing pixel values).
				left = selectedScreen.ParseSize(config.Style.OffsetLeft, Direction.Horizontal);
				top = selectedScreen.ParseSize(config.Style.OffsetTop, Direction.Vertical);
			}
			catch (Exception e) when (e is ArgumentNullException || e is FormatException || e is OverflowException)
			{
				throw new ConfigurationParseException($"Unable to parse the menu position from the config file (reason: {e.Message})", e);
			}
			try
			{
				width = selectedScreen.ParseSize(config.Style.Width, Direction.Horizontal);
				height = selectedScreen.ParseSize(config.Style.Height, Direction.Vertical);
			}
			catch (Exception e) when (e is ArgumentNullException || e is FormatException || e is OverflowException)
			{
				throw new ConfigurationParseException($"Unable to parse the menu dimensions from the config file (reason: {e.Message})", e);
			}

			Orientation orientation;

			if (!Enum.TryParse(config.Style.Orientation, true, out orientation))
			{
				throw new ConfigurationParseException("Unable to parse the menu orientation from the config file.");
			}

			return new MainWindowConfiguration
			{
				Dimensions = new Vector(width, height),
				Position = new Vector(left + selectedScreen.Bounds.Left, top + selectedScreen.Bounds.Top),
				Orientation = orientation
			};
		}
	}
}
