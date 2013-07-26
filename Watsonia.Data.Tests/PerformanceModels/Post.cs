using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Tests.PerformanceModels
{
	public class Post
	{
		public virtual long ID { get; set; }
		[StringLength(2000)]
		public virtual string Text { get; set; }
		public virtual DateTime CreationDate { get; set; }
		public virtual DateTime LastChangeDate { get; set; }
		public virtual int? Counter1 { get; set; }
		public virtual int? Counter2 { get; set; }
		public virtual int? Counter3 { get; set; }
		public virtual int? Counter4 { get; set; }
		public virtual int? Counter5 { get; set; }
		public virtual int? Counter6 { get; set; }
		public virtual int? Counter7 { get; set; }
		public virtual int? Counter8 { get; set; }
		public virtual int? Counter9 { get; set; }
	}
}
