using CoffeeTable.Common.Manifests;
using CoffeeTable.Editor.SourceParsing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using static CoffeeTable.Logging.Log;
using System.IO.Compression;

namespace CoffeeTable.Editor.Builds
{
	internal class TableBuilder : IDisposable
	{
		private class Strings
		{
			public const string kBeginningBuild = "Preparing Build";

			public const string kSavingImageDescriptionConvert = "Converting app image to PNG";
			public const string kSavingImageDescriptionWrite = "Writing app image PNG to disk";

			public const string kSavingAppManifest = "Saving app manifest";

			public const string kBuildingPlayer = "Building Player";
			public const string kBuildingPlayerPreparing = "Preparing to build player...";

			public const string kSaveAppDialogTitle = "Choose location for .table file";
			public const string kSaveAppDialogInvalidFileExtension = "Selected save path for built application had an invalid file extension. Built table apps must have the ." + kAppExtension + " extension.";
			public const string kSaveAppDialogNoParentDirectory = "Parent directory for chosen save file path does not exist.";
			public const string kAppExtension = "tableapp";

			public const string kFinishingBuild = "Finishing Build";
			public const string kCompressingContents = "Compressing archive contents to: {0}. This may take a few moments.";

			public const string kSavingArchiveNoPermissions = "Failed to compress build archive: did not have permissions to write to the chosen save file directory.";
		}

		public delegate void ProgressHandler(string title, string description, float progress);

		private static readonly string kTempBuildsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CoffeeTable", "unity", "builds", "temp");

		public Action OnDispose { get; set; }
		public ProgressHandler ProgressDelegate { get; set; }
		public LogDelegate LoggerDelegate { get; set; }
		public AppSettings Settings { get; set; }
		public EditorBuildSettingsScene[] Scenes { get; set; }

		private Texture2D mTempTexture;

		private bool mDisposed = false;

		public BuildResult Build()
		{
			// Ensure that all data passed to this object is valid
			Validate();

			// First ask the user for the path to the .tableapp file that will be generated
			var savePath = EditorUtility.SaveFilePanel(Strings.kSaveAppDialogTitle, string.Empty, Settings.mAppName.Trim(), Strings.kAppExtension);
			if (string.IsNullOrEmpty(savePath)) return BuildResult.Cancelled;
			if (!($".{Strings.kAppExtension}").Equals(Path.GetExtension(savePath))) {
				LoggerDelegate?.Invoke(LogLevels.Error, Strings.kSaveAppDialogInvalidFileExtension);
				return BuildResult.Failed;
			}
			var savePathDirectory = Path.GetDirectoryName(savePath);
			if (!Directory.Exists(savePathDirectory))
			{
				LoggerDelegate?.Invoke(LogLevels.Error, Strings.kSaveAppDialogNoParentDirectory);
				return BuildResult.Failed;
			}
			
			// Before we begin build, let's validate the source code for any misused identifiers
			if (!SourceValidator.ValidateSource())
				return BuildResult.Failed;

			// Create temp folder to house this build as we create it
			string tempDir = Path.Combine(kTempBuildsPath, GUID.Generate().ToString());
			Directory.CreateDirectory(tempDir);

			// Save the icon file
			string iconPath = Path.Combine(tempDir, "icon.png");
			ProgressDelegate?.Invoke(Strings.kBeginningBuild, Strings.kSavingImageDescriptionConvert, 0f);
			mTempTexture = new Texture2D(Settings.mIcon.width, Settings.mIcon.height, TextureFormat.ARGB32, false);
			mTempTexture.SetPixels(0, 0, Settings.mIcon.width, Settings.mIcon.height, Settings.mIcon.GetPixels());
			mTempTexture.Apply();
			var iconBytes = ImageConversion.EncodeToPNG(mTempTexture);
			
			ProgressDelegate?.Invoke(Strings.kBeginningBuild, Strings.kSavingImageDescriptionWrite, 1f);
			File.WriteAllBytes(iconPath, iconBytes);

			// Binary paths
			const string binFolder = "bin";
			const string exeFile = "app.exe";
			string binPath = Path.Combine(tempDir, binFolder);
			string exePath = Path.Combine(binPath, exeFile);
			Directory.CreateDirectory(binPath);

			// Save app manifest
			ProgressDelegate?.Invoke(Strings.kBeginningBuild, Strings.kSavingAppManifest, 1f);
			ApplicationManifest appManifest = Settings.GetManifest();
			appManifest.ExecutablePath = Path.Combine(binFolder, exeFile);
			File.WriteAllText(Path.Combine(tempDir, "manifest.json"), JsonConvert.SerializeObject(appManifest));

			// Begin build
			ProgressDelegate?.Invoke(Strings.kBuildingPlayer, Strings.kBuildingPlayerPreparing, 0);

			BuildReport report;
			using (new TablePreferencesSetter())
			{
				BuildPlayerOptions buildOptions = new BuildPlayerOptions();
				buildOptions.scenes = Scenes.Where(i => i.enabled).Select(i => i.path).ToArray();
				buildOptions.locationPathName = exePath;
				buildOptions.targetGroup = BuildTargetGroup.Standalone;
				buildOptions.target = BuildTarget.StandaloneWindows64;

				report = BuildPipeline.BuildPlayer(buildOptions);
			}

			if (report.summary.result != BuildResult.Succeeded)
				return report.summary.result;

			ProgressDelegate?.Invoke(Strings.kFinishingBuild, string.Format(Strings.kCompressingContents, savePath), 0);

			// Check for write permissions
			if (!HasWritePermissions(savePathDirectory))
			{
				LoggerDelegate?.Invoke(LogLevels.Error, Strings.kSavingArchiveNoPermissions);
				return BuildResult.Failed;
			}

			// Allow overwrites
			if (File.Exists(savePath))
			{
				File.Delete(savePath);
			}
			ZipFile.CreateFromDirectory(tempDir, savePath);

			;
			return BuildResult.Succeeded;
		}

