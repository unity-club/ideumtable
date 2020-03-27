using System;
using System.Collections.Generic;
using System.Text;

namespace CoffeeTable.Common.Messaging.Handling
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public class RequestHandlerAttribute : Attribute
	{
		/// <summary>
		/// The name of the request that the method marked with this attribute handles.
		/// </summary>
		public string RequestName { get; private set; }
		/// <summary>
		/// The priority of this request. If there are multiple methods registered for a request, those with a higher <see cref="Priority"/> value will be executed first.
		/// </summary>
		public int Priority { get; set; }

		public RequestHandlerAttribute(string requestName)
		{
			RequestName = requestName?.ToLower();
		}
	}
}
