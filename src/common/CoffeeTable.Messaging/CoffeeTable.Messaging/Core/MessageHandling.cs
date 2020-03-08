using CoffeeTable.Messaging.Core;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CoffeeTable.Messaging.Core
{
	/// <summary>
	/// Represents a null or void type parameter.
	/// </summary>
	public sealed class Null
	{
		private Null() { }

		public override bool Equals(object obj)
		{
			return obj == null || obj is Null;
		}

		public override int GetHashCode()
		{
			return 0;
		}

		public override string ToString()
		{
			return base.ToString();
		}
	}

	/// <summary>
	/// Identifies a method that should be called when a <see cref="Message"/> is received with the corresponding <see cref="RequestName"/>
	/// </summary>
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

	/// <summary>
	/// Represents a response to an out-bound <see cref="Message"/> that may have not yet been received.
	/// </summary>
	public abstract class Response
	{
		/// <summary>
		/// Gets or sets a value indicating whether a response has been received.
		/// </summary>
		public bool IsReceived { get; set; }
		/// <summary>
		/// Gets a value indicating the ID of the out-bound <see cref="Message"/> that elicits this response.
		/// </summary>
		public uint CorrelationId { get; protected set; }
		/// <summary>
		/// Gets a value indicating the time that this application requested the response.
		/// </summary>
		public DateTime Sent { get; protected set; }
		/// <summary>
		/// Gets or sets a value indicating the time that this application received the response.
		/// </summary>
		public DateTime? Received { get; set; }
		/// <summary>
		/// Gets a value indicating the number of milliseconds after which this application is no longer expecting a response.
		/// </summary>
		public int Timeout { get; protected set; }
		/// <summary>
		/// Gets a value indicating the number of milliseconds it took for this response to be received after the initial request was made. Returns 0 if the response has not yet been received.
		/// </summary>
		public int Delay => Received == null ? 0 : ((Received ?? DateTime.Now) - Sent).Milliseconds;
	}

	/// <summary>
	/// Represents a response to an out-bound <see cref="Message"/> carrying data that may have not yet been received.
	/// </summary>
	/// <typeparam name="T">The type of data that this response will return.</typeparam>
	public sealed class Response<T> : Response
	{
		/// <summary>
		/// Gets or sets a value containing the data received by this <see cref="Response"/>.
		/// </summary>
		public T Data { get; set; }
		/// <summary>
		/// Gets an <see cref="Action"/> that will be called when the data is received. This value can be null.
		/// </summary>
		public Action<T> OnReceived { get; private set; }

		public Response(Message message, int timeout, Action<T> onReceived)
		{
			Timeout = timeout;
			CorrelationId = message.Id;
			OnReceived = onReceived;
			Sent = message.Info.SentTime;
		}
	}

	/// <summary>
	/// Represents a message handler that binds request names and message responses to method invocations in user code.
	/// </summary>
	[System.Diagnostics.DebuggerNonUserCode]
	public class MessagingHandler
	{
		private static readonly string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
		private static readonly Regex RequestNameRegex = new Regex(@"^[_\-a-zA-Z0-9]+$");

		private delegate TResponse RequestDelegate<TRequest, TResponse>(Message.MessageInfo info, TRequest data);
		private delegate TResponse RequestDelegate<TResponse>(Message.MessageInfo info);
		private delegate void RequestDelegateVoid<TRequest>(Message.MessageInfo info, TRequest data);
		private delegate void RequestDelegateVoid(Message.MessageInfo info);

		protected class Request
		{
			public object Owner { get; set; }
			public RequestHandlerAttribute Attr { get; set; }
			public Type ReturnType { get; set; }
			public Type DataType { get; set; }
			public bool HasDataParameter { get; set; }
			public Delegate Invoker { get; set; }
			public bool IsStatic => Owner == null;
		}

		public int Timeout { get; set; } = 1000;

		private Action<Message> mMessageConnectionBinder;
		private Dictionary<uint, Response> mResponsesMap;
		private Dictionary<string, List<Request>> mRequestMap;
		private HashSet<MethodInfo> mRegisteredMethods;

		/// <summary>
		/// Constructs a messaging handler.
		/// </summary>
		/// <param name="messageConnectionBinder">An <see cref="Action"/> that, when invoked with a <see cref="Message"/> object, actually sends a message to a client application.</param>
		public MessagingHandler(Action<Message> messageConnectionBinder)
		{
			mMessageConnectionBinder = messageConnectionBinder;

			mResponsesMap = new Dictionary<uint, Response>();
			mRequestMap = new Dictionary<string, List<Request>>();
			mRegisteredMethods = new HashSet<MethodInfo>();
		}

		/// <summary>
		/// Sends a message that expects a response.
		/// </summary>
		/// <typeparam name="TResponse">The data which should be received when the application identified by <paramref name="destinationId"/> responds.</typeparam>
		/// <param name="request">The name of the request that should be executed by the client application.</param>
		/// <param name="destinationId">The unique identifier representing the client application where this request should be sent.</param>
		/// <param name="data">An object containing any data that should be sent to the client application when delivering this request. Can be null.</param>
		/// <param name="onReceived">An action that is invoked when the requested data is received.</param>
		/// <returns>A <see cref="Response{T}"/> object containing information about the status of the requested data.</returns>
		public Response<TResponse> Send<TResponse>(string request, uint destinationId, object data, Action<TResponse> onReceived)
		{
			Message pending = new Message(request, destinationId, JsonConvert.SerializeObject(data));

			// Get the response object
			Response<TResponse> response;
			if (typeof(TResponse).Equals(typeof(Null))) response = null;
			else response = new Response<TResponse>(pending, Timeout, onReceived);

			// Add the response to the list of pending responses if we are expecting a response
			if (response != null) mResponsesMap[response.CorrelationId] = response;

			// Actually send the message using implementation given when this object was constructed
			mMessageConnectionBinder?.Invoke(pending);

			return response;
		}

		/// <summary>
		/// Sends a message with no data that expects a response.
		/// </summary>
		/// <typeparam name="TResponse">The data which should be received when the application identified by <paramref name="destinationId"/> responds.</typeparam>
		/// <param name="request">The name of the request that should be executed by the client application.</param>
		/// <param name="destinationId">The unique identifier representing the client application where this request should be sent.</param>
		/// <param name="onReceived">An action that is invoked when the requested data is received.</param>
		/// <returns>A <see cref="Response{T}"/> object containing information about the status of the requested data.</returns>
		public Response<TResponse> Send<TResponse>(string request, uint destinationId, Action<TResponse> onReceived) => Send(request, destinationId, null, onReceived);

		/// <summary>
		/// Sends a message that doesn't expect a response.
		/// </summary>
		/// <param name="request">The name of the request that should be executed by the client application.</param>
		/// <param name="destinationId">The unique identifier representing the client application where this request should be sent.</param>
		/// <param name="data">An object containing any data that should be sent to the client application when delivering this request. Can be null.</param>
		public void Send(string request, uint destinationId, object data = null) => Send<Null>(request, destinationId, data, null);

		/// <summary>
		/// Receives a message and invokes the appropriate response and request execution.
		/// </summary>
		/// <param name="message">A message object that should be received.</param>
		public void Receive(Message message)
		{
			// Each time a message is received, we will scan the list of pending responses
			// to see whether or not any of them have timed out, and remove them.
			DateTime now = DateTime.Now;
			foreach (uint correlationId in mResponsesMap.Keys)
			{
				Response response = mResponsesMap[correlationId];
				if ((now - response.Sent).Milliseconds > response.Timeout)
					mResponsesMap.Remove(correlationId);
			}

			message.Info.ReceivedTime = DateTime.Now;

			// There are two situations that can occur when a message is received.
			// (1) The message is a request to do something
			// (2) The message is a response to a request that we made
			if (message.CorrelationId > 0)
			{
				// This message is (supposedly) a response to a request we made
				if (mResponsesMap.TryGetValue(message.CorrelationId, out Response value))
				{
					/* Note: The following code is a bit heavy on the use of reflection.
					 * This is to work around my inability to statically type the references
					 * to the generic responses. I am open to suggestions on how to do this in a
					 * way more conducive to static typing */

					// Get property infos via reflection
					Type responseType = value.GetType();
					Type responseDataType = responseType.GetGenericArguments()[0];
					PropertyInfo dataProperty = responseType.GetProperty(nameof(Response<Null>.Data));
					PropertyInfo onReceivedProperty = responseType.GetProperty(nameof(Response<Null>.OnReceived));

					// Get receiving delegate
					Delegate onReceived = onReceivedProperty.GetValue(value) as Delegate;

					// Deserializing JSON
					// If we failed to deserialize JSON, we fail to receive the reponse
					object data = null;
					if (!responseDataType.Equals(typeof(Null)))
						try { data = JsonConvert.DeserializeObject(message.Data, responseDataType); }
						catch (JsonException) { return; }

					// Succeeded in deserializing, now set data and call receiving delegate
					value.IsReceived = true;
					value.Received = message.Info.ReceivedTime;
					dataProperty.SetValue(value, data);
					onReceived?.DynamicInvoke(data);

					// Remove this response from the responses map
					mResponsesMap.Remove(message.CorrelationId);
				}
			}
			else
			{
				// This message is (supposedly) a request
				if (mRequestMap.TryGetValue(message.Request.ToLower(), out List<Request> list))
				{
					for (int i = 0; i < list.Count(); i++)
					{
						Request request = list[i];

						// Does this request really need deserialized data? If so, let's get it.
						// Once again, if we fail to deserialize, we fail to receive
						object data = null;
						if (request.HasDataParameter && !request.DataType.Equals(typeof(Null)))
						{
							try { data = JsonConvert.DeserializeObject(message.Data, request.DataType); }
							catch (JsonException) { continue; }
						}

						// Invoke the delegate
						object responseData;
						if (request.HasDataParameter) responseData = request.Invoker.DynamicInvoke(message.Info, data);
						else responseData = request.Invoker.DynamicInvoke(message.Info);

						// Check whether or not we need to send a response
						if (!request.ReturnType.Equals(typeof(Null)))
						{
							Message response = new Message(string.Empty, message.Info.SenderId, JsonConvert.SerializeObject(responseData), message.Id);
							mMessageConnectionBinder?.Invoke(response);
						}
					}
				}
			}
		}

		/// <summary>
		/// Registers any static or instance request handlers in a specified object.
		/// </summary>
		/// <param name="o">The object whose static and instance methods marked by <see cref="RequestHandlerAttribute"/> should be registered.</param>
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

		/// <summary>
		/// Registers any static request handlers in a specified type.
		/// </summary>
		/// <typeparam name="T">The type whose static methods marked by <see cref="RequestHandlerAttribute"/> should be registered.</typeparam>
		public void Register<T>() => Register(typeof(T));

		/// <summary>
		/// Registers any static request handlers in a specified type.
		/// </summary>
		/// <param name="t">The type whose static methods marked by <see cref="RequestHandlerAttribute"/> should be registered.</param>
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


			Type rawReturnType;
			Type returnType = typeof(Null), dataType = typeof(Null);

			rawReturnType = method.ReturnType;
			if (!rawReturnType.Equals(typeof(void)) && !rawReturnType.Equals(typeof(Null))) returnType = rawReturnType;

			ParameterInfo[] parameters = method.GetParameters();
			if (parameters.Length > 2) ThrowInvalidMethodSignatureException(method, "has a method signature with too many parameters");
			else if (parameters.Length == 2) dataType = parameters[1].ParameterType;
			if (parameters.Length < 1) ThrowInvalidMethodSignatureException(method, "has a method signature with too few parameters");
			if (!parameters[0].ParameterType.Equals(typeof(Message.MessageInfo))) ThrowInvalidMethodSignatureException(method, $"has an invalid method signature. The method's first parameter must have a type of {nameof(Message.MessageInfo)}");

			bool hasDataParameter = parameters.Length == 2;

			// Create delegate type
			Type delegateType;
			if (hasDataParameter)
			{
				if (rawReturnType.Equals(typeof(void)))
					delegateType = typeof(RequestDelegateVoid<>).MakeGenericType(dataType);
				else delegateType = typeof(RequestDelegate<,>).MakeGenericType(rawReturnType, dataType);
			}
			else
			{
				if (rawReturnType.Equals(typeof(void)))
					delegateType = typeof(RequestDelegateVoid);
				else delegateType = typeof(RequestDelegate<>).MakeGenericType(rawReturnType);
			}

			// Create delegate
			Delegate callback;
			if (owner == null) callback = method.CreateDelegate(delegateType);
			else callback = method.CreateDelegate(delegateType, owner);

			// Create request
			Request req = new Request()
			{
				Owner = owner,
				Attr = attribute,
				Invoker = callback,
				DataType = dataType,
				HasDataParameter = hasDataParameter,
				ReturnType = returnType
			};

			if (!mRequestMap.TryGetValue(attribute.RequestName, out var list))
			{
				list = new List<Request>();
				mRequestMap[attribute.RequestName] = list;
			}

			// Add the request to the list and sort by reverse of priority levels
			// (High priority at start of list, low priority at end)
			list.Add(req);
			list.Sort((a, b) => -a.Attr.Priority.CompareTo(b.Attr.Priority));
		}

		private void ThrowInvalidMethodSignatureException(MethodInfo method, string message)
		{
			throw new ArgumentException($"{AssemblyName}: Method {GetFormattedMethodName(method)} marked with {nameof(RequestHandlerAttribute)} {message}. Refer to the documentation.");
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