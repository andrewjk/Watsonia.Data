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
		public List<string> LoadedItems { get; } = new List<string>();

		public long GetAllPosts()
		{
			var watch = new Stopwatch();
			watch.Start();
			using (var db = new WatsoniaDatabase())
			{
				var posts = db.LoadCollection<Post>("SELECT ID, Text, DateCreated, DateModified FROM Posts").ToList();
				foreach (var p in posts)
				{
					this.LoadedItems.Add("Post: " + p.ID);
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
				var player = db.LoadCollection<Player>("SELECT ID, FirstName, LastName, DateOfBirth, TeamID FROM Players WHERE ID = @0", id).First();
				this.LoadedItems.Add("Player: " + player.ID);
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
				var players = db.LoadCollection<Player>("SELECT ID, FirstName, LastName, DateOfBirth, TeamID FROM Players WHERE TeamID = @0", teamID);
				foreach (var p in players)
				{
					this.LoadedItems.Add("Player: " + p.ID);
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
				var players = db.LoadCollection<Player>("" +
					"SELECT p.ID, p.FirstName, p.LastName, p.DateOfBirth, p.TeamID, t.ID as TeamID, t.Name, t.SportID " +
					"FROM Teams t " +
					"INNER JOIN Players p ON t.ID = p.TeamID " +
					"WHERE t.SportID = @0", sportID);
				foreach (var p in players)
				{
					this.LoadedItems.Add("Team Player: " + p.ID);
				}
			}
			watch.Stop();
			return watch.ElapsedMilliseconds;
		}
	}
}
