using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PassWinmenu.Configuration;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using FontFamily = System.Windows.Media.FontFamily;

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


		private bool isClosing;
		private bool firstActivation = true;

		private readonly StyleConfig style;

		/// <summary>
		/// Initialises the window with the provided options.
		/// </summary>
		/// <param name="options">A list of options the user should choose from.</param>
		/// <param name="position">A vector representing the position of the top-left corner of the window.</param>
		/// <param name="dimensions">A vector representing the width and height of the window.</param>
		/// <param name="orientation">The orientation along which the options will be shown.</param>
		public MainWindow(IEnumerable<string> options, Vector position, Vector dimensions, Orientation orientation)
		{
			style = ConfigManager.Config.Style;
			InitializeComponent();

			if (orientation == Orientation.Vertical)
			{
				WrapPanel.Orientation = Orientation.Vertical;
				// In order to prevent its content from wrapping, the WrapPanel should be added to a ScrollViewer.
				var scrollViewer = new ScrollViewer
				{
					VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
					Content = WrapPanel,
					Margin = new Thickness
					{
						Top = 25,
						Right = WrapPanel.Margin.Right,
						Bottom = WrapPanel.Margin.Bottom,
						Left = WrapPanel.Margin.Left
					}
				};
				// The WrapPanel must be removed from the grid before its ScrollViewer can be added.
				Grid.Children.Remove(WrapPanel);
				Grid.Children.Add(scrollViewer);

				// Reorient the searchbox so its margins match those of the WrapPanel.
				SearchBox.Margin = new Thickness(5, 5, 5, 5);
			}
			else
			{
				Grid.Children.Remove(SearchBox);
				WrapPanel.Children.Add(SearchBox);
			}

			SearchBox.CaretBrush = BrushFromColourString(style.CaretColour);
			SearchBox.Background = BrushFromColourString(style.Search.BackgroundColour);
			SearchBox.Foreground = BrushFromColourString(style.Search.TextColour);
			SearchBox.BorderThickness = new Thickness(style.Search.BorderWidth);
			SearchBox.BorderBrush = BrushFromColourString(style.Search.BorderColour);
			SearchBox.FontSize = style.FontSize;
			SearchBox.FontFamily = new FontFamily(style.FontFamily);

			Background = BrushFromColourString(style.BackgroundColour);

			Left = position.X;
			Top = position.Y;
			Width = dimensions.X;
			Height = dimensions.Y;

			foreach (var option in options)
			{
				var label = new Label
				{
					Content = option,
					FontSize = style.FontSize,
					FontFamily = new FontFamily(style.FontFamily),
					Background = BrushFromColourString(style.Options.BackgroundColour),
					Foreground = BrushFromColourString(style.Options.TextColour),
					Padding = new Thickness(0, 0, 0, 2),
					Margin = new Thickness(7, 0, 7, 0)
				};
				label.MouseLeftButtonUp += (sender, args) =>
				{
					if (label == Selected)
					{
						Success = true;
						Close();
					}
					else
					{
						Select(label);
					}
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
			var terms = SearchBox.Text.ToLower(CultureInfo.CurrentCulture).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			var firstSelectionFound = false;
			foreach (var option in Options)
			{
				var content = (string)option.Content;
				var lContent = content.ToLower(CultureInfo.CurrentCulture);

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
		/// <param name="label">The label to be selected. If this value is null, the selected label will not be changed.</param>
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
			Selected.BringIntoView();
		}

		/// <summary>
		/// Converts an ARGB hex colour code into a Color object.
		/// </summary>
		/// <param name="str">A hexadecimal colour code string (such as #AAFF00FF)</param>
		/// <returns>A colour object created from the colour code.</returns>
		private static Color ColourFromString(string str)
		{
			return (Color)ColorConverter.ConvertFromString(str);
		}

		/// <summary>
		/// Converts an ARGB hex colour code into a SolidColorBrush object.
		/// </summary>
		/// <param name="colour">A hexadecimal colour code string (such as #AAFF00FF)</param>
		/// <returns>A SolidColorBrush created from a Colour object created from the colour code.</returns>
		private static SolidColorBrush BrushFromColourString(string colour)
		{
			return new SolidColorBrush(ColourFromString(colour));
		}

		protected override void OnActivated(EventArgs e)
		{
			// If this is the first time the window is activated, we need to do a second call to Activate(),
			// otherwise it won't actually gain focus for some reason ¯\_(ツ)_/¯
			if (firstActivation)
			{
				firstActivation = false;
				Activate();
			}
			base.OnActivated(e);

			// Whenever the window is activated, the search box should gain focus.
			if(!isClosing) SearchBox.Focus();
		}
		
		// Whenever the window loses focus, we reactivate it so it's brought to the front again, allowing it
		// to regain focus.
		protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
		{
			base.OnLostKeyboardFocus(e);
			if(!isClosing) Activate();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			isClosing = true;
		}

		/// <summary>
		/// Finds the first non-hidden label to the left of the label at the specified index.
		/// </summary>
		/// <param name="index">The position in the label list where searching should begin.</param>
		/// <returns>The first label matching this condition, or null if no matching labels were found.</returns>
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

		/// <summary>
		/// Finds the first non-hidden label to the right of the label at the specified index.
		/// </summary>
		/// <param name="index">The position in the label list where searching should begin.</param>
		/// <returns>The first label matching this condition, or null if no matching labels were found.</returns>
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
			var selectionIndex = Options.IndexOf(Selected);
			switch (e.Key)
			{
				case Key.Left:
					e.Handled = true;
					if (selectionIndex > 0)
						Select(FindPrevious(selectionIndex));
					break;
				case Key.Right:
					e.Handled = true;
					if (selectionIndex < Options.Count - 1)
						Select(FindNext(selectionIndex));
					break;
				case Key.Up:
					e.Handled = true;
					if (selectionIndex > 0)
						Select(FindPrevious(selectionIndex));
					break;
				case Key.Down:
					e.Handled = true;
					if (selectionIndex < Options.Count - 1)
						Select(FindNext(selectionIndex));
					break;
				case Key.Enter:
					e.Handled = true;
					Success = true;
					Close();
					break;
				case Key.Escape:
					e.Handled = true;
					Close();
					break;
			}
		}
	}
}
