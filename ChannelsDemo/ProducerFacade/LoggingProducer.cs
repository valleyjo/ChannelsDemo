namespace ChannelsDemo.ProducerFacade
{
  using System.Threading.Tasks;
  using Microsoft.Extensions.Logging;

  /// <summary>
  /// This producer is an implementation that only logs. It can be used for
  /// unit testing but it can also be used in production to test that code
  /// is working without relying on the third party producer to be functional.
  /// </summary>
  public class LoggingProducer : IProducer
  {
    private readonly ILogger logger;
    private bool isConnected;

    public LoggingProducer(ILogger logger)
    {
      this.logger = logger;
    }

    public bool IsConnected()
    {
      this.logger.LogInformation($"{nameof(LoggingProducer)}.{nameof(LoggingProducer.IsConnected)}");
      return this.isConnected;
    }

    public virtual ValueTask ProduceAsync(char value)
    {
      this.logger.LogInformation($"produced value '{value}'");
      return default;
    }

    public void Shutdown()
    {
      this.logger.LogInformation($"{nameof(LoggingProducer)}.{nameof(LoggingProducer.Shutdown)}");
      this.isConnected = false;
    }

    public void Connect()
    {
      this.logger.LogInformation($"{nameof(LoggingProducer)}.{nameof(LoggingProducer.Connect)}");
      this.isConnected = true;
    }
  }
}
