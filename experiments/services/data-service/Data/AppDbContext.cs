using data_service.Models;
using Microsoft.EntityFrameworkCore;

namespace data_service.Data;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Record> Records { get; set; }
}
