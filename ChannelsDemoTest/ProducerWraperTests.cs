namespace ChannelsDemo
{
  using System.Collections.Generic;
  using System.Threading;
  using System.Threading.Tasks;
  using FluentAssertions;
  using Microsoft.Extensions.Logging;
  using Microsoft.VisualStudio.TestTools.UnitTesting;

  [TestClass]
  public class ProducerWraperTests
  {
    [TestMethod]
    public void ProduceThenRunTest()
    {
      Setup(out var logLines, out var producer);
      producer.ProduceAsync('c').AsTask().Wait();
      Task producerTask = producer.RunAsync();
      producer.ShutdownAsync().Wait();
      logLines[0].Should().Be("[Information]produced value 'c'");
    }

    [TestMethod]
    public void RunThenProduceTest()
    {
      Setup(out var logLines, out var producer);
      Task producerTask = producer.RunAsync();
      producer.ProduceAsync('f').AsTask().Wait();
      producer.ShutdownAsync().Wait();
      logLines[0].Should().Contain("[Information]produced value 'f'");
    }

    private static void Setup(out List<string> logLines, out ProducerWrapper producer)
    {
      logLines = new List<string>();
      ILogger log = MemoryLog.Create(logLines);
      var factory = new ProducerFactory(CancellationToken.None, log);
      producer = new ProducerWrapper(factory, CancellationToken.None, 1);
    }
  }
}
