namespace PassWinmenu.WinApi
{
	internal class SystemEnvironment : IEnvironment
	{
		public string GetEnvironmentVariable(string variableName)
		{
			return System.Environment.GetEnvironmentVariable(variableName);
		}
	}
}
