namespace ChannelsDemo.ProducerFacade
{
  using System.Threading;
  using System.Threading.Channels;
  using System.Threading.Tasks;

  /// <summary>
  /// This class wraps an IProducer and allows for unit testing via the ProducerFactory
  /// This is the class we would pass around in the rest of the product code, allowing
  /// isloation from the third party producer.
  /// </summary>
  public class ProducerWrapper
  {
    private readonly CancellationToken token;
    private readonly IProducerFactory factory;
    private readonly Channel<char> channel;
    private IProducer currentProducer;

    public ProducerWrapper(
      IProducerFactory factory,
      CancellationToken token,
      int maxSize = short.MaxValue,
      bool allowSynchronousContinuations = false)
    {
      this.token = token;
      this.factory = factory;
      var options = new BoundedChannelOptions(maxSize)
      {
        FullMode = BoundedChannelFullMode.DropWrite,
        AllowSynchronousContinuations = allowSynchronousContinuations,
      };

      this.channel = Channel.CreateBounded<char>(options);
    }

    public bool Produce(char value) => this.channel.Writer.TryWrite(value);

    public async Task RunAsync()
    {
      while (!this.channel.Reader.Completion.IsCompleted)
      {
        char value = await this.channel.Reader.ReadAsync();
        await this.ProduceInternalAsync(value);
     }
    }

    public async Task ShutdownAsync()
    {
      this.channel.Writer.Complete();

      // clear out any remaining data in the channel before we shut down
      // If this is used in a primary / secondary model is it possible that an old primary while
      // shutting down is still clearing out the queue while a new primary comes up and begins producing data?
      // TODO: Is this needed? Can we just shutdown without producing the remaining data?
      await foreach (char value in this.channel.Reader.ReadAllAsync())
      {
        await this.ProduceInternalAsync(value);
      }

      if (this.currentProducer != null)
      {
        this.currentProducer.Shutdown();
        this.currentProducer = null;
      }
    }

    private async ValueTask ProduceInternalAsync(char value)
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
