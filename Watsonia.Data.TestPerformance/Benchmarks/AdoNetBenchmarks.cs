using BenchmarkDotNet.Attributes;
using Watsonia.Data.TestPerformance.Tests;

namespace Watsonia.Data.TestPerformance.Benchmarks
{
	[MemoryDiagnoser]
	public class AdoNetBenchmarks
	{
		[GlobalSetup]
		public void Setup()
		{
			Seeder.EnsureSeeded();
		}

		[Benchmark]
		public void GetAllPostIDs()
		{
			new AdoNetTests().GetAllPostIDs();
		}

		[Benchmark]
		public void GetAllPosts()
		{
			new AdoNetTests().GetAllPosts();
		}

		[Benchmark]
		public void GetPlayerByID()
		{
			new AdoNetTests().GetPlayerByID(1);
		}

		[Benchmark]
		public void GetPlayersForTeam()
		{
			new AdoNetTests().GetPlayersForTeam(1);
		}

		[Benchmark]
		public void GetTeamsForSport()
		{
			new AdoNetTests().GetTeamsForSport(1);
		}
	}
}
