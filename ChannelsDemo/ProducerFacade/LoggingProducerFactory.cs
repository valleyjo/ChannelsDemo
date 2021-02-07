namespace ChannelsDemo.ProducerFacade
{
  using Microsoft.Extensions.Logging;

  public class LoggingProducerFactory : IProducerFactory
  {
    private readonly ILogger logger;

    public LoggingProducerFactory(ILogger logger) => this.logger = logger;

    public IProducer Get(string fileName) => new LoggingProducer(this.logger);
  }
}
