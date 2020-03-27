using System;
using System.Collections.Generic;
using System.Text;

namespace CoffeeTable.Common.Messaging.Handling
{
	public delegate void RequestHandler<TRequest, TResponse>(Request<TRequest> request, Response<TResponse> response);
}
