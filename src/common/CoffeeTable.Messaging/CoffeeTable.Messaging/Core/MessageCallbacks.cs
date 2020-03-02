using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CoffeeTable.Messaging.Core
{
	/// <summary>
	/// Represents a callback that is executed when a <see cref="Message"/> is received with data of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The data type of the object that is contained in a message's <see cref="Message.CommandData"/> field.</typeparam>
	/// <param name="senderInfo">Parameter containing information about the application that sent this message</param>
	/// <param name="callbackData">Parameter containing data of type <typeparamref name="T"/>. Can be null.</param>
	public delegate void Callback<T>(Message.SenderInfo senderInfo, T callbackData);
	/// <summary>
	/// Represents a callback that is executed when a <see cref="Message"/> is received without any data.
	/// </summary>
	/// <param name="senderInfo">Parameter containing information about the application that sent this message.</param>
	public delegate void Callback(Message.SenderInfo senderInfo);

	/// <summary>
	/// <para>Marks methods which should be treated as callbacks that are invoked when a <see cref="Message"/> is received by an application.</para>
	/// <para>When marking a method with this attribute, ensure that the method in question has a signature that is identical to <see cref="Callback"/> or <see cref="Callback{T}"/> to ensure that it will be properly invoked:</para>
	/// <list type="table">
	/// <item><code><see cref="void"/> Callback(<see cref="Message.SenderInfo"/> senderInfo)</code></item>
	/// <item><code><see cref="void"/> Callback(<see cref="Message.SenderInfo"/> senderInfo, <typeparamref name="T"/> callbackData)</code></item>
	/// </list>
	/// <para>Where <typeparamref name="T"/> refers to any serializable type.</para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public class CommandHandlerAttribute : Attribute
	{
		/// <summary>
		/// The name of this callback.
		/// </summary>
		public string Name { get; private set; }
		/// <summary>
		/// The execution priority of this callback. Callbacks with a higher execution priority run first. Default is zero.
		/// </summary>
		public int ExecutionPriority { get; set; }

		/// <summary>
		/// Constructs a <see cref="CommandHandlerAttribute"/> instance.
		/// </summary>
		/// <param name="callbackName">The name of the callback</param>
		public CommandHandlerAttribute(string callbackName)
		{
			Name = callbackName;
		}
	}

	/// <summary>
	/// Defines a contract for an object that maps names of commands and operations (as declared in <see cref="Message.CommandName"/>)
	/// to delegate invocations that should be executed when a <see cref="Message"/> is received.
	/// </summary>
	public interface IMessageCallbackHandler
	{
		/// <summary>
		/// Registers a callback containing data of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">The type of data that should be passed to this callback when a <see cref="Message"/> is received. The contents of <see cref="Message.CommandData"/> will be deserialized into <typeparamref name="T"/>.</typeparam>
		/// <param name="commandName">When a received <see cref="Message"/> object whose <see cref="Message.CommandName"/> field equals this parameter (case-insensitive), <paramref name="commandCallback"/> will be invoked.</param>
		/// <param name="commandCallback">The callback that should be invoked when a received message's <see cref="Message.CommandName"/> equals <paramref name="commandName"/> (case-insensitive).</param>
		/// <param name="executionPriority">If there are multiple callbacks for a given name, callbacks with a higher execution priority will be executed first.</param>
		void AddCallback<T>(string commandName, Callback<T> commandCallback, int executionPriority = 0);
		/// <summary>
		/// Registers a callback that contains no data.
		/// </summary>
		/// <param name="commandName">When a received <see cref="Message"/> object whose <see cref="Message.CommandName"/> field equals this parameter (case-insensitive), <paramref name="commandCallback"/> will be invoked.</param>
		/// <param name="commandCallback">The callback that should be invoked when a received message's <see cref="Message.CommandName"/> equals <paramref name="commandName"/> (case-insensitive).</param>
		/// <param name="executionPriority">If there are multiple callbacks for a given name, callbacks with a higher execution priority will be executed first.</param>
		void AddCallback(string commandName, Callback commandCallback, int executionPriority = 0);
		/// <summary>
		/// Finds methods in the given object's type that are marked with <see cref="CommandHandlerAttribute"/> and registers them as callbacks that are invoked when a <see cref="Message"/> is received with the appropriate command name.
		/// </summary>
		/// <param name="o">The object for whom callbacks will be received.</param>
		void AddCallbacks(object o);
	}

	/// <summary>
	/// A standard implementation of <see cref="IMessageCallbackHandler"/>
	/// </summary>
	public class MessageCallbackHandler : IMessageCallbackHandler
	{
		/// <summary>
		/// Stores information that is used in the mapping of command names to callbacks in <see cref="mCallbackMap"/>
		/// </summary>
		private class CallbackInfo
		{
			public bool HasData;
			public string CallbackId;
			public Delegate Callback;
			public Type DataType;
			public int ExecutionPriority;
		}

		private const string REGEX = @"^[a-zA-Z0-9_]+$";

		/* For use with reflection and implementing methods marked with CommandAttribute */
		private static readonly MethodInfo ADDCALLBACK_GENERIC;

		static MessageCallbackHandler() {
			ADDCALLBACK_GENERIC = (from mi in typeof(MessageCallbackHandler).GetMethods()
								   where mi.GetGenericArguments().Length == 1
								   where mi.Name.Equals(nameof(AddCallback))
								   select mi)
								  .FirstOrDefault();
		}

		private Dictionary<string, List<CallbackInfo>> mCallbackMap;

		public MessageCallbackHandler()
		{
			mCallbackMap = new Dictionary<string, List<CallbackInfo>>();
		}

		/// <inheritdoc/>
		public void AddCallback<T>(string commandName, Callback<T> commandCallback, int executionPriority = 0)
		{
			AddCallback(commandName, commandCallback, typeof(T), executionPriority);
		}

		/// <inheritdoc/>
		public void AddCallback(string commandName, Callback commandCallback, int executionPriority = 0)
		{
			AddCallback(commandName, commandCallback, null, executionPriority);
		}

		private void AddCallback(string commandName, Delegate commandCallback, Type dataType, int executionPriority)
		{
			if (string.IsNullOrWhiteSpace(commandName))
				throw new ArgumentException("CoffeeTable.Messaging: Cannot add a callback with a name that is null or consists only of whitespace characters.");
			if (!Regex.IsMatch(commandName, REGEX))
				throw new ArgumentException("CoffeeTable.Messaging: Callback names must consist only of alphanumeric characters and underscores.");

			CallbackInfo info = new CallbackInfo
			{
				HasData = dataType != null,
				CallbackId = commandName.ToLower(),
				Callback = commandCallback,
				DataType = dataType,
				ExecutionPriority = executionPriority
			};

			List<CallbackInfo> callbacks;
			if (!mCallbackMap.TryGetValue(info.CallbackId, out callbacks)) callbacks = mCallbackMap[info.CallbackId] = new List<CallbackInfo>();

			callbacks.Add(info);
			callbacks.Sort((x, y) => -x.ExecutionPriority.CompareTo(y.ExecutionPriority));
		}

		/// <inheritdoc/>
		public void AddCallbacks(object o)
		{
			Type oType = o.GetType();
			MethodInfo[] methods = oType.GetMethods(BindingFlags.Public
				| BindingFlags.Instance
				| BindingFlags.Static
				| BindingFlags.NonPublic);
			
			foreach (var mi in methods)
			{
				CommandHandlerAttribute attr = mi.GetCustomAttribute<CommandHandlerAttribute>();
				if (attr == null) continue;

				if (GetCallbackDelegateFromMethodInfo(mi, o, out Delegate commandDelegate, out Type commandDataType))
				{
					if (commandDataType == null)
					{
						AddCallback(attr.Name, (Callback)commandDelegate, attr.ExecutionPriority);
					} else
					{
						// need to construct method call generically
						MethodInfo genericCall = ADDCALLBACK_GENERIC.MakeGenericMethod(commandDataType);
						genericCall.Invoke(this, new object[] { attr.Name, commandDelegate, attr.ExecutionPriority });
					}
				} else
				{
					throw new ArgumentException($"CoffeeTable.Messaging: Method marked with {nameof(CommandHandlerAttribute)} has invalid method signature. " +
						$"Ensure that signature matches {nameof(Callback)} or {nameof(Callback)}<T>");
				}
			}
		}

		private bool GetCallbackDelegateFromMethodInfo (MethodInfo mi, object o, out Delegate commandDelegate, out Type commandDataType)
		{
			commandDelegate = null;
			commandDataType = null;

			// try to bind dataless invocation
			Delegate datalessDelegate;
			if (mi.IsStatic) datalessDelegate = Delegate.CreateDelegate(typeof(Callback), mi, false);
			else datalessDelegate = Delegate.CreateDelegate(typeof(Callback), o, mi, false);
			if (datalessDelegate != null)
			{
				commandDelegate = datalessDelegate;
				return true;
			}

			// try to bind data invocation
			// first get the expected parameter type (should be the second parameter of two)
			ParameterInfo[] methodParams = mi.GetParameters();
			if (methodParams.Length != 2) return false;
			Type dataParameterType = (from p in methodParams
								  where p.Position == 1
								  select p.ParameterType).First();
			Type dataDelegateType = typeof(Callback<>).MakeGenericType(dataParameterType);
			Delegate dataDelegate;
			if (mi.IsStatic) dataDelegate = Delegate.CreateDelegate(dataDelegateType, mi, false);
			else dataDelegate = Delegate.CreateDelegate(dataDelegateType, o, mi, false);

			if (dataDelegate != null)
			{
				commandDelegate = dataDelegate;
				commandDataType = dataParameterType;
				return true;
			}

			return false;
		}


		/// <summary>
		/// Receive a <see cref="Message"/> instance and invoke any relevant callbacks.
		/// </summary>
		/// <param name="message">The message to be received.</param>
		public void ReceiveMessage(Message message)
		{
			if (string.IsNullOrWhiteSpace(message.CommandName)) return;
			if (mCallbackMap.TryGetValue(message.CommandName.ToLower(), out List<CallbackInfo> callbacks))
			{
				for (int i = 0; i < callbacks.Count(); i++)
				{
					CallbackInfo ci = callbacks[i];
					if (ci.HasData)
					{
						try
						{
							object data = JsonConvert.DeserializeObject(message.CommandData, ci.DataType);
							ci.Callback.DynamicInvoke(message.Sender, data);
						}
						catch (JsonException) { }
					}
					else ci.Callback.DynamicInvoke(message.Sender);
				}
			}
		}

		/// <summary>
		/// Constructs a <see cref="Message"/> object with the relevant information.
		/// </summary>
		/// <param name="destinationId">The unique ID of the application to whom this message should be sent.</param>
		/// <param name="commandName">The name of the command or operation that should be executed by the application with ID <paramref name="destinationId"/> when the message is received.</param>
		/// <param name="commandData"><para>An object containing data that should be passed with the command or operation identified by <paramref name="commandName"/>.</para>
		/// This will be serialized into <see cref="Message.CommandData"/>. Can be null or empty.</param>
		/// <returns>A message object containing the relevant information.</returns>
		public Message GetMessage(uint destinationId, string commandName, object commandData = null)
		{
			return new Message(destinationId,
				commandName,
				JsonConvert.SerializeObject(commandData));
		}
	}
}
