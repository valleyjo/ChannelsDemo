namespace ChannelsDemo.ProducerFacade
{
  using System;
  using System.Threading;
  using System.Threading.Tasks;
  using ChannelsDemo.ChannelFacade;

  /// <summary>
  /// This class wraps an IProducer and allows for unit testing via the ProducerFactory
  /// This is the class we would pass around in the rest of the product code, allowing
  /// isloation from the third party producer.
  /// </summary>
  public class ProducerWrapper<T>
  {
    private static readonly ObjectDisposedException Disposed = new ObjectDisposedException(nameof(ProducerWrapper<T>));
    private readonly CancellationToken token;
    private readonly IProducerFactory<T> factory;
    private readonly IReadBuffer<T> readBuffer;
    private IProducer<T> currentProducer;

    public ProducerWrapper(IProducerFactory<T> factory, CancellationToken token, IReadBuffer<T> readBuffer)
    {
      this.token = token;
      this.factory = factory;
      this.readBuffer = readBuffer;
    }

    public async Task RunAsync()
    {
      while (!this.token.IsCancellationRequested)
      {
        T value = await this.readBuffer.ReadAsync(this.token);
        await this.ProduceInternalAsync(value);
      }

      this.currentProducer.Shutdown();
    }

    private async ValueTask ProduceInternalAsync(T value)
    {
      // once we have data, we need to make sure that we have a working producer.
      if (this.currentProducer == null)
      {
        // see notes on ProducerFactory.Get
        this.currentProducer = this.factory.Get("keyloggerdata.txt");
      }

      // once we have a producer, we need to make sure it's connected
      if (!this.currentProducer.IsConnected())
      {
        this.currentProducer.Connect();
      }

      await this.currentProducer.ProduceAsync(value);
    }
  }
}
