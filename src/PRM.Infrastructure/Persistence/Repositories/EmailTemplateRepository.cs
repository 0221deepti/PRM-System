using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Repositories;
using PRM.Domain.Entities;

namespace PRM.Infrastructure.Persistence.Repositories;

public class EmailTemplateRepository : Repository<EmailTemplate>, IEmailTemplateRepository
{
    public EmailTemplateRepository(PrmDbContext db) : base(db) { }

    public async Task<EmailTemplate?> GetByNameAsync(string name, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(x => x.Name == name && x.IsActive, ct);
}