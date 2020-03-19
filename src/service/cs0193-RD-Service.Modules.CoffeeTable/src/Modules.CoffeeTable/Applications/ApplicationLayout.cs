using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Applications
{
	public enum ApplicationLayout
	{
		/// <summary>
		/// Signifies that this <see cref="ApplicationInstance"/> object occupies the left portion of the screen.
		/// </summary>
		LeftPanel,
		/// <summary>
		/// Signifies that this <see cref="ApplicationInstance"/> object occupies the right portion of the screen.
		/// </summary>
		RightPanel,
		/// <summary>
		/// Signifies that this <see cref="ApplicationInstance"/> occupies the full screen area.
		/// </summary>
		Fullscreen
	}
}
