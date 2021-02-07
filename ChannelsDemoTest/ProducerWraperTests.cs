namespace ChannelsDemo
{
  using System.Collections.Generic;
  using System.Threading;
  using System.Threading.Tasks;
  using ChannelsDemo.ProducerFacade;
  using FluentAssertions;
  using Microsoft.Extensions.Logging;
  using Microsoft.VisualStudio.TestTools.UnitTesting;

  [TestClass]
  public class ProducerWraperTests
  {
    [TestMethod]
    public void ProduceThenRunTest()
    {
      Setup(out List<string> logLines, out ProducerWrapper producer, out BlockingLoggingProducer blockingLoggingProducer);
      producer.Produce('c').Should().BeTrue();
      Task producerTask = producer.RunAsync();
      blockingLoggingProducer.UnblockOne();
      logLines.Should().Contain(s => s.Contains("[Information]produced value 'c'"));
    }

    [TestMethod]
    public void RunThenProduceTest()
    {
      Setup(out List<string> logLines, out ProducerWrapper producer, out BlockingLoggingProducer blockingLoggingProducer);
      Task producerTask = producer.RunAsync();
      producer.Produce('f').Should().BeTrue();
      blockingLoggingProducer.UnblockOne();
      logLines.Should().Contain(s => s.Contains("[Information]produced value 'f'"));
    }

    [TestMethod]
    public void ProduceWhenFullTest()
    {
      Setup(out List<string> logLines, out ProducerWrapper producer, out BlockingLoggingProducer blockingProducer);
      Task runTask = producer.RunAsync();
      producer.Produce('f').Should().BeTrue();
      producer.Produce('r').Should().BeTrue();
      producer.Produce('e').Should().BeTrue();

      // allow 'f' to be written
      blockingProducer.UnblockOne();
      logLines.Should().Contain(s => s.Contains("[Information]produced value 'f'"));

      blockingProducer.UnblockOne();
      logLines.Should().Contain(s => s.Contains("[Information]produced value 'r'"));
    }

    [TestMethod]
    public void ProduceRemainingWhenShutdownTest()
    {
      Setup(out List<string> logLines, out ProducerWrapper producer, out BlockingLoggingProducer blockingProducer);
      Task runTask = producer.RunAsync();
      producer.Produce('f');
      producer.Produce('r');

      Task shutdownTask = producer.ShutdownAsync();

      blockingProducer.UnblockOne(); // unblock 'f'
      logLines.Should().Contain(s => s.Contains("[Information]produced value 'f'"));

      blockingProducer.UnblockOne(); // unblock producing 'r' in ShutdownAsync
      shutdownTask.Wait();
      logLines.Should().Contain(s => s.Contains("[Information]produced value 'r'"));
      logLines.Should().Contain(s => s.Contains("[Information]LoggingProducer.Shutdown"));
      runTask.Wait();
    }

    [TestMethod]
    public void ProduceAfterShutdownTest()
    {
      Setup(out List<string> logLines, out ProducerWrapper producer, out BlockingLoggingProducer underlyingProducer);
      Task runTask = producer.RunAsync();
      producer.Produce('f');
      producer.ShutdownAsync().Wait();
      producer.Produce('r').Should().BeFalse();
      runTask.Wait();
    }

    private static void Setup(
      out List<string> logLines,
      out ProducerWrapper producer,
      out BlockingLoggingProducer blockingLoggingProducer)
    {
      logLines = new List<string>();
      ILogger log = MemoryLog.Create(logLines);

      // use a regular logging producer unless an override is passed in
      var factory = new BlockingLoggingProducerFactory(log);
      blockingLoggingProducer = factory.Instance;
      producer = new ProducerWrapper(factory, CancellationToken.None, 1, true);
    }

    private class BlockingLoggingProducerFactory : IProducerFactory
    {
      public BlockingLoggingProducerFactory(ILogger logger) => this.Instance = new BlockingLoggingProducer(logger);

      public BlockingLoggingProducer Instance { get; private set; }

      public IProducer Get(string fileName) => this.Instance;
    }

    private class BlockingLoggingProducer : LoggingProducer
    {
      // despite using async, the entire interaction with this queue is single threaded
      // so use of concurrentqueue is not needed
      private readonly Queue<TaskCompletionSource<int>> operationQueue;

      public BlockingLoggingProducer(ILogger log)
        : base(log)
      {
        this.operationQueue = new Queue<TaskCompletionSource<int>>();
      }

      public override async ValueTask ProduceAsync(char value)
      {
        var tcs = new TaskCompletionSource<int>();
        this.operationQueue.Enqueue(tcs);
        await tcs.Task;
        await base.ProduceAsync(value);
      }

      public void UnblockOne()
      {
        TaskCompletionSource<int> nextOperation = this.operationQueue.Dequeue();
        nextOperation.SetResult(0);
      }
    }
  }
}
