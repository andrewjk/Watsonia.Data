using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.TestPerformance.Entities
{
	public class Post : IEntity
	{
		public virtual long ID { get; set; }
		[StringLength(2000)]
		public virtual string Text { get; set; }
		public virtual DateTime DateCreated { get; set; }
		public virtual DateTime DateModified { get; set; }
	}
}
