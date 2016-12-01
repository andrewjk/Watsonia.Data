﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.TestPerformance.Entities;

namespace Watsonia.Data.TestPerformance
{
	// So this is mostly adapted from https://www.exceptionnotfound.net/dapper-vs-entity-framework-vs-ado-net-performance-benchmarking/
	public class Program
	{
		private const int RunCount = 10;
		private const int MaxOperations = 50;

		private const int PostCount = 500;
		private const int SportCount = 5;
		private const int TeamsPerSportCount = 10;
		private const int PlayersPerTeamCount = 10;

		public static void Main(string[] args)
		{
			var db = new WatsoniaDatabase();

			Console.WriteLine("Checking database...");
			CheckDatabase(db);

			Console.WriteLine("Running tests...");
			List<TestResult> testResults = RunTests();

			ProcessResults(testResults);

			// Pause until a key is pressed
			Console.ReadKey();
		}

		private static void CheckDatabase(WatsoniaDatabase db)
		{
			if (!Directory.Exists("Data"))
			{
				Directory.CreateDirectory("Data");
			}
			if (!File.Exists(@"Data\Performance.sdf"))
			{
				File.Create(@"Data\Performance.sdf");
			}
			db.UpdateDatabase();

			if (db.Query<Post>().Count() == 0)
			{
				List<Post> posts = DataGenerator.GeneratePosts(db, PostCount);
				List<Sport> sports = DataGenerator.GenerateSports(db, SportCount);
				foreach (var sport in sports)
				{
					List<Team> teams = DataGenerator.GenerateTeams(db, sport, TeamsPerSportCount);
					foreach (var team in teams)
					{
						List<Player> players = DataGenerator.GeneratePlayers(db, team, PlayersPerTeamCount);
					}
				}
			}
		}

		private static List<TestResult> RunTests()
		{
			var testResults = new List<TestResult>();

			for (int i = 0; i < RunCount; i++)
			{
				Console.WriteLine("{0}/{1}", i + 1, RunCount);

				var adoTests = new AdoNetTests();
				testResults.AddRange(RunTests(i, TestFramework.AdoNet, adoTests));

				var dapperTests = new DapperTests();
				testResults.AddRange(RunTests(i, TestFramework.Dapper, dapperTests));

				var efTests = new EntityFrameworkTests();
				testResults.AddRange(RunTests(i, TestFramework.EntityFramework, efTests));

				var wsqlTests = new WatsoniaSqlTests();
				testResults.AddRange(RunTests(i, TestFramework.WatsoniaSql, wsqlTests));

				var wlinqTests = new WatsoniaLinqTests();
				testResults.AddRange(RunTests(i, TestFramework.WatsoniaLinq, wlinqTests));
			}

			return testResults;
		}

		public static List<TestResult> RunTests(int number, TestFramework framework, IPerformanceTests tests)
		{
			var results = new List<TestResult>();

			var result = new TestResult() { Number = number, Framework = framework };
			var allPostsResults = new List<long>();
			for (int i = 1; i <= Math.Min(MaxOperations, PostCount); i++)
			{
				allPostsResults.Add(tests.GetAllPosts());
			}
			result.AllPostsMilliseconds = Math.Round(allPostsResults.Average(), 2);

			var playerByIDResults = new List<long>();
			for (int i = 1; i <= Math.Min(MaxOperations, PlayersPerTeamCount * TeamsPerSportCount * SportCount); i++)
			{
				playerByIDResults.Add(tests.GetPlayerByID(i));
			}
			result.PlayerByIDMilliseconds = Math.Round(playerByIDResults.Average(), 2);

			var playersForTeamResults = new List<long>();
			for (int i = 1; i <= Math.Min(MaxOperations, TeamsPerSportCount * SportCount); i++)
			{
				playersForTeamResults.Add(tests.GetPlayersForTeam(i));
			}
			result.PlayersForTeamMilliseconds = Math.Round(playersForTeamResults.Average(), 2);
			var teamsForSportResults = new List<long>();
			for (int i = 1; i <= Math.Min(MaxOperations, SportCount); i++)
			{
				teamsForSportResults.Add(tests.GetTeamsForSport(i));
			}
			result.TeamsForSportMilliseconds = Math.Round(teamsForSportResults.Average(), 2);
			results.Add(result);
			
			return results;
		}

		public static void ProcessResults(List<TestResult> results)
		{
			var lines = new List<string>();

			double baselineAllPosts = 0;
			double baselinePlayerByID = 0;
			double baselinePlayersForTeam = 0;
			double baselineTeamsForSport = 0;

			var groupedResults = results.GroupBy(x => x.Framework);
			foreach (var group in groupedResults)
			{
				lines.Add(group.Key.ToString() + " Results");
				lines.Add("Run\tAll Posts\tPlayer by ID\tPlayers per Team\tTeams per Sport");
				var orderedResults = group.OrderBy(x => x.Number);
				foreach (var orderResult in orderedResults)
				{
					lines.Add(string.Format("{0}\t{1}\t{2}\t{3}\t{4}", 
						orderResult.Number,
						orderResult.AllPostsMilliseconds,
						orderResult.PlayerByIDMilliseconds,
						orderResult.PlayersForTeamMilliseconds,
						orderResult.TeamsForSportMilliseconds));
				}
				double averageAllPosts = group.Average(x => x.AllPostsMilliseconds);
				double averagePlayerByID = group.Average(x => x.PlayerByIDMilliseconds);
				double averagePlayersForTeam = group.Average(x => x.PlayersForTeamMilliseconds);
				double averageTeamsForSport = group.Average(x => x.TeamsForSportMilliseconds);
				lines.Add(string.Format("{0}\t{1}\t{2}\t{3}\t{4}",
					"Avg",
					averageAllPosts,
					averagePlayerByID,
					averagePlayersForTeam,
					averageTeamsForSport));
				baselineAllPosts = baselineAllPosts == 0 ? averageAllPosts : baselineAllPosts;
				baselinePlayerByID = baselinePlayerByID == 0 ? averagePlayerByID : baselinePlayerByID;
				baselinePlayersForTeam = baselinePlayersForTeam == 0 ? averagePlayersForTeam : baselinePlayersForTeam;
				baselineTeamsForSport = baselineTeamsForSport == 0 ? averageTeamsForSport : baselineTeamsForSport;
				lines.Add(string.Format("{0}\t{1}\t{2}\t{3}\t{4}",
					"%",
					(averageAllPosts / baselineAllPosts).ToString("p"),
					(averagePlayerByID / baselinePlayerByID).ToString("p"),
					(averagePlayersForTeam / baselinePlayersForTeam).ToString("p"),
					(averageTeamsForSport / baselineTeamsForSport).ToString("p")));
			}

			foreach (string line in lines)
			{
				Console.WriteLine(line);
			}

			var logFile = string.Format(@"Data\Log {0:yyyy-MM-dd-HH-mm-ss}.txt", DateTime.Now);
			using (var writer = new StreamWriter(logFile))
			{
				foreach (string line in lines)
				{
					writer.WriteLine(line);
				}
			}
		}
	}
}