using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Watsonia.Data.Tests.DynamicProxy.Entities
{
	public class Customer : Entity
	{
		public virtual long ID { get; set; }

		[Required]
		[StringLength(100)]
		public virtual string Name { get; set; }

		[StringLength(100)]
		[Display(Name = "Email address")]
		public virtual string Email { get; set; }

		public virtual ICollection<Order> Orders { get; set; }
	}
}
