using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.EventArgs;

namespace Watsonia.Data.Reference
{
	public class AuthorProxy : Author, IDynamicProxy
	{
		public event PrimaryKeyValueChangedEventHandler __PrimaryKeyValueChanged;

		private DynamicProxyStateTracker _stateTracker;
		public DynamicProxyStateTracker StateTracker
		{
			get
			{
				if (_stateTracker == null)
				{
					_stateTracker = new DynamicProxyStateTracker();
					_stateTracker.Item = this;
				}
				return _stateTracker;
			}
		}

		private long _id;
		public long ID
		{
			get
			{
				return _id;
			}
			set
			{
				_id = value;
				this.StateTracker.SetFieldValue("ID", value);
				OnPrimaryKeyValueChanged(value);
			}
		}

		public object __PrimaryKeyValue
		{
			get
			{
				return this.ID;
			}
			set
			{
				this.ID = (long)Convert.ChangeType(value, typeof(long));
			}
		}

		public override string FirstName
		{
			get
			{
				return base.FirstName;
			}
			set
			{
				base.FirstName = value;
				this.StateTracker.SetFieldValue("FirstName", value);
			}
		}

		public override string LastName
		{
			get
			{
				return base.LastName;
			}
			set
			{
				base.LastName = value;
				this.StateTracker.SetFieldValue("LastName", value);
			}
		}

		public override string Email
		{
			get
			{
				return base.Email;
			}
			set
			{
				base.Email = value;
				this.StateTracker.SetFieldValue("Email", value);
			}
		}
		
		public override DateTime? DateOfBirth
		{
			get
			{
				return base.DateOfBirth;
			}
			set
			{
				base.DateOfBirth = value;
				this.StateTracker.SetFieldValue("DateOfBirth", value);
			}
		}
		
		public override int? Age
		{
			get
			{
				return base.Age;
			}
			set
			{
				base.Age = value;
				this.StateTracker.SetFieldValue("Age", value);
			}
		}

		public override double Rating
		{
			get
			{
				return base.Rating;
			}
			set
			{
				base.Rating = value;
				this.StateTracker.SetFieldValue("Rating", value);
			}
		}

		public override IList<Book> Books
		{
			get
			{
				if (base.Books == null)
				{
					base.Books = this.StateTracker.LoadCollection<Book>("Books");
				}
				return base.Books;
			}
			set
			{
				base.Books = value;
				this.StateTracker.AddLoadedCollection("Books");
			}
		}

		public AuthorProxy()
		{
			this.StateTracker.IsLoading = true;

			if (!this.StateTracker.SetFields.Contains("FirstName"))
			{
				this.FirstName = "";
			}
			if (!this.StateTracker.SetFields.Contains("LastName"))
			{
				this.LastName = "";
			}
			if (!this.StateTracker.SetFields.Contains("Email"))
			{
				this.Email = "";
			}
			if (!this.StateTracker.SetFields.Contains("DateOfBirth"))
			{
				this.DateOfBirth = new DateTime(10000, DateTimeKind.Local);
			}
			if (!this.StateTracker.SetFields.Contains("Age"))
			{
				this.Age = 18;
			}
			if (!this.StateTracker.SetFields.Contains("Rating"))
			{
				this.Rating = 5;
			}

			this.__SetOriginalValues();

			this.StateTracker.IsLoading = false;
		}

		public override int GetHashCode()
		{
			return this.StateTracker.GetItemHashCode();
		}

		public override bool Equals(object obj)
		{
			return this.StateTracker.ItemEquals(obj);
		}

		public static bool operator ==(AuthorProxy a, AuthorProxy b)
		{
			if (object.ReferenceEquals(a, b))
			{
				return true;
			}

			if ((a is null) || (b is null))
			{
				return false;
			}

			return a.Equals(b);
		}

		public static bool operator !=(AuthorProxy a, AuthorProxy b)
		{
			return !(a == b);
		}

		private void OnPrimaryKeyValueChanged(object value)
		{
			__PrimaryKeyValueChanged?.Invoke(this, new PrimaryKeyValueChangedEventArgs(value));
		}

		public void __SetOriginalValues()
		{
			this.StateTracker.OriginalValues["FirstName"] = this.FirstName;
			this.StateTracker.OriginalValues["LastName"] = this.LastName;
			this.StateTracker.OriginalValues["Email"] = this.Email;
			this.StateTracker.OriginalValues["DateOfBirth"] = this.DateOfBirth;
			this.StateTracker.OriginalValues["Age"] = this.Age;
			this.StateTracker.OriginalValues["Rating"] = this.Rating;
		}

		public void __SetValuesFromReader(DbDataReader source)
		{
			this.StateTracker.IsLoading = true;

			for (var i = 0; i < source.FieldCount; i++)
			{
				switch (source.GetName(i).ToUpperInvariant())
				{
					case "ID":
					{
						this.ID = source.GetInt64(i);
						break;
					}
					case "FIRSTNAME":
					{
						this.FirstName = source.GetString(i);
						break;
					}
					case "LASTNAME":
					{
						this.LastName = source.GetString(i);
						break;
					}
					// etc.
					default:
					{
						break;
					}
				}
			}

			this.__SetOriginalValues();

			this.StateTracker.IsLoading = false;
		}

		public void __SetValuesFromBag(IValueBag bag)
		{
			this.StateTracker.IsLoading = true;

			var authorBag = (AuthorValueBag)bag;
			this.FirstName = authorBag.FirstName;
			this.LastName = authorBag.LastName;
			this.Email = authorBag.Email;
			this.DateOfBirth = authorBag.DateOfBirth;
			this.Age = authorBag.Age;
			this.Rating = authorBag.Rating;

			this.__SetOriginalValues();

			this.StateTracker.IsLoading = false;
		}

		public IValueBag __GetBagFromValues()
		{
			var authorBag = new AuthorValueBag();
			authorBag.FirstName = this.FirstName;
			authorBag.LastName = this.LastName;
			authorBag.Email = this.Email;
			authorBag.DateOfBirth = this.DateOfBirth;
			authorBag.Age = this.Age;
			authorBag.Rating = this.Rating;
			return authorBag;
		}
	}
}
