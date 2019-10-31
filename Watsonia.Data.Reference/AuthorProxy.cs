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
				this.StateTracker.SetFieldValue(nameof(ID), value);
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
				this.StateTracker.SetFieldValue(nameof(FirstName), value);
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
				this.StateTracker.SetFieldValue(nameof(LastName), value);
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
				this.StateTracker.SetFieldValue(nameof(Email), value);
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
				this.StateTracker.SetFieldValue(nameof(DateOfBirth), value);
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
				this.StateTracker.SetFieldValue(nameof(Age), value);
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
				this.StateTracker.SetFieldValue(nameof(Rating), value);
			}
		}

		public override IList<Book> Books
		{
			get
			{
				if (base.Books == null)
				{
					base.Books = this.StateTracker.LoadCollection<Book>(nameof(Books));
				}
				return base.Books;
			}
			set
			{
				base.Books = value;
				this.StateTracker.AddLoadedCollection(nameof(Books));
			}
		}

		public AuthorProxy()
		{
			this.StateTracker.IsLoading = true;

			if (this.FirstName == null)
			{
				this.FirstName = "";
			}
			if (this.LastName == null)
			{
				this.LastName = "";
			}
			if (this.Email == null)
			{
				this.Email = "";
			}
			this.DateOfBirth = new DateTime(10000, DateTimeKind.Local);
			this.Age = 18;
			this.Rating = 5;

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

		public void __SetValue(string name, object value)
		{
			switch (name.ToUpperInvariant())
			{
				case "FIRSTNAME":
				{
					this.FirstName = (string)value;
					break;
				}
				case "DATEOFBIRTH":
				{
					this.DateOfBirth = (DateTime?)value;
					break;
				}
				case "AGE":
				{
					this.Age = (int?)value;
					break;
				}
				case "RATING":
				{
					this.Rating = (double)value;
					break;
				}
				default:
				{
					throw new ArgumentException(name);
				}
			}
		}

		public object __GetValue(string name)
		{
			switch (name.ToUpperInvariant())
			{
				case "FIRSTNAME":
				{
					return this.FirstName;
				}
				case "DATEOFBIRTH":
				{
					return this.DateOfBirth;
				}
				case "AGE":
				{
					return this.Age;
				}
				case "RATING":
				{
					return this.Rating;
				}
			}

			throw new ArgumentException(name);
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
