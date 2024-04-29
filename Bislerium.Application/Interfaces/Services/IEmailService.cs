using Bislerium.Application.DTOs.Email;

namespace Bislerium.Application.Interfaces.Services;

public interface IEmailService
{
    void SendEmail(EmailDto email);
}