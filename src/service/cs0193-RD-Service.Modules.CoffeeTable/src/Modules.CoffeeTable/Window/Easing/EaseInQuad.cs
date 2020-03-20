using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Window.Easing
{
	public class EaseInQuad : IEase
	{
		public float Ease(float d)
		{
			return d * d;
		}
	}
}
