using System;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Moq;
using PassWinmenu.PasswordManagement;
using PassWinmenuTests.Utilities;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.PasswordManagement
{
	public class PasswordManagerTests
	{
		private string passwordStorePath = @"C:\password-store";

		[Fact]
		public void GetPasswordFiles_ReturnsFilesInPasswordDirectory()
		{
			var fileSystem = new MockFileSystemBuilder()
				.WithFile(@"C:\password-store\password_1", "password_1_content")
				.WithFile(@"C:\password-store\sub\password_2", "password_2_content")
				.WithFile(@"C:\password-store\password_3", "password_3_content")
				.WithFile(@"C:\other\password_4", "password_4_content")
				.Build();
			var passwordDirectory = new MockDirectoryInfo(fileSystem, passwordStorePath);
			var passwordManager = new PasswordManager(passwordDirectory, new FakeCryptoService(fileSystem), Mock.Of<IRecipientFinder>());

			var files = passwordManager.GetPasswordFiles(".*").ToList();

			files.ShouldSatisfyAllConditions(
				() => files.Count.ShouldBe(3),
				() => files[0].FileNameWithoutExtension.ShouldBe("password_1"),
				() => files[1].FileNameWithoutExtension.ShouldBe("password_2"),
				() => files[2].FileNameWithoutExtension.ShouldBe("password_3")
			);
		}

		[Fact]
		public void GetPasswordFiles_WithPattern_ReturnsMatchingFilesOnly()
		{
			var fileSystem = new MockFileSystemBuilder()
				.WithFile(@"C:\password-store\password_1", "password_1_content")
				.WithFile(@"C:\password-store\password_2", "password_2_content")
				.WithFile(@"C:\password-store\sub\password_3", "password_3_content")
				.WithFile(@"C:\password-store\sub\not_a_password", "not_a_password")
				.Build();
			var passwordDirectory = new MockDirectoryInfo(fileSystem, passwordStorePath);
			var passwordManager = new PasswordManager(passwordDirectory, new FakeCryptoService(fileSystem), Mock.Of<IRecipientFinder>());

			var files = passwordManager.GetPasswordFiles("password_.*").ToList();

			files.ShouldSatisfyAllConditions(
				() => files.Count.ShouldBe(3),
				() => files[0].FileNameWithoutExtension.ShouldBe("password_1"),
				() => files[1].FileNameWithoutExtension.ShouldBe("password_2"),
				() => files[2].FileNameWithoutExtension.ShouldBe("password_3")
			);
		}

		[Fact]
		public void EncryptPassword_EncryptsPasswordAtSpecifiedLocation()
		{
			var fileSystem = new MockFileSystemBuilder()
				.WithFile(@"C:\password-store\password_1", "password_1_content")
				.Build();
			var passwordDirectory = new MockDirectoryInfo(fileSystem, passwordStorePath);
			var passwordManager = new PasswordManager(passwordDirectory, new FakeCryptoService(fileSystem), Mock.Of<IRecipientFinder>());
			var newPassword = CreateDecryptedPassword(fileSystem, @"C:\password-store\new_password", "new_content");

			passwordManager.EncryptPassword(newPassword);

			fileSystem.File.ReadAllText(@"C:\password-store\new_password").ShouldBe("new_content");
		}

		[Fact]
		public void EncryptPassword_WithExistingFile_OverwritesOriginalFile()
		{
			var fileSystem = new MockFileSystemBuilder()
				.WithFile(@"C:\password-store\password_1", "password_1_content")
				.Build();
			var passwordDirectory = new MockDirectoryInfo(fileSystem, passwordStorePath);
			var passwordManager = new PasswordManager(passwordDirectory, new FakeCryptoService(fileSystem), Mock.Of<IRecipientFinder>());
			var newPassword = CreateDecryptedPassword(fileSystem, @"C:\password-store\password_1", "new_content");

			passwordManager.EncryptPassword(newPassword);

			fileSystem.File.ReadAllText(@"C:\password-store\password_1").ShouldBe("new_content");
		}

		[Fact]
		public void EncryptPassword_ReturnsSamePasswordFileAsProvided()
		{
			var fileSystem = new MockFileSystemBuilder()
				.WithFile(@"C:\password-store\password_1", "password_1_content")
				.Build();
			var passwordDirectory = new MockDirectoryInfo(fileSystem, passwordStorePath);
			var passwordManager = new PasswordManager(passwordDirectory, new FakeCryptoService(fileSystem), Mock.Of<IRecipientFinder>());
			var newPassword = CreateDecryptedPassword(fileSystem, @"C:\password-store\new_password", "password_content");

			var encrypted = passwordManager.EncryptPassword(newPassword);

			encrypted.ShouldSatisfyAllConditions(
				() => encrypted.FileNameWithoutExtension.ShouldBe(newPassword.FileNameWithoutExtension),
				() => encrypted.FullPath.ShouldBe(newPassword.FullPath),
				() => encrypted.PasswordStore.FullName.ShouldBe(newPassword.PasswordStore.FullName)
			);
		}

		[Fact]
		public void AddPassword_AbsolutePath_AddsPasswordAtSpecifiedLocation()
		{
			var fileSystem = new MockFileSystemBuilder()
				.WithFile(@"C:\password-store\password_1", "password_1_content")
				.Build();
			var passwordDirectory = new MockDirectoryInfo(fileSystem, passwordStorePath);
			var passwordManager = new PasswordManager(passwordDirectory, new FakeCryptoService(fileSystem), Mock.Of<IRecipientFinder>());

			passwordManager.AddPassword(@"C:\password-store\new_password", "new_content", null);

			fileSystem.File.ReadAllText(@"C:\password-store\new_password").ShouldBe("new_content");
		}

		[Fact]
		public void AddPassword_RelativePath_AddsPasswordAtLocationRelativeToPasswordStore()
		{
			var fileSystem = new MockFileSystemBuilder()
				.WithFile(@"C:\password-store\password_1", "password_1_content")
				.Build();
			var passwordDirectory = new MockDirectoryInfo(fileSystem, passwordStorePath);
			var passwordManager = new PasswordManager(passwordDirectory, new FakeCryptoService(fileSystem), Mock.Of<IRecipientFinder>());

			passwordManager.AddPassword(@"sub\new_password", "new_content", null);

			fileSystem.File.ReadAllText(@"C:\password-store\sub\new_password").ShouldBe("new_content");
		}

		[Fact]
		public void AddPassword_WithExistingFile_ThrowsInvalidOperationException()
		{
			var fileSystem = new MockFileSystemBuilder()
				.WithFile(@"C:\password-store\password_1", "password_1_content")
				.Build();
			var passwordDirectory = new MockDirectoryInfo(fileSystem, passwordStorePath);
			var passwordManager = new PasswordManager(passwordDirectory, new FakeCryptoService(fileSystem), Mock.Of<IRecipientFinder>());

			Should.Throw<InvalidOperationException>(() => passwordManager.AddPassword(@"C:\password-store\password_1", "new_content", null));
		}

		private DecryptedPasswordFile CreateDecryptedPassword(MockFileSystem fileSystem, string path, string content)
		{
			return new DecryptedPasswordFile(new PasswordFile(
				fileSystem.FileInfo.FromFileName(path),
				fileSystem.DirectoryInfo.FromDirectoryName(@"C:\password-store")
			), content);
		}
	}
}
