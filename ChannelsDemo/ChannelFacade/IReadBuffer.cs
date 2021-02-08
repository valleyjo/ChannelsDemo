namespace ChannelsDemo.ChannelFacade
{
  using System.Threading;
  using System.Threading.Tasks;

  public interface IReadBuffer<T>
  {
    ValueTask<T> ReadAsync(CancellationToken token);
  }
}
