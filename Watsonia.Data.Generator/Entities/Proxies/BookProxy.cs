using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data;
using Watsonia.Data.EventArgs;

namespace Watsonia.Data.Generator.Proxies
{
	public class BookProxy : Book, IDynamicProxy
	{
		public event PrimaryKeyValueChangedEventHandler __PrimaryKeyValueChanged;
		private void OnPrimaryKeyValueChanged(object value)
		{
			__PrimaryKeyValueChanged?.Invoke(this, new PrimaryKeyValueChangedEventArgs(value));
		}

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

		public override long ID
		{
			get
			{
				return base.ID;
			}
			set
			{
				base.ID = value;
				this.StateTracker.SetFieldValue(nameof(ID), value);
				OnPrimaryKeyValueChanged(value);
			}
		}

		public override string Title
		{
			get
			{
				return base.Title;
			}
			set
			{
				base.Title = value;
				this.StateTracker.SetFieldValue(nameof(Title), value);
			}
		}

		public override Author Author
		{
			get
			{
				return base.Author;
			}
			set
			{
				base.Author = value;
				this.StateTracker.SetFieldValue(nameof(Author), value);
			}
		}

		public override decimal Price
		{
			get
			{
				return base.Price;
			}
			set
			{
				base.Price = value;
				this.StateTracker.SetFieldValue(nameof(Price), value);
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

		public BookProxy()
		{
			this.StateTracker.IsLoading = true;

			if (this.Title == null)
			{
				this.Title = "";
			}
			this.Price = 10;

			this.__SetOriginalValues();

			this.StateTracker.IsLoading = false;
		}

		public object __GetValue(string name)
		{
			switch (name.ToUpperInvariant())
			{
				case "ID":
				{
					return this.ID;
				}
				case "TITLE":
				{
					return this.Title;
				}
				case "AUTHOR":
				{
					return this.Author;
				}
				case "PRICE":
				{
					return this.Price;
				}
			}

			throw new ArgumentException(name);
		}

		public void __SetValue(string name, object value)
		{
			switch (name.ToUpperInvariant())
			{
				case "ID":
				{
					this.ID = (long)value;
					break;
				}
				case "TITLE":
				{
					this.Title = (string)value;
					break;
				}
				case "AUTHOR":
				{
					this.Author = (Author)value;
					break;
				}
				case "PRICE":
				{
					this.Price = (decimal)value;
					break;
				}
			}

			throw new ArgumentException(name);
		}

		public void __SetOriginalValues()
		{
			this.StateTracker.OriginalValues["ID"] = this.ID;
			this.StateTracker.OriginalValues["Title"] = this.Title;
			this.StateTracker.OriginalValues["Author"] = this.Author;
			this.StateTracker.OriginalValues["Price"] = this.Price;
		}

		public void __SetValuesFromReader(DbDataReader source, string[] fieldNames)
		{
			this.StateTracker.IsLoading = true;

			for (var i = 0; i < fieldNames.Length; i++)
			{
				switch (fieldNames[i])
				{
					case "ID":
					{
						this.ID = source.GetInt64(i);
						break;
					}
					case "TITLE":
					{
						this.Title = source.GetString(i);
						break;
					}
					case "PRICE":
					{
						this.Price = source.GetDecimal(i);
						break;
					}
				}
			}

			this.__SetOriginalValues();

			this.StateTracker.IsLoading = false;
		}

		public IValueBag __GetBagFromValues()
		{
			var bookBag = new BookValueBag();
			bookBag.ID = this.ID;
			bookBag.Title = this.Title;
			bookBag.Author = this.Author;
			bookBag.Price = this.Price;
			return bookBag;
		}

		public void __SetValuesFromBag(IValueBag bag)
		{
			this.StateTracker.IsLoading = true;

			var bookBag = (BookValueBag)bag;
			this.ID = bookBag.ID;
			this.Title = bookBag.Title;
			this.Author = bookBag.Author;
			this.Price = bookBag.Price;

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

		public static bool operator ==(BookProxy a, BookProxy b)
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

		public static bool operator !=(BookProxy a, BookProxy b)
		{
			return !(a == b);
		}


	}
}
