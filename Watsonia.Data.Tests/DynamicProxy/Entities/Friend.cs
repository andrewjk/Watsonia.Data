using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Watsonia.Data.Tests.DynamicProxy.Entities
{
	public class Friend : Entity
	{
		public virtual long ID { get; set; }

		[Required]
		[StringLength(100)]
		public virtual string Name { get; set; }
		
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
					this.BirthdayMessage = (DateTime.Today.Month == value.Month && DateTime.Today.Day == value.Day) ? $"Happy birthday, {this.Name}!" : "It's not your birthday...";
				}
			}
		}

		public virtual string BirthdayMessage { get; set; }
	}
}
