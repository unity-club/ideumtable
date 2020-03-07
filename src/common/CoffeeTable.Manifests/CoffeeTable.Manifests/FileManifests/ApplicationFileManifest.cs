using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Manifests
{
	/// <summary>
	/// Represents the JSON manifest containing information about an application.
	/// </summary>
	/// <remarks>
	/// This class should represent the JSON manifest file stored at the root level of the Appdata/Roaming/CoffeeTable/Apps/[ApplicationName] folder.
	/// This folder is known as the "application folder" and contains all the files needed for an application to be run by the backend service.
	/// </remarks>
	public class ApplicationFileManifest
	{
		/// <summary>
		/// The name of the application.
		/// </summary>
		/// <remarks>
		/// This serves as a case-insensitive unique identifier for the application. No two applications may have the same name.
		/// </remarks>
		public string Name { get; set; }
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
		/// The path to the icon of this application, relative to the application folder.
		/// </summary>
		public string IconPath { get; set; }
	}
}
