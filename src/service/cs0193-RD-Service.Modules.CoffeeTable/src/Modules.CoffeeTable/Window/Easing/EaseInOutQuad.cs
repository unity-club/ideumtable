using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Window.Easing
{
	public class EaseInOutQuad : IEase
	{
		public float Ease(float d)
		{
			if (d <= 0.5f)
				return 2.0f * d * d;
			return -2.0f * (1.0f - d) * (1.0f - d) + 1.0f;
		}
	}
}
