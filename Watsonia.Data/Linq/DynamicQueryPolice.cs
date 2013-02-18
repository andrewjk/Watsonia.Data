using System.Linq.Expressions;
using IQToolkit.Data.Common;

namespace Watsonia.Data.Linq
{
	/// <summary>
	/// Defines query execution and materialization policies. 
	/// </summary>
	internal class DynamicQueryPolice : QueryPolice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DynamicQueryPolice" /> class.
		/// </summary>
		/// <param name="policy">The policy.</param>
		/// <param name="translator">The translator.</param>
		public DynamicQueryPolice(QueryPolicy policy, QueryTranslator translator)
			: base(policy, translator)
		{
		}

		/// <summary>
		/// Converts a query into an execution plan.  The plan is an function that executes the query and builds the
		/// resulting objects.
		/// </summary>
		/// <param name="query">The query to convert into an execution plan.</param>
		/// <param name="provider">The provider.</param>
		/// <returns>A function that executes the query and builds the resulting objects.</returns>
		public override Expression BuildExecutionPlan(Expression query, Expression provider)
		{
			return DynamicExecutionBuilder.Build(base.Translator.Linguist, base.Policy, query, provider);
		}
	}
}
