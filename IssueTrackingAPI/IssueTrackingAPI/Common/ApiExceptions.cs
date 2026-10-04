namespace IssueTrackingAPI.Common;

/// <summary>
/// Base exception cho các lỗi nghiệp vụ có thể map thẳng sang HTTP status code.
/// Được bắt bởi ExceptionHandlingMiddleware.
/// </summary>
public abstract class ApiException : Exception
{
    protected ApiException(string message) : base(message)
    {
    }
}

public class NotFoundException : ApiException
{
    public NotFoundException(string message) : base(message)
    {
    }
}

public class ForbiddenException : ApiException
{
    public ForbiddenException(string message = "Bạn không có quyền thực hiện hành động này.") : base(message)
    {
    }
}

public class BadRequestException : ApiException
{
    public BadRequestException(string message) : base(message)
    {
    }
}

public class ConflictException : ApiException
{
    public ConflictException(string message) : base(message)
    {
    }
}

public class UnauthorizedAppException : ApiException
{
    public UnauthorizedAppException(string message = "Thông tin đăng nhập không hợp lệ.") : base(message)
    {
    }
}
