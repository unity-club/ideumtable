using CoffeeTable.Common.Messaging.Core;

namespace CoffeeTable.Comm
{
  internal interface IReceiver
  {
    void Receive(Message m);
  }
}