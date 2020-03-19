using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Manifests
{
	/// <summary>
	/// Represents information about an application
	/// </summary>
	/// <remarks>
	/// This class should represent the JSON manifest file stored at the root level of the Appdata/Roaming/CoffeeTable/Apps/[ApplicationName] folder.
	/// This folder is known as the "application folder" and contains all the files needed for an application to be run by the backend service.
	/// </remarks>
	public class ApplicationManifest
	{
		/// <summary>
		/// The name of the application.
		/// </summary>
		/// <remarks>
		/// This serves as a case-insensitive unique identifier for the application. No two applications may have the same name.
		/// </remarks>
		public string Name { get; set; }
		/// <summary>
		/// The type that this application is.
		/// </summary>
		[JsonConverter(typeof(StringEnumConverter))]
		public ApplicationType Type { get; set; }
		/// <summary>
		/// Whether this application should be launched in fullscreen or halfscreen mode.
		/// </summary>
		public bool LaunchInFullscreen { get; set; }
		/// <summary>
		/// The author(s) of the application.
		/// </summary>
		public string Author { get; set; }
		/// <summary>
		/// A short description about the application.
		/// </summary>
		public string Description { get; set; }
		/// <summary>
		/// The path to the executable file of this application, relative to the application folder.
		/// </summary>
		public string ExecutablePath { get; set; }
		/// <summary>
		/// A2019 string identifying which subroutine the backend service should use to launch the application.
		/// </summary>
		public string LauncherName { get; set; }
	}
}
