using Ionic.Zip;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using MahApps.Metro.Controls.Dialogs;
using System.IO;
using CoffeeTable.Manifests;
using Newtonsoft.Json;
using System.ComponentModel;

namespace CoffeeTableLauncher
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : MetroWindow
	{
		private static readonly string RootFolder;
		private static readonly string AppsFolder;

		private const string HEADER_DEPLOY_FAILURE = "Failed to deploy";
		private const string HEADER_APPLICATION_ADD_FAILURE = "Failed to add your app";
		private const string HEADER_APPLICATION_ADD = "Add application";
		private const string HEADER_APPLICATION_UPDATE = "Update application";

		private const string MANIFEST_ICON = "icon";
		private const string MANIFEST = "manifest.json";

		private string mPendingAppPath = null;
		private string mPendingAppName = null;

		private List<ApplicationData> mAppBindings;

		private FileSystemWatcher mFileWatcher;

		static MainWindow()
		{
			RootFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CoffeeTable");
			AppsFolder = Path.Combine(RootFolder, "apps");
		}

		public MainWindow()
		{
			InitializeComponent();

			ProgressBar.Visibility = Visibility.Hidden;

			mAppBindings = new List<ApplicationData>();

			ServicePathSelector.OnPathChanged += Manifest_SaveServiceExecutablePath;

			WatchRootDirectoryForChanges();

			RefreshApplicationData();
		}

		private void WatchRootDirectoryForChanges ()
		{
			// Set up a file system watcher to refresh the application data
			// any time the coffee table directory is modified
			mFileWatcher = new FileSystemWatcher();
			mFileWatcher.Path = RootFolder;
			mFileWatcher.IncludeSubdirectories = true;

			mFileWatcher.NotifyFilter = NotifyFilters.Size | NotifyFilters.LastWrite;

			mFileWatcher.Filter = string.Empty;

			// Add event handlers.
			mFileWatcher.Changed += RefreshApplicationData;
			mFileWatcher.Created += RefreshApplicationData;
			mFileWatcher.Deleted += RefreshApplicationData;
			mFileWatcher.Renamed += RefreshApplicationData;

			// Begin watching.
			mFileWatcher.EnableRaisingEvents = true;
		}

		private async void ShowAlert (string message)
		{
			AlertFlyout_Text.Text = message;
			AlertFlyout.IsOpen = true;
			await Task.Delay(2500);
			AlertFlyout.IsOpen = false;
		}

		// Finds all the applications again and rebuilds the list of applications
		// Also refreshes the path to the service executable
		private void RefreshApplicationData(object sender = null, FileSystemEventArgs e = null)
		{
			// Invoke this method on the UI thread to insure that the FileSystemWatcher
			// properly interacts with the UI.
			Dispatcher.Invoke(new Action(() =>
			{
				// Using this try-finally loop here is a bit hacky. Solution found at:
				// https://stackoverflow.com/questions/1764809/filesystemwatcher-changed-event-is-raised-twice
				try
				{
					mFileWatcher.EnableRaisingEvents = false;
					Manifest_Refresh();
					RebuildAppList();
				}
				finally
				{
					mFileWatcher.EnableRaisingEvents = true;
				}
			}));
		}

		#region Coffee Table Manifest

		// Set up the Coffee Table file manifest by saving the path to the launcher
		// and loading the path to the service if it exists
		private void Manifest_Refresh ()
		{
			CoffeeTableManifest manifest = Extensions.GetCoffeeTableManifest();
			manifest.LauncherPath = Process.GetCurrentProcess().MainModule.FileName;
			manifest.Set();

			ServicePathSelector.FileName = manifest.ServiceExecutablePath ?? string.Empty;
		}

		private void Manifest_SaveServiceExecutablePath (object owner, RoutedEventArgs args)
		{
			CoffeeTableManifest manifest = Extensions.GetCoffeeTableManifest();
			manifest.ServiceExecutablePath = ServicePathSelector.FileName;
			manifest.Set();
		}

		#endregion

		#region Apps

		private void RebuildAppList ()
		{
			if (!Directory.Exists(AppsFolder)) return;
			mAppBindings.Clear();
			List<string> failedBindings = new List<string>();
			DirectoryInfo appsDirectory = new DirectoryInfo(AppsFolder);
			appsDirectory.Refresh();
			foreach (DirectoryInfo di in appsDirectory.GetDirectories("*", SearchOption.AllDirectories))
			{
				ApplicationData binding = GetApplicationData(di.FullName);
				if (binding == null)
				{
					failedBindings.Add(di.Name);
					continue;
				}
				mAppBindings.Add(binding);
			}

			// alphabetize this list
			mAppBindings.Sort((a, b) => a.Name.CompareTo(b.Name));

			// move the sidebar and homescreen apps to the front of the list
			mAppBindings.PrependElement(x => x.Type == ApplicationType.Homescreen);
			mAppBindings.PrependElement(x => x.Type == ApplicationType.Sidebar);

			ItemList.ItemsSource = null;
			ItemList.ItemsSource = mAppBindings;

			if (failedBindings.Count > 0)
			{
				string failedApps = string.Empty;
				for (int i = 0; i < failedBindings.Count(); i++)
				{
					failedApps += failedBindings[i];
					if (i < failedBindings.Count() - 1) failedApps += ", ";
				}
				this.ShowMessageAsync("Failed to load apps", $"Failed to show the following apps: {failedApps}");
			}
		}

		private void ItemList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count < 1)
				return;

			App_ShowFlyout(e.AddedItems[0] as ApplicationData);

			ApplicationFlyout.IsOpen = true;
			ItemList.SelectedItem = null;
		}

		private void App_ShowFlyout (ApplicationData data)
		{
			ApplicationFlyout.Tag = data;
			ApplicationFlyout.Header = data.Name;
			App_Icon.ImageSource = data.Icon;
			App_Name.Text = data.Name;
			App_Author.Text = data.Authors;
			App_Description.Text = data.Description;
		}

		private async void App_Uninstall(object sender, RoutedEventArgs e)
		{
			ApplicationData data = ApplicationFlyout.Tag as ApplicationData;

			MessageDialogResult result = await this.ShowMessageAsync("Are you sure?",
				$"Are you sure you want to uninstall {data.Name}? This cannot be undone.",
				MessageDialogStyle.AffirmativeAndNegative);
			if (result == MessageDialogResult.Affirmative)
			{
				Directory.Delete(Path.Combine(AppsFolder, data.Name.ToLower()), true);
				RebuildAppList();
				ApplicationFlyout.IsOpen = false;
				ShowAlert($"Uninstalled {data.Name}");
			}
		}

		private void App_OpenFolder(object sender, RoutedEventArgs e)
		{
			Button b = (sender as Button);
			ApplicationData data = b.Tag as ApplicationData;
			string appDirectory = Path.Combine(AppsFolder, data.Name);
			if (Directory.Exists(appDirectory)) Process.Start(appDirectory);
		}

		#endregion

		#region ApplicationData

		public class ApplicationData
		{
			public BitmapImage Icon { get; set; }
			public string Name { get; set; }
			public string Authors { get; set; }
			public string Description { get; set; }
			public ApplicationType Type;
		}

		private async Task<ApplicationData> ExtractApplicationData (string applicationPath)
		{
			// Check that file exists
			if (!File.Exists(applicationPath))
			{
				await this.ShowMessageAsync(HEADER_APPLICATION_ADD_FAILURE, "The file you selected does not exist");
				return null;
			}

			// Get the manifest file inside of the application zip
			CoffeeTable.Manifests.ApplicationManifest appManifest;
			BitmapImage appIcon;
			ZipFile zip;
			try { zip = ZipFile.Read(applicationPath); }
			catch
			{
				await this.ShowMessageAsync(HEADER_APPLICATION_ADD_FAILURE, "The file you added could not be read");
				return null;
			}
			using (zip)
			{
				// Get the manifest file
				ZipEntry manifestEntry = (from entry in zip
										  where entry.FileName.Equals(MANIFEST, StringComparison.OrdinalIgnoreCase)
										  select entry).FirstOrDefault();
				if (manifestEntry == null)
				{
					await this.ShowMessageAsync(HEADER_APPLICATION_ADD_FAILURE, "The app you added does not contain a manifest.json. Did you build it correctly?");
					return null;
				}

				string appManifestJson;
				try {
					using (var stream = new MemoryStream())
					{
						manifestEntry.Extract(stream);
						stream.Position = 0;
						using (var streamReader = new StreamReader(stream))
						{
							appManifestJson = streamReader.ReadToEnd();
						}
					}
				}
				catch
				{
					await this.ShowMessageAsync(HEADER_APPLICATION_ADD_FAILURE, "Could not read your application's manifest.");
					return null;
				}

				try { appManifest = JsonConvert.DeserializeObject<CoffeeTable.Manifests.ApplicationManifest>(appManifestJson); }
				catch (JsonException)
				{
					AddApp_BadFormattingError();
					return null;
				}

				// Validate the given manifest. If anything here isn't caught by the launcher, it will be caught
				// by the service once it is ran.
				bool badFormattingSentinel = false;
				if (string.IsNullOrWhiteSpace(appManifest.Name)) badFormattingSentinel = true;
				if (string.IsNullOrWhiteSpace(appManifest.ExecutablePath)) badFormattingSentinel = true;

				if (badFormattingSentinel)
				{
					AddApp_BadFormattingError();
					return null;
				}

				// Get the app icon image
				ZipEntry iconEntry = (from entry in zip.Entries
									  where Path.GetFileNameWithoutExtension(entry.FileName).Equals(MANIFEST_ICON, StringComparison.OrdinalIgnoreCase)
									  select entry).FirstOrDefault();
				if (iconEntry == null)
				{
					await this.ShowMessageAsync(HEADER_APPLICATION_ADD_FAILURE, "The selected app did not have an icon.");
					return null;
				}

				// get the icon type 
				using (var iconStream = new MemoryStream()) {
					iconEntry.Extract(iconStream);
					appIcon = GetAppImageFromStream(iconStream);
				}
			}

			ApplicationData dataBinding = GetAppBinding(appManifest);
			dataBinding.Icon = appIcon;

			return dataBinding;
		}

		private ApplicationData GetApplicationData (string appDirectoryPath)
		{
			if (!Directory.Exists(appDirectoryPath))
				return null;
			DirectoryInfo appDirectory = new DirectoryInfo(appDirectoryPath);
			string manifestFile = (from file in appDirectory.EnumerateFiles()
									 where file.Name.Equals(MANIFEST, StringComparison.OrdinalIgnoreCase)
									 select file).FirstOrDefault()?.FullName;
			CoffeeTable.Manifests.ApplicationManifest appManifest;
			try { appManifest = JsonConvert.DeserializeObject<CoffeeTable.Manifests.ApplicationManifest>(File.ReadAllText(manifestFile)); }
			catch { return null; }

			// Get app icon
			// First we must find the location of the icon file
			string iconFile = (from file in appDirectory.EnumerateFiles()
							   where Path.GetFileNameWithoutExtension(file.Name).Equals(MANIFEST_ICON, StringComparison.OrdinalIgnoreCase)
							   select file).FirstOrDefault()?.FullName;
			if (iconFile == null) return null;
			byte[] iconBytes;
			try { iconBytes = File.ReadAllBytes(iconFile); }
			catch { return null; }
			BitmapImage appIcon = new BitmapImage();
			using (MemoryStream iconStream = new MemoryStream(iconBytes))
			{
				appIcon = GetAppImageFromStream(iconStream);
			}

			ApplicationData binding = GetAppBinding(appManifest);
			binding.Icon = appIcon;

			return binding;
		}

		private ApplicationData GetAppBinding (CoffeeTable.Manifests.ApplicationManifest appManifest)
		{
			return new ApplicationData
			{
				Name = appManifest.Name,
				Authors = appManifest.Author,
				Description = appManifest.Description,
				Type = appManifest.Type
			};
		}

		private BitmapImage GetAppImageFromStream (MemoryStream stream)
		{
			BitmapImage appIcon = new BitmapImage();
			appIcon.BeginInit();
			appIcon.CacheOption = BitmapCacheOption.OnLoad;
			appIcon.StreamSource = stream;
			appIcon.EndInit();
			return appIcon;
		}

		private bool ApplicationExists(string appName)
		{
			return mAppBindings.Any(x => x.Name.Equals(mPendingAppName, StringComparison.OrdinalIgnoreCase));
		}

		#endregion

		#region AddApp

		private async void AddApplication_Click(object sender, RoutedEventArgs e)
		{
			// Ask the user to select a file
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Title = "Add or update an application";
			dlg.Filter = "Table App|*.tableapp;*.zip";
			dlg.CheckFileExists = true;
			dlg.CheckPathExists = true;
			dlg.Multiselect = false;
			dlg.DefaultExt = "tableapp";

			bool successful = dlg.ShowDialog() ?? false;
			if (!successful) return;

			string filePath = dlg.FileName;

			ApplicationData binding = await ExtractApplicationData(filePath);
			if (binding == null) return;

			// Everything is okay, so we will temporarily cache the manifest and app file location
			// and open the "Add Application Flyout" for the user to confirm
			mPendingAppPath = filePath;
			mPendingAppName = binding.Name;
			AddApp_ShowFlyout(binding);
		}

		private void AddApp_ShowFlyout (ApplicationData binding)
		{
			if (ApplicationExists(binding.Name))
				AddApplicationFlyout.Header = HEADER_APPLICATION_UPDATE;
			else AddApplicationFlyout.Header = HEADER_APPLICATION_ADD;

			AddApp_AppIcon.ImageSource = binding.Icon;
			AddApp_AppName.Text = binding.Name;
			AddApp_AppAuthor.Text = binding.Authors;
			AddApp_AppDescription.Text = binding.Description;

			AddApplicationFlyout.IsOpen = true;
		}

		private async void AddApp_BadFormattingError()
		{
			await this.ShowMessageAsync(HEADER_APPLICATION_ADD_FAILURE, "This app's manifest.json is improperly formatted or is missing required fields.");
		}

		private void AddApp_Confirm(object sender, RoutedEventArgs e)
		{
			// Ensure the apps folder exists
			if (!Directory.Exists(AppsFolder))
				Directory.CreateDirectory(AppsFolder);

			// Does the pending app already exist?
			// If so, we need to delete it's folder (we are updating it)
			if (ApplicationExists(mPendingAppName))
				Directory.Delete(Path.Combine(AppsFolder, mPendingAppName), true);

			// Extract file to the proper directory in the apps folder
			string newAppPath = Path.Combine(AppsFolder, mPendingAppName.ToLower());
			Directory.CreateDirectory(newAppPath);
			using (ZipFile zip = ZipFile.Read(mPendingAppPath))
			{
				ProgressBar.Visibility = Visibility.Visible;
				zip.ExtractProgress += (o, args) =>
				{
					if (args.TotalBytesToTransfer > 0)
					{
						ProgressBar.Value = 100 * args.BytesTransferred / args.TotalBytesToTransfer;
					}
				};
				zip.ExtractAll(newAppPath);
				ProgressBar.Visibility = Visibility.Hidden;
				ProgressBar.Value = 0;
			}

			AddApplicationFlyout.IsOpen = false;

			ShowAlert($"Added {mPendingAppName}");

			RebuildAppList();
		}

		private void AddApp_Cancel(object sender, RoutedEventArgs e)
		{
			AddApplicationFlyout.IsOpen = false;
		}

		#endregion

		#region Deploy

		private async void LaunchService(object sender, RoutedEventArgs e)
		{
			CoffeeTableManifest manifest = Extensions.GetCoffeeTableManifest();

			if (string.IsNullOrWhiteSpace(manifest.ServiceExecutablePath)
				|| !manifest.ServiceExecutablePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
				|| !File.Exists(manifest.ServiceExecutablePath))
			{
				await this.ShowMessageAsync(HEADER_DEPLOY_FAILURE, "A valid path to the service executable could not be found. Did you provide one in the settings page?");
				return;
			}

			// Check that there is a sidebar and a homescreen application loaded
			if (!mAppBindings.Any(x => x.Type == ApplicationType.Homescreen)
				|| !mAppBindings.Any(x => x.Type == ApplicationType.Sidebar))
			{
				await this.ShowMessageAsync(HEADER_DEPLOY_FAILURE, "No sidebar application or homescreen application could be found. Have you added them?");
				return;
			}

			try
			{
				// Start the service and exit the current application
				Process.Start(manifest.ServiceExecutablePath);
				System.Windows.Application.Current.Shutdown();
			} catch (Exception ex) {
				if (ex is Win32Exception || ex is ObjectDisposedException)
				{
					await this.ShowMessageAsync(HEADER_DEPLOY_FAILURE, "Failed to start the service process.");
					return;
				}

				throw;
			}
		}

		#endregion
	}
}
