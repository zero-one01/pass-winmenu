using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PassWinmenu.PasswordManagement;

namespace PassWinmenu.Tests
{
	[TestClass]
	public class ParserTests
	{
		private const string Category = "Core: Password File Parsing";

		private readonly PasswordFile dummyFile = new PasswordFile(new DirectoryInfo("\\password-store"), "dummy-password");

		[TestMethod, TestCategory(Category)]
		public void Test_EmptyFile()
		{
			var text = "";
			var p = new PasswordFileParser();
			var parsed = p.Parse(dummyFile, text, false);

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

			var parsedCrlf = p.Parse(dummyFile, crlf, false);
			Assert.AreEqual(parsedCrlf.Password, "password");
			Assert.AreEqual(parsedCrlf.Metadata, "meta-data");

			var parsedCr = p.Parse(dummyFile, cr, false);
			Assert.AreEqual(parsedCr.Password, "password");
			Assert.AreEqual(parsedCr.Metadata, "meta-data");

			var parsedLf = p.Parse(dummyFile, lf, false);
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

			var parsedCrlf = p.Parse(dummyFile, crlf, false);
			Assert.AreEqual(parsedCrlf.Password, "password");
			Assert.AreEqual(parsedCrlf.Metadata, string.Empty);

			var parsedCr = p.Parse(dummyFile, cr, false);
			Assert.AreEqual(parsedCr.Password, "password");
			Assert.AreEqual(parsedCr.Metadata, string.Empty);

			var parsedLf = p.Parse(dummyFile, lf, false);
			Assert.AreEqual(parsedLf.Password, "password");
			Assert.AreEqual(parsedLf.Metadata, string.Empty);

			var parsedNone = p.Parse(dummyFile, none, false);
			Assert.AreEqual(parsedNone.Password, "password");
			Assert.AreEqual(parsedNone.Metadata, string.Empty);
		}

		[TestMethod, TestCategory(Category)]
		public void Test_Metadata_LineEndings()
		{
			const string crlf = "password\r\n" +
			                    "Username: user\r\n" +
			                    "Key: value";
			const string cr = "password\r" +
			                  "Username: user\r" +
			                  "Key: value";
			const string lf = "password\n" +
			                  "Username: user\n" +
			                  "Key: value";
			const string mixed = "password\r\n" +
			                     "Username: user\n" +
			                     "Key: value\r";

			var p = new PasswordFileParser();

			var parsedCrlf = p.Parse(dummyFile, crlf, false);
			Assert.IsTrue(parsedCrlf.Keys[0].Key == "Username");
			Assert.IsTrue(parsedCrlf.Keys[0].Value == "user");
			Assert.IsTrue(parsedCrlf.Keys[1].Key == "Key");
			Assert.IsTrue(parsedCrlf.Keys[1].Value == "value");

			var parsedCr = p.Parse(dummyFile, cr, false);
			Assert.IsTrue(parsedCr.Keys[0].Key == "Username");
			Assert.IsTrue(parsedCr.Keys[0].Value == "user");
			Assert.IsTrue(parsedCr.Keys[1].Key == "Key");
			Assert.IsTrue(parsedCr.Keys[1].Value == "value");

			var parsedLf = p.Parse(dummyFile, lf, false);
			Assert.IsTrue(parsedLf.Keys[0].Key == "Username");
			Assert.IsTrue(parsedLf.Keys[0].Value == "user");
			Assert.IsTrue(parsedLf.Keys[1].Key == "Key");
			Assert.IsTrue(parsedLf.Keys[1].Value == "value");

			var parsedMixed = p.Parse(dummyFile, mixed, false);
			Assert.IsTrue(parsedMixed.Keys[0].Key == "Username");
			Assert.IsTrue(parsedMixed.Keys[0].Value == "user");
			Assert.IsTrue(parsedMixed.Keys[1].Key == "Key");
			Assert.IsTrue(parsedMixed.Keys[1].Value == "value");

		}

		[TestMethod, TestCategory(Category)]
		public void Test_Metadata_KeyFormat()
		{
			var duplicate = "password\r\n" +
			                "Username: user\r\n" +
			                "With-Dash: value\r\n" +
			                "_WithUnderline: value2\r\n";

			var p = new PasswordFileParser();
			var parsed = p.Parse(dummyFile, duplicate, false);

			Assert.IsTrue(parsed.Keys[0].Key == "Username");
			Assert.IsTrue(parsed.Keys[0].Value == "user");
			Assert.IsTrue(parsed.Keys[1].Key == "With-Dash");
			Assert.IsTrue(parsed.Keys[1].Value == "value");
			Assert.IsTrue(parsed.Keys[2].Key == "_WithUnderline");
			Assert.IsTrue(parsed.Keys[2].Value == "value2");
		}

		[TestMethod, TestCategory(Category)]
		public void Test_Metadata_Multiple_Keys()
		{
			var duplicate = "password\r\n" +
						  "Username: user\r\n" +
			              "Duplicate: value1\r\n" +
			              "Duplicate: value2\r\n" +
			              "Duplicate: value3\r\n";

			var p = new PasswordFileParser();
			var parsed = p.Parse(dummyFile, duplicate, false);

			Assert.IsTrue(parsed.Keys[0].Key == "Username");
			Assert.IsTrue(parsed.Keys[0].Value == "user");
			Assert.IsTrue(parsed.Keys[1].Key == "Duplicate");
			Assert.IsTrue(parsed.Keys[1].Value == "value1");
			Assert.IsTrue(parsed.Keys[2].Key == "Duplicate");
			Assert.IsTrue(parsed.Keys[2].Value == "value2");
			Assert.IsTrue(parsed.Keys[3].Key == "Duplicate");
			Assert.IsTrue(parsed.Keys[3].Value == "value3");
		}
	}
}
