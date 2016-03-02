using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PassWinmenu
{
	public static class EmbeddedResources
	{
		public static Icon Icon => new Icon(Assembly.GetEntryAssembly().GetManifestResourceStream("PassWinmenu.embedded.pass-winmenu.ico"));
		public static Stream DefaultConfig => Assembly.GetExecutingAssembly().GetManifestResourceStream("PassWinmenu.embedded.default-config.yaml");
	}
}