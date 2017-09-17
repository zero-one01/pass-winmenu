using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassWinmenu.PasswordGeneration
{
	class PasswordGenerationOptions
	{
		public bool AllowSymbols { get; set; }
		public bool AllowNumbers { get; set; }
		public bool AllowLower { get; set; }
		public bool AllowUpper { get; set; }
		public bool AllowWhitespace { get; set; }
	}
}
