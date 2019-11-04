using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Watsonia.Data.TestPerformance.Entities;

namespace Watsonia.Data.TestPerformance.Tests
{
	public class DapperTests : IPerformanceTests
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
			using (var conn = Config.OpenConnection())
			{
				var query = "SELECT ID FROM Posts";
				var allPostIDs = conn.Query<long>(query).ToList();
				foreach (var id in allPostIDs)
				{
					this.LoadedPostIDs.Add(id);
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetAllPosts()
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var conn = Config.OpenConnection())
			{
				var query = "SELECT ID, Text, DateCreated, DateModified FROM Posts";
				var allPosts = conn.Query<Post>(query).ToList();
				foreach (var post in allPosts)
				{
					this.LoadedPosts.Add(post);
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetPlayerByID(long id)
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var conn = Config.OpenConnection())
			{
				var query = "SELECT ID, FirstName, LastName, DateOfBirth, TeamsID FROM Players WHERE ID = @ID";
				var player = conn.QuerySingle<Player>(query, new { ID = id });
				this.LoadedPlayers.Add(player);
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetPlayersForTeam(long teamID)
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var conn = Config.OpenConnection())
			{
				var query = "SELECT ID, FirstName, LastName, DateOfBirth, TeamsID FROM Players WHERE TeamsID = @ID";
				var playersForTeam = conn.Query<Player>(query, new { ID = teamID });
				foreach (var player in playersForTeam)
				{
					this.LoadedPlayersForTeam.Add(player);
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetTeamsForSport(long sportID)
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var conn = Config.OpenConnection())
			{
				var query = "" +
					"SELECT p.ID, p.FirstName, p.LastName, p.DateOfBirth, p.TeamsID, t.ID as TeamsID, t.Name, t.SportsID " +
					"FROM Teams t " +
					"INNER JOIN Players p ON t.ID = p.TeamsID " +
					"WHERE t.SportsID = @ID";
				var playersForSport = conn.Query<Player, Team, Player>(query,
					(player, team) => { return player; }, splitOn: "TeamsID", param: new { ID = sportID });
				foreach (var player in playersForSport)
				{
					this.LoadedTeamsForSport.Add(player);
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}
	}
}
