using CoffeeTable.Common.Manifests;
using CoffeeTable.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CoffeeTable
{
	[CreateAssetMenu(fileName = "AppSettings", menuName = "CoffeeTable/AppSettings")]
	public class AppSettings : ScriptableObject
	{
		private static AppSettings mInstance;
		public static AppSettings Instance
		{
			get
			{
				if (mInstance != null) return mInstance;
				var instances = Resources.LoadAll<AppSettings>(string.Empty);

#if UNITY_EDITOR
				if (instances != null && instances.Length > 1)
					Log.Warn($"Multiple {nameof(AppSettings)} resources were found." +
						$" There should be at most one {nameof(AppSettings)} per project." +
						$" Using the first {nameof(AppSettings)} found at {UnityEditor.AssetDatabase.GetAssetPath(instances[0].GetInstanceID())}");
#endif
				if (instances == null || instances.Length == 0) mInstance = CreateInstance<AppSettings>();
				else mInstance = instances[0];
				return mInstance;
			}
		}

		[SerializeField]
		internal int mFallbackHttpPort = 8080;
		public static int FallbackHttpPort => Instance.mFallbackHttpPort;

		[SerializeField]
		internal int mFallbackTcpPort = 4747;
		public static int FallbackTcpPort => Instance.mFallbackTcpPort;

		[SerializeField]
		internal string mAppName = string.Empty;
		public static string AppName => Instance.mAppName;

		[SerializeField]
		internal string mAuthorName = string.Empty;
		public static string AuthorName => Instance.mAuthorName;

		[SerializeField]
		internal string mDescription = string.Empty;
		public static string Description => Instance.mDescription;

		[SerializeField]
		internal bool mLaunchInFullscreen = false;
		public static bool LaunchInFullscreen => Instance.mLaunchInFullscreen;

		[SerializeField]
		internal ApplicationType mApplicationType = ApplicationType.Application;
		public static ApplicationType ApplicationType => Instance.mApplicationType;

		[SerializeField]
		internal bool mIsApiEnabled = true;
		public static bool IsApiEnabled => Instance.mIsApiEnabled; 

		public static ApplicationManifest GetManifest ()
		{
			return new ApplicationManifest
			{
				Author = AuthorName,
				Description = Description,
				ExecutablePath = string.Empty,
				LauncherName = string.Empty,
				LaunchInFullscreen = LaunchInFullscreen,
				Name = AppName,
				Type = ApplicationType
			};
		}
	}
}
