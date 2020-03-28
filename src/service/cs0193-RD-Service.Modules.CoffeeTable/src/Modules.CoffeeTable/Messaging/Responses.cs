using CoffeeTable.Common.Manifests.Networking;
using Ideum.Networking.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Messaging
{
	public class GenericResponse<T> : StandardResponse
	{
		public T Result { get; set; }
	}
}
