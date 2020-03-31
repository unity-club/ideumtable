using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CoffeeTable.Comm
{
  internal interface IComm : IDisposable
  {
    void Bind(Action<byte[]> reciever = null, Action closed = null, Action<Exception> onException = null);
    void Close();
    void Send(byte[] bytes);
    Socket Socket { get; }
    bool IsClosed();
  }
}
