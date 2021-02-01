namespace ChannelsDemo
{
  using System;
  using System.IO;
  using System.Threading;
  using System.Threading.Tasks;
  using Microsoft.Extensions.Logging;

  /// <summary>
  /// Dummy implementation of a third party "producer" which produces a value
  /// to an external resource. This implementation uses a file but imagine
  /// other scenarios where an implementation could use a web service as a dest.
  /// </summary>
  public class FileProducer
  {
    private readonly CancellationToken cancellationToken;
    private readonly ILogger logger;
    private FileStream file;

    public FileProducer(string rootDirectory, string fileName, CancellationToken token, ILogger logger)
    {
      this.file = File.OpenWrite(Path.Combine(rootDirectory, fileName));
      this.cancellationToken = token;
      this.logger = logger;
    }

    public bool IsConnected() => this.file == null;

    public async ValueTask ProduceAsync(char value)
    {
      this.logger.LogInformation($" Attempting to write '{value}' to file");
      var data = new ReadOnlyMemory<byte>(BitConverter.GetBytes(value));
      await this.file.WriteAsync(data, this.cancellationToken);
      this.logger.LogInformation($"Successfully wrote '{value}' to file");
    }

    public async Task ShutdownAsync()
    {
      this.logger.LogInformation("Closing the file");
      await this.file.DisposeAsync();
      this.file = null;
      this.logger.LogInformation("File closed successfully");
    }
  }
}
