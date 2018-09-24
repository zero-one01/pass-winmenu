using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassWinmenu.Actions
{
	internal class DecryptAction : IAction
	{
		public void Execute(IEnumerable<TargetItem> data, IEnumerable<ItemOutput> outputs)
		{
			
		}
	}

	internal abstract class ItemOutput
	{
	}

	internal abstract class TargetItem
	{
		public static TargetItem Password { get; } = new PasswordTargetItem();
		public static TargetItem Metadata { get; } = new MetadataTargetItem();
		public static TargetItem Username { get; } = new UsernameTargetItem();
	}

	internal class PasswordTargetItem : TargetItem
	{
	}

	internal class MetadataTargetItem : TargetItem
	{
	}

	internal class UsernameTargetItem : TargetItem
	{
		
	}
}
