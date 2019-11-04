using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.TestPerformance.Entities;

namespace Watsonia.Data.TestPerformance
{
	internal static class DataGenerator
	{
		public static async Task CheckDatabaseAsync(WatsoniaDatabase db)
		{
			await db.EnsureDatabaseCreatedAsync();
			await db.UpdateDatabaseAsync();

			if (db.Query<Player>().Count() == 0)
			{
				await GeneratePosts(db, Config.PostCount);
				var sports = await GenerateSports(db, Config.SportCount);
				foreach (var sport in sports)
				{
					var teams = await GenerateTeams(db, sport, Config.TeamsPerSportCount);
					foreach (var team in teams)
					{
						var players = GeneratePlayers(db, team, Config.PlayersPerTeamCount);
					}
				}
			}
		}

		internal static async Task<List<Post>> GeneratePosts(WatsoniaDatabase db, int count)
		{
			var posts = new List<Post>();

			for (var i = 0; i < count; i++)
			{
				var post = db.Create<Post>();
				post.Text = new string('x', 2000);
				post.DateCreated = DateTime.Now;
				post.DateModified = DateTime.Now;
				await db.SaveAsync(post);
			}

			return posts;
		}

		internal static async Task<List<Player>> GeneratePlayers(WatsoniaDatabase db, Team team, int count)
		{
			var players = new List<Player>();

			var allFirstNames = Names.GetFirstNames();
			var allLastNames = Names.GetLastNames();
			var rand = new Random();
			var start = new DateTime(1975, 1, 1);
			var end = new DateTime(1998, 1, 1);

			for (var i = 0; i < count; i++)
			{
				var player = db.Create<Player>();
				var newFirst = rand.Next(0, allFirstNames.Count - 1);
				player.FirstName = allFirstNames[newFirst];
				var newLast = rand.Next(0, allLastNames.Count - 1);
				player.LastName = allLastNames[newLast];
				player.DateOfBirth = RandomDay(rand, start, end);
				player.Team = team;
				//player.ID = (((teamId - 1) * count) + (i + 1));
				players.Add(player);

				await db.SaveAsync(player);
			}

			return players;
		}

		internal static async Task<List<Team>> GenerateTeams(WatsoniaDatabase db, Sport sport, int count)
		{
			var teams = new List<Team>();

			var allCityNames = Names.GetCityNames();
			var allTeamNames = Names.GetTeamNames();
			var rand = new Random();
			var start = new DateTime(1900, 1, 1);
			var end = new DateTime(2010, 1, 1);

			for (var i = 0; i < count; i++)
			{
				var team = db.Create<Team>();
				var newCity = rand.Next(0, allCityNames.Count - 1);
				var newTeam = rand.Next(0, allTeamNames.Count - 1);
				team.Name = allCityNames[newCity] + " " + allTeamNames[newTeam];
				team.FoundingDate = RandomDay(rand, start, end);
				team.Sport = sport;
				//team.ID = (((sportId - 1) * count) + (i + 1));
				teams.Add(team);

				await db.SaveAsync(team);
			}

			return teams;
		}

		internal static async Task<List<Sport>> GenerateSports(WatsoniaDatabase db, int count)
		{
			var sports = new List<Sport>();
			var allSportNames = Names.GetSportNames();
			var rand = new Random();

			for (var i = 0; i < count; i++)
			{
				var newSport = rand.Next(0, allSportNames.Count - 1);
				var sport = db.Create<Sport>();
				sport.Name = allSportNames[newSport];
				//sport.ID = i + 1;
				sports.Add(sport);

				await db.SaveAsync(sport);
			}

			return sports;
		}

		private static DateTime RandomDay(Random rand, DateTime start, DateTime end)
		{
			var range = (end - start).Days;
			return start.AddDays(rand.Next(range));
		}
	}
}
