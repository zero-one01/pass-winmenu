using System;
using System.Collections.Generic;
using System.ComponentModel;
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
	internal abstract partial class MainWindow
	{
		protected readonly List<Label> Options = new List<Label>();
		/// <summary>
		/// The label that is currently selected.
		/// </summary>
		public Label Selected { get; protected set; }
		/// <summary>
		/// True if the user has chosen one of the options, false otherwise.
		/// </summary>
		public bool Success { get; protected set; }


		private bool isClosing;
		private bool firstActivation = true;
		private MainWindowConfiguration configuration;

		protected readonly StyleConfig StyleConfig;

		/// <summary>
		/// Initialises the window with the provided options.
		/// </summary>
		protected MainWindow(MainWindowConfiguration configuration)
		{
			this.configuration = configuration;
			StyleConfig = ConfigManager.Config.Style;
			InitializeComponent();

			if (configuration.Orientation == Orientation.Vertical)
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

			SearchBox.CaretBrush = BrushFromColourString(StyleConfig.CaretColour);
			SearchBox.Background = BrushFromColourString(StyleConfig.Search.BackgroundColour);
			SearchBox.Foreground = BrushFromColourString(StyleConfig.Search.TextColour);
			SearchBox.BorderThickness = new Thickness(StyleConfig.Search.BorderWidth);
			SearchBox.BorderBrush = BrushFromColourString(StyleConfig.Search.BorderColour);
			SearchBox.FontSize = StyleConfig.FontSize;
			SearchBox.FontFamily = new FontFamily(StyleConfig.FontFamily);

			Background = BrushFromColourString(StyleConfig.BackgroundColour);

			BorderBrush = BrushFromColourString(StyleConfig.BorderColour);
			BorderThickness = new Thickness(1);
		}

		protected override void OnContentRendered(EventArgs e)
		{
			var transfomedPos = PointFromScreen(configuration.Position);
			var transformedDims = PointFromScreen(configuration.Dimensions);

			Left = transfomedPos.X;
			Top = transfomedPos.Y;
			Width = transformedDims.X;
			Height = transformedDims.Y;

			base.OnContentRendered(e);
		}

		/// <summary>
		/// Handles text input in the textbox.
		/// </summary>
		protected abstract void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e);

		/// <summary>
		/// Selects a label; deselecting the previously selected label.
		/// </summary>
		/// <param name="label">The label to be selected. If this value is null, the selected label will not be changed.</param>
		protected void Select(Label label)
		{
			if (label == null) return;

			if (Selected != null)
			{
				Selected.Background = BrushFromColourString(StyleConfig.Options.BackgroundColour);
				Selected.Foreground = BrushFromColourString(StyleConfig.Options.TextColour);
				Selected.BorderThickness = new Thickness(StyleConfig.Options.BorderWidth);
			}
			Selected = label;
			Selected.Background = BrushFromColourString(StyleConfig.Selection.BackgroundColour);
			Selected.Foreground = BrushFromColourString(StyleConfig.Selection.TextColour);
			Selected.BorderThickness = new Thickness(StyleConfig.Selection.BorderWidth);
			Selected.BringIntoView();
		}

		/// <summary>
		/// Returns the text on the currently selected label.
		/// </summary>
		/// <returns></returns>
		public string GetSelection()
		{
			return (string)Selected.Content;
		}

		protected Label CreateLabel(string content)
		{
			var label = new Label
			{
				Content = content,
				FontSize = StyleConfig.FontSize,
				FontFamily = new FontFamily(StyleConfig.FontFamily),
				Background = BrushFromColourString(StyleConfig.Options.BackgroundColour),
				Foreground = BrushFromColourString(StyleConfig.Options.TextColour),
				Padding = new Thickness(0, 0, 0, 2),
				Margin = new Thickness(7, 0, 7, 0),
				Cursor = Cursors.Hand
			};
			label.MouseLeftButtonUp += (sender, args) =>
			{
				if (label == Selected)
				{
					HandleSelect();
				}
				else
				{
					Select(label);
					HandleSelectionChange(label);
				}
				// Return focus to the searchbox so the user can continue typing immediately.
				SearchBox.Focus();
			};
			return label;
		}

		protected void AddLabel(Label label)
		{
			Options.Add(label);
			WrapPanel.Children.Add(label);
		}

		protected void ClearLabels()
		{
			Selected = null;
			foreach (var label in Options)
			{
				WrapPanel.Children.Remove(label);
			}
			Options.Clear();
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
		protected static SolidColorBrush BrushFromColourString(string colour)
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
			if (!isClosing) SearchBox.Focus();
		}

		// Whenever the window loses focus, we reactivate it so it's brought to the front again, allowing it
		// to regain focus.
		protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
		{
			base.OnLostKeyboardFocus(e);
			if (!isClosing) Activate();
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
			var previous = index - 1;
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
			var next = index + 1;
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

		protected void SetSearchBoxText(string text)
		{
			SearchBox.Text = text;
			SearchBox.CaretIndex = text.Length;
		}

		protected virtual void HandleSelectionChange(Label selection)
		{

		}

		protected abstract void HandleSelect();

		private void SearchBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
		{
			var selectionIndex = Options.IndexOf(Selected);
			switch (e.Key)
			{
				case Key.Left:
					e.Handled = true;
					if (selectionIndex > 0)
					{
						var label = FindPrevious(selectionIndex);
						Select(label);
						HandleSelectionChange(label);
					}
					break;
				case Key.Right:
					e.Handled = true;
					if (selectionIndex < Options.Count - 1)
					{
						var label = FindNext(selectionIndex);
						Select(label);
						HandleSelectionChange(label);
					}
					break;
				case Key.Up:
					e.Handled = true;
					if (selectionIndex > 0)
					{
						var label = FindPrevious(selectionIndex);
						Select(label);
						HandleSelectionChange(label);
					}
					break;
				case Key.Down:
					e.Handled = true;
					if (selectionIndex < Options.Count - 1)
					{
						var label = FindNext(selectionIndex);
						Select(label);
						HandleSelectionChange(label);
					}
					break;
				case Key.Enter:
					e.Handled = true;
					HandleSelect();
					break;
				case Key.Escape:
					e.Handled = true;
					Close();
					break;
			}
		}
	}
}
