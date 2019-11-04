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
	public class DapperTests : IPerformanceTests
	{
		public List<long> LoadedPosts { get; } = new List<long>();
		public List<long> LoadedPlayers { get; } = new List<long>();
		public List<long> LoadedPlayersForTeam { get; } = new List<long>();
		public List<long> LoadedTeamsForSport { get; } = new List<long>();

		public long GetAllPosts()
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var conn = Config.OpenConnection())
			{
				var posts = conn.Query<Player>("SELECT ID, Text, DateCreated, DateModified FROM Posts").ToList();
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
			using (var conn = Config.OpenConnection())
			{
				var p = conn.Query<Player>("SELECT ID, FirstName, LastName, DateOfBirth, TeamsID FROM Players WHERE ID = @ID", new { ID = id }).First();
				this.LoadedPlayers.Add(p.ID);
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
				var players = conn.Query<Player>("SELECT ID, FirstName, LastName, DateOfBirth, TeamsID FROM Players WHERE TeamsID = @ID", new { ID = teamID });
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
			using (var conn = Config.OpenConnection())
			{
				var players = conn.Query<Player, Team, Player>("" +
					"SELECT p.ID, p.FirstName, p.LastName, p.DateOfBirth, p.TeamsID, t.ID as TeamsID, t.Name, t.SportsID " +
					"FROM Teams t " +
					"INNER JOIN Players p ON t.ID = p.TeamsID " +
					"WHERE t.SportsID = @ID", (player, team) => { return player; }, splitOn: "TeamsID", param: new { ID = sportID });
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
