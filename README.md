# ChannelsDemo
Need to use System.Threading.Channels at work and want to understand how it works.

Code in the ThirdPartyProducer folder is an approximation for a third party library which cannot be changed.

Code in the ProducerFacade folder is code which adapts the ThirdPartyProducer and provides a ProducerWrapper class which is intended to be used throughout product code. The objectives of the facade is to:

1) Provide a simpler interface over the ThirdPartyProducer for our specific use cases (imagine that ThirdPartyProducer is substantially more complicated)
2) Provide a unit testable implementation of ProducerWrapper where we can add in extra functionality on top of the provided ThirdPartyProducer
3) Provide deterministic unit tests for the ProducerWrapper
