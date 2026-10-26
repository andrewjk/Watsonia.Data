using BenchmarkDotNet.Attributes;
using Watsonia.Data.TestPerformance.Tests;

namespace Watsonia.Data.TestPerformance.Benchmarks
{
	[MemoryDiagnoser]
	public class WatsoniaLinqBenchmarks
	{
		[GlobalSetup]
		public void Setup()
		{
			Seeder.EnsureSeeded();
		}

		[Benchmark]
		public void GetAllPostIDs()
		{
			new WatsoniaLinqTests().GetAllPostIDs();
		}

		[Benchmark]
		public void GetAllPosts()
		{
			new WatsoniaLinqTests().GetAllPosts();
		}

		[Benchmark]
		public void GetPlayerByID()
		{
			new WatsoniaLinqTests().GetPlayerByID(1);
		}

		[Benchmark]
		public void GetPlayersForTeam()
		{
			new WatsoniaLinqTests().GetPlayersForTeam(1);
		}

		[Benchmark]
		public void GetTeamsForSport()
		{
			new WatsoniaLinqTests().GetTeamsForSport(1);
		}
	}
}
