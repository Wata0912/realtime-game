using Microsoft.EntityFrameworkCore;
using realtime_game.Server.Models.Entities;

namespace realtime_game.Server.Models.Contexts
{
    public class GameDbContext:DbContext
    {
        public DbSet<User> Users { get; set; }

#if DEBUG
        readonly string connectionString = "server=localhost;database=realtime_game;user=jobi;password=jobi;";
#else
       readonly string connectionString = "server=db-ge0202400.mysql.database.azure.com;port=3306;database=realtime_game241211;user=student;password=Yoshidajobi2024;SslMode=Required;";

#endif
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0)));
        }
    }
}
