using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.TestPerformance
{
	public class TestResult
	{
		public List<long> LoadedPosts { get; set; }
		public List<long> LoadedPlayers { get; set; }
		public List<long> LoadedPlayersForTeam { get; set; }
		public List<long> LoadedTeamsForSport { get; set; }

		public int Number { get; set; }
		public TestFramework Framework { get; set; }

		public double AllPostsMilliseconds { get; set; }
		public double PlayerByIDMilliseconds { get; set; }
		public double PlayersForTeamMilliseconds { get; set; }
		public double TeamsForSportMilliseconds { get; set; }
	}
}
