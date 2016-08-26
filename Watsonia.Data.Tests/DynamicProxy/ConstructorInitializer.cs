using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Watsonia.Data.Tests.DynamicProxy
{
	public class ConstructorInitializer
	{
		public virtual string Name { get; set; }

		public virtual string Description { get; set; }

		public ConstructorInitializer()
		{
			this.Name = "Hello";
			this.Description = "Hey";
		}
	}
}
