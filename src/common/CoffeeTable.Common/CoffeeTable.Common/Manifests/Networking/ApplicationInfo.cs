using CoffeeTable.Common.Manifests.Networking.Comparers;
using CoffeeTable.Common.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Common.Manifests.Networking
{
	public class ApplicationInfo : IEquatable<ApplicationInfo>
	{
		/// <summary>
		/// An <see cref="IEqualityComparer{T}"/> that compares <see cref="ApplicationInfo"/> objects to see if their <see cref="AppId"/> properties are equal.
		/// </summary>
		public static readonly IEqualityComparer<ApplicationInfo> IdComparer = new ApplicationInfoComparer();

		public uint AppId { get; set; }
		public string Name { get; set; }
		[JsonConverter(typeof(StringEnumConverter))]
		public ApplicationType Type { get; set; }
		public string Author { get; set; }
		public string Description { get; set; }
		public string IconPath { get; set; }
		public bool LaunchInFullscreen { get; set; }

		public override bool Equals (object obj)
		{
			if (obj == null) return false;
			if (!(obj is ApplicationInfo info)) return false;
			return info.AppId == AppId
				&& string.Equals(info.Name, Name, StringComparison.OrdinalIgnoreCase)
				&& info.Type == Type
				&& string.Equals(info.Author, Author)
				&& string.Equals(info.Description, Description)
				&& string.Equals(info.IconPath, IconPath)
				&& info.LaunchInFullscreen == LaunchInFullscreen;
		}

		public bool Equals(ApplicationInfo other) => Equals(other as object);

		public override int GetHashCode() =>
			HashCode.Combine(AppId,
				Name?.ToLower(),
				Type,
				Author,
				Description,
				IconPath,
				LaunchInFullscreen);

		public static bool Equals (ApplicationInfo objA, ApplicationInfo objB)
		{
			if (objA == null)
			{
				if (objB != null) return false;
				else return true;
			}
			else return objA.Equals(objB);
		}
	}
}
