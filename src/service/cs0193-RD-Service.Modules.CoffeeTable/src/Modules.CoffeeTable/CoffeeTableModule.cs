using CoffeeTable.Module.Applications;
using CoffeeTable.Module.Messaging;
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
	public class CoffeeTableModule : ModuleBase, ITransportLayerReceiver, ITransportLayerDisconnectReceiver
	{
		private ApplicationManager mAppManager;
		private IMessageRouter mMessageRouter;

		protected override void Initialize()
		{
			base.Initialize();

			mMessageRouter = new MessageRouter(Service.Send);
			mAppManager = new ApplicationManager(Log, mMessageRouter);
			
		}

		protected override void Deinitialize()
		{
			base.Deinitialize();
		}

		public void Receive(TcpMessage message) => mMessageRouter.OnMessageReceived(message);
		public void OnClientDisconnected(IClient client) => mMessageRouter.OnClientDisconnected(client);
	}
}
