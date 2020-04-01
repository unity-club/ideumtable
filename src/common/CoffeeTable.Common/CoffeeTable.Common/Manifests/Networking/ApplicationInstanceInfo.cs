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
	public class ApplicationInstanceInfo : IEquatable<ApplicationInstanceInfo>
	{
		/// <summary>
		/// An <see cref="IEqualityComparer{T}"/> that compares <see cref="ApplicationInstanceInfo"/> objects to see if their <see cref="ApplicationInstanceInfo.DestinationId"/> properties are equal.
		/// </summary>
		public static readonly IEqualityComparer<ApplicationInstanceInfo> IdComparer = new ApplicationInstanceInfoComparer();

		public ApplicationInfo AppInfo { get; set; }
		public uint DestinationId { get; set; }
		[JsonConverter(typeof(StringEnumConverter))]
		public ApplicationLayout Layout { get; set; }
		public ConnectionStatus Connection { get; set; }
		[JsonConverter(typeof(StringEnumConverter))]
		public ApplicationState State { get; set; }
		public ApplicationRect Rect { get; set; }
		public bool IsSimulator { get; set; }

		public override bool Equals(object obj)
		{
			if (obj == null) return false;
			if (!(obj is ApplicationInstanceInfo info)) return false;
			return ApplicationInfo.Equals(info.AppInfo, AppInfo)
				&& info.DestinationId == DestinationId
				&& info.Layout == Layout
				&& info.Connection == Connection
				&& info.State == State
				&& info.Rect == Rect
				&& info.IsSimulator == IsSimulator;
		}

		public bool Equals(ApplicationInstanceInfo other) => Equals(other as object);

		public override int GetHashCode() => 
			HashCode.Combine(AppInfo,
				DestinationId,
				Layout,
				Connection,
				State,
				Rect,
				IsSimulator);

		public static bool Equals(ApplicationInstanceInfo objA, ApplicationInstanceInfo objB)
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
