using CoffeeTable.Common.Manifests.Networking;
using CoffeeTable.Module.Applications;
using CoffeeTable.Module.Messaging;
using CoffeeTable.Module.Util;
using CoffeeTable.Module.Window;
using Ideum;
using Ideum.Networking.Application;
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

			// Ensures that all running applications are closed when the console is closed
			ConsoleShutdownHandler.OnShutdown += mApplicationStore.Dispose;

			var inst1 = mAppManager.LaunchApplication(mApplicationStore.GetApplication("RollABall"));
		}

		protected override void Deinitialize()
		{
			base.Deinitialize();
			mApplicationStore.Dispose();
		}

		public void Receive(TcpMessage message) => mMessageRouter.OnMessageReceived(message);
		public void OnClientDisconnected(IClient client) => mMessageRouter.OnClientDisconnected(client);

		#region Http

		[Get("apps")]
		public void GetApplicationsManifest(Http http, ref IResponse response)
		{
			response = new GenericResponse<ApplicationsManifest>
			{
				Result = mApplicationStore.ToManifest()
			};
		}

		[Get("apps/installed")]
		public void GetInstalledApplicationsManifest (Http http, ref IResponse response)
		{
			response = new GenericResponse<ApplicationInfo[]>
			{
				Result = mApplicationStore.Applications.Select(app => app.ToManifest()).ToArray()
			};
		}

		[Get("apps/running")]
		public void GetRunningApplicationsManifest (Http http, ref IResponse response)
		{
			response = new GenericResponse<ApplicationInstanceInfo[]>
			{
				Result = mApplicationStore.Instances.Select(instance => instance.ToManifest()).ToArray()
			};
		}

		#endregion
	}
}
