using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PassWinmenu.Windows
{
	/// <summary>
	/// Interaction logic for PathWindow.xaml
	/// </summary>
	public partial class PasswordWindow : Window
	{
		public PasswordWindow()
		{
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			InitializeComponent();

			Password.Text = GeneratePassword();
		}

		private string GeneratePassword(int length = 20)
		{
			var chars = new char[length];
			var rand = new Random();
			for (int i = 0; i < length; i++)
			{
				chars[i] = (char)rand.Next(32, 127);
			}
			return new string(chars);
		}

		private void Btn_Generate_Click(object sender, RoutedEventArgs e)
		{
			Password.Text = GeneratePassword();
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
	}
}
