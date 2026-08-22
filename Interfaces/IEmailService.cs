using System.Threading.Tasks;

namespace Parkly_Backend.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }
}