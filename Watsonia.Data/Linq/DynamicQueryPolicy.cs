using IQToolkit.Data.Common;

namespace Watsonia.Data.Linq
{
	/// <summary>
	/// Provides the object that defines query execution and materialization policies.
	/// </summary>
	internal class DynamicQueryPolicy : QueryPolicy
	{
		/// <summary>
		/// Creates the object that defines query execution and mataerialization policies.
		/// </summary>
		/// <param name="translator">The translator.</param>
		/// <returns></returns>
		public override QueryPolice CreatePolice(QueryTranslator translator)
		{
			return new DynamicQueryPolice(this, translator);
		}

		/// <summary>
		/// The default query policy.
		/// </summary>
		public new static readonly DynamicQueryPolicy Default = new DynamicQueryPolicy();
	}
}