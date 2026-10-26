using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.TestPerformance.Entities;

namespace Watsonia.Data.TestPerformance.Tests
{
	public interface IPerformanceTests
	{
		List<long> LoadedPostIDs { get; }
		List<IEntity> LoadedPosts { get; }
		List<IEntity> LoadedPlayers { get; }
		List<IEntity> LoadedPlayersForTeam { get; }
		List<IEntity> LoadedTeamsForSport { get; }

		void GetAllPostIDs();
		void GetAllPosts();
		void GetPlayerByID(long id);
		void GetPlayersForTeam(long teamId);
		void GetTeamsForSport(long sportId);
	}
}
