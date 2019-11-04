using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.EventArgs;

namespace Watsonia.Data.Reference
{
	public class BookProxy : Book, IDynamicProxy
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

		private long? _authorID = null;
		public long? AuthorID
		{
			get
			{
				return _authorID;
			}
			set
			{
				_authorID = value;
				this.StateTracker.SetFieldValue(nameof(AuthorID), value);
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
					SetAuthorID(authorProxy);
					authorProxy.__PrimaryKeyValueChanged += AuthorProxy_PrimaryKeyValueChanged;
				}
				else
				{
					this.AuthorID = null;
				}
			}
		}

		private void SetAuthorID(IDynamicProxy value)
		{
			this.AuthorID = (long?)value.__PrimaryKeyValue;
		}

		private void AuthorProxy_PrimaryKeyValueChanged(object sender, PrimaryKeyValueChangedEventArgs e)
		{
			var authorProxy = (IDynamicProxy)sender;
			SetAuthorID(authorProxy);
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

		public override bool Bool
		{
			get
			{
				return base.Bool;
			}
			set
			{
				base.Bool = value;
				this.StateTracker.SetFieldValue(nameof(Bool), value);
			}
		}

		public override bool? BoolNullable
		{
			get
			{
				return base.BoolNullable;
			}
			set
			{
				base.BoolNullable = value;
				this.StateTracker.SetFieldValue(nameof(BoolNullable), value);
			}
		}

		public override DateTime DateTime
		{
			get
			{
				return base.DateTime;
			}
			set
			{
				base.DateTime = value;
				this.StateTracker.SetFieldValue(nameof(DateTime), value);
			}
		}

		public override DateTime? DateTimeNullable
		{
			get
			{
				return base.DateTimeNullable;
			}
			set
			{
				base.DateTimeNullable = value;
				this.StateTracker.SetFieldValue(nameof(DateTimeNullable), value);
			}
		}

		public override decimal Decimal
		{
			get
			{
				return base.Decimal;
			}
			set
			{
				base.Decimal = value;
				this.StateTracker.SetFieldValue(nameof(Decimal), value);
			}
		}

		public override decimal? DecimalNullable
		{
			get
			{
				return base.DecimalNullable;
			}
			set
			{
				base.DecimalNullable = value;
				this.StateTracker.SetFieldValue(nameof(DecimalNullable), value);
			}
		}

		public override double Double
		{
			get
			{
				return base.Double;
			}
			set
			{
				base.Double = value;
				this.StateTracker.SetFieldValue(nameof(Double), value);
			}
		}

		public override double? DoubleNullable
		{
			get
			{
				return base.DoubleNullable;
			}
			set
			{
				base.DoubleNullable = value;
				this.StateTracker.SetFieldValue(nameof(DoubleNullable), value);
			}
		}

		public override short Short
		{
			get
			{
				return base.Short;
			}
			set
			{
				base.Short = value;
				this.StateTracker.SetFieldValue(nameof(Short), value);
			}
		}

		public override short? ShortNullable
		{
			get
			{
				return base.ShortNullable;
			}
			set
			{
				base.ShortNullable = value;
				this.StateTracker.SetFieldValue(nameof(ShortNullable), value);
			}
		}

		public override int Int
		{
			get
			{
				return base.Int;
			}
			set
			{
				base.Int = value;
				this.StateTracker.SetFieldValue(nameof(Int), value);
			}
		}

		public override int? IntNullable
		{
			get
			{
				return base.IntNullable;
			}
			set
			{
				base.IntNullable = value;
				this.StateTracker.SetFieldValue(nameof(IntNullable), value);
			}
		}

		public override long Long
		{
			get
			{
				return base.Long;
			}
			set
			{
				base.Long = value;
				this.StateTracker.SetFieldValue(nameof(Long), value);
			}
		}

		public override long? LongNullable
		{
			get
			{
				return base.LongNullable;
			}
			set
			{
				base.LongNullable = value;
				this.StateTracker.SetFieldValue(nameof(LongNullable), value);
			}
		}

		public override byte Byte
		{
			get
			{
				return base.Byte;
			}
			set
			{
				base.Byte = value;
				this.StateTracker.SetFieldValue(nameof(Byte), value);
			}
		}

		public override byte? ByteNullable
		{
			get
			{
				return base.ByteNullable;
			}
			set
			{
				base.ByteNullable = value;
				this.StateTracker.SetFieldValue(nameof(ByteNullable), value);
			}
		}

		public override Guid Guid
		{
			get
			{
				return base.Guid;
			}
			set
			{
				base.Guid = value;
				this.StateTracker.SetFieldValue(nameof(Guid), value);
			}
		}

		public BookProxy()
		{
			this.StateTracker.IsLoading = true;

			this.Price = 10;

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

		private void OnPrimaryKeyValueChanged(object value)
		{
			__PrimaryKeyValueChanged?.Invoke(this, new PrimaryKeyValueChangedEventArgs(value));
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
				// etc
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
				case "ID":
				{
					return this.ID;
				}
				// etc
			}

			throw new ArgumentException(name);
		}

		public void __SetOriginalValues()
		{
			this.StateTracker.OriginalValues["Title"] = this.Title;
			this.StateTracker.OriginalValues["AuthorID"] = this.AuthorID;
			this.StateTracker.OriginalValues["Price"] = this.Price;
			this.StateTracker.OriginalValues["Bool"] = this.Bool;
			this.StateTracker.OriginalValues["BoolNullable"] = this.BoolNullable;
			this.StateTracker.OriginalValues["Int"] = this.Int;
			this.StateTracker.OriginalValues["IntNullable"] = this.IntNullable;
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
					case "AUTHORID":
					{
						if (source.IsDBNull(i))
						{
							this.AuthorID = null;
						}
						else
						{
							this.AuthorID = source.GetInt64(i);
						}
						break;
					}
					case "PRICE":
					{
						this.Price = source.GetDecimal(i);
						break;
					}
					case "BOOL":
					{
						this.Bool = source.GetBoolean(i);
						break;
					}
					case "BOOLNULLABLE":
					{
						if (source.IsDBNull(i))
						{
							this.BoolNullable = null;
						}
						else
						{
							this.BoolNullable = source.GetBoolean(i);
						}
						break;
					}
					case "INT":
					{
						this.Int = source.GetInt32(i);
						break;
					}
					case "INTNULLABLE":
					{
						if (source.IsDBNull(i))
						{
							this.IntNullable = null;
						}
						else
						{
							this.IntNullable = source.GetInt32(i);
						}
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

			var bookBag = (BookValueBag)bag;
			this.Title = bookBag.Title;
			this.AuthorID = bookBag.AuthorID;
			this.Price = bookBag.Price;
			this.Bool = bookBag.Bool;
			this.BoolNullable = bookBag.BoolNullable;
			this.DateTime = bookBag.DateTime;
			this.DateTimeNullable = bookBag.DateTimeNullable;
			this.Decimal = bookBag.Decimal;
			this.DecimalNullable = bookBag.DecimalNullable;
			this.Double = bookBag.Double;
			this.DoubleNullable = bookBag.DoubleNullable;
			this.Short = bookBag.Short;
			this.ShortNullable = bookBag.ShortNullable;
			this.Int = bookBag.Int;
			this.IntNullable = bookBag.IntNullable;
			this.Long = bookBag.Long;
			this.LongNullable = bookBag.LongNullable;
			this.Byte = bookBag.Byte;
			this.ByteNullable = bookBag.ByteNullable;
			this.Guid = bookBag.Guid;

			this.__SetOriginalValues();

			this.StateTracker.IsLoading = false;
		}

		public IValueBag __GetBagFromValues()
		{
			var bookBag = new BookValueBag();
			bookBag.Title = this.Title;
			bookBag.AuthorID = this.AuthorID;
			bookBag.Price = this.Price;
			bookBag.Bool = this.Bool;
			bookBag.BoolNullable = this.BoolNullable;
			bookBag.DateTime = this.DateTime;
			bookBag.DateTimeNullable = this.DateTimeNullable;
			bookBag.Decimal = this.Decimal;
			bookBag.DecimalNullable = this.DecimalNullable;
			bookBag.Double = this.Double;
			bookBag.DoubleNullable = this.DoubleNullable;
			bookBag.Short = this.Short;
			bookBag.ShortNullable = this.ShortNullable;
			bookBag.Int = this.Int;
			bookBag.IntNullable = this.IntNullable;
			bookBag.Long = this.Long;
			bookBag.LongNullable = this.LongNullable;
			bookBag.Byte = this.Byte;
			bookBag.ByteNullable = this.ByteNullable;
			bookBag.Guid = this.Guid;
			return bookBag;
		}
	}
}
