namespace ChannelsDemo.ProducerFacade
{
  using System;
  using System.Threading;
  using System.Threading.Tasks;
  using ChannelsDemo.ThirdPartyProducer;
  using Microsoft.Extensions.Logging;

  /// <summary>
  /// This class is used to create IProducer implementations. It allows for
  /// testability in the ProducerWrapper class.
  /// </summary>
  public class ProducerFactory<T> : IProducerFactory<T>
  {
    private readonly CancellationToken token;
    private readonly string rootDirectory;
    private readonly ILogger logger;
    private readonly Func<T, ReadOnlyMemory<byte>> serializer;

    public ProducerFactory(
      CancellationToken token,
      ILogger logger,
      string rootDirectory,
      Func<T, ReadOnlyMemory<byte>> serializer)
    {
      this.token = token;
      this.rootDirectory = rootDirectory;
      this.logger = logger;
      this.serializer = serializer;
    }

    /// <summary>
    /// Create an IProducer<typeparamref name="T"/>
    /// </summary>
    /// <param name="fileName">The file to produce to. Passing this at construction
    /// time allows for the ability to inject a construction time value.
    /// Provided for simulation.</param>
    /// <returns></returns>
    public IProducer<T> Get(string fileName)
    {
      var fileConnection = new FileConnection(this.rootDirectory, fileName, this.token, this.logger);
      return new RealFileProducer(fileConnection, this.serializer);
    }

    /// <summary>
    /// This class is an internal wrapper for an external producer.
    /// In the case the external producer does not have a provided interface,
    /// this is where we can provide an implementation for our own interface.
    /// It's just a pass through to the external implementation.
    /// </summary>
    private class RealFileProducer : IProducer<T>
    {
      private readonly FileConnection connection;
      private readonly Func<T, ReadOnlyMemory<byte>> serializer;

      public RealFileProducer(FileConnection connection, Func<T, ReadOnlyMemory<byte>> serializer)
      {
        this.connection = connection;
        this.serializer = serializer;
      }

      public bool IsConnected() => this.connection.IsConnected();

      public async ValueTask ProduceAsync(T value)
      {
        await this.connection.Client.ProduceAsync(this.serializer(value));
      }

      public void Shutdown() => this.connection.Disconnect();

      public void Connect() => this.connection.Connect();
    }
  }
}
