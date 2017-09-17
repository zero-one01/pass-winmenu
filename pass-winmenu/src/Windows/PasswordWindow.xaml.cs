using System;
using System.Windows;
using System.Windows.Input;
using PassWinmenu.Configuration;
using PassWinmenu.PasswordGeneration;

namespace PassWinmenu.Windows
{
	/// <summary>
	/// Interaction logic for PathWindow.xaml
	/// </summary>
	public partial class PasswordWindow
	{
		private PasswordGenerator passwordGenerator = new PasswordGenerator();

		public PasswordWindow(string filename)
		{
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			InitializeComponent();

			// We'll keep supporting DefaultUsername for now, but eventually it'll have to go.
			var defaultUsername = ConfigManager.Config.PasswordGeneration.DefaultUsername;
			if (defaultUsername == null)
			{
				var now = DateTime.Now;
				var extraContent = ConfigManager.Config.PasswordGeneration.DefaultContent
					.Replace("$filename", filename)
					.Replace("$date", now.ToString("yyyy-MM-dd"))
					.Replace("$time", now.ToString("HH:mm:ss"));

				ExtraContent.Text = extraContent;
			}
			else
			{
				var username = defaultUsername == "$filename" ? filename : defaultUsername;
				ExtraContent.Text = $"Username: {username}\n";
			}


			RegeneratePassword();
			Password.Focus();
		}

		private void RegeneratePassword()
		{
			Password.Text = passwordGenerator.GeneratePassword();
			Password.CaretIndex = Password.Text.Length;
		}

		private void Btn_Generate_Click(object sender, RoutedEventArgs e)
		{
			Password.Text = passwordGenerator.GeneratePassword();
		}

		private void Btn_OK_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void Window_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				DialogResult = false;
				Close();
			}
		}

		private void HandleCheckedChanged(object sender, RoutedEventArgs e)
		{
			passwordGenerator.Options.AllowSymbols = Cbx_Symbols?.IsChecked ?? false;
			passwordGenerator.Options.AllowNumbers = Cbx_Numbers?.IsChecked ?? false;
			passwordGenerator.Options.AllowLower = Cbx_Lower?.IsChecked ?? false;
			passwordGenerator.Options.AllowUpper = Cbx_Upper?.IsChecked ?? false;
			passwordGenerator.Options.AllowWhitespace = Cbx_Whitespace?.IsChecked ?? false;

			RegeneratePassword();
		}
	}
}
