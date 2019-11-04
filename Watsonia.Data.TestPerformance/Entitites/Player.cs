using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.TestPerformance.Entities
{
	public class Player : IEntity
	{
		public virtual long ID { get; set; }
		public virtual string FirstName { get; set; }
		public virtual string LastName { get; set; }
		public virtual DateTime DateOfBirth { get; set; }
		[ForeignKey("TeamsID")]	// For EF
		public virtual Team Team { get; set; }
		public virtual long TeamsID { get; set; }
	}
}
