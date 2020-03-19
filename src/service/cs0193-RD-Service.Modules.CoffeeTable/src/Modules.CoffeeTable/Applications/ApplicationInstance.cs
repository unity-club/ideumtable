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
		public uint Id { get; private set; }
		public Application App { get; private set; }
		public Process Process { get; private set; }
		public int ProcessId => Process.Id;
		public bool IsFullscreen { get; set; }
		public ConnectionStatus Connection { get; set; }
	}
}
