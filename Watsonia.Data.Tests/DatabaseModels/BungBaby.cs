using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Tests.DatabaseModels
{
	public class BungBaby
	{
		[StringLength(10)]
		public virtual string Name
		{
			get;
			set;
		}
	}
}
