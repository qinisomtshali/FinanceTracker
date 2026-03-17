namespace FinanceTracker.Application.Common.Models;

/// <summary>
/// A generic Result wrapper — every operation returns either a success with data
/// or a failure with error messages. No exceptions for business logic errors.
/// 
/// WHY NOT EXCEPTIONS: Exceptions are for exceptional/unexpected situations
/// (database down, null reference). "User entered invalid email" is NOT exceptional
/// — it's expected. Using Result for expected failures is:
///   1. Faster (exceptions are expensive — they capture stack traces)
///   2. Explicit (the return type tells you "this can fail")
///   3. Composable (you can chain operations and handle errors cleanly)
/// 
/// USAGE:
///   return Result<UserDto>.Success(userDto);
///   return Result<UserDto>.Failure("Email already registered");
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public List<string> Errors { get; private set; } = new();

    public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Errors = new List<string> { error } };
    public static Result<T> Failure(List<string> errors) => new() { IsSuccess = false, Errors = errors };
}
