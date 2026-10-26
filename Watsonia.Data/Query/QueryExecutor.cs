using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.QueryBuilder;
using Watsonia.QueryBuilder;

namespace Watsonia.Data.Query
{
	public class QueryExecutor
	{
		private readonly Database _database;

		public QueryExecutor(Database database)
		{
			_database = database;
		}

		public IEnumerable<T> Execute<T>(QueryCommand command, Func<FieldReader, T> fnProjector, MappingEntity entity, object[] paramValues)
		{
			SelectStatement select = StatementCreator.Compile(command.Expression);
			if (entity != null)
			{
				select.IncludePaths.AddRange(entity.IncludePaths);
			}

			// Entity queries materialize tracked proxy items through the database
			if (entity != null || fnProjector == null)
			{
				return _database.LoadCollection<T>(select);
			}

			// Other projections are materialized client-side by the projector
			var results = new List<T>();
			using (var connection = _database.OpenConnection())
			using (var dbCommand = _database.Configuration.DataAccessProvider.BuildCommand(select, _database.Configuration))
			{
				dbCommand.Connection = connection;
				using (var reader = dbCommand.ExecuteReader())
				{
					var fieldReader = new DbFieldReader(reader);
					while (reader.Read())
					{
						results.Add(fnProjector(fieldReader));
					}
				}
			}
			return results;
		}
	}
}
