using BenchmarkDotNet.Attributes;
using Watsonia.Data.TestPerformance.Tests;

namespace Watsonia.Data.TestPerformance.Benchmarks
{
	[MemoryDiagnoser]
	public class DapperBenchmarks
	{
		[GlobalSetup]
		public void Setup()
		{
			Seeder.EnsureSeeded();
		}

		[Benchmark]
		public void GetAllPostIDs()
		{
			new DapperTests().GetAllPostIDs();
		}

		[Benchmark]
		public void GetAllPosts()
		{
			new DapperTests().GetAllPosts();
		}

		[Benchmark]
		public void GetPlayerByID()
		{
			new DapperTests().GetPlayerByID(1);
		}

		[Benchmark]
		public void GetPlayersForTeam()
		{
			new DapperTests().GetPlayersForTeam(1);
		}

		[Benchmark]
		public void GetTeamsForSport()
		{
			new DapperTests().GetTeamsForSport(1);
		}
	}
}
