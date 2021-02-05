namespace ChannelsDemo.ThirdPartyProducer
{
  using System.Threading.Tasks;
  using Microsoft.Extensions.Logging;

  /// <summary>
  /// Dummy implementation of a second party "producer" which produces a value
  /// to an external resource. This implementation uses a file but imagine
  /// other scenarios where an implementation could use a web service as a dest.
  /// </summary>
  public class FileProducer
  {
    private readonly ILogger logger;
    private readonly FileConnection connection;

    public FileProducer(FileConnection connection, ILogger logger)
    {
      this.connection = connection;
      this.logger = logger;
    }

    public async ValueTask ProduceAsync(char value)
    {
      this.logger.LogInformation($" Attempting to write '{value}' to file");
      await this.connection.WriteAsync(value);
      this.logger.LogInformation($"Successfully wrote '{value}' to file");
    }
  }
}
