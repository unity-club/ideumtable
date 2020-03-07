using CoffeeTable.Manifests;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTableLauncher
{
	static class Extensions
	{
		private static readonly string ManifestPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CoffeeTable", "coffeetable.json");
		private static readonly string ManifestPathRoot = Path.GetPathRoot(ManifestPath);

		public static CoffeeTableFileManifest GetCoffeeTableManifest()
		{
			if (!File.Exists(ManifestPath)) return new CoffeeTableFileManifest();
			else
			{
				string json = File.ReadAllText(ManifestPath);
				try { return JsonConvert.DeserializeObject<CoffeeTableFileManifest>(json); }
				catch (JsonException) { return new CoffeeTableFileManifest(); }
			}
		} 

		public static void Set(this CoffeeTableFileManifest manifest)
		{
			if (!Directory.Exists(ManifestPathRoot)) Directory.CreateDirectory(ManifestPathRoot);
			string json = JsonConvert.SerializeObject(manifest);
			File.WriteAllText(ManifestPath, json);
		}
	}
}
