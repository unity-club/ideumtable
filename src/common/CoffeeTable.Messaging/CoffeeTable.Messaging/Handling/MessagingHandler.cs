using CoffeeTable.Messaging.Core;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace CoffeeTable.Messaging.Handling
{
	public class MessagingHandler : IMessagingHandler
	{
		private class RequestHandlerInfo {
			public object Owner;
			public RequestHandlerAttribute Attr;
			public Delegate Invoker;
			public Type RequestType;
			public Type ResponseType;
		}

		private static readonly string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
		private static readonly Regex RequestNameRegex = new Regex(@"^[_\-a-zA-Z0-9]+$");

		private Action<Message> mConnectionBinder;
		private Dictionary<uint, Exchange> mPendingExchangesMap = new Dictionary<uint, Exchange>();
		private Dictionary<string, List<RequestHandlerInfo>> mRequestHandlersMap = new Dictionary<string, List<RequestHandlerInfo>>();
		private HashSet<MethodInfo> mRegisteredMethods = new HashSet<MethodInfo>();

		public int Timeout { get; set; } = 1000;

		public MessagingHandler(Action<Message> connectionBinder)
		{
			mConnectionBinder = connectionBinder;
		}

		public Exchange<T> Send<T>(uint destinationId, string requestName, object data = null)
		{
			Exchange<T> exchange = new Exchange<T>(Timeout);
			Message pendingMessage = new Message();

			pendingMessage.Request = requestName?.ToLower();
			pendingMessage.DestinationId = destinationId;
			if (data != null) pendingMessage.Data = JsonConvert.SerializeObject(data);

			exchange.Requested = pendingMessage.Sent;
			exchange.CorrelationId = pendingMessage.Id;

			mPendingExchangesMap[pendingMessage.Id] = exchange;

			mConnectionBinder?.Invoke(pendingMessage);

			return exchange;
		}

		public Exchange<Null> Send(uint destinationId, string requestName, object data = null)
			=> Send<Null>(destinationId, requestName, data);

		public void Receive(Message message)
		{
			// Each time a message is received, we will scan the list of pending exchanges
			// to see whether or not any of them have timed out, and remove them.
			DateTime now = DateTime.Now;
			foreach (uint correlationId in mPendingExchangesMap.Keys.ToList())
			{
				Exchange exchange = mPendingExchangesMap[correlationId];
				if ((now - exchange.Requested).Milliseconds > exchange.Timeout)
					mPendingExchangesMap.Remove(correlationId);
			}

			message.Received = now;

			/* Note: The following code is a bit heavy on the use of reflection.
			 * This is to work around my inability to statically type the references
			 * to the generic exchanges. I am open to suggestions on how to do this in a
			 * way more conducive to static typing */

			// There are two situations that can occur when a message is received.
			// (1) The message is a request to do something
			// (2) The message is a response to a request that we made (i.e. completing a pending exchange)
			if (message.CorrelationId > 0)
			{
				// This message is (supposedly) a response to a request we made
				if (mPendingExchangesMap.TryGetValue(message.CorrelationId, out Exchange exchange))
				{
					// Get property infos via reflection
					Type exchangeType = exchange.GetType();
					Type exchangeDataType = exchangeType.GetGenericArguments()[0];

					exchange.Complete = true;
					exchange.Success = message.Success;
					exchange.Details = message.Details;
					exchange.Completed = now;

					// Deserializing JSON
					// If we failed to deserialize JSON, we fail to receive the response to this exchange
					object data = null;
					if (!exchangeDataType.Equals(Null.NullType))
					{
						try { data = JsonConvert.DeserializeObject(message.Data, exchangeDataType); }
						catch (JsonException)
						{
							exchange.Success = false;
							exchange.Details = $"Failed to deserialize incoming response data. " +
								$"Expected to receive a {exchangeDataType.Name}, but received an object that could not be deserialized into this type. " +
								$"Are you asking for the correct type?";
						}
					}

					// Succeeded in deserializing, now set data and call receiving delegate
					if (data != null)
						exchange.Property_Data.SetValue(exchange, data);

					// Call OnComplete delegate no matter what
					Delegate onCompleteDelegate = exchange.Field_OnCompleted.GetValue(exchange) as Delegate;
					onCompleteDelegate?.DynamicInvoke(exchange);

					// Conditionally call success and failure delegates
					if (exchange.Success)
					{
						Delegate onSuccessDelegate = exchange.Field_OnSucceeded.GetValue(exchange) as Delegate;
						onSuccessDelegate?.DynamicInvoke(exchange);
					} else
					{
						Delegate onFailedDelegate = exchange.Field_OnFailed.GetValue(exchange) as Delegate;
						onFailedDelegate?.DynamicInvoke(exchange);
					}

					// Remove this response from the pending exchanges map
					mPendingExchangesMap.Remove(message.CorrelationId);
				}
			}
			else
			{
				// This message is (supposedly) a request
				if (mRequestHandlersMap.TryGetValue(message.Request.ToLower(), out List<RequestHandlerInfo> list))
				{
					for (int i = 0; i < list.Count(); i++)
					{
						RequestHandlerInfo requestHandlerInfo = list[i];

						// Does this request really need deserialized data? If so, let's get it.
						// Once again, if we fail to deserialize, we fail to receive
						object requestData = null;
						if (!requestHandlerInfo.RequestType.Equals(Null.NullType))
						{
							try { requestData = JsonConvert.DeserializeObject(message.Data, requestHandlerInfo.RequestType); }
							catch (JsonException) { continue; }
						}

						// Create the request object to be given to the handler invoker
						Type requestType = Request.GenericType.MakeGenericType(requestHandlerInfo.RequestType);
						Request request = Activator.CreateInstance(requestType) as Request;
						request.SenderName = message.SenderName;
						request.SenderId = message.SenderId;
						request.Sent = message.Sent;
						request.Received = message.Received ?? now;

						// Give data to the request object if we need to
						if (requestData != null)
							request.Property_Data.SetValue(request, requestData);

						// Create the response object to be given to the handler invoker
						Type responseType = Response.GenericType.MakeGenericType(requestHandlerInfo.ResponseType);
						Response response = Activator.CreateInstance(responseType) as Response;

						// Invoke the delegate
						requestHandlerInfo.Invoker.DynamicInvoke(request, response);

						// Get the data after calling delegate
						object responseData = null;
						if (!requestHandlerInfo.ResponseType.Equals(Null.NullType))
							responseData = response.Property_Data.GetValue(response);

						Message responseMessage = new Message()
						{
							CorrelationId = message.Id,
							DestinationId = message.SenderId,
							Success = response.Success,
							Details = response.Details,
							Data = JsonConvert.SerializeObject(responseData)
						};
						
						mConnectionBinder?.Invoke(responseMessage);
					}
				}
			}
		}

		public void Register(object o)
		{
			if (o == null) return;
			Type type = o.GetType();
			var instanceMethods = from method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
								  let attr = method.GetCustomAttribute<RequestHandlerAttribute>()
								  where attr != null
								  select new { Method = method, Attribute = attr };
			foreach (var methodPair in instanceMethods)
				RegisterMethod(methodPair.Method, methodPair.Attribute, o);

			Register(type);
		}

		public void Register<T>() => Register(typeof(T));

		public void Register(Type t)
		{
			if (t == null) return;
			var staticMethods = from method in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
								let attr = method.GetCustomAttribute<RequestHandlerAttribute>()
								where attr != null
								select new { Method = method, Attribute = attr };
			foreach (var methodPair in staticMethods)
				RegisterMethod(methodPair.Method, methodPair.Attribute, null);
		}

		private void RegisterMethod(MethodInfo method, RequestHandlerAttribute attribute, object owner)
		{
			if (mRegisteredMethods.Contains(method)) return;
			mRegisteredMethods.Add(method);

			// Verify request name
			if (string.IsNullOrWhiteSpace(attribute.RequestName) || !(RequestNameRegex.IsMatch(attribute.RequestName)))
				ThrowInvalidMethodSignatureException(method, "has an invalid request name. Request names are case-insensitive, and must consist only of alphanumeric characters, hyphens and underscores");

			// Verify request handler method signature
			ParameterInfo[] parameters = method.GetParameters();
			if (parameters.Length != 2)
				ThrowInvalidMethodSignatureException(method, $"has an invalid method signature. Request handler methods must have only two parameters, {nameof(Request)} and {nameof(Response)}.");
			if (IsRefInOut(parameters[0]) || IsRefInOut(parameters[0]))
				ThrowInvalidMethodSignatureException(method, "has an invalid method signature. The parameters in a request handler method cannot be marked as in, out, or pass by reference.");
			if (!IsGenericType(parameters[0].ParameterType, Request.GenericType) || !IsGenericType(parameters[1].ParameterType, Response.GenericType))
				ThrowInvalidMethodSignatureException(method, $"has an invalid method signature. " +
					$"The first parameter must be a {nameof(Request)}, whose generic type represents the type of data this request will receive. " +
					$"The second parameter must be a {nameof(Response)}, whose generic type represents the type of data this request will respond with. " +
					$"Either type parameter can be the {nameof(Null)} type if the request handler should not receive or respond, respectively, with any data.");
			if (!method.ReturnType.Equals(typeof(void)))
				ThrowInvalidMethodSignatureException(method, "has an invalid method signature. Request handlers should not return anything and should have a void return type.");

			Type requestType = parameters[0].ParameterType.GetGenericArguments()[0];
			Type responseType = parameters[1].ParameterType.GetGenericArguments()[0];

			// Check that response data type is concrete
			if (!IsConcrete(responseType))
				ThrowInvalidMethodSignatureException(method, $"has an invalid method signature. The response type parameter, {responseType}, is not a concrete class. Response type parameters must be concrete so that the deserializer knows what type to deserialize into.");

			// Create delegate type
			Type delegateType = typeof(RequestHandler<,>).MakeGenericType(requestType, responseType);

			// Create delegate
			Delegate callback;
			if (owner == null) callback = method.CreateDelegate(delegateType);
			else callback = method.CreateDelegate(delegateType, owner);

			// Create request handler info
			RequestHandlerInfo requestHandlerInfo = new RequestHandlerInfo
			{
				Owner = owner,
				Attr = attribute,
				Invoker = callback,
				RequestType = requestType,
				ResponseType = responseType
			};

			if (!mRequestHandlersMap.TryGetValue(attribute.RequestName, out var list))
			{
				list = new List<RequestHandlerInfo>();
				mRequestHandlersMap[attribute.RequestName] = list;
			}

			// Add the request handler to the list and sort by reverse of priority levels
			// (High priority handlers at start of list, low priority handlers at end)
			list.Add(requestHandlerInfo);
			list.Sort((a, b) => -a.Attr.Priority.CompareTo(b.Attr.Priority));
		}

		private bool IsGenericType (Type type, Type genericType)
		{
			if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(genericType)) return true;
			return false;
		}

		private bool IsRefInOut(ParameterInfo info)
		{
			return info.IsOut || info.IsIn || info.ParameterType.IsByRef;
		}

		private bool IsConcrete (Type type)
		{
			return !type.IsInterface && !type.IsAbstract;
		}

		private void ThrowInvalidMethodSignatureException(MethodInfo method, string message)
		{
			throw new ArgumentException($"{AssemblyName}: Method {GetFormattedMethodName(method)} marked with {nameof(RequestHandlerAttribute)} {message} Refer to the documentation.");
		}

		private string GetFormattedMethodName(MethodInfo method)
		{
			ParameterInfo[] parameters = method.GetParameters();
			string parametersList = string.Empty;
			for (int i = 0; i < parameters.Length; i++)
			{
				parametersList += parameters[i];
				if (i < parameters.Length - 1)
					parametersList += ", ";
			}
			return $"{method.Name} ({parametersList})";
		}
	}
}
