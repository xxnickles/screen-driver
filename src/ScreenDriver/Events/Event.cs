namespace ScreenDriver.Events;

public abstract record Event(string Source, string Message);
public record Info(string Source, string Message) : Event(Source, Message);
public record Warning(string Source, string Message) : Event(Source, Message);
public record Error(string Source, Exception Exception) : Event(Source, Exception.Message);
