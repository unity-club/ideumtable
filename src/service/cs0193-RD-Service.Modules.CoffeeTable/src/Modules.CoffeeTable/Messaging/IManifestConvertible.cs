using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Messaging
{
	public interface IManifestConvertible<T>
	{
		T ToManifest();
	}
}
