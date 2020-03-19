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
		public int MinX { get; set; }
		public int MinY { get; set; }
		public int MaxX { get; set; }
		public int MaxY { get; set; }

		public int Width => MaxX - MinX;
		public int Height => MaxY - MinY;

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
