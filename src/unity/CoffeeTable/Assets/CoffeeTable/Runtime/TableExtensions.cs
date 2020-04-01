using CoffeeTable.Common.Manifests.Networking;
using CoffeeTable.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable
{
	public static class TableExtensions
	{
		public static ApplicationInstanceInfo GetRunningApp(this ApplicationsManifest manifest, uint id)
		{
			if (manifest == null) return null;
			return manifest.RunningApplications.GetAppInstance(id);
		}

		public static IEnumerable<ApplicationInstanceInfo> GetRunningApps(this ApplicationsManifest manifest, string appName)
		{
			if (manifest == null) return null;
			return manifest.RunningApplications.GetAppInstances(appName);
		}

		public static ApplicationInstanceInfo GetRunningApp(this ApplicationsManifest manifest, string appName)
		{
			if (manifest == null) return null;
			return manifest.RunningApplications.GetAppInstance(appName);
		}

		public static ApplicationInfo GetInstalledApp(this ApplicationsManifest manifest, string appName)
		{
			if (manifest == null) return null;
			return manifest.InstalledApplications.GetApp(appName);
		}

		public static ApplicationInstanceInfo GetAppInstance(this ApplicationInstanceInfo[] instances, uint id)
		{
			if (instances == null) return null;
			return instances.Where(i => i.DestinationId == id).FirstOrDefault();
		}

		public static IEnumerable<ApplicationInstanceInfo> GetAppInstances(this ApplicationInstanceInfo[] instances, string appName)
		{
			if (instances == null) return null;
			return instances.Where(i => string.Equals(appName, i.AppInfo.Name, StringComparison.OrdinalIgnoreCase));
		}

		public static ApplicationInstanceInfo GetAppInstance(this ApplicationInstanceInfo[] instances, string appName)
		{
			return GetAppInstances(instances, appName).FirstOrDefault();
		}

		public static ApplicationInfo GetApp(this ApplicationInfo[] apps, string appName)
		{
			if (apps == null) return null;
			return apps.Where(a => string.Equals(appName, a.Name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
		}

		/// <summary>
		/// Returns true if this <see cref="ApplicationInstanceInfo"/> object represents this application.
		/// </summary>
		/// <param name="info">An <see cref="ApplicationInstanceInfo"/> object.</param>
		/// <returns>A boolean indicating whether or not this <see cref="ApplicationInstanceInfo"/> object represents this application.</returns>
		public static bool IsSelf (this ApplicationInstanceInfo info)
		{
			if (!Table.IsOnline)
			{
				Log.Warn("Invalid operation: a connection to the table has not been established.");
				return false;
			}
			if (info == null) return false;
			return Table.CurrentApp.DestinationId == info.DestinationId;
		}

		/// <summary>
		/// Returns an enumerable of changes in the elements between two enumerables.
		/// </summary>
		/// <remarks>
		/// This method will attempt to find the changes in the elements of two enumerations, <param name="oldElements"/> and <param name="newElements"/>,
		/// where an injective relationship exists between <paramref name="oldElements"/> and <paramref name="newElements"/>. That is to say that for 
		/// an element <c>a</c> in <paramref name="oldElements"/>, there exists at most one element <c>b</c> in <paramref name="newElements"/> such that
		/// <c>a</c> can be mapped to <c>b</c>. This relationship is defined by <paramref name="linkerPredicate"/>, whereby <paramref name="linkerPredicate"/><c>(a, b)</c>
		/// returns true. This method will find all pairs <c>(a, b)</c> such that <paramref name="isModified"/><c>(a, b)</c> returns true. Verbally, this
		/// method will find all pairs of related elements that have been modified in some way.
		/// 
		/// This method additionally expects that the elements of <paramref name="oldElements"/> and <paramref name="newElements"/> are unique and distinct from
		/// one another, but if this is not the case (i.e. <paramref name="oldElements"/> or <paramref name="oldElements"/> have multiple identical elements), then
		/// the sets containing multiple identical elements (as defined by <see cref="object.Equals(object, object)"/>) will be flattened such that the first unique
		/// element in a subgroup of identical elements is used for the purposes of the injective relationship descriped above.
		/// 
		/// If null values are present in <paramref name="oldElements"/> or <paramref name="newElements"/>, this method will pretend as if those collections did not have null-values at all.
		/// </remarks>
		/// <typeparam name="TSource"></typeparam>
		/// <param name="oldElements">An enumeration of elements that may have been modified.</param>
		/// <param name="newElements">An enumeration of elements that contains potentially mutated elements of <paramref name="oldElements"/></param>
		/// <param name="linkerPredicate">A function that relates an element of <paramref name="oldElements"/> with an element of <paramref name="newElements"/> (see remarks).</param>
		/// <param name="isModified">A function that determines whether or not an element of <paramref name="newElements"/> is a modified version of an element of <paramref name="oldElements"/></param>
		/// <returns>An enumeration of modified pairs of elements, each containing the unmodified (Old) and modified (New) <typeparamref name="TSource"/> objects.</returns>
		internal static IEnumerable<(TSource Old, TSource New)> GetModified<TSource> (
			IEnumerable<TSource> oldElements,
			IEnumerable<TSource> newElements,
			Func<TSource, TSource, bool> linkerPredicate,
			Func<TSource, TSource, bool> isModified)
		{
			if (oldElements == null) throw new ArgumentException(nameof(oldElements));
			if (newElements == null) throw new ArgumentException(nameof(newElements));
			Dictionary<TSource, TSource> dict = oldElements.Distinct()
				.Select(oldElement =>
				new
				{
					Key = oldElement,
					Value = newElements.Where(newElement => linkerPredicate?.Invoke(oldElement, newElement) ?? false).FirstOrDefault()
				}).Where(pair => pair.Key != null && pair.Value != null)
				.ToDictionary(pair => pair.Key, pair => pair.Value);

			foreach (var oldElement in oldElements)
				if (dict.TryGetValue(oldElement, out TSource newElement))
					if (isModified?.Invoke(oldElement, newElement) ?? false)
						yield return (oldElement, newElement);
		}
	}
}
