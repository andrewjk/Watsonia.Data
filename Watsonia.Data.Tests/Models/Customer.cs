using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Watsonia.Data.Tests.Models
{
	public class Customer : Entity
	{
		public event EventHandler NameChanging;
		public event EventHandler NameChanged;
		public event EventHandler AgeChanging;
		public event EventHandler AgeChanged;

		public virtual long ID
		{
			get;
			set;
		}

		[Required]
		[StringLength(100)]
		public virtual string Name
		{
			get;
			set;
		}

		protected virtual void OnNameChanging(string value)
		{
			var changing = NameChanging;
			if (changing != null)
			{
				changing(this, EventArgs.Empty);
			}
		}

		protected virtual void OnNameChanged()
		{
			var changed = NameChanged;
			if (changed != null)
			{
				changed(this, EventArgs.Empty);
			}
		}

		public virtual int? Age
		{
			get;
			set;
		}

		protected virtual void OnAgeChanging(int? value)
		{
			var changing = AgeChanging;
			if (changing != null)
			{
				changing(this, EventArgs.Empty);
			}
		}

		protected virtual void OnAgeChanged()
		{
			var changed = AgeChanged;
			if (changed != null)
			{
				changed(this, EventArgs.Empty);
			}
		}

		[StringLength(100)]
		[Display(Name = "Email address")]
		public virtual string Email
		{
			get;
			set;
		}

		[StringLength(40)]
		[Display(Name = "ABN")]
		public virtual string Abn
		{
			get;
			set;
		}

		[Display(Name = "License count")]
		public virtual int LicenseCount
		{
			get;
			set;
		}

		public virtual ICollection<Order> Orders
		{
			get;
			set;
		}
	}
}