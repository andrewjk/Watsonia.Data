using System.Linq.Expressions;
using IQToolkit.Data.Common;

namespace Watsonia.Data.Linq
{
	internal class FluentSqlLinguist : QueryLinguist
	{
		public FluentSqlLinguist(FluentSqlLanguage language, QueryTranslator translator)
			: base(language, translator)
		{
		}

		public override Expression Translate(Expression expression)
		{
			// fix up any order-by's
			expression = OrderByRewriter.Rewrite(this.Language, expression);

			expression = base.Translate(expression);

			// convert skip/take info into RowNumber pattern
			expression = SkipToRowNumberRewriter.Rewrite(this.Language, expression);

			// fix up any order-by's we may have changed
			expression = OrderByRewriter.Rewrite(this.Language, expression);

			return expression;
		}
	}
}
