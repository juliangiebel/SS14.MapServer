namespace SS14.MapServer.Exceptions;

public class ApiErrorMessage
{
    private const string DefaultMessage = "An error occured while handling the request";
    
    public string Message { get; set; }
    public string? CausedBy { get; set; }
    
    public ApiErrorMessage()
    {
        Message = DefaultMessage;
    }

    public ApiErrorMessage(string? message)
    {
        Message = message ?? DefaultMessage;
    }

    public ApiErrorMessage(string? message, Exception? innerException)
    {
        Message = message ?? DefaultMessage;
        CausedBy = innerException?.Message;
    }
}