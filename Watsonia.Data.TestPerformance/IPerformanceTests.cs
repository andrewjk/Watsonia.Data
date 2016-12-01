﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.TestPerformance
{
	public interface IPerformanceTests
	{
		List<string> LoadedItems { get; }
		long GetAllPosts();
		long GetPlayerByID(int id);
		long GetPlayersForTeam(int teamId);
		long GetTeamsForSport(int sportId);
	}
}