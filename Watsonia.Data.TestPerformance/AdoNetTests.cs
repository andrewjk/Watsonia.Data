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
		public List<string> LoadedItems { get; } = new List<string>();

		public long GetAllPosts()
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var conn = new SqliteConnection(WatsoniaDatabase.ConnectionString))
			{
				conn.Open();
				using (var command = new SqliteCommand("SELECT ID, Text, DateCreated, DateModified FROM Posts", conn))
				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						this.LoadedItems.Add("Post: " + reader["ID"]);
					}
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
				using (var command = new SqliteCommand("SELECT ID, FirstName, LastName, DateOfBirth, TeamID FROM Players WHERE ID = @ID", conn))
				{
					command.Parameters.Add(new SqliteParameter("@ID", id));
					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							this.LoadedItems.Add("Player: " + reader["ID"]);
						}
					}
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
				using (var command = new SqliteCommand("SELECT ID, FirstName, LastName, DateOfBirth, TeamID FROM Players WHERE TeamID = @ID", conn))
				{
					command.Parameters.Add(new SqliteParameter("@ID", teamID));
					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							this.LoadedItems.Add("Player: " + reader["ID"]);
						}
					}
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
					"SELECT p.ID, p.FirstName, p.LastName, p.DateOfBirth, p.TeamID, t.ID as TeamID, t.Name, t.SportID " +
					"FROM Players p " +
					"INNER JOIN Teams t ON p.TeamID = t.ID " +
					"WHERE t.SportID = @ID";
				using (var command = new SqliteCommand(query, conn))
				{
					command.Parameters.Add(new SqliteParameter("@ID", sportID));
					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							this.LoadedItems.Add("Team Player: " + reader["ID"]);
						}
					}
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}
	}
}
