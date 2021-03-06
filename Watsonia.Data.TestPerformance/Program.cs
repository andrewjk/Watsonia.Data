﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.TestPerformance.Entities;
using Watsonia.Data.TestPerformance.Tests;

namespace Watsonia.Data.TestPerformance
{
	// So this is mostly adapted from https://www.exceptionnotfound.net/dapper-vs-entity-framework-vs-ado-net-performance-benchmarking/
	public static class Program
	{
		public static async Task Main(/* string[] args */)
		{
			var color = Console.ForegroundColor;
			var backColor = Console.BackgroundColor;

			Console.BackgroundColor = ConsoleColor.Black;

			var db = new WatsoniaDatabase("Checking");

			Console.WriteLine("Checking database...");
			await DataGenerator.CheckDatabaseAsync(db);

			Console.WriteLine("Running tests...");
			var testResults = RunTests();

			ProcessResults(testResults);

			Console.ForegroundColor = color;
			Console.BackgroundColor = backColor;

			// Pause until a key is pressed
			Console.ReadKey();
		}

		private static List<TestResult> RunTests()
		{
			var testResults = new List<TestResult>();
			var frameworkCount = 0;

			for (var i = 0; i < Config.RunCount; i++)
			{
				if (i > 0)
				{
					Console.SetCursorPosition(0, Console.CursorTop);
				}
				Console.Write($"Run {i + 1}/{Config.RunCount}, test 1/5");

				GC.Collect();
				GC.WaitForPendingFinalizers();

				var adoTests = new AdoNetTests();
				var adoResults = RunTests(i, TestFramework.AdoNet, adoTests);
				testResults.Add(adoResults);

				Console.SetCursorPosition(0, Console.CursorTop);
				Console.Write($"Run {i + 1}/{Config.RunCount}, test 2/5");

				GC.Collect();
				GC.WaitForPendingFinalizers();

				// Dapper is fast
				var dapperTests = new DapperTests();
				var dapperResults = RunTests(i, TestFramework.Dapper, dapperTests);
				testResults.Add(dapperResults);

				Console.SetCursorPosition(0, Console.CursorTop);
				Console.Write($"Run {i + 1}/{Config.RunCount}, test 3/5");

				GC.Collect();
				GC.WaitForPendingFinalizers();

				// EntityFramework is full-featured
				var efTests = new EntityFrameworkTests();
				var efResults = RunTests(i, TestFramework.EntityFramework, efTests);
				testResults.Add(efResults);

				// TODO: Should we test an "optimised" EF? No change tracking etc?

				Console.SetCursorPosition(0, Console.CursorTop);
				Console.Write($"Run {i + 1}/{Config.RunCount}, test 4/5");

				GC.Collect();
				GC.WaitForPendingFinalizers();

				// Use Linq for a better dev experience

				var wlinqTests = new WatsoniaLinqTests();
				var wlinqResults = RunTests(i, TestFramework.WatsoniaLinq, wlinqTests);
				testResults.Add(wlinqResults);

				Console.SetCursorPosition(0, Console.CursorTop);
				Console.Write($"Run {i + 1}/{Config.RunCount}, test 5/5");

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

			var allPostIDsResults = new List<long>();
			for (var i = 1; i <= Math.Min(Config.MaxOperations, Config.PostCount); i++)
			{
				allPostIDsResults.Add(tests.GetAllPostIDs());
			}
			result.AllPostIDsMilliseconds = Math.Round(allPostIDsResults.Average(), 2);

			var allPostsResults = new List<long>();
			for (var i = 1; i <= Math.Min(Config.MaxOperations, Config.PostCount); i++)
			{
				allPostsResults.Add(tests.GetAllPosts());
			}
			result.AllPostsMilliseconds = Math.Round(allPostsResults.Average(), 2);

			var playerByIDResults = new List<long>();
			for (var i = 1; i <= Math.Min(Config.MaxOperations, Config.PlayersPerTeamCount * Config.TeamsPerSportCount * Config.SportCount); i++)
			{
				playerByIDResults.Add(tests.GetPlayerByID(i));
			}
			result.PlayerByIDMilliseconds = Math.Round(playerByIDResults.Average(), 2);

			var playersForTeamResults = new List<long>();
			for (var i = 1; i <= Math.Min(Config.MaxOperations, Config.TeamsPerSportCount * Config.SportCount); i++)
			{
				playersForTeamResults.Add(tests.GetPlayersForTeam(i));
			}
			result.PlayersForTeamMilliseconds = Math.Round(playersForTeamResults.Average(), 2);

			var teamsForSportResults = new List<long>();
			for (var i = 1; i <= Math.Min(Config.MaxOperations, Config.SportCount); i++)
			{
				teamsForSportResults.Add(tests.GetTeamsForSport(i));
			}
			result.TeamsForSportMilliseconds = Math.Round(teamsForSportResults.Average(), 2);

			result.LoadedPostIDs = tests.LoadedPostIDs;
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

			if (result && !CompareLists(a.LoadedPostIDs, b.LoadedPostIDs))
			{
				result = false;
				badthing = "loaded post ids";
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

		private static bool CompareLists(List<IEntity> a, List<IEntity> b)
		{
			return CompareLists(
				a.Select(a => a.ID).ToList(),
				b.Select(b => b.ID).ToList());
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
			double baselineAllPostIDs = 0;
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
					new ConsoleLinePart("Post IDs"),
					new ConsoleLinePart("Posts"),
					new ConsoleLinePart("Player by ID"),
					new ConsoleLinePart("Players / Team"),
					new ConsoleLinePart("Teams / Sport")
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
				var maxAllPostIDs = group.Max(x => x.AllPostIDsMilliseconds);
				var maxAllPosts = group.Max(x => x.AllPostsMilliseconds);
				var maxPlayerByID = group.Max(x => x.PlayerByIDMilliseconds);
				var maxPlayersForTeam = group.Max(x => x.PlayersForTeamMilliseconds);
				var maxTeamsForSport = group.Max(x => x.TeamsForSportMilliseconds);
				lines.Add(new ConsoleLine(
					new ConsoleLinePart("Max"),
					new ConsoleLinePart(maxAllPostIDs.ToString()),
					new ConsoleLinePart(maxAllPosts.ToString()),
					new ConsoleLinePart(maxPlayerByID.ToString()),
					new ConsoleLinePart(maxPlayersForTeam.ToString()),
					new ConsoleLinePart(maxTeamsForSport.ToString())
				));

				// Average
				var averageAllPostIDs = group.Average(x => x.AllPostIDsMilliseconds);
				var averageAllPosts = group.Average(x => x.AllPostsMilliseconds);
				var averagePlayerByID = group.Average(x => x.PlayerByIDMilliseconds);
				var averagePlayersForTeam = group.Average(x => x.PlayersForTeamMilliseconds);
				var averageTeamsForSport = group.Average(x => x.TeamsForSportMilliseconds);
				lines.Add(new ConsoleLine(
					new ConsoleLinePart("Avg"),
					new ConsoleLinePart(averageAllPostIDs.ToString("n4")),
					new ConsoleLinePart(averageAllPosts.ToString("n4")),
					new ConsoleLinePart(averagePlayerByID.ToString("n4")),
					new ConsoleLinePart(averagePlayersForTeam.ToString("n4")),
					new ConsoleLinePart(averageTeamsForSport.ToString("n4"))
				));

				// Baseline (if first result)
				if (!haveBaseline)
				{
					baselineAllPostIDs = averageAllPostIDs;
					baselineAllPosts = averageAllPosts;
					baselinePlayerByID = averagePlayerByID;
					baselinePlayersForTeam = averagePlayersForTeam;
					baselineTeamsForSport = averageTeamsForSport;
					haveBaseline = true;
				}

				// Percentage of baseline
				var percentAllPostIDs = averageAllPostIDs / baselineAllPostIDs;
				var percentAllPosts = averageAllPosts / baselineAllPosts;
				var percentPlayerByID = averagePlayerByID / baselinePlayerByID;
				var percentPlayersForTeam = averagePlayersForTeam / baselinePlayersForTeam;
				var percentTeamsForSport = averageTeamsForSport / baselineTeamsForSport;

				lines.Add(new ConsoleLine(
					new ConsoleLinePart("%"),
					new ConsoleLinePart(percentAllPostIDs.ToString("p"), ColorForPercent(percentAllPostIDs)),
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
