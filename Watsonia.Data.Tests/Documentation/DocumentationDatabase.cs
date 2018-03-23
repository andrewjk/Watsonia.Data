using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.SQLite;

namespace Watsonia.Data.Tests.Documentation
{
	public class DocumentationDatabase : Database
	{
		private const string ConnectionString = @"Data Source=Data\DocumentationTests.sqlite";

		public DocumentationDatabase()
			: base(new SQLiteDataAccessProvider(), DocumentationDatabase.ConnectionString, "Watsonia.Data.Tests.Documentation")
		{
		}
	}
}
