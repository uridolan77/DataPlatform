using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.API.Models.Auth;

namespace GenericDataPlatform.API.Services.Auth
{
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(string id);
        Task<User> GetByUsernameAsync(string username);
        Task<User> GetByEmailAsync(string email);
        Task<bool> CreateAsync(User user);
        Task<bool> UpdateAsync(User user);
        Task<bool> DeleteAsync(string id);
        Task<IEnumerable<User>> GetAllAsync();
    }
}
