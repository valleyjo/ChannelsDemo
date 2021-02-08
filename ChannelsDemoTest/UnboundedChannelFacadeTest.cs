namespace ChannelsDemo.Test
{
  using ChannelsDemo.ChannelFacade;
  using Microsoft.VisualStudio.TestTools.UnitTesting;

  [TestClass]
  public sealed class UnboundedChannelFacadeTest : ChannelTestBase
  {
    protected override IChannel<T> Create<T>() => new UnboundedChannelFacade<T>();
  }
}
