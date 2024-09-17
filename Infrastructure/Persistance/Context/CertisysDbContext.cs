
using Domain.Entities.Certificates;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Context
{
    public class CertisysDbContext : DbContext
    {
        public DbSet<Certificate> Certificates {  get; set; }

        public CertisysDbContext(DbContextOptions options) : base(options)
        {
        }
    }
}
