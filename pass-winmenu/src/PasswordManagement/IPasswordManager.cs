using System.Collections.Generic;

namespace PassWinmenu.PasswordManagement
{
	internal interface IPasswordManager
	{
		string DecryptFile(string path);
		DecryptedPasswordFile DecryptPassword(string path, bool passwordOnFirstLine);
		string DecryptText(string path);
		PasswordFile EncryptFile(string file);
		void EncryptPassword(DecryptedPasswordFile file);
		void EncryptText(string text, string path);
		string GetPasswordFilePath(string relativePath);
		IEnumerable<string> GetPasswordFiles(string pattern);
	}
}