namespace Watsonia.Data.Tests.DynamicProxy.Entities
{
	public class Address
	{
		public virtual Customer Customer { get; set; }

		public virtual CardinalDirection Direction { get; set; }
	}
}
