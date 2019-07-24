using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Watsonia.Data.TestPerformance.Entities;
using Microsoft.Data.Sqlite;

namespace Watsonia.Data.TestPerformance
{
	public class WatsoniaSqlTests : IPerformanceTests
	{
		public List<long> LoadedPosts { get; } = new List<long>();
		public List<long> LoadedPlayers { get; } = new List<long>();
		public List<long> LoadedPlayersForTeam { get; } = new List<long>();
		public List<long> LoadedTeamsForSport { get; } = new List<long>();

		public long GetAllPosts()
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var db = new WatsoniaDatabase())
			{
				var posts = db.LoadCollectionAsync<Post>("SELECT ID, Text, DateCreated, DateModified FROM Posts").GetAwaiter().GetResult();
				foreach (var p in posts)
				{
					this.LoadedPosts.Add(p.ID);
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetPlayerByID(long id)
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var db = new WatsoniaDatabase())
			{
				// TODO: LoadItem?
				var p = db.LoadCollectionAsync<Player>("SELECT ID, FirstName, LastName, DateOfBirth, TeamsID FROM Players WHERE ID = @0", id).GetAwaiter().GetResult().First();
				this.LoadedPlayers.Add(p.ID);
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetPlayersForTeam(long teamID)
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var db = new WatsoniaDatabase())
			{
				var players = db.LoadCollectionAsync<Player>("SELECT ID, FirstName, LastName, DateOfBirth, TeamsID FROM Players WHERE TeamsID = @0", teamID).GetAwaiter().GetResult();
				foreach (var p in players)
				{
					this.LoadedPlayersForTeam.Add(p.ID);
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetTeamsForSport(long sportID)
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var db = new WatsoniaDatabase())
			{
				var players = db.LoadCollectionAsync<Player>("" +
					"SELECT p.ID, p.FirstName, p.LastName, p.DateOfBirth, p.TeamsID, t.ID as TeamsID, t.Name, t.SportsID " +
					"FROM Teams t " +
					"INNER JOIN Players p ON t.ID = p.TeamsID " +
					"WHERE t.SportsID = @0", sportID).GetAwaiter().GetResult();
				foreach (var p in players)
				{
					this.LoadedTeamsForSport.Add(p.ID);
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}
	}
}
