using CoffeeTable.Common.Manifests.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Publishers
{
	public interface IOnApplicationCreated
	{
		void OnApplicationCreated(ApplicationInstanceInfo instance);
	}

	internal class OnApplicationCreatedPublisher : GenericDataPublisher<IOnApplicationCreated, ApplicationInstanceInfo>
	{
		protected override void PublishItem(IOnApplicationCreated i, ApplicationInstanceInfo data) => i.OnApplicationCreated(data);
	}
}
