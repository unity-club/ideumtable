using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace CoffeeTable.Utils
{
	internal class Dispatcher : MonoBehaviour
	{
		public event Action<Exception> ExceptionThrown;

		private Queue<Action> _dispatchQueue = new Queue<Action>();

		public void Dispatch(Action a)
		{
			if (a == null) return;
			lock (_dispatchQueue)
			{
				_dispatchQueue.Enqueue(a);
			}
		}

		private void ProcessDispatchQueue()
		{
			lock (_dispatchQueue)
			{
				while (_dispatchQueue.Count > 0)
				{
					try
					{
						var a = _dispatchQueue.Dequeue();
						a?.Invoke();
					}
					catch (Exception e)
					{
						ExceptionThrown?.Invoke(e);
					}
				}
			}
		}

		void Update()
		{
			ProcessDispatchQueue();
		}
	}
}
