using BenchmarkDotNet.Attributes;
using Watsonia.Data.TestPerformance.Tests;

namespace Watsonia.Data.TestPerformance.Benchmarks
{
	[MemoryDiagnoser]
	public class WatsoniaSqlBenchmarks
	{
		[GlobalSetup]
		public void Setup()
		{
			Seeder.EnsureSeeded();
		}

		[Benchmark]
		public void GetAllPostIDs()
		{
			new WatsoniaSqlTests().GetAllPostIDs();
		}

		[Benchmark]
		public void GetAllPosts()
		{
			new WatsoniaSqlTests().GetAllPosts();
		}

		[Benchmark]
		public void GetPlayerByID()
		{
			new WatsoniaSqlTests().GetPlayerByID(1);
		}

		[Benchmark]
		public void GetPlayersForTeam()
		{
			new WatsoniaSqlTests().GetPlayersForTeam(1);
		}

		[Benchmark]
		public void GetTeamsForSport()
		{
			new WatsoniaSqlTests().GetTeamsForSport(1);
		}
	}
}
