using Core.CrossCuttingConcerns.Exceptions.Types;

namespace Core.CrossCuttingConcerns.Exceptions.Handlers;

public abstract class ExceptionHandler
{
    public Task HandleExceptionAsync(Exception exception) =>
        exception switch
        {
            AuthorizationException authorizationException => HandleException(authorizationException),
            BusinessException businessException => HandleException(businessException),
            NotFoundException notFoundException => HandleException(notFoundException),
            ValidationException validationException => HandleException(validationException),
            _ => HandleException(exception)
        };

    protected abstract Task HandleException(AuthorizationException businessException);
    protected abstract Task HandleException(BusinessException validationException);
    protected abstract Task HandleException(NotFoundException validationException);
    protected abstract Task HandleException(ValidationException validationException);
    protected abstract Task HandleException(Exception exception);
}
