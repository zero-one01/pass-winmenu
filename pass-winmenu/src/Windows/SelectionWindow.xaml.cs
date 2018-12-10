using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using PassWinmenu.Configuration;
using PassWinmenu.Hotkeys;
using PassWinmenu.Utilities;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Cursors = System.Windows.Input.Cursors;
using FontFamily = System.Windows.Media.FontFamily;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Label = System.Windows.Controls.Label;
using Orientation = System.Windows.Controls.Orientation;

namespace PassWinmenu.Windows
{
	/// <summary>
	/// Interaction logic for SelectionWindow.xaml
	/// </summary>
	internal abstract partial class SelectionWindow
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
		private readonly StyleConfig styleConfig;

		/// <summary>
		/// Initialises the window with the provided options.
		/// </summary>
		protected SelectionWindow(MainWindowConfiguration configuration)
		{
			TimerHelper.Current.TakeSnapshot("mainwnd-creating");
			this.configuration = configuration;
			InitializeComponent();

			if (configuration.Orientation == Orientation.Horizontal)
			{
				WrapPanel.Orientation = Orientation.Horizontal;
			}

			styleConfig = ConfigManager.Config.Interface.Style;
			SearchBox.CaretBrush = Helpers.BrushFromColourString(styleConfig.CaretColour);
			SearchBox.Background = Helpers.BrushFromColourString(styleConfig.Search.BackgroundColour);
			SearchBox.Foreground = Helpers.BrushFromColourString(styleConfig.Search.TextColour);
			SearchBox.BorderThickness = new Thickness(styleConfig.Search.BorderWidth);
			SearchBox.BorderBrush = Helpers.BrushFromColourString(styleConfig.Search.BorderColour);
			SearchBox.FontSize = styleConfig.FontSize;
			SearchBox.FontFamily = new FontFamily(styleConfig.FontFamily);

			Background = Helpers.BrushFromColourString(styleConfig.BackgroundColour);

			BorderBrush = Helpers.BrushFromColourString(styleConfig.BorderColour);
			BorderThickness = new Thickness(1);
			TimerHelper.Current.TakeSnapshot("mainwnd-created");
		}

		protected override void OnInitialized(EventArgs e)
		{
			TimerHelper.Current.TakeSnapshot("mainwnd-oninitialized-start");
			base.OnInitialized(e);
			TimerHelper.Current.TakeSnapshot("mainwnd-oninitialized-base-end");
		}

		protected override void OnContentRendered(EventArgs e)
		{
			TimerHelper.Current.TakeSnapshot("mainwnd-oncontentrendered-start");
			var transfomedPos = PointFromScreen(configuration.Position);
			var transformedDims = PointFromScreen(configuration.Dimensions);
			TimerHelper.Current.TakeSnapshot("mainwnd-oncontentrendered-transformed");

			Left = transfomedPos.X;
			Top = transfomedPos.Y;
			Width = transformedDims.X;
			Height = transformedDims.Y;

			TimerHelper.Current.TakeSnapshot("mainwnd-oncontentrendered-end");
			base.OnContentRendered(e);
			TimerHelper.Current.TakeSnapshot("mainwnd-oncontentrendered-base-end");
		}

		/// <summary>
		/// Handles text input in the textbox.
		/// </summary>
		protected abstract void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e);

		/// <summary>
		/// Redraws the labels, using the given strings.
		/// </summary>
		/// <param name="options"></param>
		protected void RedrawLabels(IEnumerable<string> options)
		{
			var sizeTest = CreateLabel("size-test");
			sizeTest.Measure(new Size(double.MaxValue, double.MaxValue));
			var labelHeight = sizeTest.DesiredSize.Height;

			var labelCount = Height / labelHeight;

			ClearLabels();
			var first = true;
			foreach (var option in options.Take((int)labelCount))
			{
				var label = CreateLabel(option);
				AddLabel(label);
				if (first)
				{
					first = false;
					Select(label);
				}
			}
		}

		/// <summary>
		/// Selects a label; deselecting the previously selected label.
		/// </summary>
		/// <param name="label">The label to be selected. If this value is null, the selected label will not be changed.</param>
		protected void Select(Label label)
		{
			if (label == null) return;

			if (Selected != null)
			{
				Selected.Background = Helpers.BrushFromColourString(styleConfig.Options.BackgroundColour);
				Selected.Foreground = Helpers.BrushFromColourString(styleConfig.Options.TextColour);
				Selected.BorderThickness = new Thickness(styleConfig.Options.BorderWidth);
			}
			Selected = label;
			Selected.Background = Helpers.BrushFromColourString(styleConfig.Selection.BackgroundColour);
			Selected.Foreground = Helpers.BrushFromColourString(styleConfig.Selection.TextColour);
			Selected.BorderThickness = new Thickness(styleConfig.Selection.BorderWidth);
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
				FontSize = styleConfig.FontSize,
				FontFamily = new FontFamily(styleConfig.FontFamily),
				Background = Helpers.BrushFromColourString(styleConfig.Options.BackgroundColour),
				Foreground = Helpers.BrushFromColourString(styleConfig.Options.TextColour),
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

		protected bool IsPressed(HotkeyConfig hotkey)
		{
			var combination = KeyCombination.Parse(hotkey.Hotkey);

			if (combination.Key != Key.None)
			{
				if (!Keyboard.IsKeyDown(combination.Key)) return false;
			}
			return (Keyboard.Modifiers == combination.ModifierKeys);
		}

		private void SelectNext()
		{
			var selectionIndex = Options.IndexOf(Selected);
			if (selectionIndex < Options.Count - 1)
			{
				var label = FindNext(selectionIndex);
				Select(label);
				HandleSelectionChange(label);
			}
		}

		private void SelectPrevious()
		{
			var selectionIndex = Options.IndexOf(Selected);
			if (selectionIndex > 0)
			{
				var label = FindPrevious(selectionIndex);
				Select(label);
				HandleSelectionChange(label);
			}
		}

		private void SelectFirst()
		{
			var label = Options.First();
			Select(label);
			HandleSelectionChange(label);
		}

		private void SelectLast()
		{
			var label = Options.Last();
			Select(label);
			HandleSelectionChange(label);
		}

		private void SearchBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
		{
			var matches = ConfigManager.Config.Interface.Hotkeys.Where(IsPressed).ToList();

			// Prefer manually defined shortcuts over default shortcuts.
			if (matches.Any())
			{
				e.Handled = true;
				foreach (var match in matches)
				{
					switch (match.Action)
					{
						case HotkeyAction.SelectNext:
							SelectNext();
							break;
						case HotkeyAction.SelectPrevious:
							SelectPrevious();
							break;
						case HotkeyAction.SelectFirst:
							break;
						case HotkeyAction.SelectLast:
							break;
					}
				}
				return;
			}

			switch (e.Key)
			{
				case Key.Left:
				case Key.Up:
					e.Handled = true;
					SelectPrevious();
					break;
				case Key.Right:
				case Key.Down:
					e.Handled = true;
					SelectNext();
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

		private void WrapPanel_OnMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (e.Delta > 0)
			{
				SelectPrevious();
			}
			else if (e.Delta < 0)
			{
				SelectNext();
			}
		}
	}
}
