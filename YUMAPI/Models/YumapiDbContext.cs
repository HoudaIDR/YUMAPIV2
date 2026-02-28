using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Configuration;

namespace YUMAPI.Models
{
    public class YumapiDbContext : DbContext
    {
        // Une table "Users" dans la BDD (comme dans le cours)
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Lire la connexion depuis app.config (comme dans le cours)
            string host = ConfigurationManager.AppSettings["host"] ?? "localhost";
            string port = ConfigurationManager.AppSettings["port"] ?? "3306";
            string user = ConfigurationManager.AppSettings["user"] ?? "root";
            string password = ConfigurationManager.AppSettings["password"] ?? "";
            string database = ConfigurationManager.AppSettings["database"] ?? "yumapi_db";
            string version = ConfigurationManager.AppSettings["mysqlVer"] ?? "8.0.21-mysql";

            string connectionString = $"server={host};port={port};user={user};password={password};database={database}";

            optionsBuilder.UseMySql(connectionString,
                Microsoft.EntityFrameworkCore.ServerVersion.Parse(version));
        }
    }
}