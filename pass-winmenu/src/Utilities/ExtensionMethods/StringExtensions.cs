using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PassWinmenu.Utilities.ExtensionMethods
{
	internal static class StringExtensions
	{
		private static string ToCamelOrPascalCase(string str, Func<char, char> firstLetterTransform)
		{
			if (string.IsNullOrWhiteSpace(str)) return str;
			var input = str;
			var pattern = "([_\\-])(?<char>[a-z])";
			var num = 1;
			var str1 = Regex.Replace(input, pattern, (MatchEvaluator)(match => match.Groups["char"].Value.ToUpperInvariant()), (RegexOptions)num);
			return firstLetterTransform(str1[0]).ToString() + str1.Substring(1);
		}

		/// <summary>
		/// Convert the string with underscores (this_is_a_test) or hyphens (this-is-a-test) to
		/// camel case (thisIsATest). Camel case is the same as Pascal case, except the first letter
		/// is lowercase.
		/// </summary>
		/// <param name="str">String to convert</param>
		/// <returns>
		/// Converted string
		/// </returns>
		public static string ToCamelCase(this string str)
		{
			return ToCamelOrPascalCase(str, char.ToLowerInvariant);
		}

		/// <summary>
		/// Convert the string with underscores (this_is_a_test) or hyphens (this-is-a-test) to
		/// pascal case (ThisIsATest). Pascal case is the same as camel case, except the first letter
		/// is uppercase.
		/// </summary>
		/// <param name="str">String to convert</param>
		/// <returns>
		/// Converted string
		/// </returns>
		public static string ToPascalCase(this string str)
		{
			return ToCamelOrPascalCase(str, char.ToUpperInvariant);
		}

		/// <summary>
		/// Convert the string from camelcase (thisIsATest) to a hyphenated (this-is-a-test) or
		/// underscored (this_is_a_test) string
		/// </summary>
		/// <param name="str">String to convert</param><param name="separator">Separator to use between segments</param>
		/// <returns>
		/// Converted string
		/// </returns>
		public static string FromCamelCase(this string str, string separator)
		{
			str = char.ToLower(str[0]).ToString() + str.Substring(1);
			str = Regex.Replace(ToCamelCase(str), "(?<char>[A-Z])", match => separator + match.Groups["char"].Value.ToLowerInvariant());
			return str;
		}

		/// <summary>
		/// Extracts the Unicode code points from a string.
		/// </summary>
		/// <param name="str">The string from which the code points should be extracted.</param>
		/// <returns>An integer array representing the discovered code points.</returns>
		public static int[] ToCodePoints(this string str)
		{
			if (str == null) throw new ArgumentNullException(nameof(str));

			if (!str.IsNormalized())
			{
				str = str.Normalize();
			}

			var codePoints = new List<int>();
			for (var i = 0; i < str.Length; i++)
			{
				codePoints.Add(char.ConvertToUtf32(str, i));
				if (char.IsHighSurrogate(str[i]))
					i += 1;
			}

			return codePoints.ToArray();
		}
	}
}
