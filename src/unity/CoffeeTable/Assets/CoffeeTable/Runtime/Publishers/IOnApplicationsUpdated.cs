using CoffeeTable.Common.Manifests.Networking;
using CoffeeTable.Publishers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Publishers
{
	public interface IOnApplicationsUpdated
	{
		void OnApplicationsChanged(ApplicationsManifest manifest);
	}

	public class OnApplicationsUpdatedPublisher : GenericDataPublisher<IOnApplicationsUpdated, ApplicationsManifest>
	{
		protected override void PublishItem(IOnApplicationsUpdated i, ApplicationsManifest data) => i.OnApplicationsChanged(data);
	}
}
