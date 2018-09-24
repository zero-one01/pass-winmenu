using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PassWinmenu.Parsing
{
	[TestClass]
	public class ParserTests
	{
		private const string Category = "Core: Password File Parsing";

		[TestMethod, TestCategory(Category)]
		public void Test_EmptyFile()
		{
			var text = "";
			var p = new PasswordFileParser();
			var parsed = p.Parse(text, false);

			Assert.AreEqual(parsed.Password, string.Empty);
			Assert.AreEqual(parsed.ExtraContent, string.Empty);
		}

		[TestMethod, TestCategory(Category)]
		public void Test_LineEndings_Metadata()
		{
			var crlf = "password\r\nmeta-data";
			var cr= "password\rmeta-data";
			var lf = "password\nmeta-data";

			var p = new PasswordFileParser();

			var parsedCrlf = p.Parse(crlf, false);
			Assert.AreEqual(parsedCrlf.Password, "password");
			Assert.AreEqual(parsedCrlf.ExtraContent, "meta-data");

			var parsedCr = p.Parse(crlf, false);
			Assert.AreEqual(parsedCr.Password, "password");
			Assert.AreEqual(parsedCr.ExtraContent, "meta-data");

			var parsedLf = p.Parse(crlf, false);
			Assert.AreEqual(parsedLf.Password, "password");
			Assert.AreEqual(parsedLf.ExtraContent, "meta-data");
		}

		[TestMethod, TestCategory(Category)]
		public void Test_LineEndings_PasswordOnly()
		{

			var crlf = "password\r\n";
			var cr = "password\r";
			var lf = "password\n";
			var none = "password";

			var p = new PasswordFileParser();

			var parsedCrlf = p.Parse(crlf, false);
			Assert.AreEqual(parsedCrlf.Password, "password");
			Assert.AreEqual(parsedCrlf.ExtraContent, string.Empty);

			var parsedCr = p.Parse(crlf, false);
			Assert.AreEqual(parsedCr.Password, "password");
			Assert.AreEqual(parsedCr.ExtraContent, string.Empty);

			var parsedLf = p.Parse(crlf, false);
			Assert.AreEqual(parsedLf.Password, "password");
			Assert.AreEqual(parsedLf.ExtraContent, string.Empty);
		}
	}
}
