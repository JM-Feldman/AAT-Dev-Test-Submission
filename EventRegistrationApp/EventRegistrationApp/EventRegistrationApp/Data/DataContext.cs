using Microsoft.EntityFrameworkCore;
using EventRegistrationApp.Models;

namespace EventRegistrationApp.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)  
        {

        }

        public DbSet<Event> events { get; set; }
        public DbSet<User> users { get; set; }
    }
}
