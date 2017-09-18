using System;
using System.Security.Cryptography;

namespace PassWinmenu.PasswordGeneration
{
	internal class PasswordGenerator : IDisposable
	{
		public PasswordGenerationOptions Options { get; }

		private readonly RNGCryptoServiceProvider csprng = new RNGCryptoServiceProvider();
		private int bufferIndex;
		private readonly byte[] buffer = new byte[1024];

		public PasswordGenerator() : this(new PasswordGenerationOptions()) { }

		public PasswordGenerator(PasswordGenerationOptions options)
		{
			Options = options;
			// Initialise the buffer with random bytes.
			csprng.GetNonZeroBytes(buffer);
		}

		public string GeneratePassword(int length = 20)
		{
			if (!Options.AllowSymbols && !Options.AllowNumbers && !Options.AllowLower && !Options.AllowUpper) return null;

			var chars = new char[length];

			for (var i = 0; i < length;)
			{
				var ch = NextCharacter(32, 127);
				if (((char.IsSymbol(ch) || char.IsPunctuation(ch)) && Options.AllowSymbols)
					|| (char.IsNumber(ch) && Options.AllowNumbers)
					|| (char.IsLower(ch) && Options.AllowLower)
					|| (char.IsUpper(ch) && Options.AllowUpper)
					|| (char.IsWhiteSpace(ch) && Options.AllowWhitespace))
				{
					chars[i++] = ch;
				}
			}
			return new string(chars);
		}

		/// <summary>
		/// Advances the index to the next position in the buffer,
		/// regenerating a new buffer if the end is reached.
		/// </summary>
		private void MoveNext()
		{
			if (++bufferIndex >= buffer.Length)
			{
				csprng.GetNonZeroBytes(buffer);
				bufferIndex = 0;
			}
		}

		/// <summary>
		/// Returns the next suitable random character from the buffer.
		/// </summary>
		private char NextCharacter(byte min, byte max)
		{
			if (min >= max) throw new ArgumentException($"'{nameof(min)}' cannot be greater than {nameof(max)}");

			// Search the buffer for the first byte within the given bounds.
			while (buffer[bufferIndex] < min || buffer[bufferIndex] >= max)
			{
				MoveNext();
			}
			// We've found a byte matching our requirements;
			// return it and advance the buffer index.
			var found = (char)buffer[bufferIndex];
			MoveNext();
			return found;
		}

		public void Dispose()
		{
			csprng.Dispose();
		}
	}
}
