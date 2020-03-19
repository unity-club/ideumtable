using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Applications
{
	/// <summary>
	/// Represents the life cycle state of an <see cref="ApplicationInstance"/>
	/// </summary>
	public enum ApplicationState
	{
		/// <summary>
		/// The <see cref="ApplicationInstance"/> object has been created but does not yet have an active process.
		/// </summary>
		Uninitialized = 0,
		/// <summary>
		/// The <see cref="ApplicationInstance"/> object has been created and has a live process, but is still being opened by the service.
		/// </summary>
		Starting = 1,
		/// <summary>
		/// The <see cref="ApplicationInstance"/> object is currently running.
		/// </summary>
		Running = 2,
		/// <summary>
		/// The <see cref="ApplicationInstance"/> is closing and its process will soon be terminated.
		/// </summary>
		Stopping = 3,
		/// <summary>
		/// The <see cref="ApplicationInstance"/> object is no longer running and its process has been terminated.
		/// </summary>
		Destroyed = 4
	}
}
