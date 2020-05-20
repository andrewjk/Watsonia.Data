using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data;
using Watsonia.Data.EventArgs;

namespace Watsonia.Data.Reference
{
	public class BookProxy : Book, IDynamicProxy
	{
		public event PrimaryKeyValueChangedEventHandler __PrimaryKeyValueChanged;

		private void OnPrimaryKeyValueChanged(object value)
		{
			__PrimaryKeyValueChanged?.Invoke(this, new PrimaryKeyValueChangedEventArgs(value));
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
				if (base.Author == null && this.AuthorID != null)
				{
					base.Author = this.StateTracker.LoadItem<Author>(this.AuthorID.Value, nameof(Author));
				}
				return base.Author;
			}
			set
			{
				if (base.Author != null)
				{
					var authorProxy = (IDynamicProxy)base.Author;
					authorProxy.__PrimaryKeyValueChanged -= AuthorProxy_PrimaryKeyValueChanged;
				}
				base.Author = value;
				if (value != null)
				{
					this.StateTracker.AddLoadedItem(nameof(Author));
					var authorProxy = (IDynamicProxy)value;
					this.AuthorID = (long?)authorProxy.__PrimaryKeyValue;
					authorProxy.__PrimaryKeyValueChanged += AuthorProxy_PrimaryKeyValueChanged;
				}
				else
				{
					this.AuthorID = null;
				}
			}
		}

		private void AuthorProxy_PrimaryKeyValueChanged(object sender, PrimaryKeyValueChangedEventArgs e)
		{
			var authorProxy = (IDynamicProxy)sender;
			this.AuthorID = (long?)authorProxy.__PrimaryKeyValue;
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

		private long? _authorid;
		public long? AuthorID
		{
			get
			{
				return _authorid;
			}
			set
			{
				_authorid = value;
				this.StateTracker.SetFieldValue(nameof(AuthorID), value);
			}
		}

		public string __TableName { get; } = "Book";

		public string __PrimaryKeyColumnName { get; } = "ID";

		public Dictionary<string, ColumnMapping> __ColumnMappings { get; } = new Dictionary<string, ColumnMapping>() {
			{ "ID", new ColumnMapping() { Name = "ID", TypeName = "long" } },
			{ "TITLE", new ColumnMapping() { Name = "Title", TypeName = "string" } },
			{ "AUTHOR", new ColumnMapping() { Name = "Author", TypeName = "Author", IsRelatedItem = true } },
			{ "PRICE", new ColumnMapping() { Name = "Price", TypeName = "decimal" } },
			{ "AUTHORID", new ColumnMapping() { Name = "AuthorID", TypeName = "long?" } },
		};

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
				case "PRICE":
				{
					return this.Price;
				}
				case "AUTHORID":
				{
					return this.AuthorID;
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
				case "PRICE":
				{
					this.Price = (decimal)value;
					break;
				}
				case "AUTHORID":
				{
					this.AuthorID = (long?)value;
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
			this.StateTracker.OriginalValues["AuthorID"] = this.AuthorID;
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
					case "AUTHORID":
					{
						this.AuthorID = source.GetInt64(i);
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
			bookBag.Price = this.Price;
			bookBag.AuthorID = this.AuthorID;
			return bookBag;
		}

		public void __SetValuesFromBag(IValueBag bag)
		{
			this.StateTracker.IsLoading = true;

			var bookBag = (BookValueBag)bag;
			this.ID = bookBag.ID;
			this.Title = bookBag.Title;
			this.Price = bookBag.Price;
			this.AuthorID = bookBag.AuthorID;

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
