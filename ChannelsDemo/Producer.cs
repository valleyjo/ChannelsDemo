namespace ChannelsDemo
{
  using System.Threading;
  using System.Threading.Channels;
  using System.Threading.Tasks;

  public class Producer : Channel<int>
  {
    private readonly CancellationToken token;

    public Producer(CancellationToken token)
    {
      this.token = token;
    }

    public async ValueTask ProduceAsync(int value) => await this.Writer.WriteAsync(value);

    public async Task RunAsync()
    {
      await Task.Yield();

      while (!this.token.IsCancellationRequested)
      {
        int value = await this.Reader.ReadAsync();
      }
    }
  }
}
