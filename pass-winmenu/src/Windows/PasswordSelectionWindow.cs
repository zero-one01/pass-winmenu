using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PassWinmenu.Configuration;

namespace PassWinmenu.Windows
{
	class PasswordSelectionWindow : MainWindow
	{
		public PasswordSelectionWindow(IEnumerable<string> options, MainWindowConfiguration configuration) : base(configuration)
		{
			foreach (var option in options)
			{
				var label = CreateLabel(option);
				AddLabel(label);
			}

			Select(Options.First());
		}

		protected override void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			// We split on spaces to allow the user to quickly search for a certain term, as it allows them
			// to search, for example, for reddit.com/username by entering "re us"
			var terms = SearchBox.Text.ToLower(CultureInfo.CurrentCulture).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			var firstSelectionFound = false;
			foreach (var option in Options)
			{
				var content = ((string)option.Content).ToLower(CultureInfo.CurrentCulture);

				// The option is only a match if it contains every term in the terms array.
				if (terms.All(term => content.Contains(term)))
				{
					// The first matched item should be pre-selected for convenience.
					if (!firstSelectionFound)
					{
						firstSelectionFound = true;
						Select(option);
					}
					option.Visibility = Visibility.Visible;
				}
				else
				{
					option.Visibility = Visibility.Collapsed;
				}
			}
		}

		protected override void HandleEnterKey()
		{
			Success = true;
			Close();
		}
	}
}
