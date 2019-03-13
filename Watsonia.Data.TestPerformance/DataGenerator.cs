﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.TestPerformance.Entities;

namespace Watsonia.Data.TestPerformance
{
	internal static class DataGenerator
	{
		internal static List<Post> GeneratePosts(WatsoniaDatabase db, int count)
		{
			var posts = new List<Post>();

			for (var i = 0; i < count; i++)
			{
				var post = db.Create<Post>();
				post.Text = new string('x', 2000);
				post.DateCreated = DateTime.Now;
				post.DateModified = DateTime.Now;
				db.Save(post);
			}

			return posts;
		}

		internal static List<Player> GeneratePlayers(WatsoniaDatabase db, Team team, int count)
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

				db.Save(player);
			}

			return players;
		}

		internal static List<Team> GenerateTeams(WatsoniaDatabase db, Sport sport, int count)
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

				db.Save(team);
			}

			return teams;
		}

		internal static List<Sport> GenerateSports(WatsoniaDatabase db, int count)
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

				db.Save(sport);
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
