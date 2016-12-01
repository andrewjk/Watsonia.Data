using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Watsonia.Data.TestPerformance
{
	internal sealed class WatsoniaDatabase : Database
	{
		public const string ConnectionString = @"Data Source=Data\Performance.sdf;Persist Security Info=False";
		private const string EntityNamespace = "Watsonia.Data.TestPerformance.Entities";

		public WatsoniaDatabase()
			: base(new WatsoniaConfiguration(ConnectionString, EntityNamespace))
		{
		}
	}
}
