namespace ChannelsDemo
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
    private readonly ProducerFactory factory;
    private readonly Channel<char> channel;
    private IProducer currentProducer;
    private volatile bool shutdownInitiated;

    public ProducerWrapper(ProducerFactory factory, CancellationToken token, int maxSize)
    {
      this.token = token;
      this.factory = factory;
      var options = new BoundedChannelOptions(maxSize)
      {
        FullMode = BoundedChannelFullMode.Wait,
      };

      this.channel = Channel.CreateBounded<char>(options);
    }

    public async ValueTask ProduceAsync(char value)
    {
      await this.channel.Writer.WriteAsync(value, this.token);
    }

    public async Task RunAsync()
    {
      while (!this.token.IsCancellationRequested)
      {
        char value = await this.channel.Reader.ReadAsync(this.token);

        // once we have data, we need to make sure that we have a working producer.
        if (this.currentProducer == null)
        {
          // see notes on ProducerFactory.Get
          this.currentProducer = this.factory.Get("keyloggerdata.txt");
        }

        // once we have a working producer, we need to make sure it's connected
        if (!this.currentProducer.IsConnected())
        {
          this.currentProducer.Connect();
        }

        // reading a primitive type is atomic
        if (!this.shutdownInitiated)
        {
          await this.currentProducer.ProduceAsync(value);
        }
      }
    }

    public async Task ShutdownAsync()
    {
      if (this.currentProducer != null && !this.shutdownInitiated)
      {
        // primitive type assignment is atomic
        this.shutdownInitiated = true;
        this.channel.Writer.Complete();

        // clear out any remaining data in the channel before we shut down
        // TODO: Is this needed? Can we just shutdown without producing the remaining data?
        await foreach (char item in this.channel.Reader.ReadAllAsync())
        {
          await this.currentProducer.ProduceAsync(item);
        }

        this.currentProducer.Shutdown();
      }

      this.currentProducer = null;
    }
  }
}
