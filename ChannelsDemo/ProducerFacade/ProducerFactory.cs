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
    /// This method uses the rootDirectory ctor argument as a trigger to determine
    /// which type of Producer is required.
    /// </summary>
    /// <param name="fileName">The file to produce to. Passing this at construction
    /// time allows for the ability to inject a construction time value.
    /// Provided for simulation.</param>
    /// <returns></returns>
    public IProducer Get(string fileName)
    {
      if (string.IsNullOrEmpty(this.rootDirectory))
      {
        return new LoggingProducer(this.logger);
      }
      else
      {
        var fileConnection = new FileConnection(this.rootDirectory, fileName, this.token, this.logger);
        return new RealFileProducer(fileConnection);
      }
    }

    // TODO: Place LoggingProducer as internal class also?

    /// <summary>
    /// This class is an internal wrapper for an external producer.
    /// In the case the external producer does not have a provided interface,
    /// this is where we can provide an implementation for our own interface.
    /// It's just a pass through to the external implementation.
    /// </summary>
    private class RealFileProducer : IProducer
    {
      private readonly FileConnection connection;

      public RealFileProducer(FileConnection connection) => this.connection = connection;

      public bool IsConnected() => this.connection.IsConnected();

      public async ValueTask ProduceAsync(char value) => await this.connection.Client.ProduceAsync(value);

      public void Shutdown() => this.connection.Disconnect();

      public void Connect() => this.connection.Connect();
    }
  }
}
