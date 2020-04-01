using CoffeeTable.Publishers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Publishers
{
	public interface IOnTableConnected
	{
		void OnTableConnected();
	}

	internal class OnTableConnectedPublisher : GenericPublisher<IOnTableConnected>
	{
		protected override void PublishItem(IOnTableConnected i) => i.OnTableConnected();
	}
}
