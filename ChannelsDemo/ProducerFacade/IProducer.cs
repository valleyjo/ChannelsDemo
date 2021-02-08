namespace ChannelsDemo.ProducerFacade
{
  using System.Threading.Tasks;

  /// <summary>
  /// Interface for the producer. This interface allows us to use a factory
  /// in the wrapper which allows for testability.
  /// </summary>
  public interface IProducer<T>
  {
    public ValueTask ProduceAsync(T value);

    public bool IsConnected();

    public void Shutdown();

    public void Connect();
  }
}
