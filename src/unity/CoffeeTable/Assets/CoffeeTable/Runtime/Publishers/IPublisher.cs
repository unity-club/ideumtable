namespace CoffeeTable.Publishers
{

	internal interface IPublisherBase<in T>
	{
		void Subscribe(T sub);
		void Unsubscribe(T sub);
	}

	internal interface IDataPublisher<in T, in TData> : IPublisherBase<T>
	{
		void Publish(TData data);
	}

	internal interface IPublisher<in T> : IPublisherBase<T>
	{
		void Publish();
	}
}