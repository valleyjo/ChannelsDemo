namespace ChannelsDemo.ChannelFacade
{
  public interface IWriteBuffer<T>
  {
    bool TryWrite(T item);
  }
}
