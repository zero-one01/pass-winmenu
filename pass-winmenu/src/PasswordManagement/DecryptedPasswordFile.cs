using System.Collections.Generic;
using System.Windows.Documents;

namespace PassWinmenu.PasswordManagement
{
	internal class DecryptedPasswordFile : PasswordFile
	{
		public string Content => $"{Password}\n{Metadata}";

		public string Password { get; }
		public string Metadata { get; }
		public List<KeyValuePair<string, string>> Keys { get; }

		public DecryptedPasswordFile(string relativePath, string password, string metaData, List<KeyValuePair<string,string>> keys = null) : base(relativePath)
		{
			Password = password;
			Metadata = metaData;
			Keys = keys;
		}
	}
}
