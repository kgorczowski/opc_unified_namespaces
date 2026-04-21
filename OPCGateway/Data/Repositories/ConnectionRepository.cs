using Microsoft.EntityFrameworkCore;
using OPCGateway.Data.Entities;

namespace OPCGateway.Data.Repositories;

public class ConnectionRepository(ApplicationDbContext context, PasswordEncryptor passwordEncryptor) : IConnectionRepository
{
    public async Task SaveConnectionParametersAsync(ConnectionParameters parameters)
    {
        // Encrypt the password before saving
        parameters.Password = passwordEncryptor.Encrypt(parameters.Password);

        context.ConnectionParameters.Add(parameters);
        await context.SaveChangesAsync();
    }

    public async Task<ConnectionParameters?> LoadConnectionParametersAsync(string connectionId)
    {
        var parameters = await context.ConnectionParameters
            .FirstOrDefaultAsync(c => c.ConnectionId == connectionId);

        if (parameters == null)
        {
            return null;
        }

        parameters.Password = passwordEncryptor.Decrypt(parameters.Password);

        context.Entry(parameters).State = EntityState.Detached;

        return parameters;
    }
}