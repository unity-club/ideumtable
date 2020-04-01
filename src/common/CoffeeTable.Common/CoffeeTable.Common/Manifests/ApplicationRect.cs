using CoffeeTable.Common.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Common.Manifests
{
	public struct ApplicationRect : IEquatable<ApplicationRect>
	{
		public static readonly ApplicationRect Zero = new ApplicationRect
		{
			MinX = 0,
			MinY = 0,
			MaxX = 0,
			MaxY = 0
		};

		public float MinX { get; set; }
		public float MinY { get; set; }
		public float MaxX { get; set; }
		public float MaxY { get; set; }

		[JsonIgnore]
		public float Width => MaxX - MinX;
		[JsonIgnore]
		public float Height => MaxY - MinY;

		public override bool Equals(object obj) => obj is ApplicationRect r && Equals(r);
		public static bool operator ==(ApplicationRect left, ApplicationRect right) => left.Equals(right);
		public static bool operator !=(ApplicationRect left, ApplicationRect right) => !(left == right);

		public bool Equals(ApplicationRect other)
		{
			return other.MinX == MinX
				&& other.MinY == MinY
				&& other.MaxX == MaxX
				&& other.MaxY == MaxY;
		}

		public override int GetHashCode() => HashCode.Combine(MinX, MinY, MaxX, MaxY);
	}
}
