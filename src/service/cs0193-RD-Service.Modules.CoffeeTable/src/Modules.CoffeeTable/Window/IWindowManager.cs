using CoffeeTable.Module.Applications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Window
{
	public interface IWindowManager : IApplicationCallbacksReceiver
	{
		void ConfigureApplicationWindow(ApplicationInstance instance);
		WindowTween AnimateWindow(ApplicationInstance instance, AnimateWindowMode mode, bool animate = true);
	}
}
