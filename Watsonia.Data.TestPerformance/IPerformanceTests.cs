using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.TestPerformance
{
	public interface IPerformanceTests
	{
		List<long> LoadedPosts { get; }
		List<long> LoadedPlayers { get; }
		List<long> LoadedPlayersForTeam { get; }
		List<long> LoadedTeamsForSport { get; }

		long GetAllPosts();
		long GetPlayerByID(long id);
		long GetPlayersForTeam(long teamId);
		long GetTeamsForSport(long sportId);
	}
}
