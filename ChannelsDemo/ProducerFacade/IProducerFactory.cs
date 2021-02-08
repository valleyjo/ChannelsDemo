namespace ChannelsDemo.ProducerFacade
{
  public interface IProducerFactory<T>
  {
    public IProducer<T> Get(string fileName);
  }
}
