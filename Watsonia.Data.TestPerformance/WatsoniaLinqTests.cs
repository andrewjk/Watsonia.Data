using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Watsonia.Data.TestPerformance.Entities;

namespace Watsonia.Data.TestPerformance
{
	public class WatsoniaLinqTests : IPerformanceTests
	{
		public List<long> LoadedPosts { get; } = new List<long>();
		public List<long> LoadedPlayers { get; } = new List<long>();
		public List<long> LoadedPlayersForTeam { get; } = new List<long>();
		public List<long> LoadedTeamsForSport { get; } = new List<long>();

		public long GetAllPosts()
		{
			var watch = new Stopwatch();
			watch.Start();
			var db = new WatsoniaDatabase();
			var posts = db.Query<Post>();
			foreach (var p in posts)
			{
				this.LoadedPosts.Add(p.ID);
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetPlayerByID(long id)
		{
			var watch = new Stopwatch();
			watch.Start();
			var db = new WatsoniaDatabase();
			var p = Task.Run(() => db.LoadAsync<Player>(id)).GetAwaiter().GetResult();
			this.LoadedPlayers.Add(p.ID);
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetPlayersForTeam(long teamID)
		{
			var watch = new Stopwatch();
			watch.Start();
			var db = new WatsoniaDatabase();
			var players = db.Query<Player>().Where(x => x.TeamsID == teamID);
			foreach (var p in players)
			{
				this.LoadedPlayersForTeam.Add(p.ID);
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetTeamsForSport(long sportID)
		{
			var watch = new Stopwatch();
			watch.Start();
			var db = new WatsoniaDatabase();
			var teams = db.Query<Team>().Include(x => x.Players).Where(x => x.SportsID == sportID);
			foreach (var t in teams)
			{
				foreach (var p in t.Players)
				{
					this.LoadedTeamsForSport.Add(p.ID);
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}
	}
}
