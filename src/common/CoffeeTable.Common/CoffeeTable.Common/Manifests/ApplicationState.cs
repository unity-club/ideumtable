using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Common.Manifests
{
	/// <summary>
	/// Represents the life cycle state of an <see cref="ApplicationInstance"/>
	/// </summary>
	public enum ApplicationState
	{
		/// <summary>
		/// The application has begun to allocate resources for the running process, but either the application's process or main window has not yet opened.
		/// </summary>
		Starting = 0,
		/// <summary>
		/// The application has an active process and main window, and is actively running.
		/// </summary>
		Running = 1,
		/// <summary>
		/// The application's process and window have formally been terminated.
		/// </summary>
		Destroyed = 2
	}
}
