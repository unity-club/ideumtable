using CoffeeTable.Module.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Window
{
	public struct Rect : IEquatable<Rect>
	{
		public static readonly Rect Zero = new Rect
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

		public override bool Equals(object obj) => obj is Rect r && Equals(r);
		public override int GetHashCode() => HashCode.Combine(MinX, MinY, MaxX, MaxY);
		public static bool operator ==(Rect left, Rect right) => left.Equals(right);
		public static bool operator !=(Rect left, Rect right) => !(left == right);

		public bool Equals(Rect other)
		{
			return other.MinX == MinX
				&& other.MinY == MinY
				&& other.MaxX == MaxX
				&& other.MaxY == MaxY;
		}
	}
}
