using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PassWinmenu.WinApi
{
	public interface IExecutablePathResolver
	{
		string Resolve(string executable);
	}
}
