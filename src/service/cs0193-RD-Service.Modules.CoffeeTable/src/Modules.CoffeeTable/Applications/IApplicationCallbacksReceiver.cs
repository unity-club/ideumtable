using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Applications
{
	public interface IApplicationCallbacksReceiver
	{
		void OnApplicationInstanceCreated(ApplicationInstance instance);
		void OnApplicationInstanceDestroyed(ApplicationInstance instance);
	}
}
