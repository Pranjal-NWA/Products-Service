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
public class BadRequestApiException : ApiException
{
    public override int StatusCode => 400;
    public BadRequestApiException(string message) : base(message) { }
}
public class ConflictApiException : ApiException
{
    public override int StatusCode => 409;
    public ConflictApiException(string message) : base(message) { }
}