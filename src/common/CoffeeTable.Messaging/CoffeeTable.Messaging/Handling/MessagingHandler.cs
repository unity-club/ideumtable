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
		private Dictionary<uint, Exchange> mPendingExchangesMap;
		private Dictionary<string, List<RequestHandlerInfo>> mRequestHandlersMap;
		private HashSet<MethodInfo> mRegisteredMethods;

		public MessagingHandler(Action<Message> connectionBinder)
		{
			mConnectionBinder = connectionBinder;

			mPendingExchangesMap = new Dictionary<uint, Exchange>();
			mRegisteredMethods = new HashSet<MethodInfo>();
			mRequestHandlersMap = new Dictionary<string, List<RequestHandlerInfo>>();
		}

		public Exchange<T> Send<T>(uint destinationId, string requestName, object data = null)
		{
			Exchange<T> exchange = new Exchange<T>();
			Message pendingMessage = new Message();

			pendingMessage.Request = requestName;
			pendingMessage.DestinationId = destinationId;
			if (data != null) pendingMessage.Data = JsonConvert.SerializeObject(data);

			exchange.Requested = pendingMessage.Sent;

			mPendingExchangesMap[pendingMessage.Id] = exchange;

			mConnectionBinder?.Invoke(pendingMessage);

			return exchange;
		}

		public void Receive(Message message)
		{
			throw new NotImplementedException();
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
			if (!IsGenericType(parameters[0].ParameterType, typeof(Request<>)) || !IsGenericType(parameters[1].ParameterType, typeof(Response<>)))
				ThrowInvalidMethodSignatureException(method, $"has an invalid method signature. " +
					$"The first parameter must be a {nameof(Request)}, whose generic type represents the type of data this request will receive. " +
					$"The second parameter must be a {nameof(Response)}, whose generic type represents the type of data this request will respond with. " +
					$"Either type parameter can be the {nameof(Null)} type if the request handler should not receive or respond, respectively, with any data.");
			if (!method.ReturnType.Equals(typeof(void)))
				ThrowInvalidMethodSignatureException(method, "has an invalid method signature. Request handlers should not return anything and should have a void return type.");
			if (IsRefInOut(parameters[0]) || IsRefInOut(parameters[0]))
				ThrowInvalidMethodSignatureException(method, "has an invalid method signature. The parameters in a request handler method cannot be marked as in, out, or pass by reference.");

			Type requestType = parameters[0].ParameterType.GetGenericArguments()[0];
			Type responseType = parameters[1].ParameterType.GetGenericArguments()[0];

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
