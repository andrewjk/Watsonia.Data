using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Watsonia.Data.TestPerformance.Entities;

namespace Watsonia.Data.TestPerformance.Tests
{
	public class WatsoniaSqlTests : IPerformanceTests
	{
		public List<long> LoadedPostIDs { get; } = new List<long>();
		public List<IEntity> LoadedPosts { get; } = new List<IEntity>();
		public List<IEntity> LoadedPlayers { get; } = new List<IEntity>();
		public List<IEntity> LoadedPlayersForTeam { get; } = new List<IEntity>();
		public List<IEntity> LoadedTeamsForSport { get; } = new List<IEntity>();

		public void GetAllPostIDs()
		{
			var db = new WatsoniaDatabase("Sql");
			var allPostIDs = db.LoadCollection<long>("SELECT ID FROM Posts");
			foreach (var id in allPostIDs)
			{
				this.LoadedPostIDs.Add(id);
			}
		}

		public void GetAllPosts()
		{
			var db = new WatsoniaDatabase("Sql");
			var allPosts = db.LoadCollection<Post>("SELECT ID, Text, DateCreated, DateModified FROM Posts");
			foreach (var post in allPosts)
			{
				this.LoadedPosts.Add(post);
			}
		}

		public void GetPlayerByID(long id)
		{
			var db = new WatsoniaDatabase("Sql");
			var player = db.Load<Player>(id);
			this.LoadedPlayers.Add(player);
		}

		public void GetPlayersForTeam(long teamID)
		{
			var db = new WatsoniaDatabase("Sql");
			var playersForTeam = db.LoadCollection<Player>("SELECT ID, FirstName, LastName, DateOfBirth, TeamsID FROM Players WHERE TeamsID = @0", teamID);
			foreach (var player in playersForTeam)
			{
				this.LoadedPlayersForTeam.Add(player);
			}
		}

		public void GetTeamsForSport(long sportID)
		{
			var db = new WatsoniaDatabase("Sql");
			var playersForSport = db.LoadCollection<Player>("" +
				"SELECT p.ID, p.FirstName, p.LastName, p.DateOfBirth, p.TeamsID, t.ID as TeamsID, t.Name, t.SportsID " +
				"FROM Teams t " +
				"INNER JOIN Players p ON t.ID = p.TeamsID " +
				"WHERE t.SportsID = @0", sportID);
			foreach (var player in playersForSport)
			{
				this.LoadedTeamsForSport.Add(player);
			}
		}
	}
}
