using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.TestPerformance.Entities;

namespace Watsonia.Data.TestPerformance
{
	public class TestResult
	{
		public List<long> LoadedPostIDs { get; set; }
		public List<IEntity> LoadedPosts { get; set; }
		public List<IEntity> LoadedPlayers { get; set; }
		public List<IEntity> LoadedPlayersForTeam { get; set; }
		public List<IEntity> LoadedTeamsForSport { get; set; }

		public int Number { get; set; }
		public TestFramework Framework { get; set; }

		public double AllPostIDsMilliseconds { get; set; }
		public double AllPostsMilliseconds { get; set; }
		public double PlayerByIDMilliseconds { get; set; }
		public double PlayersForTeamMilliseconds { get; set; }
		public double TeamsForSportMilliseconds { get; set; }
	}
}
