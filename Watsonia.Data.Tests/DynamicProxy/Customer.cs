using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Watsonia.Data.Tests.DynamicProxy
{
	public class Customer : Entity
	{
		public virtual long ID { get; set; }

		[Required]
		[StringLength(100)]
		public virtual string Name { get; set; }
		
		public virtual int? Age { get; set; }

		[StringLength(100)]
		[Display(Name = "Email address")]
		public virtual string Email { get; set; }

		[StringLength(40)]
		[Display(Name = "ABN")]
		public virtual string Abn { get; set; }

		[Display(Name = "License count")]
		public virtual int LicenseCount { get; set; }

		private DateTime _dateOfBirth;
		public virtual DateTime DateOfBirth
		{
			get
			{
				return _dateOfBirth;
			}
			set
			{
				if (_dateOfBirth != value)
				{
					_dateOfBirth = value;
					this.BirthdayMessage = DateTime.Today.DayOfYear == value.DayOfYear ? string.Format("Happy birthday, {0}!", this.Name) : "It's not your birthday...";
				}
			}
		}

		public virtual string BirthdayMessage { get; set; }

		public virtual ICollection<Order> Orders { get; set; }
	}
}