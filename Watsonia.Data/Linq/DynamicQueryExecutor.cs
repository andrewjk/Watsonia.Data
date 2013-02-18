using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IQToolkit.Data.Common;

namespace Watsonia.Data.Linq
{
	/// <summary>
	/// A class that executes queries against a database.
	/// </summary>
	internal class DynamicQueryExecutor : QueryExecutor
	{
		private readonly Database _database;

		public override int RowsAffected
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DynamicQueryExecutor" /> class.
		/// </summary>
		/// <param name="database">The database to execute queries against.</param>
		public DynamicQueryExecutor(Database database)
		{
			_database = database;
		}

		/// <summary>
		/// Executes the specified command.
		/// </summary>
		/// <typeparam name="T">The type of object that is returned by the executed command.</typeparam>
		/// <param name="command">The command to execute.</param>
		/// <param name="projector">The function projector.</param>
		/// <param name="entity">The entity.</param>
		/// <param name="paramValues">The parameter values to use with the command.</param>
		/// <returns>An IEnumerable collection of objects of type T.</returns>
		public override IEnumerable<T> Execute<T>(QueryCommand command, Func<FieldReader, T> projector, MappingEntity entity, object[] paramValues)
		{
			QueryBlock block = (QueryBlock)command;
			Select select = StatementCreator.Compile(block.Expression);
			return _database.LoadCollection<T>(select);
		}

		public override object Convert(object value, Type type)
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<T> ExecuteBatch<T>(QueryCommand query, IEnumerable<object[]> paramSets, Func<FieldReader, T> projector, MappingEntity entity, int batchSize, bool stream)
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<int> ExecuteBatch(QueryCommand query, IEnumerable<object[]> paramSets, int batchSize, bool stream)
		{
			throw new NotImplementedException();
		}

		public override int ExecuteCommand(QueryCommand query, object[] paramValues)
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<T> ExecuteDeferred<T>(QueryCommand query, Func<FieldReader, T> projector, MappingEntity entity, object[] paramValues)
		{
			throw new NotImplementedException();
		}
	}
}
