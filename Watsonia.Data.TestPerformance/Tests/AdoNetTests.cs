using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Watsonia.Data.TestPerformance.Entities;

namespace Watsonia.Data.TestPerformance.Tests
{
	public class AdoNetTests : IPerformanceTests
	{
		public List<long> LoadedPostIDs { get; } = new List<long>();
		public List<IEntity> LoadedPosts { get; } = new List<IEntity>();
		public List<IEntity> LoadedPlayers { get; } = new List<IEntity>();
		public List<IEntity> LoadedPlayersForTeam { get; } = new List<IEntity>();
		public List<IEntity> LoadedTeamsForSport { get; } = new List<IEntity>();

		public void GetAllPostIDs()
		{
			using (var conn = Config.OpenConnection())
			{
				var query = "SELECT ID FROM Posts";
				using var command = Config.CreateCommand(query, conn);
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					this.LoadedPostIDs.Add(reader.GetInt64(0));
				}
			}
		}

		public void GetAllPosts()
		{
			using (var conn = Config.OpenConnection())
			{
				var query = "SELECT ID, Text, DateCreated, DateModified FROM Posts";
				using var command = Config.CreateCommand(query, conn);
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					this.LoadedPosts.Add(new Post()
					{
						ID = reader.GetInt64(0),
						Text = reader.GetString(1),
						DateCreated = reader.GetDateTime(2),
						DateModified = reader.GetDateTime(3)
					});
				}
			}
		}

		public void GetPlayerByID(long id)
		{
			using (var conn = Config.OpenConnection())
			{
				var query = "SELECT ID, FirstName, LastName, DateOfBirth, TeamsID FROM Players WHERE ID = @ID";
				using var command = Config.CreateCommand(query, conn);
				command.Parameters.Add(Config.CreateParameter("@ID", id));
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					this.LoadedPlayers.Add(new Player()
					{
						ID = reader.GetInt64(0),
						FirstName = reader.GetString(1),
						LastName = reader.GetString(2),
						DateOfBirth = reader.GetDateTime(3),
						TeamsID = reader.GetInt64(4)
					});
				}
			}
		}

		public void GetPlayersForTeam(long teamID)
		{
			using (var conn = Config.OpenConnection())
			{
				var query = "SELECT ID, FirstName, LastName, DateOfBirth, TeamsID FROM Players WHERE TeamsID = @ID";
				using var command = Config.CreateCommand(query, conn);
				command.Parameters.Add(Config.CreateParameter("@ID", teamID));
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					this.LoadedPlayersForTeam.Add(new Player()
					{
						ID = reader.GetInt64(0),
						FirstName = reader.GetString(1),
						LastName = reader.GetString(2),
						DateOfBirth = reader.GetDateTime(3),
						TeamsID = reader.GetInt64(4)
					});
				}
			}
		}

		public void GetTeamsForSport(long sportID)
		{
			using (var conn = Config.OpenConnection())
			{
				var query = "" +
					"SELECT p.ID, p.FirstName, p.LastName, p.DateOfBirth, p.TeamsID, t.ID as TeamsID, t.Name, t.SportsID " +
					"FROM Teams t " +
					"INNER JOIN Players p ON t.ID = p.TeamsID " +
					"WHERE t.SportsID = @ID";
				using var command = Config.CreateCommand(query, conn);
				command.Parameters.Add(Config.CreateParameter("@ID", sportID));
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					this.LoadedTeamsForSport.Add(new Player()
					{
						ID = reader.GetInt64(0),
						FirstName = reader.GetString(1),
						LastName = reader.GetString(2),
						DateOfBirth = reader.GetDateTime(3),
						TeamsID = reader.GetInt64(4)
					});
				}
			}
		}
	}
}
