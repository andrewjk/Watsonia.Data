using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Watsonia.Data.TestPerformance.Entities;

namespace Watsonia.Data.TestPerformance.Tests
{
	public class WatsoniaLinqTests : IPerformanceTests
	{
		private static readonly ConcurrentDictionary<string, WatsoniaDatabase> Databases =
			new ConcurrentDictionary<string, WatsoniaDatabase>();

		private static WatsoniaDatabase GetDatabase(string databaseName)
		{
			return Databases.GetOrAdd(databaseName, n => new WatsoniaDatabase(n));
		}

		public List<long> LoadedPostIDs { get; } = new List<long>();
		public List<IEntity> LoadedPosts { get; } = new List<IEntity>();
		public List<IEntity> LoadedPlayers { get; } = new List<IEntity>();
		public List<IEntity> LoadedPlayersForTeam { get; } = new List<IEntity>();
		public List<IEntity> LoadedTeamsForSport { get; } = new List<IEntity>();

		public void GetAllPostIDs()
		{
			var db = GetDatabase("Linq");
			var allPostIDs = from p in db.Query<Post>() select p.ID;
			foreach (var id in allPostIDs)
			{
				this.LoadedPostIDs.Add(id);
			}
		}

		public void GetAllPosts()
		{
			var db = GetDatabase("Linq");
			var allPosts = db.Query<Post>();
			foreach (var post in allPosts)
			{
				this.LoadedPosts.Add(post);
			}
		}

		public void GetPlayerByID(long id)
		{
			var db = GetDatabase("Linq");
			var player = db.Load<Player>(id);
			this.LoadedPlayers.Add(player);
		}

		public void GetPlayersForTeam(long teamID)
		{
			var db = GetDatabase("Linq");
			var playersForTeam = db.Query<Player>().Where(x => x.TeamsID == teamID);
			foreach (var player in playersForTeam)
			{
				this.LoadedPlayersForTeam.Add(player);
			}
		}

		public void GetTeamsForSport(long sportID)
		{
			var db = GetDatabase("Linq");
			// TODO:
			//var teamsForSport = db.Query<Player>().Where(p => p.Team.SportsID == sportID);
			var teamsForSport = db.Query<Team>().Include(x => x.Players).Where(x => x.SportsID == sportID);
			foreach (var team in teamsForSport)
			{
				foreach (var player in team.Players)
				{
					this.LoadedTeamsForSport.Add(player);
				}
			}
		}
	}
}
