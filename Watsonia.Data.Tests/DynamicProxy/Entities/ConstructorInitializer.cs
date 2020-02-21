using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Watsonia.Data.Tests.DynamicProxy.Entities
{
	public class ConstructorInitializer
	{
		public virtual string Name { get; set; }

		private string _description = "Hey";
		public virtual string Description
		{
			get
			{
				return _description;
			}
			set
			{
				_description = value;
			}
		}

		public ConstructorInitializer()
		{
			this.Name = "Hello";
		}
	}
}
