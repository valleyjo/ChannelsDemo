namespace ChannelsDemo
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
    private const int LowerBoundMaxFileSize = 8;
    private const int UpperBoundMaxFileSize = 12;
    private readonly string fullFilePath;
    private readonly Random random;
    private readonly CancellationToken token;
    private readonly ILogger logger;
    private FileStream fileStream;
    private FileProducer client;
    private int charsWritten;

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

    public async ValueTask WriteAsync(char value)
    {
      if (this.fileStream == null)
      {
        throw new InvalidOperationException("attempted to write to a disconnected file");
      }

      var data = new ReadOnlyMemory<byte>(BitConverter.GetBytes(value));
      this.charsWritten++;
      await this.fileStream.WriteAsync(data, this.token);

      int randSizeLimit = this.random.Next(LowerBoundMaxFileSize, UpperBoundMaxFileSize + 1);
      if (this.fileStream != null && this.charsWritten >= randSizeLimit)
      {
        // Syncronously disconnect from the file. This is the best we can do to
        // simulate a failure in the connection.
        this.Disconnect();
      }
    }

    public void Connect() => this.fileStream = File.OpenWrite(this.fullFilePath);

    public bool IsConnected() => this.fileStream != null;

    public void Disconnect()
    {
      this.fileStream.DisposeAsync().AsTask().Wait();
      this.fileStream = null;
    }
  }
}
