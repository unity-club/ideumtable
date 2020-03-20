using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Window.Easing
{
	public class EaseOutQuad : IEase
	{
		public float Ease(float d)
		{
			return 1.0f - (d - 1.0f) * (d - 1.0f);
		}
	}
}
