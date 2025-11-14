using Proyecto1.Models;

namespace Proyecto1.Services.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}