namespace AuthService.Exceptions
{
    public class AuthException : Exception
    {
        public string ErrorCode { get; }
        
        public AuthException(string errorCode) : base(errorCode) 
        { 
            ErrorCode = errorCode;
        }
        
        public AuthException(string errorCode, string message) : base(message) 
        { 
            ErrorCode = errorCode;
        }
        
        public AuthException(string errorCode, Exception innerException) : base(errorCode, innerException) 
        { 
            ErrorCode = errorCode;
        }
        
        public AuthException(string errorCode, string message, Exception innerException) : base(message, innerException) 
        { 
            ErrorCode = errorCode;
        }
    }

    public class UserNotFoundException : AuthException
    {
        public UserNotFoundException(string email) : base("USER_NOT_FOUND", $"User with email '{email}' not found") { }
        public UserNotFoundException(Guid id) : base("USER_NOT_FOUND", $"User with ID '{id}' not found") { }
    }

    public class InvalidCredentialsException : AuthException
    {
        public InvalidCredentialsException() : base("INVALID_CREDENTIALS", "Invalid email or password") { }
    }

    public class UserAlreadyExistsException : AuthException
    {
        public UserAlreadyExistsException(string email) : base("USER_ALREADY_EXISTS", $"User with email '{email}' already exists") { }
    }

    public class UsernameAlreadyExistsException : AuthException
    {
        public UsernameAlreadyExistsException(string username) : base("USERNAME_ALREADY_EXISTS", $"Username '{username}' already exists") { }
    }

    public class UserLockedException : AuthException
    {
        public UserLockedException(string message) : base("ACCOUNT_LOCKED", message) { }
    }

    public class InvalidTokenException : AuthException
    {
        public InvalidTokenException() : base("TOKEN_INVALID", "Invalid or expired token") { }
    }

    public class InvalidResetTokenException : AuthException
    {
        public InvalidResetTokenException() : base("INVALID_RESET_TOKEN", "Invalid or expired reset token") { }
    }

    public class PasswordMismatchException : AuthException
    {
        public PasswordMismatchException() : base("PASSWORD_MISMATCH", "Current password is incorrect") { }
    }

    public class InvalidGoogleTokenException : AuthException
    {
        public InvalidGoogleTokenException(string message) : base("GOOGLE_AUTH_FAILED", message) { }
    }

    public class EmailNotExistsException : AuthException
    {
        public EmailNotExistsException(string email) : base("EMAIL_NOT_AVAILABLE", $"Email does not exist: {email}") { }
    }

    public class EmailNotVerifiedException : AuthException
    {
        public EmailNotVerifiedException(string email) : base("EMAIL_VERIFICATION_FAILED", $"Email is not verified: {email}") { }
    }

    public class AccountDeletedException : AuthException
    {
        public AccountDeletedException() : base("ACCOUNT_DELETED", "Account has been deleted") { }
    }

    public class AccountBannedException : AuthException
    {
        public AccountBannedException() : base("ACCOUNT_BANNED", "Your account has been banned") { }
    }

    public class AccountNotVerifiedException : AuthException
    {
        public AccountNotVerifiedException() : base("ACCOUNT_NOT_VERIFIED", "Account is not verified") { }
    }
} 