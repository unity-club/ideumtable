using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoffeeTable.Module.Applications;

namespace CoffeeTable.Module.Launchers
{
	[Launcher("default")]
	public class DefaultLauncher : ILauncher
	{
		public Process LaunchApplication(Application app)
		{
			if (app == null) return null;
			var p = new Process();
			p.StartInfo = new ProcessStartInfo(app.ExecutablePath);
			p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			p.Start();
			return p;
		}
	}
}
