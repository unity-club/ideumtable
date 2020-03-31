using CoffeeTable.Common.Manifests;
using CoffeeTable.Module.Applications;
using Ideum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Window
{
	public sealed class WindowManager
	{
		private const float SidebarScreenPercentage = 0.05f;

		private static int ScreenWidth;
		private static int ScreenHeight;

		private float mLeftSidebarThreshold => mApplicationStore?.LeftSidebar?.WindowRect.MaxX ?? 0;
		private float mRightSidebarThreshold => mApplicationStore?.RightSidebar?.WindowRect.MinX ?? ScreenWidth;

		private ILog mLog = LogManager.GetLogger(typeof(ApplicationManager));

		private ApplicationStore mApplicationStore;

		public WindowManager (ApplicationStore appStore)
		{
			mApplicationStore = appStore;
			NativeMethods.GetScreenResolution(out ScreenWidth, out ScreenHeight);
		}

		// Returns true if the window was successfully opened and sized
		public async Task<bool> OpenWindow (ApplicationInstance instance)
		{
			if (instance == null) return false;
			// First we must ensure that the window is open
			if (!await instance.OpenWindowAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false))
				return false;

			NativeMethods.StyleWindow(instance.Process.MainWindowHandle);
			SizeWindow(instance);

			return true;
		}

		public void SizeWindow (ApplicationInstance instance)
		{
			if (instance == null) return;
			if (instance.IsSimulator) return; // Do not try to resize simulated applications
			if (instance.App.Type != ApplicationType.Homescreen) // Do not allow resizing homescreen
			{
				ApplicationRect rect = GetApplicationRect(instance);
				instance.WindowRect = rect;
				NativeMethods.SetWindowCoords(instance.Process.MainWindowHandle,
					(int)Math.Round(rect.MinX),
					(int)Math.Round(rect.MinY),
					(int)Math.Round(rect.Width),
					(int)Math.Round(rect.Height));
			}

			// Resize the homescreen accordingly
			if (mApplicationStore.HomeScreen == null 
				|| mApplicationStore.HomeScreen.State != ApplicationState.Running
				|| mApplicationStore.HomeScreen.IsSimulator) return;
			float leftThreshold;
			float rightThreshold;
			IEnumerable<ApplicationInstance> appInstances = mApplicationStore.GetInstancesOfType(ApplicationType.Application);
			if (appInstances.Any(i => i.Layout == ApplicationLayout.Fullscreen))
			{
				leftThreshold = mLeftSidebarThreshold;
				rightThreshold = mLeftSidebarThreshold;
			} else {
				leftThreshold = appInstances
					.Where(i => i.Layout == ApplicationLayout.LeftPanel)
					.FirstOrDefault()?
					.WindowRect.MaxX ?? mLeftSidebarThreshold;
				rightThreshold = appInstances
					.Where(i => i.Layout == ApplicationLayout.RightPanel)
					.FirstOrDefault()?
					.WindowRect.MinX ?? mRightSidebarThreshold;
			}

			NativeMethods.SetWindowCoords(mApplicationStore.HomeScreen.Process.MainWindowHandle,
				(int)leftThreshold,
				0,
				(int) (rightThreshold - leftThreshold),
				ScreenHeight);
		}

		private ApplicationRect GetApplicationRect (ApplicationInstance instance)
		{
			switch (instance.App.Type)
			{
				case ApplicationType.Sidebar:
					if (instance.Layout == ApplicationLayout.LeftPanel)
					{
						return new ApplicationRect
						{
							MinX = 0,
							MinY = 0,
							MaxX = SidebarScreenPercentage * ScreenWidth,
							MaxY = ScreenHeight
						};
					}
					else if (instance.Layout == ApplicationLayout.RightPanel)
					{
						return new ApplicationRect
						{
							MinX = ScreenWidth * (1 - SidebarScreenPercentage),
							MinY = 0,
							MaxX = ScreenWidth,
							MaxY = ScreenHeight
						};
					}
					break;

				case ApplicationType.Homescreen:
					return ApplicationRect.Zero;

				case ApplicationType.Application:
					switch (instance.Layout)
					{
						case ApplicationLayout.Fullscreen:
							return new ApplicationRect
							{
								MinX = mLeftSidebarThreshold,
								MinY = 0,
								MaxX = mRightSidebarThreshold,
								MaxY = ScreenHeight
							};
						case ApplicationLayout.LeftPanel:
							return new ApplicationRect
							{
								MinX = mLeftSidebarThreshold,
								MinY = 0,
								MaxX = ScreenWidth / 2f,
								MaxY = ScreenHeight
							};
						case ApplicationLayout.RightPanel:
							return new ApplicationRect
							{
								MinX = ScreenWidth / 2f,
								MinY = 0,
								MaxX = mRightSidebarThreshold,
								MaxY = ScreenHeight
							};
					}
					break;
			}

			return ApplicationRect.Zero;
		}
	}
}
