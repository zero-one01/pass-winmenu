using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PassWinmenu.PasswordManagement;

namespace PassWinmenu.Tests
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
			var parsed = p.Parse(new PasswordFile(""), text, false);

			Assert.AreEqual(parsed.Password, string.Empty);
			Assert.AreEqual(parsed.Metadata, string.Empty);
		}

		[TestMethod, TestCategory(Category)]
		public void Test_LineEndings_Metadata()
		{
			var crlf = "password\r\nmeta-data";
			var cr= "password\rmeta-data";
			var lf = "password\nmeta-data";

			var p = new PasswordFileParser();

			var parsedCrlf = p.Parse(new PasswordFile(""), crlf, false);
			Assert.AreEqual(parsedCrlf.Password, "password");
			Assert.AreEqual(parsedCrlf.Metadata, "meta-data");

			var parsedCr = p.Parse(new PasswordFile(""), cr, false);
			Assert.AreEqual(parsedCr.Password, "password");
			Assert.AreEqual(parsedCr.Metadata, "meta-data");

			var parsedLf = p.Parse(new PasswordFile(""), lf, false);
			Assert.AreEqual(parsedLf.Password, "password");
			Assert.AreEqual(parsedLf.Metadata, "meta-data");
		}

		[TestMethod, TestCategory(Category)]
		public void Test_LineEndings_PasswordOnly()
		{

			var crlf = "password\r\n";
			var cr = "password\r";
			var lf = "password\n";
			var none = "password";

			var p = new PasswordFileParser();

			var parsedCrlf = p.Parse(new PasswordFile(""), crlf, false);
			Assert.AreEqual(parsedCrlf.Password, "password");
			Assert.AreEqual(parsedCrlf.Metadata, string.Empty);

			var parsedCr = p.Parse(new PasswordFile(""), cr, false);
			Assert.AreEqual(parsedCr.Password, "password");
			Assert.AreEqual(parsedCr.Metadata, string.Empty);

			var parsedLf = p.Parse(new PasswordFile(""), lf, false);
			Assert.AreEqual(parsedLf.Password, "password");
			Assert.AreEqual(parsedLf.Metadata, string.Empty);

			var parsedNone = p.Parse(new PasswordFile(""), none, false);
			Assert.AreEqual(parsedNone.Password, "password");
			Assert.AreEqual(parsedNone.Metadata, string.Empty);
		}
	}
}
