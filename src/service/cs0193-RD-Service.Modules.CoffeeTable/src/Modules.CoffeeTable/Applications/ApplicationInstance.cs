using System;
using System.Collections.Generic;
using System.ComponentModel;
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
		public const uint LeftSidebarId = 2;
		public const uint RightSidebarId = 3;
		public const uint HomescreenId = 4;

		private static uint _id = new[] { ModuleId, LeftSidebarId, RightSidebarId, HomescreenId }.Max() + 1;

		public uint Id { get; }
		public Application App { get; }
		public Process Process { get; }
		public int ProcessId => Process?.Id ?? 0;
		public ApplicationLayout Layout { get; set; }
		public ConnectionStatus Connection { get; set; }
		public ApplicationState State { get; set; }
		public Rect WindowRect { get; set; }

		public ApplicationInstance(Application app, Process process, ApplicationLayout layout)
		{
			switch (app.Type)
			{
				case ApplicationType.Application:
					Id = _id++;
					break;

				case ApplicationType.Homescreen:
					Id = HomescreenId;
					break;

				case ApplicationType.Sidebar:
					switch (layout)
					{
						case ApplicationLayout.LeftPanel:
						case ApplicationLayout.Fullscreen:
							Id = LeftSidebarId;
							break;

						case ApplicationLayout.RightPanel:
							Id = RightSidebarId;
							break;
					}
					break;
			}

			Id = _id++;
			App = app;
			Process = process;
			Layout = layout;
		}

		/// <summary>
		/// Waits for the application's process to open its main window within the given time frame.
		/// </summary>
		/// <param name="timeout">A timespan indicating how long this method should wait for the instance's window to open.</param>
		/// <returns>A task returning a boolean indicating whether opening the window succeeded or not.
		/// If true, the window successfully opened. If false, the window failed to open or failed to do so within the alotted time span.</returns>
		public Task<bool> OpenWindowAsync (TimeSpan timeout)
		{
			if (Process == null) return Task.FromResult(false);
			if (Process.HasExited) return Task.FromResult(false);

			return Task.Run(() =>
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
			});
		}

		/// <summary>
		/// Attempts to close the process asynchronously within the allotted timeout. If it does not close within the alotted timeout,
		/// forcefully kills the process.
		/// </summary>
		/// <param name="timeout">The maximum amount of time this function should wait for the process to close.</param>
		public Task TerminateProcessAsync (TimeSpan timeout)
		{
			if (Process == null) return Task.CompletedTask;
			if (Process.HasExited) return Task.CompletedTask;

			return Task.Run(() =>
			{
				Stopwatch st = new Stopwatch();

				Process.CloseMainWindow();
				Process.Close();

				st.Start();

				for (;;)
				{
					if (st.Elapsed > timeout)
						break;
					if (Process.HasExited) return;
					Process.Refresh();
				}

				try { Process.Kill(); }
				catch (SystemException) { }
			});
		}
	}
}
