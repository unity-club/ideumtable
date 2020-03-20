using CoffeeTable.Manifests;
using CoffeeTable.Module.Applications;
using CoffeeTable.Module.Window.Easing;
using Ideum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Window
{
	public sealed class WindowManager : IWindowManager, IDisposable
	{
		private static Rect LeftSidebarAdjacent => new Rect
		{
			MinX = _leftSidebarThreshold,
			MinY = 0,
			MaxX = _leftSidebarThreshold,
			MaxY = ScreenHeight
		};

		private static Rect RightSidebarAdjacent => new Rect
		{
			MinX = _rightSidebarThreshold,
			MinY = 0,
			MaxX = _rightSidebarThreshold,
			MaxY = ScreenHeight
		};

		private const float SidebarScreenPercentage = 0.05f;
		private readonly object _object = new object();

		private static int ScreenWidth;
		private static int ScreenHeight;

		private static float _leftSidebarThreshold => SidebarScreenPercentage * ScreenWidth;
		private static float _rightSidebarThreshold => ScreenWidth - SidebarScreenPercentage * ScreenWidth;

		private ILog mLog = LogManager.GetLogger(typeof(ApplicationManager));

		private Thread mWorkerThread;
		private EventWaitHandle mWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
		private volatile bool mTerminateSignal;
		private bool hasWork
		{
			get
			{
				lock (_object)
				{
					return mTweensMap.Any();
				}
			}
		}

		private ApplicationInstance mHomeScreen;

		private Dictionary<ApplicationInstance, WindowTween> mTweensMap = new Dictionary<ApplicationInstance, WindowTween>();
		private List<ApplicationInstance> mAppInstances = new List<ApplicationInstance>();

		public WindowManager ()
		{
			NativeMethods.GetScreenResolution(out ScreenWidth, out ScreenHeight);

			mWorkerThread = new Thread(ProcessTweens);
			mWorkerThread.Start();

			mLog.Info($"Began {GetType().Name} worker thread.");
		}

		private void SizeWindow (Rect rect, ApplicationInstance instance)
		{
			if (instance == null) return;
			NativeMethods.SetWindowCoords(instance.Process.MainWindowHandle,
				(int)Math.Round(rect.MinX),
				(int)Math.Round(rect.MinY),
				(int)Math.Round(rect.Width),
				(int)Math.Round(rect.Height));
			instance.Window.WindowRect = rect;
		}

		private void ProcessTweens ()
		{
			while (!mTerminateSignal)
			{
				if (!hasWork)
					mWaitHandle.WaitOne();

				lock (_object)
				{
					var keys = mTweensMap.Keys.ToList();
					foreach (var key in keys)
					{
						WindowTween tween = mTweensMap[key];
						tween.Tick();
						if (tween.Completed)
							mTweensMap.Remove(key);
					}

					// Recalculate the home screen rect
					float leftThreshold = mAppInstances
						.Where(i => i.Layout == ApplicationLayout.LeftPanel)
						.FirstOrDefault()?.Window.WindowRect.MaxX ?? _leftSidebarThreshold;

					float rightThreshold = mAppInstances
						.Where(i => i.Layout == ApplicationLayout.RightPanel)
						.FirstOrDefault()?.Window.WindowRect.MinX ?? _rightSidebarThreshold;

					SizeWindow(new Rect
					{
						MinX = leftThreshold,
						MinY = 0,
						MaxX = rightThreshold,
						MaxY = ScreenHeight
					}, mHomeScreen);
				}
			}
		}

		public WindowTween AnimateWindow (ApplicationInstance instance, AnimateWindowMode mode, bool animate = true)
		{
			WindowTween tween;

			// Style this window
			NativeMethods.StyleWindow(instance.Process.MainWindowHandle);

			lock (_object)
			{
				// get new tween
				tween = new WindowTween(rect => SizeWindow(rect, instance)); 

				tween.EaseFunction = new EaseOutQuad();
				
				if (mode == AnimateWindowMode.CloseWindow)
				{
					tween.From = instance.Window.OpenRect;
					tween.To = instance.Window.ClosedRect;
				} else if (mode == AnimateWindowMode.OpenWindow)
				{
					tween.From = instance.Window.ClosedRect;
					tween.To = instance.Window.OpenRect;
				}

				if (instance.Layout == ApplicationLayout.Fullscreen)
					tween.Duration = 1.0f;
				else tween.Duration = 0.5f;

				if (!animate) tween.Duration = 0f;

				if (mTweensMap.TryGetValue(instance, out WindowTween runningTween))
					runningTween.Kill();

				mTweensMap.Add(instance, tween);

				tween.Start();
			}

			mWaitHandle.Set();

			return tween;
		}

		public void ConfigureApplicationWindow (ApplicationInstance instance)
		{
			ApplicationWindow window = instance.Window;

			switch (instance.App.Type)
			{
				case ApplicationType.Sidebar:
					if (instance.Layout == ApplicationLayout.LeftPanel)
					{
						window.OpenRect = new Rect
						{
							MinX = 0,
							MinY = 0,
							MaxX = _leftSidebarThreshold,
							MaxY = ScreenHeight
						};
					}
					else if (instance.Layout == ApplicationLayout.RightPanel)
					{
						window.OpenRect = new Rect
						{
							MinX = _rightSidebarThreshold,
							MinY = 0,
							MaxX = ScreenWidth,
							MaxY = ScreenHeight
						};
					}
					window.ClosedRect = window.OpenRect;
					break;

				case ApplicationType.Homescreen:
					window.OpenRect = Rect.Zero;
					window.ClosedRect = Rect.Zero;
					break;

				case ApplicationType.Application:
					switch (instance.Layout)
					{
						case ApplicationLayout.Fullscreen:
							window.OpenRect = new Rect
							{
								MinX = _leftSidebarThreshold,
								MinY = 0,
								MaxX = _rightSidebarThreshold,
								MaxY = ScreenHeight
							};
							window.ClosedRect = LeftSidebarAdjacent;
							break;
						case ApplicationLayout.LeftPanel:
							window.OpenRect = new Rect
							{
								MinX = _leftSidebarThreshold,
								MinY = 0,
								MaxX = ScreenWidth / 2f,
								MaxY = ScreenHeight
							};
							window.ClosedRect = LeftSidebarAdjacent;
							break;
						case ApplicationLayout.RightPanel:
							window.OpenRect = new Rect
							{
								MinX = ScreenWidth / 2f,
								MinY = 0,
								MaxX = _rightSidebarThreshold,
								MaxY = ScreenHeight
							};
							window.ClosedRect = RightSidebarAdjacent;
							break;
					}
					break;
			}
		}

		public void OnApplicationInstanceCreated(ApplicationInstance instance)
		{
			if (instance.App.Type == ApplicationType.Homescreen)
					mHomeScreen = instance;
			mAppInstances.Add(instance);
		}

		public void OnApplicationInstanceDestroyed(ApplicationInstance instance)
		{
			if (ReferenceEquals(instance, mHomeScreen))
				mHomeScreen = null;
			mAppInstances.Remove(instance);

			lock (_object)
			{
				if (mTweensMap.TryGetValue(instance, out WindowTween tween))
				{
					tween.Kill();
					mTweensMap.Remove(instance);
				}
			}
		}

		public void Dispose()
		{
			if (mWorkerThread != null)
			{
				mTerminateSignal = true;
				mWaitHandle.Set();
				if (!mWorkerThread.Join(TimeSpan.FromSeconds(5)))
					mWorkerThread.Abort();
				mWorkerThread = null;
			}
		}
	}
}
