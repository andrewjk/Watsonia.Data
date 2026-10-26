using System;
using System.Linq;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using Watsonia.Data.TestPerformance.Entities;

namespace Watsonia.Data.TestPerformance.Benchmarks
{
	/// <summary>
	/// Diagnostic benchmarks that separate the phases of a LINQ query execution.
	/// </summary>
	[MemoryDiagnoser]
	public class ResearchBenchmarks
	{
		private WatsoniaDatabase _database;
		private Expression _playersPlan;
		private Expression _teamsPlan;

		[GlobalSetup]
		public void Setup()
		{
			Seeder.EnsureSeeded();
			_database = new WatsoniaDatabase("Linq");

			var provider = new Watsonia.Data.Query.QueryProvider(_database);
			_playersPlan = provider.GetExecutionPlan(
				_database.Query<Player>().Where(x => x.TeamsID == 1).Expression);
			_teamsPlan = provider.GetExecutionPlan(
				_database.Query<Team>().Include(x => x.Players).Where(x => x.SportsID == 1).Expression);
		}

		[Benchmark]
		public void TranslatePlayersQuery()
		{
			var provider = new Watsonia.Data.Query.QueryProvider(_database);
			provider.GetExecutionPlan(_database.Query<Player>().Where(x => x.TeamsID == 1).Expression);
		}

		[Benchmark]
		public object CompilePlayersPlan()
		{
			return Expression.Lambda<Func<object>>(Expression.Convert(_playersPlan, typeof(object))).Compile();
		}

		[Benchmark]
		public void FullPlayersQuery()
		{
			foreach (var player in _database.Query<Player>().Where(x => x.TeamsID == 1))
			{
			}
		}

		[Benchmark]
		public void TranslateTeamsIncludeQuery()
		{
			var provider = new Watsonia.Data.Query.QueryProvider(_database);
			provider.GetExecutionPlan(
				_database.Query<Team>().Include(x => x.Players).Where(x => x.SportsID == 1).Expression);
		}

		[Benchmark]
		public object CompileTeamsPlan()
		{
			return Expression.Lambda<Func<object>>(Expression.Convert(_teamsPlan, typeof(object))).Compile();
		}

		[Benchmark]
		public void FullTeamsIncludeQuery()
		{
			foreach (var team in _database.Query<Team>().Include(x => x.Players).Where(x => x.SportsID == 1))
			{
				foreach (var player in team.Players)
				{
				}
			}
		}
	}
}
