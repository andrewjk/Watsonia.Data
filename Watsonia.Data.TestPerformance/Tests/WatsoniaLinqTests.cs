using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Watsonia.Data.TestPerformance.Entities;

namespace Watsonia.Data.TestPerformance.Tests
{
	public class WatsoniaLinqTests : IPerformanceTests
	{
		public List<long> LoadedPostIDs { get; } = new List<long>();
		public List<IEntity> LoadedPosts { get; } = new List<IEntity>();
		public List<IEntity> LoadedPlayers { get; } = new List<IEntity>();
		public List<IEntity> LoadedPlayersForTeam { get; } = new List<IEntity>();
		public List<IEntity> LoadedTeamsForSport { get; } = new List<IEntity>();

		public long GetAllPostIDs()
		{
			var watch = new Stopwatch();
			watch.Start();
			var db = new WatsoniaDatabase("Linq");
			var allPostIDs = db.Query<Post>().Select(p => p.ID);
			foreach (var id in allPostIDs)
			{
				this.LoadedPostIDs.Add(id);
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetAllPosts()
		{
			var watch = new Stopwatch();
			watch.Start();
			var db = new WatsoniaDatabase("Linq");
			var allPosts = db.Query<Post>();
			foreach (var post in allPosts)
			{
				this.LoadedPosts.Add(post);
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetPlayerByID(long id)
		{
			var watch = new Stopwatch();
			watch.Start();
			var db = new WatsoniaDatabase("Linq");
			var player = db.Load<Player>(id);
			this.LoadedPlayers.Add(player);
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetPlayersForTeam(long teamID)
		{
			var watch = new Stopwatch();
			watch.Start();
			var db = new WatsoniaDatabase("Linq");
			var playersForTeam = db.Query<Player>().Where(x => x.TeamsID == teamID);
			foreach (var player in playersForTeam)
			{
				this.LoadedPlayersForTeam.Add(player);
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetTeamsForSport(long sportID)
		{
			var watch = new Stopwatch();
			watch.Start();
			var db = new WatsoniaDatabase("Linq");
			var teamsForSport = db.Query<Team>().Include(x => x.Players).Where(x => x.SportsID == sportID);
			foreach (var team in teamsForSport)
			{
				foreach (var player in team.Players)
				{
					this.LoadedTeamsForSport.Add(player);
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}
	}
}
