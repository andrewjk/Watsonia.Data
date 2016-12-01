using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.TestPerformance.Entities;

namespace Watsonia.Data.TestPerformance
{
	public partial class EntityFrameworkContext : DbContext
	{
		public EntityFrameworkContext()
			: base("name=Performance")
		{
		}

		public virtual DbSet<Post> Posts { get; set; }
		public virtual DbSet<Player> Players { get; set; }
		public virtual DbSet<Sport> Sports { get; set; }
		public virtual DbSet<Team> Teams { get; set; }

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			// NOTE: Could remove this convention if we wanted?
			//modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

			modelBuilder.Entity<Sport>()
				.HasMany(e => e.Teams)
				.WithRequired(e => e.Sport)
				.WillCascadeOnDelete(false);

			modelBuilder.Entity<Team>()
				.HasMany(e => e.Players)
				.WithRequired(e => e.Team)
				.WillCascadeOnDelete(false);
		}
	}
}
