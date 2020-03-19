using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Launchers
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class LauncherAttribute : Attribute
	{
		public string LauncherName { get; }

		public LauncherAttribute(string launcherName) => LauncherName = launcherName?.ToLower();
	}
}
