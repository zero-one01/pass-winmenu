using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PassWinmenu.ExternalPrograms.Gpg;
using Shouldly;
using Xunit;

namespace PassWinmenu.ExternalPrograms
{
	public class GpgResultTests
	{
		[Fact]
		public void GenerateError_IncludesStderrMessages()
		{
			var stdErrMessages = new[] {"message_1", "message_2"};
			var result = new GpgResult(0, null, new StatusMessage[0], stdErrMessages);

			try
			{
				result.GenerateError();
			}
			catch (GpgException e)
			{
				e.ShouldSatisfyAllConditions(
					() => e.Message.ShouldContain(stdErrMessages[0]),
					() => e.Message.ShouldContain(stdErrMessages[1])
				);
			}
		}
	}
}
