using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.TestPerformance.Entities
{
	public class Team
	{
		public virtual long ID { get; set; }
		public virtual string Name { get; set; }
		public virtual DateTime FoundingDate { get; set; }
		[ForeignKey("SportsID")]	// For EF
		public virtual Sport Sport { get; set; }
		public virtual long SportsID { get; set; }
		public virtual ICollection<Player> Players { get; set; }
	}
}
