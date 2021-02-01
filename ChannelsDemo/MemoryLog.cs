namespace ChannelsDemo
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using Microsoft.Extensions.Logging;

  /// <summary>
  /// This class is an ILogger implementation which stores log lines in memory.
  /// It can be used for unit tests or in production when logging is not needed.
  /// </summary>
  public static class MemoryLog
  {
    public static ILogger Create(IList<string> logLines, Func<object, IDisposable> beginScope = null, Func<LogLevel, bool> isEnabled = null)
    {
      return new LoggerImpl(logLines, beginScope, isEnabled);
    }

    private sealed class LoggerImpl : ILogger
    {
      private readonly IList<string> logLines;
      private readonly Func<object, IDisposable> beginScope;
      private readonly Func<LogLevel, bool> isEnabled;

      public LoggerImpl(IList<string> logLines, Func<object, IDisposable> beginScope, Func<LogLevel, bool> isEnabled)
      {
        this.logLines = logLines;
        this.beginScope = beginScope ?? (_ => null);
        this.isEnabled = isEnabled ?? (_ => true);
      }

      public IDisposable BeginScope<TState>(TState state) => this.beginScope(state);

      public bool IsEnabled(LogLevel logLevel) => this.isEnabled(logLevel);

      public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
      {
        this.logLines.Add(string.Format(CultureInfo.InvariantCulture, "[{0}]{1}", logLevel, formatter(state, exception)));
      }
    }
  }
}
