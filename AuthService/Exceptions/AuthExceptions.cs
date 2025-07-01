namespace AuthService.Exceptions
{
    public class AuthException : Exception
    {
        public AuthException(string message) : base(message) { }
        public AuthException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class UserNotFoundException : AuthException
    {
        public UserNotFoundException(string email) : base($"User with email '{email}' not found") { }
        public UserNotFoundException(Guid id) : base($"User with ID '{id}' not found") { }
    }

    public class InvalidCredentialsException : AuthException
    {
        public InvalidCredentialsException() : base("Invalid email or password") { }
    }

    public class UserAlreadyExistsException : AuthException
    {
        public UserAlreadyExistsException(string email) : base($"User with email '{email}' already exists") { }
    }

    public class InvalidTokenException : AuthException
    {
        public InvalidTokenException() : base("Invalid or expired token") { }
    }

    public class InvalidResetTokenException : AuthException
    {
        public InvalidResetTokenException() : base("Invalid or expired reset token") { }
    }

    public class PasswordMismatchException : AuthException
    {
        public PasswordMismatchException() : base("Current password is incorrect") { }
    }
} 