using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
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
			using (var conn = new SqlCeConnection(WatsoniaDatabase.ConnectionString))
			{
				conn.Open();
				using (var adapter = new SqlCeDataAdapter("SELECT ID, Text, DateCreated, DateModified FROM Posts", conn))
				{
					var table = new DataTable();
					adapter.Fill(table);
					foreach (DataRow p in table.Rows)
					{
						this.LoadedItems.Add("Post: " + p["ID"]);
					}
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetPlayerByID(int id)
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var conn = new SqlCeConnection(WatsoniaDatabase.ConnectionString))
			{
				conn.Open();
				using (var adapter = new SqlCeDataAdapter("SELECT ID, FirstName, LastName, DateOfBirth, TeamID FROM Players WHERE ID = @ID", conn))
				{
					adapter.SelectCommand.Parameters.Add(new SqlCeParameter("@ID", id));
					var table = new DataTable();
					adapter.Fill(table);
					this.LoadedItems.Add("Player: " + table.Rows[0]["ID"]);
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetPlayersForTeam(int teamID)
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var conn = new SqlCeConnection(WatsoniaDatabase.ConnectionString))
			{
				conn.Open();
				using (var adapter = new SqlCeDataAdapter("SELECT ID, FirstName, LastName, DateOfBirth, TeamID FROM Players WHERE TeamID = @ID", conn))
				{
					adapter.SelectCommand.Parameters.Add(new SqlCeParameter("@ID", teamID));
					var table = new DataTable();
					adapter.Fill(table);
					foreach (DataRow p in table.Rows)
					{
						this.LoadedItems.Add("Player: " + p["ID"]);
					}
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}

		public long GetTeamsForSport(int sportID)
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var conn = new SqlCeConnection(WatsoniaDatabase.ConnectionString))
			{
				conn.Open();
				string query = "" +
					"SELECT p.ID, p.FirstName, p.LastName, p.DateOfBirth, p.TeamID, t.ID as TeamID, t.Name, t.SportID " +
					"FROM Players p " +
					"INNER JOIN Teams t ON p.TeamID = t.ID " +
					"WHERE t.SportID = @ID";
				using (var adapter = new SqlCeDataAdapter(query, conn))
				{
					adapter.SelectCommand.Parameters.Add(new SqlCeParameter("@ID", sportID));
					var table = new DataTable();
					adapter.Fill(table);
					foreach (DataRow p in table.Rows)
					{
						this.LoadedItems.Add("Team Player: " + p["ID"]);
					}
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}
	}
}
