using Practical.MultipleAuthenticationSchemes.Api.Models;

namespace Practical.MultipleAuthenticationSchemes.Api.Services;

public class UserService : IUserService
{
    // In-memory user store for demonstration purposes
    // In a real application, this would query a database
    private static readonly List<(string Username, string Password, UserModel User)> _users = new()
    {
        ("admin", "password123", new UserModel { Id = "1", UserName = "admin", Email = "admin@example.com" }),
        ("user", "user123", new UserModel { Id = "2", UserName = "user", Email = "user@example.com" }),
        ("testuser", "test123", new UserModel { Id = "3", UserName = "testuser", Email = "test@example.com" })
    };

    public Task<UserModel?> ValidateCredentialsAsync(string username, string password)
    {
        var user = _users.FirstOrDefault(u => 
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && 
            u.Password == password);

        return Task.FromResult(user.User);
    }

    public Task<UserModel?> GetByUsernameAsync(string username)
    {
        var user = _users.FirstOrDefault(u => 
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(user.User);
    }
}
