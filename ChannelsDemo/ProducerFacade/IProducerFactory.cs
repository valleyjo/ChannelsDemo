namespace ChannelsDemo.ProducerFacade
{
  public interface IProducerFactory
  {
    public IProducer Get(string fileName);
  }
}
