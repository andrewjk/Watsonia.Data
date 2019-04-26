using System;
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
			var testResults = RunTests();

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
			if (!File.Exists(@"Data\Performance.sqlite"))
			{
				File.Create(@"Data\Performance.sqlite");
			}
			db.UpdateDatabase();

			if (db.Query<Post>().Count() == 0)
			{
				DataGenerator.GeneratePosts(db, PostCount);
				var sports = DataGenerator.GenerateSports(db, SportCount);
				foreach (var sport in sports)
				{
					var teams = DataGenerator.GenerateTeams(db, sport, TeamsPerSportCount);
					foreach (var team in teams)
					{
						var players = DataGenerator.GeneratePlayers(db, team, PlayersPerTeamCount);
					}
				}
			}
		}

		private static List<TestResult> RunTests()
		{
			var testResults = new List<TestResult>();

			for (var i = 0; i < RunCount; i++)
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
			for (var i = 1; i <= Math.Min(MaxOperations, PostCount); i++)
			{
				allPostsResults.Add(tests.GetAllPosts());
			}
			result.AllPostsMilliseconds = Math.Round(allPostsResults.Average(), 2);

			var playerByIDResults = new List<long>();
			for (var i = 1; i <= Math.Min(MaxOperations, PlayersPerTeamCount * TeamsPerSportCount * SportCount); i++)
			{
				playerByIDResults.Add(tests.GetPlayerByID(i));
			}
			result.PlayerByIDMilliseconds = Math.Round(playerByIDResults.Average(), 2);

			var playersForTeamResults = new List<long>();
			for (var i = 1; i <= Math.Min(MaxOperations, TeamsPerSportCount * SportCount); i++)
			{
				playersForTeamResults.Add(tests.GetPlayersForTeam(i));
			}
			result.PlayersForTeamMilliseconds = Math.Round(playersForTeamResults.Average(), 2);
			var teamsForSportResults = new List<long>();
			for (var i = 1; i <= Math.Min(MaxOperations, SportCount); i++)
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

				var lineParts = new List<string[]>();
				var lineWidths = new List<int>();

				lineParts.Add(new string[] { "Run", "All Posts", "Player by ID", "Players per Team", "Teams per Sport" });
				var orderedResults = group.OrderBy(x => x.Number);
				foreach (var orderResult in orderedResults)
				{
					lineParts.Add(new string[] {
						orderResult.Number.ToString(),
						orderResult.AllPostsMilliseconds.ToString(),
						orderResult.PlayerByIDMilliseconds.ToString(),
						orderResult.PlayersForTeamMilliseconds.ToString(),
						orderResult.TeamsForSportMilliseconds.ToString()
					});
				}
				var averageAllPosts = group.Average(x => x.AllPostsMilliseconds);
				var averagePlayerByID = group.Average(x => x.PlayerByIDMilliseconds);
				var averagePlayersForTeam = group.Average(x => x.PlayersForTeamMilliseconds);
				var averageTeamsForSport = group.Average(x => x.TeamsForSportMilliseconds);
				lineParts.Add(new string[] {
					"Avg",
					averageAllPosts.ToString(),
					averagePlayerByID.ToString(),
					averagePlayersForTeam.ToString(),
					averageTeamsForSport.ToString()
				});
				baselineAllPosts = baselineAllPosts == 0 ? averageAllPosts : baselineAllPosts;
				baselinePlayerByID = baselinePlayerByID == 0 ? averagePlayerByID : baselinePlayerByID;
				baselinePlayersForTeam = baselinePlayersForTeam == 0 ? averagePlayersForTeam : baselinePlayersForTeam;
				baselineTeamsForSport = baselineTeamsForSport == 0 ? averageTeamsForSport : baselineTeamsForSport;
				lineParts.Add(new string[] {
					"%",
					(averageAllPosts / baselineAllPosts).ToString("p"),
					(averagePlayerByID / baselinePlayerByID).ToString("p"),
					(averagePlayersForTeam / baselinePlayersForTeam).ToString("p"),
					(averageTeamsForSport / baselineTeamsForSport).ToString("p")
				});

				for (var i = 0; i < lineParts[0].Length; i++)
				{
					lineWidths.Add(0);
					foreach (var part in lineParts)
					{
						lineWidths[i] = Math.Max(lineWidths[i], part[i].Length);
					}
				}

				foreach (var part in lineParts)
				{
					for (var i = 0; i < part.Length; i++)
					{
						part[i]  = part[i].PadRight(lineWidths[i] + 2);
					}

					lines.Add(string.Join("", part));
				}
			}

			foreach (var line in lines)
			{
				Console.WriteLine(line);
			}

			var logFile = $@"Data\Log {DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt";
			using (var writer = new StreamWriter(logFile))
			{
				foreach (var line in lines)
				{
					writer.WriteLine(line);
				}
			}
		}
	}
}
