using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CoffeeTableLauncher
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		private static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		private static extern int SetForegroundWindow(IntPtr hwnd);

		private enum ShowWindowEnum
		{
			Hide = 0,
			ShowNormal = 1, ShowMinimized = 2, ShowMaximized = 3,
			Maximize = 3, ShowNormalNoActivate = 4, Show = 5,
			Minimize = 6, ShowMinNoActivate = 7, ShowNoActivate = 8,
			Restore = 9, ShowDefault = 10, ForceMinimized = 11
		};

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			// This code will allow only one instance of this window to be active at a time.
			// Useful because only one launcher process should be modifying the CoffeeTable filesystem at once.
			Process currentProcess = Process.GetCurrentProcess();

			Process runningProcess = (from proc in Process.GetProcessesByName(currentProcess.ProcessName)
									  where proc.Id != currentProcess.Id
									  select proc).FirstOrDefault();
			if (runningProcess != null)
			{
				// There is an already running launcher. Check to see if it's window is hidden and show it if it is.
				if (runningProcess.MainWindowHandle == IntPtr.Zero)
					ShowWindow(runningProcess.Handle, ShowWindowEnum.Restore);

				// Set focus on the already running launcher
				SetForegroundWindow(runningProcess.MainWindowHandle);

				// Now close our current app
				Shutdown();
			}
		}
	}
}
