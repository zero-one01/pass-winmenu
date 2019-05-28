using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PassWinmenu.ExternalPrograms.Gpg;

namespace PassWinmenu.Utilities
{
	class FakeGpgAgentConfigReader : IGpgAgentConfigReader
	{
		private string[] currentLines;

		public FakeGpgAgentConfigReader(string[] startLines)
		{
			currentLines = startLines;
		}

		public string[] ReadConfigLines()
		{
			return currentLines;
		}

		public void WriteConfigLines(string[] lines)
		{
			currentLines = lines;
		}
	}
}
