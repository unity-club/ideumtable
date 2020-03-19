using CoffeeTable.Module.Applications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Window
{
	public class ApplicationWindow
	{
		/// <summary>
		/// This current window <see cref="Rect"/>
		/// </summary>
		public Rect WindowRect { get; set; }
		/// <summary>
		/// Represents the <see cref="Rect"/> that the window occupies when this application is closed
		/// </summary>
		public Rect ClosedRect { get; }
		/// <summary>
		/// Represents the <see cref="Rect"/> that the window occupies when this application is open and running
		/// </summary>
		public Rect OpenRect { get; }

		/// <summary>
		/// Configures the window's <see cref="ClosedRect"/> and <see cref="OpenRect"/> properties to correspond with the given application instance's desired layout.
		/// </summary>
		/// <param name="instance"></param>
		public void Configure (ApplicationInstance instance)
		{

		}
	}
}
