using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace Watsonia.Data.TestPerformance
{
	public class EntityFrameworkTests : IPerformanceTests
	{
		public List<string> LoadedItems { get; } = new List<string>();

		public long GetAllPosts()
		{
			var watch = new Stopwatch();
			watch.Start();
			using (EntityFrameworkContext context = new EntityFrameworkContext())
			{
				var posts = context.Posts.ToList();
				foreach (var p in posts)
				{
					this.LoadedItems.Add("Post: " + p.ID);
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetPlayerByID(int id)
		{
			var watch = new Stopwatch();
			watch.Start();
			using (EntityFrameworkContext context = new EntityFrameworkContext())
			{
				var player = context.Players.Find(id);
				this.LoadedItems.Add("Player: " + player.ID);
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetPlayersForTeam(int teamID)
		{
			var watch = new Stopwatch();
			watch.Start();
			using (EntityFrameworkContext context = new EntityFrameworkContext())
			{
				var players = context.Players.AsNoTracking().Where(x => x.TeamsID == teamID).ToList();
				foreach (var p in players)
				{
					this.LoadedItems.Add("Player: " + p.ID);
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetTeamsForSport(int sportID)
		{
			var watch = new Stopwatch();
			watch.Start();
			using (EntityFrameworkContext context = new EntityFrameworkContext())
			{
				var teams = context.Teams.AsNoTracking().Include(x => x.Players).Where(x => x.SportsID == sportID).ToList();
				foreach (var t in teams)
				{
					foreach (var p in t.Players)
					{
						this.LoadedItems.Add("Team Player: " + p.ID);
					}
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}
	}
}
