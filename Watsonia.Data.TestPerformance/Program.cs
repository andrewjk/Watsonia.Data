using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Running;
using Watsonia.Data.TestPerformance.Entities;
using Watsonia.Data.TestPerformance.Tests;

namespace Watsonia.Data.TestPerformance
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			if (args.Length > 0 && string.Equals(args[0], "verify", StringComparison.OrdinalIgnoreCase))
			{
				Verification.Run();
				return;
			}

			BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
		}
	}

	/// <summary>
	/// Runs each framework once and verifies they return identical data.
	/// </summary>
	internal static class Verification
	{
		public static void Run()
		{
			Seeder.EnsureSeeded();

			var results = new List<(string Framework, IPerformanceTests Tests)>
			{
				("AdoNet", new AdoNetTests()),
				("Dapper", new DapperTests()),
				("EntityFramework", new EntityFrameworkTests()),
				("Watsonia LINQ", new WatsoniaLinqTests()),
				("Watsonia SQL", new WatsoniaSqlTests()),
			};

			foreach (var (framework, tests) in results)
			{
				tests.GetAllPostIDs();
				tests.GetAllPosts();
				tests.GetPlayerByID(1);
				tests.GetPlayersForTeam(1);
				tests.GetTeamsForSport(1);
				Console.WriteLine("Ran: " + framework);
			}

			var baseline = results[0].Tests;
			var allMatch = true;
			foreach (var (framework, tests) in results.Skip(1))
			{
				var matches = CompareLists(baseline.LoadedPostIDs, tests.LoadedPostIDs)
					&& CompareLists(baseline.LoadedPosts, tests.LoadedPosts)
					&& CompareLists(baseline.LoadedPlayers, tests.LoadedPlayers)
					&& CompareLists(baseline.LoadedPlayersForTeam, tests.LoadedPlayersForTeam)
					&& CompareLists(baseline.LoadedTeamsForSport, tests.LoadedTeamsForSport);
				Console.WriteLine((matches ? "OK       " : "MISMATCH ") + framework);
				allMatch &= matches;
			}

			Console.WriteLine(allMatch
				? "All frameworks returned the same data."
				: "Frameworks returned different data!");
		}

		private static bool CompareLists(List<IEntity> a, List<IEntity> b)
		{
			return CompareLists(a.Select(x => x.ID).ToList(), b.Select(x => x.ID).ToList());
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
	}

	/// <summary>
	/// Ensures the benchmark database exists and is populated.
	/// </summary>
	internal static class Seeder
	{
		public static void EnsureSeeded()
		{
			var database = new WatsoniaDatabase("Checking");
			DataGenerator.CheckDatabaseAsync(database).GetAwaiter().GetResult();
		}
	}
}
