using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Tests.DynamicProxy
{
	public class InvalidChild
	{
		[StringLength(10)]
		public virtual string Name
		{
			get;
			set;
		}
	}
}
