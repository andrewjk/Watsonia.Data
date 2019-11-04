using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.TestPerformance.Entities;

namespace Watsonia.Data.TestPerformance
{
	public interface IPerformanceTests
	{
		List<long> LoadedPostIDs { get; }
		List<IEntity> LoadedPosts { get; }
		List<IEntity> LoadedPlayers { get; }
		List<IEntity> LoadedPlayersForTeam { get; }
		List<IEntity> LoadedTeamsForSport { get; }

		long GetAllPostIDs();
		long GetAllPosts();
		long GetPlayerByID(long id);
		long GetPlayersForTeam(long teamId);
		long GetTeamsForSport(long sportId);
	}
}
