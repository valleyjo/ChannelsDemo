namespace ChannelsDemo
{
  using System;
  using System.Diagnostics;
  using System.IO;
  using System.Threading;
  using System.Threading.Tasks;
  using ChannelsDemo.ProducerFacade;
  using Microsoft.Extensions.Logging;

  public class Program
  {
    public static void Main(string[] args)
    {
      ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
      ILogger logger = loggerFactory.CreateLogger("ChannelsDemo");
      var cts = new CancellationTokenSource();

      logger.LogInformation("Starting Producer / Consumer demo");

      var producerFactory = new ProducerFactory(cts.Token, logger, GetWorkingDirectory());
      var producer = new ProducerWrapper(producerFactory, cts.Token, 10);

      Task producerTask = producer.RunAsync();

      RunLoop(producer, logger);
      cts.Cancel();

      Console.WriteLine();
      logger.LogInformation("Waiting for producer to shutdown");
      producer.ShutdownAsync().Wait();
      logger.LogInformation("Finished. Exiting.");
    }

    private static string GetWorkingDirectory() =>
      Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

    private static void RunLoop(ProducerWrapper producer, ILogger logger)
    {
      logger.LogInformation("Press 'esc' to exit");
      logger.LogInformation("Press any key to stream it to the output file:");

      while (true)
      {
        ConsoleKeyInfo keyPress = Console.ReadKey();
        if (keyPress.Key == ConsoleKey.Escape)
        {
          return;
        }

        producer.Produce(keyPress.KeyChar);
      }
    }
 }
}
