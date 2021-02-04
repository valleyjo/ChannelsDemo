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
    private TaskCompletionSource<Task> tcs;
    private bool isConnected;

    public LoggingProducer(ILogger logger)
    {
      this.logger = logger;
      this.isConnected = false;

      // start off allowing production unless toggled off by the user
      this.tcs = new TaskCompletionSource<Task>();
      this.tcs.SetResult(Task.CompletedTask);
    }

    public void ToggleConnected() => this.isConnected = !this.isConnected;

    public bool IsConnected()
    {
      this.logger.LogInformation($"{nameof(LoggingProducer)}.{nameof(LoggingProducer.IsConnected)}");
      return this.isConnected;
    }

    public async ValueTask ProduceAsync(char value)
    {
      // if the task is set to CompletedTask, this completes syncronously
      // if the TCS is not set, then we will pause here until it is set.
      // This allows us to UT this asyncronous code in a syncronous way
      await this.tcs.Task;
      this.logger.LogInformation($"produced value '{value}'");
    }

    public void PauseProduction() => this.tcs = new TaskCompletionSource<Task>();

    public void CompleteProduction()
    {
      this.tcs.SetResult(Task.CompletedTask);
    }

    public void Shutdown() => this.logger.LogInformation($"{nameof(LoggingProducer)}.{nameof(LoggingProducer.Shutdown)}");

    public void Connect() => this.logger.LogInformation($"{nameof(LoggingProducer)}.{nameof(LoggingProducer.Connect)}");
  }
}
