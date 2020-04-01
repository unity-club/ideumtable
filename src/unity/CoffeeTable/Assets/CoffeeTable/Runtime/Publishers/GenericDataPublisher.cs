using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoffeeTable.Publishers
{
  internal abstract class GenericDataPublisher<TSub, TData> : IDataPublisher<TSub, TData>
  {

    private struct SubData
    {
      public bool DoAdd;
      public TSub Item;
    }

    private Queue<SubData> _modificationBuffer = new Queue<SubData>();
    private List<TSub> _subscriptions = new List<TSub>();
    private Queue<int> _toRemoveIndex = new Queue<int>();
    private bool _doClear, _isInvoking;


    public void Subscribe(TSub subscriber)
    {
      if (subscriber == null) return;
      _modificationBuffer.Enqueue(new SubData { DoAdd = true, Item = subscriber });
    }

    public void Unsubscribe(TSub subscriber)
    {
      if (subscriber == null) return;
      _modificationBuffer.Enqueue(new SubData { DoAdd = false, Item = subscriber });
    }

    public void Publish(TData data)
    {
      while (_modificationBuffer.Count > 0)
      {
        var s = _modificationBuffer.Dequeue();
        if (s.DoAdd)
        {
          _subscriptions.Add(s.Item);
        }
        else
        {
          _subscriptions.Remove(s.Item);
        }
      }

      _isInvoking = true;
      for (var i = 0; i < _subscriptions.Count; i++)
      {
        var s = _subscriptions[i];
        if (s == null)
        {
          _toRemoveIndex.Enqueue(i);
        }
        else
        {
          try
          {
            PublishItem(s, data);
          }
          catch (Exception e)
          {
            Debug.LogException(e);
          }
        }
      }

      while (_toRemoveIndex.Count > 0)
      {
        var i = _toRemoveIndex.Dequeue();
        _subscriptions.RemoveAt(i);
      }

      _isInvoking = false;
      if (_doClear)
      {
        _subscriptions.Clear();
        _doClear = false;
      }
    }

    public void Clear()
    {
      if (_isInvoking)
      {
        _doClear = true;
      }
      else
      {
        _subscriptions.Clear();
        _doClear = false;
      }
    }

    protected abstract void PublishItem(TSub i, TData data);

  }
}