using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Repositories;

public interface IEmailTemplateRepository : IRepository<EmailTemplate>
{
    Task<EmailTemplate?> GetByNameAsync(string name, CancellationToken ct = default);
}