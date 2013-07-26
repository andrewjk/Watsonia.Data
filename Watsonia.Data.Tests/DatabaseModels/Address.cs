namespace Watsonia.Data.Tests.DatabaseModels
{
	public class Address
	{
		public Customer Customer
		{
			get;
			set;
		}

		public virtual CardinalDirection Direction
		{
			get;
			set;
		}
	}
}