		private void Validate()
		{
			if (Settings == null)
				throw new ArgumentException(nameof(Settings));
			if (Settings.mIcon == null)
				throw new ArgumentException("No icon in AppSettings.");
			else if (!Settings.mIcon.isReadable)
				throw new ArgumentException("App icon not readable.");
			if (Scenes == null
				|| Scenes.Length == 0
				|| !Scenes.Where(scene => scene.enabled).Any())
				throw new ArgumentException(nameof(Settings));
			if (string.IsNullOrWhiteSpace(Settings.mAppName))
				throw new ArgumentException("No app name.");
		}

		private void DisposeOfTempBuilds ()
		{
			if (!Directory.Exists(kTempBuildsPath)) return;
			DirectoryInfo tempBuilds = new DirectoryInfo(kTempBuildsPath);

			try { tempBuilds.Delete(true); }
			catch (Exception e)
			{
				if (!(e is UnauthorizedAccessException || e is IOException)) throw;
			}
		}

		public void Dispose() => Dispose(true);

		public void Dispose(bool disposing)
		{
			if (mDisposed) return;
			mDisposed = true;
			if (mTempTexture != null) UnityEngine.Object.DestroyImmediate(mTempTexture);
			DisposeOfTempBuilds();
			try
			{
				OnDispose?.Invoke();
			} catch (Exception e)
			{
				LoggerDelegate?.Invoke(LogLevels.Error, $"Error occured while invoking {nameof(OnDispose)} callback: {e.ToString()}");
			}
		}

		~TableBuilder() => Dispose(false);

		//
		// Taken from: https://stackoverflow.com/q/1410127/10149816
		// 
		private static bool HasWritePermissions (string directoryPath)
		{
			try
			{
				// Attempt to get a list of security permissions from the folder. 
				// This will raise an exception if the path is read only or do not have access to view the permissions. 
				System.Security.AccessControl.DirectorySecurity ds = Directory.GetAccessControl(directoryPath);
				return true;
			}
			catch (UnauthorizedAccessException)
			{
				return false;
			}
		}
	}
}
