using Practical.MultipleAuthenticationSchemes.Api.Models;

namespace Practical.MultipleAuthenticationSchemes.Api.Services;

public interface IUserService
{
    Task<UserModel?> ValidateCredentialsAsync(string username, string password);
    Task<UserModel?> GetByUsernameAsync(string username);
}
