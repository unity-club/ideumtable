using CoffeeTable.Common.Manifests.Networking;
using CoffeeTable.Publishers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Publishers
{
	public interface IOnApplicationUpdated
	{
		void OnApplicationUpdated((ApplicationInstanceInfo Old, ApplicationInstanceInfo New) delta);
	}

	internal class OnApplicationUpdatedPublisher : GenericDataPublisher<IOnApplicationUpdated, (ApplicationInstanceInfo Old, ApplicationInstanceInfo New)>
	{
		protected override void PublishItem(IOnApplicationUpdated i, (ApplicationInstanceInfo Old, ApplicationInstanceInfo New) data) => i.OnApplicationUpdated(data);
	}
}
