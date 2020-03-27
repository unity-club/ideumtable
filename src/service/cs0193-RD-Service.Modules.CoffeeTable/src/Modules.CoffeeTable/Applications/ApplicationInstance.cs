using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoffeeTable.Manifests;
using CoffeeTable.Module.Messaging;
using CoffeeTable.Module.Window;

namespace CoffeeTable.Module.Applications
{
	public class ApplicationInstance
	{
		public const uint ModuleId = 1;
		private static uint _id = ModuleId + 1;

		public uint Id { get; }
		public Application App { get; }
		public Process Process { get; }
		public int ProcessId => Process?.Id ?? 0;
		public ApplicationLayout Layout { get; set; }
		public ConnectionStatus Connection { get; set; }
		public ApplicationState State { get; set; }

		public ApplicationInstance(Application app, Process process)
		{
			Id = _id++;
			App = app;
			Process = process;
		}

		/// <summary>
		/// Waits for the application's process to open its main window within the given time frame.
		/// </summary>
		/// <param name="timeout">A timespan indicating how long this method should wait for the instance's window to open.</param>
		/// <returns>A task returning a boolean indicating whether opening the window succeeded or not.
		/// If true, the window successfully opened. If false, the window failed to open or failed to do so within the alotted time span.</returns>
		public async Task<bool> OpenWindowAsync (TimeSpan timeout)
		{
			if (Process == null) return false;
			if (Process.HasExited) return false;

			return await Task.Run(() =>
			{
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();

				while (string.IsNullOrEmpty(Process.MainWindowTitle))
				{
					if (stopwatch.Elapsed > timeout) return false;
					Process.Refresh();
				}

				stopwatch.Stop();
				return true;
			}).ConfigureAwait(false);
		}
	}
}
