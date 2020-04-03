using CoffeeTable.Editor.Inspector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace CoffeeTable.Editor
{
	public static class MenuItems
	{
		private static readonly string RootResourcesPath = Path.Combine("Assets", "Resources");
		private static readonly string RootResourcesPathAbsolute = Path.Combine(Application.dataPath, "Resources");
		private static readonly Regex InResourcesDirectory = new Regex($@"\b[{Regex.Escape(Path.DirectorySeparatorChar.ToString())}{Regex.Escape(Path.AltDirectorySeparatorChar.ToString())}](?i)resources[{Regex.Escape(Path.DirectorySeparatorChar.ToString())}{Regex.Escape(Path.AltDirectorySeparatorChar.ToString())}]\b");
		private const string FixKeyword = "Fix";
		private const string CancelKeyword = "Cancel";

		[MenuItem("Window/CoffeeTable/Create or Find Settings")]
		public static void CreateAppSettings() => GetAppSettings(true);

		public static AppSettings GetAppSettings (bool selectInEditor)
		{
			// First validate that there are not multiple app settings
			var appSettingsPaths = AssetDatabase.FindAssets($"t:{nameof(AppSettings)}", new[] { "Assets" })
				.Select(guid => AssetDatabase.GUIDToAssetPath(guid))
				.Where(path => !string.IsNullOrEmpty(path));

			// Ensure that there is only one AppSettings instance
			if (appSettingsPaths.Count() > 1)
			{
				var settingsPaths = string.Join("\n", appSettingsPaths);
				EditorUtility.DisplayDialog($"Multiple {nameof(AppSettings)} Found",
					$"Multiple app settings were found in your project:\n\n{settingsPaths}\n\n" +
					$"Ensure that there is only one {nameof(AppSettings)} object located inside the Resources folder of your project and try again.",
					"Ok");
				return null;
			}

			// Ensure that the AppSettings object is not outside the resources folder
			var outsideOfResources = appSettingsPaths.Where(path => !InResourcesDirectory.IsMatch(path));
			if (outsideOfResources.Any())
			{
				if (EditorUtility.DisplayDialog($"{nameof(AppSettings)} Outside of Resources",
					$"Your {nameof(AppSettings)} object was found outside of the resources folder:\n\n{outsideOfResources.First()}\n\n" +
					$"There should be one and only one instance of {nameof(AppSettings)} inside of the Resources folder. " +
					$"Click {FixKeyword} to move this instance into the Resources folder or click {CancelKeyword} to fix this issue yourself.",
					FixKeyword, CancelKeyword))
				{
					
					var newFileName = Path.Combine(RootResourcesPath, Path.GetFileName(outsideOfResources.First()));
					if (!Directory.Exists(RootResourcesPathAbsolute)) Directory.CreateDirectory(RootResourcesPathAbsolute);
					AssetDatabase.Refresh();
					Debug.Log(AssetDatabase.MoveAsset(outsideOfResources.First(), newFileName));
					AssetDatabase.Refresh();
				} else return null;
			}

			// Now use Resources load the instance of the AppSettings object.
			// If there are none, then simply create a new one in Assets/Resources/AppSettings.asset
			AppSettings settings;
			var resources = Resources.LoadAll<AppSettings>(string.Empty);
			if (resources == null || resources.Length == 0)
			{
				settings = ScriptableObject.CreateInstance<AppSettings>();
				if (!Directory.Exists(RootResourcesPathAbsolute)) Directory.CreateDirectory(RootResourcesPathAbsolute);
				AssetDatabase.CreateAsset(settings, Path.Combine(RootResourcesPath, "AppSettings.asset"));
				AssetDatabase.SaveAssets();
			}
			else settings = resources.First();

			if (selectInEditor)
			{
				EditorUtility.FocusProjectWindow();
				Selection.activeObject = settings;
			}

			return settings;
		}
	}
}
