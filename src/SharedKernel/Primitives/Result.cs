namespace SharedKernel.Primitives
{
    public readonly record struct Result(bool IsSuccess, string? Error)
    {
        public static Result Success() => new(true, null);

        public static Result Fail(string error) => new(false, error);

        public void EnsureSuccess()
        {
            if (!IsSuccess)
            {
                throw new DomainException(Error ?? "Operation failed.");
            }
        }
    }

    public readonly record struct Result<T>(bool IsSuccess, T? Value, string? Error)
    {
        public T EnsureValue()
            => IsSuccess
                ? Value!
                : throw new DomainException(Error ?? "Operation failed.");
    }

    /// Factory methods for generic Result{T}
    public static class Results
    {
        public static Result<T> Success<T>(T value) => new(true, value, null);

        public static Result<T> Fail<T>(string error) => new(false, default, error);
    }
}
