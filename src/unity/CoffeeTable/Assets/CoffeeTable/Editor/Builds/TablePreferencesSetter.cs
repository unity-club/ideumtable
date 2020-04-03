using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace CoffeeTable.Editor.Builds
{
	public class TablePreferencesSetter : IDisposable
	{
		private bool mDisposed = false;

		private RestoreableProperty<bool> mAllowFullscreenSwitchProperty = new RestoreableProperty<bool>(() => PlayerSettings.allowFullscreenSwitch, x => PlayerSettings.allowFullscreenSwitch = x);
		private RestoreableProperty<bool> mForceSingleInstanceProperty = new RestoreableProperty<bool>(() => PlayerSettings.forceSingleInstance, x => PlayerSettings.forceSingleInstance = x);
		private RestoreableProperty<FullScreenMode> mFullScreenModeProperty = new RestoreableProperty<FullScreenMode>(() => PlayerSettings.fullScreenMode, x => PlayerSettings.fullScreenMode = x);
		private RestoreableProperty<bool> mMuteOtherAudioSourcesProperty = new RestoreableProperty<bool>(() => PlayerSettings.muteOtherAudioSources, x => PlayerSettings.muteOtherAudioSources = x);
		private RestoreableProperty<bool> mResizableWindowProperty = new RestoreableProperty<bool>(() => PlayerSettings.resizableWindow, x => PlayerSettings.resizableWindow = x);
		private RestoreableProperty<bool> mRunInBackgroundProperty = new RestoreableProperty<bool>(() => PlayerSettings.runInBackground, x => PlayerSettings.runInBackground = x);

		private IRestoreableProperty[] mProperties;

		public TablePreferencesSetter()
		{
			mProperties = new IRestoreableProperty[]
			{
				mAllowFullscreenSwitchProperty,
				mForceSingleInstanceProperty,
				mFullScreenModeProperty,
				mMuteOtherAudioSourcesProperty,
				mResizableWindowProperty,
				mRunInBackgroundProperty
			};

			CachePreferences();
			SetPreferences();
		}

		private void CachePreferences ()
		{
			foreach (var property in mProperties)
				property.CacheValue();
		}

		private void SetPreferences ()
		{
			mAllowFullscreenSwitchProperty.SetValue(false);
			mForceSingleInstanceProperty.SetValue(false);
			mFullScreenModeProperty.SetValue(FullScreenMode.Windowed);
			mMuteOtherAudioSourcesProperty.SetValue(false);
			mResizableWindowProperty.SetValue(true);
			mRunInBackgroundProperty.SetValue(true);
		}

		private void RestorePreferences ()
		{
			foreach (var property in mProperties)
				property.RestoreValue();
		}

		private void Dispose(bool disposing)
		{
			if (mDisposed) return;
			mDisposed = true;
			RestorePreferences();
		}

		public void Dispose() => Dispose(true);
		~TablePreferencesSetter() => Dispose(false);
	}
}
