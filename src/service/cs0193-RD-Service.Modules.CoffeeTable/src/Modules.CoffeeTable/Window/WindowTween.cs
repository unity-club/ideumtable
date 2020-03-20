using CoffeeTable.Module.Window.Easing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Window
{
	public class WindowTween
	{
		public bool Completed { get; private set; }
		public bool Killed { get; private set; }
		public bool Started { get; private set; }
		public float Duration { get; set; }
		public Rect From { get; set; }
		public Rect To { get; set; }
		public IEase EaseFunction { get; set; }

		private Action<Rect> mSetter;
		private Stopwatch mStopwatch;

		public WindowTween(Action<Rect> setter)
		{
			mStopwatch = new Stopwatch();
			mSetter = setter;
		}

		public void Kill ()
		{
			Completed = true;
			Killed = true;
			mStopwatch.Stop();
		}

		public void Start ()
		{
			if (Started == true) return;
			Started = true;

			if (Duration == 0)
			{
				mSetter.Invoke(To);
				Completed = true;
			}

			mSetter.Invoke(From);

			mStopwatch.Start();
		}

		public void Tick ()
		{
			if (!Started || Completed) return;

			float elapsed = (float) mStopwatch.Elapsed.TotalSeconds;
			if (elapsed > Duration)
			{
				Completed = true;
				mSetter.Invoke(To);
				return;
			}

			float normalized = elapsed / Duration;
			float progress = EaseFunction.Ease(normalized);

			Rect current = new Rect()
			{
				MinX = From.MinX + progress * (To.MinX - From.MinX),
				MinY = From.MinY + progress * (To.MinY - From.MinY),
				MaxX = From.MaxX + progress * (To.MaxX - From.MaxX),
				MaxY = From.MaxY + progress * (To.MaxY - From.MaxY)
			};

			mSetter.Invoke(current);
		}

		public TaskAwaiter<bool> GetAwaiter()
		{
			return Task.Run(() =>
			{
				if (!Started) return false;
				while (!Completed) {}
				return !Killed;
			}).GetAwaiter();
		}
	}
}
