using System.Collections.Generic;

namespace Watsonia.Data.TestPerformance
{
	/// <summary>
	/// Deterministic name pools for the benchmark fixture.
	/// </summary>
	/// <remarks>
	/// These are synthetic combinations generated for this benchmark, not a copy of
	/// any third-party name list.
	/// </remarks>
	public static class Names
	{
		private static readonly string[] FirstPrefixes =
		{
			"Al", "Bel", "Cor", "Dar", "El", "Fen", "Gar", "Hal", "Is", "Jor",
			"Kel", "Lor", "Mar", "Nor", "Or", "Per", "Quin", "Ros", "Sel", "Tor",
			"Ul", "Val", "Wyn", "Xan", "Yor", "Zel"
		};

		private static readonly string[] FirstSuffixes =
		{
			"a", "an", "en", "ia", "in", "is", "on", "or", "us", "yn"
		};

		private static readonly string[] LastPrefixes =
		{
			"Ald", "Bran", "Cal", "Dun", "Eld", "Fal", "Glen", "Harrow", "Iven", "Jar",
			"Kes", "Lan", "Mor", "Nal", "Os", "Pell", "Quar", "Rav", "Sten", "Thal",
			"Ul", "Vale", "Wes", "Yar"
		};

		private static readonly string[] LastSuffixes =
		{
			"ard", "born", "dale", "ford", "gan", "hill", "ley", "more", "ridge", "ton"
		};

		private static readonly string[] CityPrefixes =
		{
			"Ash", "Bram", "Cald", "Dun", "Elm", "Fair", "Green", "Har", "Inver", "Jor",
			"Kings", "Lang", "Midd", "North", "Oak", "Port", "Red", "Stone", "Thorn", "West"
		};

		private static readonly string[] CitySuffixes =
		{
			"bridge", "bury", "dale", "field", "ford", "haven", "holm", "mouth", "stead", "worth"
		};

		private static readonly string[] TeamSuffixes =
		{
			"Comets", "Falcons", "Foxes", "Harbour", "Hawks", "Lions", "Marlins", "Pioneers",
			"Rangers", "Rockets", "Sentinels", "Sharks", "Titans", "Voyagers", "Wolves"
		};

		private static readonly string[] Sports =
		{
			"Athletics", "Basketball", "Cricket", "Cycling", "Football", "Handball",
			"Hockey", "Netball", "Rugby", "Swimming", "Tennis", "Volleyball"
		};

		public static List<string> GetFirstNames()
		{
			return Combine(FirstPrefixes, FirstSuffixes);
		}

		public static List<string> GetLastNames()
		{
			return Combine(LastPrefixes, LastSuffixes);
		}

		public static List<string> GetCityNames()
		{
			return Combine(CityPrefixes, CitySuffixes);
		}

		public static List<string> GetTeamNames()
		{
			return new List<string>(TeamSuffixes);
		}

		public static List<string> GetSportNames()
		{
			return new List<string>(Sports);
		}

		private static List<string> Combine(string[] prefixes, string[] suffixes)
		{
			var values = new List<string>(prefixes.Length * suffixes.Length);
			foreach (var prefix in prefixes)
			{
				foreach (var suffix in suffixes)
				{
					values.Add(prefix + suffix);
				}
			}
			return values;
		}
	}
}
