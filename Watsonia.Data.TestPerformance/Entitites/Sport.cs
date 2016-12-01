using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.TestPerformance.Entities
{
	public class Sport
	{
		public virtual long ID { get; set; }
		public virtual string Name { get; set; }
		public virtual ICollection<Team> Teams { get; set; }
	}
}
