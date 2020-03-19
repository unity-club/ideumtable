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
		private IMessageRouter mMessageRouter;

		protected override void Initialize()
		{
			base.Initialize();

			//mMessageRouter = new MessageRouter(Service.Send);
			//mAppManager = new ApplicationManager(mMessageRouter);

			int width, height;
			NativeMethods.GetScreenResolution(out width, out height);
			Log.Info($"Screen resolution is {width}x{height}");

			NativeMethods.StyleWindow(Process.GetCurrentProcess().MainWindowHandle, true);
			NativeMethods.SetWindowCoords(Process.GetCurrentProcess().MainWindowHandle, 0, 0, width, height);
		}

		protected override void Deinitialize()
		{
			base.Deinitialize();
		}

		public void Receive(TcpMessage message) => mMessageRouter.OnMessageReceived(message);
		public void OnClientDisconnected(IClient client) => mMessageRouter.OnClientDisconnected(client);
	}
}
