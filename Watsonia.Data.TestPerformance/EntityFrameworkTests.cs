using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Watsonia.Data.TestPerformance
{
	public class EntityFrameworkTests : IPerformanceTests
	{
		public List<string> LoadedItems { get; } = new List<string>();

		public long GetAllPosts()
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var context = new EntityFrameworkContext())
			{
				var posts = context.Posts;
				foreach (var p in posts)
				{
					this.LoadedItems.Add("Post: " + p.ID);
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetPlayerByID(long id)
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var context = new EntityFrameworkContext())
			{
				var player = context.Players.Find(id);
				this.LoadedItems.Add("Player: " + player.ID);
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetPlayersForTeam(long teamID)
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var context = new EntityFrameworkContext())
			{
				var players = context.Players.AsNoTracking().Where(x => x.TeamsID == teamID);
				foreach (var p in players)
				{
					this.LoadedItems.Add("Player: " + p.ID);
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetTeamsForSport(long sportID)
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var context = new EntityFrameworkContext())
			{
				var teams = context.Teams.AsNoTracking().Include(x => x.Players).Where(x => x.SportsID == sportID);
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
