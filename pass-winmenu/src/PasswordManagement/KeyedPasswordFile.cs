using System.Collections.Generic;
using System.IO;
using System.Windows.Documents;

namespace PassWinmenu.PasswordManagement
{
	/// <summary>
	/// Represents a decrypted password file of the 'password store' format
	/// which additionally stores a number of key-value pairs in its metadata.
	/// </summary>
	internal class KeyedPasswordFile : ParsedPasswordFile
	{
		public List<KeyValuePair<string, string>> Keys { get; }

		public KeyedPasswordFile(DirectoryInfo passwordStore, string path, string password, string metadata, List<KeyValuePair<string,string>> keys) : base(passwordStore, path, password, metadata)
		{
			// The keys list should always be initialised.
			Keys = keys ?? new List<KeyValuePair<string, string>>();
		}
	}

	/// <summary>
	/// Represents a decrypted password file of the 'password store' format,
	/// with a password on the first line, and metadata on the other lines.
	/// </summary>
	internal class ParsedPasswordFile : DecryptedPasswordFile
	{
		public string Password { get; }
		public string Metadata { get; }

		public ParsedPasswordFile(DirectoryInfo passwordStore, string path, string password, string metadata) : base(passwordStore, path)
		{
			Password = password;
			Metadata = metadata;

			if (string.IsNullOrEmpty(metadata))
			{
				// TODO: do we want to include a newline here?
				Content = password;
			}
			else
			{
				Content = $"{Password}\n{Metadata}";
			}
		}
	}

	/// <summary>
	/// Represents a decrypted password file of an unknown format.
	/// </summary>
	internal class DecryptedPasswordFile : PasswordFile
	{
		// Allow derived types to set the content.
		public string Content { get; protected set; }

		// Only derived types may call this constructor, since they may want to determine the content themselves.
		protected DecryptedPasswordFile(DirectoryInfo passwordStore, string path): base(passwordStore, path) { }

		public DecryptedPasswordFile(DirectoryInfo passwordStore, string path, string content) : base(passwordStore, path)
		{
			Content = content;
		}

	}
}
