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
		public Rect ClosedRect { get; set; }
		/// <summary>
		/// Represents the <see cref="Rect"/> that the window occupies when this application is open and running
		/// </summary>
		public Rect OpenRect { get; set; }
	}
}
