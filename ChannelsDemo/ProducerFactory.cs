namespace ChannelsDemo
{
  using System.Threading;
  using System.Threading.Tasks;
  using Microsoft.Extensions.Logging;

  /// <summary>
  /// This class is used to create IProducer implementations. It allows for
  /// testability in the ProducerWrapper class.
  /// </summary>
  public class ProducerFactory
  {
    private readonly CancellationToken token;
    private readonly string rootDirectory;
    private readonly ILogger logger;

    public ProducerFactory(CancellationToken token, ILogger logger, string rootDirectory = null)
    {
      this.token = token;
      this.rootDirectory = rootDirectory;
      this.logger = logger;
    }

    /// <summary>
    /// This method uses the fileName argument as a trigger to determine
    /// which type of Producer is required.
    /// </summary>
    /// <param name="fileName">The file to produce to. Null or empty indicates to use the LoggingProducer.</param>
    /// <returns></returns>
    public IProducer Get(string fileName)
    {
      if (string.IsNullOrEmpty(this.rootDirectory))
      {
        return new LoggingProducer(this.logger);
      }
      else
      {
        return new RealFileProducer(this.rootDirectory, fileName, this.token, this.logger);
      }
    }

    /// <summary>
    /// This class is an internal wrapper for an external producer.
    /// In the case the external producer does not have a provided interface,
    /// this is where we can provide an implementation for our own interface.
    /// </summary>
    private class RealFileProducer : IProducer
    {
      private readonly FileProducer producer;

      public RealFileProducer(string rootDirectory, string fileName, CancellationToken token, ILogger logger) =>
        this.producer = new FileProducer(rootDirectory, fileName, token, logger);

      public bool IsConnected() => this.producer.IsConnected();

      public async ValueTask ProduceAsync(char value) => await this.producer.ProduceAsync(value);

      public async Task ShutdownAsync() => await this.producer.ShutdownAsync();
    }
  }
}
