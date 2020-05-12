namespace Watsonia.Data.Tests.DynamicProxy.Entities
{
	public class Suburb
	{
		public virtual long? StateID { get; set; }

		public virtual State State { get; set; }

		public virtual string Name { get; set; }
	}
}
