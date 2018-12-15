namespace PassWinmenu.ExternalPrograms
{
	internal interface ICryptoService
	{
		string Decrypt(string file);
		void DecryptToFile(string encryptedFile, string outputFile);
		void Encrypt(string data, string outputFile, params string[] recipients);
		void EncryptFile(string inputFile, string outputFile, params string[] recipients);
	}
}
