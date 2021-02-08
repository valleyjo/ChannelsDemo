namespace ChannelsDemo.Test
{
  using System;
  using System.Collections.Generic;
  using System.Threading;
  using System.Threading.Tasks;
  using ChannelsDemo.ChannelFacade;
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
      Setup(out List<string> logLines, out IWriteBuffer<char> writeBuffer, out BlockingLoggingProducer blockingLoggingProducer, out ProducerWrapper<char> producer);
      writeBuffer.TryWrite('c').Should().BeTrue();
      Task producerTask = producer.RunAsync();
      blockingLoggingProducer.UnblockOne();
      logLines.Should().Contain(s => s.Contains("[Information]produced value 'c'"));
    }

    [TestMethod]
    public void RunThenProduceTest()
    {
      Setup(out List<string> logLines, out IWriteBuffer<char> writeBuffer, out BlockingLoggingProducer blockingLoggingProducer, out ProducerWrapper<char> producer);
      Task producerTask = producer.RunAsync();
      writeBuffer.TryWrite('f').Should().BeTrue();
      blockingLoggingProducer.UnblockOne();
      logLines.Should().Contain(s => s.Contains("[Information]produced value 'f'"));
    }

    [TestMethod]
    public void ProduceWhenFullTest()
    {
      Setup(out List<string> logLines, out IWriteBuffer<char> writeBuffer, out BlockingLoggingProducer blockingProducer, out ProducerWrapper<char> producer);
      Task runTask = producer.RunAsync();
      writeBuffer.TryWrite('f').Should().BeTrue();
      writeBuffer.TryWrite('r').Should().BeFalse();
      writeBuffer.TryWrite('e').Should().BeFalse();

      // allow 'f' to be written
      blockingProducer.UnblockOne();
      logLines.Should().Contain(s => s.Contains("[Information]produced value 'f'"));

      // allow 'r' to be written
      blockingProducer.UnblockOne();
      logLines.Should().Contain(s => s.Contains("[Information]produced value 'r'"));
    }

    [TestMethod]
    public void ProduceAfterShutdownTest()
    {
      var cts = new CancellationTokenSource();
      Setup(
        out List<string> logLines,
        out IWriteBuffer<char> writeBuffer,
        out BlockingLoggingProducer underlyingProducer,
        out ProducerWrapper<char> producer,
        cts);

      Task runTask = producer.RunAsync();
      cts.Cancel();
      runTask.Wait();
      Action act = () => writeBuffer.TryWrite('f');
      act.Should().Throw<ObjectDisposedException>();
    }

    private static void Setup(
      out List<string> logLines,
      out IWriteBuffer<char> writeBuffer,
      out BlockingLoggingProducer blockingLoggingProducer,
      out ProducerWrapper<char> producer,
      CancellationTokenSource cts = null)
    {
      CancellationToken token = cts == null ? CancellationToken.None : cts.Token;
      logLines = new List<string>();
      ILogger log = MemoryLog.Create(logLines);

      // use a regular logging producer unless an override is passed in
      var factory = new BlockingLoggingProducerFactory(log);
      blockingLoggingProducer = factory.Instance;
      var channel = new UnboundedChannelFacade<char>();
      producer = new ProducerWrapper<char>(factory, token, channel);
      writeBuffer = channel;
    }

    private class BlockingLoggingProducerFactory : IProducerFactory<char>
    {
      public BlockingLoggingProducerFactory(ILogger logger) => this.Instance = new BlockingLoggingProducer(logger);

      public BlockingLoggingProducer Instance { get; private set; }

      public IProducer<char> Get(string fileName) => this.Instance;
    }

    private class BlockingLoggingProducer : LoggingProducer<char>
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
