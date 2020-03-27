using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Common.Manifests
{
	/// <summary>
	/// An enumeration representing the type of this application
	/// </summary>
	public enum ApplicationType
	{
		/// <summary>
		/// A standard application that runs on the CoffeeTable
		/// </summary>
		Application = 0,
		/// <summary>
		/// An application representing the sidebars on the CoffeeTable
		/// </summary>
		Sidebar = 1,
		/// <summary>
		/// An application representing the homescreen on the CoffeeTable
		/// </summary>
		Homescreen = 2
	}
}
