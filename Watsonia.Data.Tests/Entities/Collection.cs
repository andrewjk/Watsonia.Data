using System;
using System.Linq;

namespace Watsonia.Data.Tests.Entities
{
	// Used for checking collection loading
	public class Collection
	{
		public virtual int Value { get; set; }

		public virtual string Description { get; set; }
	}
}