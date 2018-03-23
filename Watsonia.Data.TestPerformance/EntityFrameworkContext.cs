using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.TestPerformance.Entities;

namespace Watsonia.Data.TestPerformance
{
	public partial class EntityFrameworkContext : DbContext
	{
		public const string ConnectionString = @"Data Source=Data\Performance.sqlite";

		public virtual DbSet<Post> Posts { get; set; }
		public virtual DbSet<Player> Players { get; set; }
		public virtual DbSet<Sport> Sports { get; set; }
		public virtual DbSet<Team> Teams { get; set; }

		public EntityFrameworkContext()
			//: base("name=Performance")
		{
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
			{
				//IConfigurationRoot configuration = new ConfigurationBuilder()
				//   .SetBasePath(Directory.GetCurrentDirectory())
				//   .AddJsonFile("appsettings.json")
				//   .Build();
				//var connectionString = configuration.GetConnectionString("DbCoreConnectionString");
				optionsBuilder.UseSqlite(ConnectionString);
			}
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			// NOTE: Could remove this convention if we wanted?
			//modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

			modelBuilder.Entity<Sport>()
				.HasMany(e => e.Teams);
			// TODO: .WithRequired(e => e.Sport)
			// TODO: .WillCascadeOnDelete(false);

			modelBuilder.Entity<Team>()
				.HasMany(e => e.Players);
				// TODO: .WithRequired(e => e.Team)
				// TODO: .WillCascadeOnDelete(false);
		}
	}
}
