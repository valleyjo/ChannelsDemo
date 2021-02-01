namespace ChannelsDemo
{
  using System.Threading.Tasks;
  using Microsoft.Extensions.Logging;

  /// <summary>
  /// This producer is an implementation that only logs. It can be used for
  /// unit testing but it can also be used in production to test that code
  /// is working without relying on the third party producer to be functional.
  /// </summary>
  internal class LoggingProducer : IProducer
  {
    private readonly ILogger logger;
    private bool isConnected;

    public LoggingProducer(ILogger logger)
    {
      this.logger = logger;
      this.isConnected = false;
    }

    public void ToggleConnected() => this.isConnected = !this.isConnected;

    public bool IsConnected()
    {
      this.logger.LogInformation($"{nameof(LoggingProducer)}.{nameof(LoggingProducer.IsConnected)}");
      return this.isConnected;
    }

    public ValueTask ProduceAsync(char value)
    {
      this.logger.LogInformation($"produced value '{value}'");
      return new ValueTask(Task.CompletedTask);
    }

    public Task ShutdownAsync()
    {
      this.logger.LogInformation($"{nameof(LoggingProducer)}.{nameof(LoggingProducer.ShutdownAsync)}");
      return Task.CompletedTask;
    }
  }
}
