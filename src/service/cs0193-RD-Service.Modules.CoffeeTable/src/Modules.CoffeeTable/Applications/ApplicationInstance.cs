using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoffeeTable.Manifests;
using CoffeeTable.Module.Messaging;

namespace CoffeeTable.Module.Applications
{
	public class ApplicationInstance
	{
		private static uint _id = 2;

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
	}
}
