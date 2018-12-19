using System.Collections.Generic;
using System.IO;

namespace PassWinmenu.PasswordManagement
{
	internal interface IPasswordManager
	{
		DirectoryInfo PasswordStore { get; }
		IEnumerable<PasswordFile> GetPasswordFiles(string pattern);

		KeyedPasswordFile DecryptPassword(PasswordFile file, bool passwordOnFirstLine);

		PasswordFile EncryptPassword(DecryptedPasswordFile file, bool overwrite);
	}
}
