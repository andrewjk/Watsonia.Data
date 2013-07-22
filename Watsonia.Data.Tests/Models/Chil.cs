using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Tests.Models
{
	public class Chil
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

		[Cascade]
		public virtual ICollection<SubChil> SubChils
		{
			get;
			set;
		}
	}
}
