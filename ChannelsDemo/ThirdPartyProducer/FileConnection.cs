namespace ChannelsDemo.ThirdPartyProducer
{
  using System;
  using System.IO;
  using System.Threading;
  using System.Threading.Tasks;
  using Microsoft.Extensions.Logging;

  /// <summary>
  /// The metaphor is stretching pretty thin, but imagine that a 'connection'
  /// to a remote resource is used by the 'producer'. This class provides
  /// a trivial implementation of a 'connection' to a file. The trick here is
  /// that this connection can disconnect at random forcing the ProducerWrapper
  /// to handle that scenario.
  /// </summary>
  public class FileConnection
  {
    private const int LowerboundMaxWrites = 8;
    private const int UpperboundMaxWrites = 12;
    private readonly string fullFilePath;
    private readonly Random random;
    private readonly CancellationToken token;
    private readonly ILogger logger;
    private FileStream fileStream;
    private FileProducer client;
    private int dataWritten;

    public FileConnection(string rootDirectory, string fileName, CancellationToken token, ILogger logger)
    {
      this.fullFilePath = Path.Combine(rootDirectory, fileName);
      this.random = new Random();
      this.token = token;
      this.logger = logger;
    }

    public FileProducer Client
    {
      get
      {
        if (this.client == null)
        {
          this.client = new FileProducer(this, this.logger);
        }

        return this.client;
      }
    }

    public async ValueTask WriteAsync(ReadOnlyMemory<byte> data)
    {
      if (this.fileStream == null)
      {
        throw new InvalidOperationException("attempted to write to a disconnected file");
      }

      this.dataWritten++;
      await this.fileStream.WriteAsync(data, this.token);

      int randSizeLimit = this.random.Next(LowerboundMaxWrites, UpperboundMaxWrites + 1);
      if (this.fileStream != null && this.dataWritten >= randSizeLimit)
      {
        // Syncronously disconnect from the file. This is the best we can do to
        // simulate a failure in the connection.
        this.Disconnect();
        this.dataWritten = 0;
        this.logger.LogWarning("Channel randomly disconnected");
      }
    }

    public void Connect()
    {
      this.fileStream = File.OpenWrite(this.fullFilePath);
      this.logger.LogInformation($"{nameof(FileConnection)}.{nameof(FileConnection.Connect)}");
    }

    public bool IsConnected() => this.fileStream != null;

    public void Disconnect()
    {
      if (this.fileStream != null)
      {
        this.fileStream.DisposeAsync().AsTask().Wait();
        this.fileStream = null;
      }
    }
  }
}
