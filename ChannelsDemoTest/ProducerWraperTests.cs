namespace ChannelsDemo
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Threading;
  using System.Threading.Channels;
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
      Setup(out List<string> logLines, out ProducerWrapper producer, out IProducer _);
      producer.ProduceAsync('c').AsTask().Wait();
      Task producerTask = producer.RunAsync();
      producer.ShutdownAsync().Wait();
      logLines.Should().Contain(s => s.Contains("[Information]produced value 'c'"));
    }

    [TestMethod]
    public void RunThenProduceTest()
    {
      Setup(out List<string> logLines, out ProducerWrapper producer, out IProducer _);
      Task producerTask = producer.RunAsync();
      producer.ProduceAsync('f').AsTask().Wait();
      producer.ShutdownAsync().Wait();
      logLines.Should().Contain(s => s.Contains("[Information]produced value 'f'"));
    }

    [TestMethod]
    public void ProduceWhenFullTest()
    {
      Setup(out List<string> logLines, out ProducerWrapper producer, out IProducer underlyingProducer, true);
      var blockingProducer = underlyingProducer as BlockingLoggingProducer;
      Task runTask = producer.RunAsync();
      Task produceOneTask = producer.ProduceAsync('f').AsTask();
      Task produceTwoTask = producer.ProduceAsync('r').AsTask();
      Task produceThreeTask = producer.ProduceAsync('e').AsTask();

      // attempting to produce when full should complete syncronously and that value should not be produced
      produceThreeTask.IsCompleted.Should().BeTrue();

      // allow 'f' to be written
      blockingProducer.UnblockOne();
      produceOneTask.Wait();
      logLines.Should().Contain(s => s.Contains("[Information]produced value 'f'"));

      blockingProducer.UnblockOne();
      produceTwoTask.Wait();
      logLines.Should().Contain(s => s.Contains("[Information]produced value 'r'"));

      producer.ShutdownAsync().Wait();

      logLines.Should().Contain(s => s.Contains("[Information]LoggingProducer.Shutdown"));
      logLines.Should().NotContain(s => s.Contains("[Information]produced value 'e'"));
    }

    [TestMethod]
    public void ProduceRemainingWhenShutdownTest()
    {
      Setup(out List<string> logLines, out ProducerWrapper producer, out IProducer underlyingProducer, true);
      var blockingProducer = underlyingProducer as BlockingLoggingProducer;
      Task runTask = producer.RunAsync();
      Task produceOneTask = producer.ProduceAsync('f').AsTask();
      Task produceTwoTask = producer.ProduceAsync('r').AsTask();

      Task shutdownTask = producer.ShutdownAsync();

      blockingProducer.UnblockOne(); // unblock 'f'
      produceOneTask.Wait();
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
      Setup(out List<string> logLines, out ProducerWrapper producer, out IProducer underlyingProducer, true);
      var blockingProducer = underlyingProducer as BlockingLoggingProducer;
      Task runTask = producer.RunAsync();
      Task produceOneTask = producer.ProduceAsync('f').AsTask();
      producer.ShutdownAsync().Wait();
      produceOneTask.Wait();

      Action act = () => producer.ProduceAsync('r').AsTask().Wait();
      act.Should().Throw<ChannelClosedException>();
      Debugger.Break();
    }

    private static void Setup(
      out List<string> logLines,
      out ProducerWrapper producer,
      out IProducer underlyingProducer,
      bool createBlockingProducer = false)
    {
      logLines = new List<string>();
      ILogger log = MemoryLog.Create(logLines);

      // use a regular logging producer unless an override is passed in
      underlyingProducer = createBlockingProducer ? new BlockingLoggingProducer(log) : new LoggingProducer(log);
      var factory = new ProducerFactory(CancellationToken.None, log, @"c:\dummy\dir", underlyingProducer);
      producer = new ProducerWrapper(factory, CancellationToken.None, 1);
    }

    private class BlockingLoggingProducer : LoggingProducer
    {
      // despite using async, the entire interaction with this queue is single threaded
      // so use of concurrentqueue is not needed
      private readonly Queue<char> productionQueue;
      private TaskCompletionSource<int> tcs;

      public BlockingLoggingProducer(ILogger log)
        : base(log)
      {
        this.productionQueue = new Queue<char>();
        this.tcs = new TaskCompletionSource<int>();
      }

      public override async ValueTask ProduceAsync(char value)
      {
        this.productionQueue.Enqueue(value);
        await this.tcs.Task;
        char produceValue = this.productionQueue.Dequeue();
        this.tcs = new TaskCompletionSource<int>();
        await base.ProduceAsync(produceValue);
      }

      public void UnblockOne() => this.tcs.SetResult(0);
    }
  }
}
