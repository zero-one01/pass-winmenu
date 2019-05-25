using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PassWinmenu.Configuration;
using PassWinmenu.Utilities;
using Xunit;

namespace PassWinmenu.PasswordGeneration
{
	public class PasswordGeneratorTests
	{
		private const string Category = "Core: Password Generation";

		[Fact, TestCategory(Category)]
		public void GeneratePassword_MatchesRequiredLength()
		{
			var options = new PasswordGenerationConfig
			{
				Length = 32
			};
			var generator = new PasswordGenerator(options);

			var password = generator.GeneratePassword();

			Assert.Equal(32, password.Length);
		}

		[Theory, TestCategory(Category)]
		[InlineData("0123456789")]
		[InlineData("abcABC")]
		[InlineData("1")]
		public void GeneratePassword_OnlyContainsAllowedCharacters(string allowedCharacters)
		{
			var allowedSuperset = new HashSet<char>(allowedCharacters.Distinct());
			var options = new PasswordGenerationConfig
			{
				CharacterGroups = new []
				{
					new CharacterGroupConfig("test", allowedCharacters, true), 
				}
			};
			var generator = new PasswordGenerator(options);

			var password = generator.GeneratePassword();
			var passwordSet = new HashSet<char>(password);

			Assert.Subset(allowedSuperset, passwordSet);
		}
	}
}
