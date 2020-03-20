using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Window.Easing
{
	public interface IEase
	{
		/// <summary>
		/// Represents an easing function.
		/// </summary>
		/// <param name="duration">A normalized duration parameter between 0 and 1</param>
		/// <returns>A normalized output parameter between 0 and 1</returns>
		float Ease(float duration);
	}
}
