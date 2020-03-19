using CoffeeTable.Module.Applications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Launchers
{
	public interface ILauncher
	{
		Process LaunchApplication(Application app);
	}
}
