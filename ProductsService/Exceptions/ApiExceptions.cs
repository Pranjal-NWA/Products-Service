namespace ProductsService.Exceptions;

public abstract class ApiException : Exception
{
    public abstract int StatusCode { get; }
    protected ApiException(string message) : base(message) { }
}
public class NotFoundApiException : ApiException
{
    public override int StatusCode => 404;
    public NotFoundApiException(string message) : base(message) { }
}
