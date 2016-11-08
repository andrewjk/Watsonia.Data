using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.SqlServerCe;

namespace Watsonia.Data.Tests.Documentation
{
	public class DocumentationDatabase : Database
	{
		public const string ConnectionString = @"Data Source=Data\DocumentationTests.sdf;Persist Security Info=False";

		public DocumentationDatabase()
			: base(new SqlServerCeDataAccessProvider(), DocumentationDatabase.ConnectionString, "Watsonia.Data.Tests.Documentation")
		{
		}
	}
}
