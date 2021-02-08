namespace ChannelsDemo.ChannelFacade
{
  using System;

  public interface IChannel<T> : IReadBuffer<T>, IWriteBuffer<T>, IDisposable
  {
  }
}
