namespace PassWinmenu
{
	internal interface INotificationService
	{
		void Raise(string message, Severity level);
		void ShowErrorWindow(string message, string title = "An error occurred.");
	}
}