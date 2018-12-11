namespace PassWinmenu.PasswordManagement
{
	internal class DecryptedPasswordFile : PasswordFile
	{
		public string Content => $"{Password}\n{Metadata}";

		public string Password { get; }
		public string Metadata { get; }

		public DecryptedPasswordFile(string relativePath, string password, string metaData) : base(relativePath)
		{
			Password = password;
			Metadata = metaData;
		}
	}
}
