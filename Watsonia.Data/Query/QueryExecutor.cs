using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
			Select select = StatementCreator.Compile(command.Expression);
			if (entity != null)
			{
				select.IncludePaths.AddRange(entity.IncludePaths);
			}
			return _database.LoadCollection<T>(select);
		}
	}
}
