using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PassWinmenu.Configuration;

namespace PassWinmenu.Windows
{
	class FileSelectionWindow : MainWindow
	{
		private DirectoryAutocomplete autocomplete;
		private string baseDirectory;

		public FileSelectionWindow(string baseDirectory, MainWindowConfiguration configuration) : base(configuration)
		{
			this.baseDirectory = baseDirectory;
			autocomplete = new DirectoryAutocomplete(baseDirectory);
			var completions = autocomplete.GetCompletionList("");
			RedrawLabels(completions);
		}

		private void RedrawLabels(IEnumerable<string> options)
		{
			ClearLabels();
			bool first = true;
			foreach (var option in options)
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

		protected override void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			var completions = autocomplete.GetCompletionList(SearchBox.Text).ToList();
			if (!string.IsNullOrWhiteSpace(SearchBox.Text)
				&& !SearchBox.Text.EndsWith("/")
				&& !completions.Contains(SearchBox.Text))
			{
				completions.Insert(0, SearchBox.Text);
			}
			RedrawLabels(completions);
		}

		protected override void HandleEnterKey()
		{
			// If enter is pressed on a suggestion, put that suggestion in the searchbox.
			if (Options.IndexOf(Selected) > 0)
			{
				var selection = GetSelection();
				if (selection.EndsWith(".gpg"))
				{
					selection = selection.Substring(0, selection.Length - 4);
				}
				SetSearchBoxText(selection);
			}
			else
			{
				if (SearchBox.Text.EndsWith("/"))
				{
					return;
				}
				var selection = GetSelection();
				if (selection.EndsWith(".gpg"))
				{
					MessageBox.Show("A .gpg extension will be added automatically and does not need to be entered here.");
					selection = selection.Substring(0, selection.Length - 4);
					SetSearchBoxText(selection);
					return;
				}
				if (File.Exists(Path.Combine(baseDirectory, selection + ".gpg")))
				{
					MessageBox.Show($"The password file \"{selection + ".gpg"}\" already exists.");
					return;
				}
				Success = true;
				Close();
			}
		}
	}
}
