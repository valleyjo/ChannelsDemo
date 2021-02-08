namespace ChannelsDemo.ChannelFacade
{
  using System;
  using System.Threading;
  using System.Threading.Channels;
  using System.Threading.Tasks;

  public sealed class UnboundedChannelFacade<T> : IChannel<T>
  {
    private static readonly Exception Disposed = new ObjectDisposedException(nameof(UnboundedChannelFacade<T>));

    private readonly Channel<T> channel;

    public UnboundedChannelFacade(bool allowSynchronousContinuations = true)
    {
      this.channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
      {
        AllowSynchronousContinuations = allowSynchronousContinuations,
        SingleReader = true,
        SingleWriter = true,
      });
    }

    public bool TryWrite(T item)
    {
      if (!this.channel.Writer.TryWrite(item))
      {
        throw Disposed;
      }

      return true;
    }

    public async ValueTask<T> ReadAsync(CancellationToken token)
    {
      try
      {
        return await this.channel.Reader.ReadAsync(token);
      }
      catch (ChannelClosedException)
      {
        throw Disposed;
      }
    }

    public void Dispose() => this.channel.Writer.TryComplete();
  }
}
