using CoffeeTable.Common.Manifests.Networking;
using CoffeeTable.Publishers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Publishers
{
	public interface IOnApplicationDestroyed
	{
		void OnapplicationDestroyed(ApplicationInstanceInfo instance);
	}

	internal class OnApplicationDestroyedPublisher : GenericDataPublisher<IOnApplicationDestroyed, ApplicationInstanceInfo>
	{
		protected override void PublishItem(IOnApplicationDestroyed i, ApplicationInstanceInfo data) => i.OnapplicationDestroyed(data);
	}
}
