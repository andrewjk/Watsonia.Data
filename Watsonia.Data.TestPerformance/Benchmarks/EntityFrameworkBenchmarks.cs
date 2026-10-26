using BenchmarkDotNet.Attributes;
using Watsonia.Data.TestPerformance.Tests;

namespace Watsonia.Data.TestPerformance.Benchmarks
{
	[MemoryDiagnoser]
	public class EntityFrameworkBenchmarks
	{
		[GlobalSetup]
		public void Setup()
		{
			Seeder.EnsureSeeded();
		}

		[Benchmark]
		public void GetAllPostIDs()
		{
			new EntityFrameworkTests().GetAllPostIDs();
		}

		[Benchmark]
		public void GetAllPosts()
		{
			new EntityFrameworkTests().GetAllPosts();
		}

		[Benchmark]
		public void GetPlayerByID()
		{
			new EntityFrameworkTests().GetPlayerByID(1);
		}

		[Benchmark]
		public void GetPlayersForTeam()
		{
			new EntityFrameworkTests().GetPlayersForTeam(1);
		}

		[Benchmark]
		public void GetTeamsForSport()
		{
			new EntityFrameworkTests().GetTeamsForSport(1);
		}
	}
}
