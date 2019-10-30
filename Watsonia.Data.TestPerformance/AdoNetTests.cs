using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.TestPerformance
{
	public class AdoNetTests : IPerformanceTests
	{
		public List<long> LoadedPosts { get; } = new List<long>();
		public List<long> LoadedPlayers { get; } = new List<long>();
		public List<long> LoadedPlayersForTeam { get; } = new List<long>();
		public List<long> LoadedTeamsForSport { get; } = new List<long>();

		public long GetAllPosts()
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var conn = new SqliteConnection(WatsoniaDatabase.ConnectionString))
			{
				conn.Open();
				using var command = new SqliteCommand("SELECT ID, Text, DateCreated, DateModified FROM Posts", conn);
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					this.LoadedPosts.Add((long)reader["ID"]);
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetPlayerByID(long id)
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var conn = new SqliteConnection(WatsoniaDatabase.ConnectionString))
			{
				conn.Open();
				using var command = new SqliteCommand("SELECT ID, FirstName, LastName, DateOfBirth, TeamsID FROM Players WHERE ID = @ID", conn);
				command.Parameters.Add(new SqliteParameter("@ID", id));
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					this.LoadedPlayers.Add((long)reader["ID"]);
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetPlayersForTeam(long teamID)
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var conn = new SqliteConnection(WatsoniaDatabase.ConnectionString))
			{
				conn.Open();
				using var command = new SqliteCommand("SELECT ID, FirstName, LastName, DateOfBirth, TeamsID FROM Players WHERE TeamsID = @ID", conn);
				command.Parameters.Add(new SqliteParameter("@ID", teamID));
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					this.LoadedPlayersForTeam.Add((long)reader["ID"]);
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetTeamsForSport(long sportID)
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var conn = new SqliteConnection(WatsoniaDatabase.ConnectionString))
			{
				conn.Open();
				var query = "" +
					"SELECT p.ID, p.FirstName, p.LastName, p.DateOfBirth, p.TeamsID, t.ID as TeamsID, t.Name, t.SportsID " +
					"FROM Players p " +
					"INNER JOIN Teams t ON p.TeamsID = t.ID " +
					"WHERE t.SportsID = @ID";
				using var command = new SqliteCommand(query, conn);
				command.Parameters.Add(new SqliteParameter("@ID", sportID));
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					this.LoadedTeamsForSport.Add((long)reader["ID"]);
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}
	}
}
