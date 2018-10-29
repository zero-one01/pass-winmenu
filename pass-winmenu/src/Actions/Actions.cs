using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassWinmenu.Actions
{
	class Actions
	{
		public EditAction Edit { get; }

		public void Run(IAction action)
		{
			action.Run();
		}
	}
}
