namespace CoffeeTable.Publishers
{

  public interface IPublisherBase<in T>
  {
    void Subscribe(T sub);
    void Unsubscribe(T sub);
  }

  public interface IDataPublisher<in T, in TData> : IPublisherBase<T>
  {
    void Publish(TData data);
  }

  public interface IPublisher<in T> : IPublisherBase<T>
  {
    void Publish();
  }
}