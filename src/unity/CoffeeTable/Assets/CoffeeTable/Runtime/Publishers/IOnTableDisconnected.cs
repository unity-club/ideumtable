using CoffeeTable.Publishers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Publishers
{
	public interface IOnTableDisconnected
	{
		void OnTableDisconnected();
	}

	internal class OnTableDisconnectedPublisher : GenericPublisher<IOnTableDisconnected>
	{
		protected override void PublishItem(IOnTableDisconnected i) => i.OnTableDisconnected();
	}
}
