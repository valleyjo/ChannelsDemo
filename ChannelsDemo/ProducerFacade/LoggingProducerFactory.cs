namespace ChannelsDemo.ProducerFacade
{
  using Microsoft.Extensions.Logging;

  public class LoggingProducerFactory<T> : IProducerFactory<T>
  {
    private readonly ILogger logger;

    public LoggingProducerFactory(ILogger logger) => this.logger = logger;

    public IProducer<T> Get(string fileName) => new LoggingProducer<T>(this.logger);
  }
}
