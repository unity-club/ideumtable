using Ideum;
using Ideum.Networking.Transport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module
{
	[Module("coffeetable")]
	public class CoffeeTableModule : ModuleBase, ITransportLayerReceiver
	{
		protected override void Initialize()
		{
			base.Initialize();
			Console.WriteLine("does this work??? ;ddddd");
			Log.Debug(Service.Manifest.HttpPort);
			Log.Debug(Service.Manifest.TcpPort);
			foreach (string ip in Service.Manifest.IpAddresses)
				Log.Debug(ip);
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
