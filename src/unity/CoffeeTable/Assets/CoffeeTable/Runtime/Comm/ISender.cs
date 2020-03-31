using CoffeeTable.Common.Messaging.Core;

namespace CoffeeTable.Comm
{
  internal interface ISender
  {
    void Send(Message m);
    void Close();
    string Destination { get; }
  }
}