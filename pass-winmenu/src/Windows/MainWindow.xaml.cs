using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PassWinmenu.Windows
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly List<Label> Options = new List<Label>();
		/// <summary>
		/// The label that is currently selected.
		/// </summary>
		public Label Selected { get; private set; }
		/// <summary>
		/// True if the user has chosen one of the options, false otherwise.
		/// </summary>
		public bool Success { get; private set; }

		private StyleConfig style;
		/// <summary>
		/// Initialises the window with the provided options.
		/// </summary>
		/// <param name="options">A list of options the user should choose from.</param>
		public MainWindow(IEnumerable<string> options)
		{
			style = ConfigManager.Config.Style;
			InitializeComponent();

			SearchBox.Background = BrushFromColourString(style.Search.BackgroundColour);
			SearchBox.Foreground = BrushFromColourString(style.Search.TextColour);
			SearchBox.BorderThickness = new Thickness(style.Search.BorderWidth);
			SearchBox.BorderBrush = BrushFromColourString(style.Search.BorderColour);
			SearchBox.FontSize = style.FontSize;
			SearchBox.FontFamily = new FontFamily(style.FontFamily);

			Background = BrushFromColourString(style.BackgroundColour);
			Left = style.OffsetLeft;
			Top = style.OffsetTop;
			Width = style.Width;
			Height = style.Height;
			foreach (var option in options)
			{
				var label = new Label
				{
					Content = option,
					FontSize = style.FontSize,
					FontFamily = new FontFamily(style.FontFamily),
					Background = BrushFromColourString(style.Options.BackgroundColour),
					Foreground = BrushFromColourString(style.Options.TextColour),
					Padding = new Thickness(0,0,0,2),
					Margin = new Thickness(7, 0, 7, 0)
				};
				Options.Add(label);
				WrapPanel.Children.Add(label);
			}
			Select(Options.First());
		}

		/// <summary>
		/// Handles text input in the textbox.
		/// </summary>
		private void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			// We split on spaces to allow the user to quickly search for a certain term, as it allows them
			// to search, for example, for reddit.com/username by entering "re us"
			var terms = SearchBox.Text.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			var firstSelectionFound = false;
			foreach (var option in Options)
			{
				var content = (string)option.Content;
				var lContent = content.ToLower();

				// The option is only a match if it contains every term in the terms array.
				if (terms.All(term => lContent.Contains(term)))
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

		/// <summary>
		/// Selects a label; deselecting the previously selected label.
		/// </summary>
		/// <param name="label">The label to be selected.</param>
		private void Select(Label label)
		{
			if (label == null) return;

			if (Selected != null)
			{
				Selected.Background = BrushFromColourString(style.Options.BackgroundColour);
				Selected.Foreground = BrushFromColourString(style.Options.TextColour);
				Selected.BorderThickness = new Thickness(style.Options.BorderWidth);
			}
			Selected = label;
			Selected.Background = BrushFromColourString(style.Selection.BackgroundColour);
			Selected.Foreground = BrushFromColourString(style.Selection.TextColour);
			Selected.BorderThickness = new Thickness(style.Selection.BorderWidth);
		}

		private static Color ColourFromString(string str)
		{
			return (Color)ColorConverter.ConvertFromString(str);
		}

		private static SolidColorBrush BrushFromColourString(string colour)
		{
			return new SolidColorBrush(ColourFromString(colour));
		}

		private void MainWindow_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			SearchBox.Focus();
		}

		private void MainWindow_OnLostFocus(object sender, RoutedEventArgs e)
		{
			Topmost = true;
			Focus();
		}

		private Label FindPrevious(int index)
		{
			int previous = index - 1;
			if (previous >= 0)
			{
				var opt = Options[previous];
				return opt.Visibility == Visibility.Visible ? Options[previous] : FindPrevious(previous);
			}
			else
			{
				return null;
			}
		}
		private Label FindNext(int index)
		{
			int next = index + 1;
			if (next < Options.Count)
			{
				var opt = Options[next];
				return opt.Visibility == Visibility.Visible ? Options[next] : FindNext(next);
			}
			else
			{
				return null;
			}
		}

		private void SearchBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Left)
			{
				e.Handled = true;
				var index = Options.IndexOf(Selected);
				if (index > 0) Select(FindPrevious(index));
			}
			else if (e.Key == Key.Right)
			{
				e.Handled = true;
				var index = Options.IndexOf(Selected);
				if (index < Options.Count - 1) Select(FindNext(index));
			}
			else if (e.Key == Key.Enter)
			{
				e.Handled = true;
				Success = true;
				Close();
			}
			else if (e.Key == Key.Escape)
			{
				Close();
			}
		}
	}
}
