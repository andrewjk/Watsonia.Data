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
	public static class Program
	{
		private const int RunCount = 10;
		private const int MaxOperations = 50;

		private const int PostCount = 500;
		private const int SportCount = 5;
		private const int TeamsPerSportCount = 10;
		private const int PlayersPerTeamCount = 10;

		public static async Task Main(/* string[] args */)
		{
			var color = Console.ForegroundColor;
			var backColor = Console.BackgroundColor;

			Console.BackgroundColor = ConsoleColor.Black;

			var db = new WatsoniaDatabase();

			Console.WriteLine("Checking database...");
			await CheckDatabaseAsync(db);

			Console.WriteLine("Running tests...");
			var testResults = RunTests();

			ProcessResults(testResults);

			Console.ForegroundColor = color;
			Console.BackgroundColor = backColor;

			// Pause until a key is pressed
			Console.ReadKey();
		}

		private static async Task CheckDatabaseAsync(WatsoniaDatabase db)
		{
			if (!Directory.Exists("Data"))
			{
				Directory.CreateDirectory("Data");
			}
			if (!File.Exists(@"Data\Performance.sqlite"))
			{
				var file = File.Create(@"Data\Performance.sqlite");
				file.Dispose();
			}
			await db.UpdateDatabaseAsync();

			if (db.Query<Post>().Count() == 0)
			{
				await DataGenerator.GeneratePosts(db, PostCount);
				var sports = await DataGenerator.GenerateSports(db, SportCount);
				foreach (var sport in sports)
				{
					var teams = await DataGenerator.GenerateTeams(db, sport, TeamsPerSportCount);
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
			var frameworkCount = 0;

			for (var i = 0; i < RunCount; i++)
			{
				if (i > 0)
				{
					Console.SetCursorPosition(0, Console.CursorTop);
				}
				Console.Write("{0}/{1}", i + 1, RunCount);

				GC.Collect();
				GC.WaitForPendingFinalizers();

				var adoTests = new AdoNetTests();
				var adoResults = RunTests(i, TestFramework.AdoNet, adoTests);
				testResults.Add(adoResults);

				GC.Collect();
				GC.WaitForPendingFinalizers();

				// Dapper is fast
				var dapperTests = new DapperTests();
				var dapperResults = RunTests(i, TestFramework.Dapper, dapperTests);
				testResults.Add(dapperResults);

				GC.Collect();
				GC.WaitForPendingFinalizers();

				// EntityFramework is full-featured
				var efTests = new EntityFrameworkTests();
				var efResults = RunTests(i, TestFramework.EntityFramework, efTests);
				testResults.Add(efResults);

				// TODO: Should we test an "optimised" EF? No change tracking etc?

				GC.Collect();
				GC.WaitForPendingFinalizers();

				// Use Linq for a better dev experience

				var wlinqTests = new WatsoniaLinqTests();
				var wlinqResults = RunTests(i, TestFramework.WatsoniaLinq, wlinqTests);
				testResults.Add(wlinqResults);

				GC.Collect();
				GC.WaitForPendingFinalizers();

				// Use SQL for speed
				var wsqlTests = new WatsoniaSqlTests();
				var wsqlResults = RunTests(i, TestFramework.WatsoniaSql, wsqlTests);
				testResults.Add(wsqlResults);

				GC.Collect();
				GC.WaitForPendingFinalizers();

				if (i == 0)
				{
					frameworkCount = testResults.Count;
				}
			}
			Console.WriteLine();

			// Make sure everything's ok...
			for (var i = 0; i < frameworkCount; i++)
			{
				CompareResults(testResults[0], testResults[i], testResults[i].Framework.ToString());
			}

			return testResults;
		}

		private static TestResult RunTests(int number, TestFramework framework, IPerformanceTests tests)
		{
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

			result.LoadedPosts = tests.LoadedPosts;
			result.LoadedPlayers = tests.LoadedPlayers;
			result.LoadedPlayersForTeam = tests.LoadedPlayersForTeam;
			result.LoadedTeamsForSport = tests.LoadedTeamsForSport;

			return result;
		}

		private static void CompareResults(TestResult a, TestResult b, string framework)
		{
			var result = true;
			var badthing = "";

			if (a.LoadedPosts.Count != b.LoadedPosts.Count ||
				a.LoadedPlayers.Count != b.LoadedPlayers.Count ||
				a.LoadedPlayersForTeam.Count != b.LoadedPlayersForTeam.Count ||
				a.LoadedTeamsForSport.Count != b.LoadedTeamsForSport.Count)
			{
				result = false;
			}

			if (result && !CompareLists(a.LoadedPosts, b.LoadedPosts))
			{
				result = false;
				badthing = "loaded posts";
			}

			if (result && !CompareLists(a.LoadedPlayers, b.LoadedPlayers))
			{
				result = false;
				badthing = "loaded players";
			}

			if (result && !CompareLists(a.LoadedPlayersForTeam, b.LoadedPlayersForTeam))
			{
				result = false;
				badthing = "loaded players for team";
			}

			if (result && !CompareLists(a.LoadedTeamsForSport, b.LoadedTeamsForSport))
			{
				result = false;
				badthing = "loaded teams for sport";
			}

			if (!result)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"{framework} has incorrect results for {badthing}");
				Console.ForegroundColor = ConsoleColor.Gray;
			}
		}

		private static bool CompareLists(List<long> a, List<long> b)
		{
			if (a.Count != b.Count)
			{
				return false;
			}

			a.Sort();
			b.Sort();

			for (var i = 0; i < a.Count; i++)
			{
				if (a[i] != b[i])
				{
					return false;
				}
			}

			return true;
		}

		private static void ProcessResults(List<TestResult> results)
		{
			var lines = new List<ConsoleLine>();

			var haveBaseline = false;
			double baselineAllPosts = 0;
			double baselinePlayerByID = 0;
			double baselinePlayersForTeam = 0;
			double baselineTeamsForSport = 0;

			var groupedResults = results.GroupBy(x => x.Framework);
			foreach (var group in groupedResults)
			{
				lines.Add(new ConsoleLine($"{group.Key} Results", ConsoleColor.DarkCyan));

				lines.Add(new ConsoleLine(
					new ConsoleLinePart("Run"),
					new ConsoleLinePart("All Posts"),
					new ConsoleLinePart("Player by ID"),
					new ConsoleLinePart("Players per Team"),
					new ConsoleLinePart("Teams per Sport")
				));

				//var orderedResults = group.OrderBy(x => x.Number);
				//foreach (var orderResult in orderedResults)
				//{
				//	lineParts.Add(new string[] {
				//		orderResult.Number.ToString(),
				//		orderResult.AllPostsMilliseconds.ToString(),
				//		orderResult.PlayerByIDMilliseconds.ToString(),
				//		orderResult.PlayersForTeamMilliseconds.ToString(),
				//		orderResult.TeamsForSportMilliseconds.ToString()
				//	});
				//}

				// Max
				var maxAllPosts = group.Max(x => x.AllPostsMilliseconds);
				var maxPlayerByID = group.Max(x => x.PlayerByIDMilliseconds);
				var maxPlayersForTeam = group.Max(x => x.PlayersForTeamMilliseconds);
				var maxTeamsForSport = group.Max(x => x.TeamsForSportMilliseconds);
				lines.Add(new ConsoleLine(
					new ConsoleLinePart("Max"),
					new ConsoleLinePart(maxAllPosts.ToString()),
					new ConsoleLinePart(maxPlayerByID.ToString()),
					new ConsoleLinePart(maxPlayersForTeam.ToString()),
					new ConsoleLinePart(maxTeamsForSport.ToString())
				));

				// Average
				var averageAllPosts = group.Average(x => x.AllPostsMilliseconds);
				var averagePlayerByID = group.Average(x => x.PlayerByIDMilliseconds);
				var averagePlayersForTeam = group.Average(x => x.PlayersForTeamMilliseconds);
				var averageTeamsForSport = group.Average(x => x.TeamsForSportMilliseconds);
				lines.Add(new ConsoleLine(
					new ConsoleLinePart("Avg"),
					new ConsoleLinePart(averageAllPosts.ToString("n4")),
					new ConsoleLinePart(averagePlayerByID.ToString("n4")),
					new ConsoleLinePart(averagePlayersForTeam.ToString("n4")),
					new ConsoleLinePart(averageTeamsForSport.ToString("n4"))
				));

				// Baseline (if first result)
				if (!haveBaseline)
				{
					baselineAllPosts = averageAllPosts;
					baselinePlayerByID = averagePlayerByID;
					baselinePlayersForTeam = averagePlayersForTeam;
					baselineTeamsForSport = averageTeamsForSport;
					haveBaseline = true;
				}

				// Percentage of baseline
				var percentAllPosts = averageAllPosts / baselineAllPosts;
				var percentPlayerByID = averagePlayerByID / baselinePlayerByID;
				var percentPlayersForTeam = averagePlayersForTeam / baselinePlayersForTeam;
				var percentTeamsForSport = averageTeamsForSport / baselineTeamsForSport;

				lines.Add(new ConsoleLine(
					new ConsoleLinePart("%"),
					new ConsoleLinePart(percentAllPosts.ToString("p"), ColorForPercent(percentAllPosts)),
					new ConsoleLinePart(percentPlayerByID.ToString("p"), ColorForPercent(percentPlayerByID)),
					new ConsoleLinePart(percentPlayersForTeam.ToString("p"), ColorForPercent(percentPlayersForTeam)),
					new ConsoleLinePart(percentTeamsForSport.ToString("p"), ColorForPercent(percentTeamsForSport))
				));

				// Get maxwidths
				var lineWidths = new List<int>();
				foreach (var line in lines.Where(l => l.Parts.Count > 1))
				{
					for (var i = 0; i < line.Parts.Count; i++)
					{
						if (lineWidths.Count <= i)
						{
							lineWidths.Add(0);
						}
						lineWidths[i] = Math.Max(lineWidths[i], line.Parts[i].Text.Length);
					}
				}

				// Set widths
				foreach (var line in lines)
				{
					for (var i = 0; i < line.Parts.Count; i++)
					{
						line.Parts[i].Text = line.Parts[i].Text.PadRight(lineWidths[i] + 2);
					}
				}
			}

			foreach (var line in lines)
			{
				foreach (var part in line.Parts)
				{
					Console.ForegroundColor = part.Color;
					Console.Write(part.Text);
				}
				Console.WriteLine();
			}

			var logFile = $@"Data\Log {DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt";
			using var writer = new StreamWriter(logFile);
			foreach (var line in lines)
			{
				writer.WriteLine(line);
			}
		}

		private static ConsoleColor ColorForPercent(double percentAllPosts)
		{
			if (percentAllPosts <= 1)
			{
				return ConsoleColor.Green;
			}
			else if (percentAllPosts <= 1.5)
			{
				return ConsoleColor.DarkGreen;
			}
			else if (percentAllPosts <= 5.0)
			{
				return ConsoleColor.Yellow;
			}
			else if (percentAllPosts <= 10.00)
			{
				return ConsoleColor.Magenta;
			}
			else
			{
				return ConsoleColor.Red;
			}
		}
	}
}
