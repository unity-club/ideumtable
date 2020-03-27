using CoffeeTable.Common.Manifests;
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

		public static CoffeeTableManifest GetCoffeeTableManifest()
		{
			if (!File.Exists(ManifestPath)) return new CoffeeTableManifest();
			else
			{
				string json = File.ReadAllText(ManifestPath);
				try { return JsonConvert.DeserializeObject<CoffeeTableManifest>(json); }
				catch (JsonException) { return new CoffeeTableManifest(); }
			}
		}

		public static void Set(this CoffeeTableManifest manifest)
		{
			if (!Directory.Exists(ManifestPathRoot)) Directory.CreateDirectory(ManifestPathRoot);
			string json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
			File.WriteAllText(ManifestPath, json);
		}

		/// <summary>
		/// Moves an element in the given list to the front of the list according to the given query. Stops after the first element in the list that matches the query is found.
		/// </summary>
		/// <typeparam name="T">The type of each element in the list</typeparam>
		/// <param name="list">The containing list</param>
		/// <param name="query">A query to determine which item to move. <paramref name="query"/> should return <c>true</c> when the item to be moved is found.</param>
		public static void PrependElement<T> (this IList<T> list, Func<T, bool> query)
		{
			if (list == null) throw new ArgumentException("You must pass in a non-null list!");
			if (query == null) throw new ArgumentException("You must pass a non-null query!");
			for (int i = 0; i < list.Count(); i++)
			{
				T item = list[i];
				if (query.Invoke(item))
				{
					list.RemoveAt(i);
					list.Insert(0, item);
					return;
				}
			}
		}
	}
}
