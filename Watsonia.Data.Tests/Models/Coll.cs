using System;
using System.Linq;

namespace Watsonia.Data.Tests.Models
{
	// Used for checking collection loading
	public class Coll
	{
		public virtual int Value
		{
			get;
			set;
		}

		public virtual string Description
		{
			get;
			set;
		}
	}
}