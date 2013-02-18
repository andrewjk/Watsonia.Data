using System;
using System.Linq.Expressions;
using System.Reflection;
using IQToolkit.Data;
using IQToolkit.Data.Common;

namespace Watsonia.Data.Linq
{
	internal class FluentSqlLanguage : QueryLanguage
	{
		DbTypeSystem _typeSystem = new DbTypeSystem();

		public override QueryTypeSystem TypeSystem
		{
			get
			{
				return _typeSystem;
			}
		}

		public override bool AllowsMultipleCommands
		{
			get
			{
				return true;
			}
		}

		public override bool AllowSubqueryInSelectWithoutFrom
		{
			get
			{
				return true;
			}
		}

		public override bool AllowDistinctInAggregates
		{
			get
			{
				return true;
			}
		}

		public FluentSqlLanguage()
		{
		}

		public override string Quote(string name)
		{
			throw new NotImplementedException();
		}

		public override Expression GetGeneratedIdExpression(MemberInfo member)
		{
			throw new NotImplementedException();
		}

		public override QueryLinguist CreateLinguist(QueryTranslator translator)
		{
			return new FluentSqlLinguist(this, translator);
		}

		private static FluentSqlLanguage _default;

		public static FluentSqlLanguage Default
		{
			get
			{
				if (_default == null)
				{
					System.Threading.Interlocked.CompareExchange(ref _default, new FluentSqlLanguage(), null);
				}
				return _default;
			}
		}
	}
}
