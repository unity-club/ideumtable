using CoffeeTable.Module.Applications;
using CoffeeTable.Module.Messaging;
using CoffeeTable.Module.Window;
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
		private ApplicationStore mApplicationStore;
		private ApplicationManager mAppManager;
		private WindowManager mWindowManager;
		private MessageRouter mMessageRouter;

		protected override async void Initialize()
		{
			base.Initialize();

			mApplicationStore = new ApplicationStore();
			mWindowManager = new WindowManager(mApplicationStore);
			mMessageRouter = new MessageRouter(mApplicationStore, Service.Send);
			mAppManager = new ApplicationManager(mApplicationStore, mMessageRouter, mWindowManager);

			var inst = mAppManager.LaunchApplication(mApplicationStore.GetApplication("RollABall"));
			;
		}

		protected override void Deinitialize()
		{
			base.Deinitialize();
		}

		public void Receive(TcpMessage message) => mMessageRouter.OnMessageReceived(message);
		public void OnClientDisconnected(IClient client) => mMessageRouter.OnClientDisconnected(client);
	}
}
