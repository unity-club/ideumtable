using Ideum;
using Ideum.Networking.Transport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoffeeTable.Module
{
	[Module("coffeetable")]
	public class CoffeeTableModule : ModuleBase, ITransportLayerReceiver
	{
		protected override void Initialize()
		{
			base.Initialize();
		}

		protected override void Deinitialize()
		{
			base.Deinitialize();
		}

		public void Receive(TcpMessage message)
		{
			
		}
	}
}
