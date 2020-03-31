using CoffeeTable.Common.Messaging.Core;
using System;
using System.Collections.Generic;

namespace CoffeeTable.Providers
{
  internal interface ITableProvider : IDisposable
  {
    event Action Connected;
    event Action Disconnected;
    event Action FailedToConnect;
    event Action<Message> MessageReceived;
    bool StartProvider();
  }
}
