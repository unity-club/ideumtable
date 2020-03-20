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
		private ApplicationManager mAppManager;
		private IWindowManager mWindowManager;
		private IMessageRouter mMessageRouter;

		protected override async void Initialize()
		{
			base.Initialize();

			mWindowManager = new WindowManager();
			mMessageRouter = new MessageRouter(Service.Send);
			mAppManager = new ApplicationManager(mMessageRouter, mWindowManager);

			var inst = mAppManager.LaunchApplication(mAppManager.GetApplication("RollABall"));
			Thread.Sleep(5000);
			await mWindowManager.AnimateWindow(inst, AnimateWindowMode.CloseWindow);
			inst.Process.Kill();
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
