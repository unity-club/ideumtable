using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Util
{
	public static class ConsoleShutdownHandler
	{
		private static ConsoleEventDelegate handler;

		public static event Action OnShutdown;

		static ConsoleShutdownHandler()
		{
			handler = ConsoleEventCallback;
			SetConsoleCtrlHandler(handler, true);
		}

		private static bool ConsoleEventCallback(int eventType)
		{
			if (eventType == 2) OnShutdown?.Invoke();
			return false;
		}

		private delegate bool ConsoleEventDelegate(int eventType);
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
	}
}
