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

		public float Width => MaxX - MinX;
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

		/*
		 * See https://stackoverflow.com/a/34006336/10149816
		 * for information concerning best algorithms for generation of
		 * hashcodes that produce the fewest collisions.
		 */
		public override int GetHashCode()
		{
			const int seed = 1009;
			const int factor = 9176;

			unchecked
			{
				int hash = seed;
				hash = (hash * factor) + MinX.GetHashCode();
				hash = (hash * factor) + MinY.GetHashCode();
				hash = (hash * factor) + MaxX.GetHashCode();
				hash = (hash * factor) + MaxY.GetHashCode();
				return hash;
			}
		}
	}
}
