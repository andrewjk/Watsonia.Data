using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Reference
{
	public class BookProxy : Book, IDynamicProxy
	{
		public event PrimaryKeyValueChangedEventHandler PrimaryKeyValueChanged;

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

		private long? _id;
		public long? ID
		{
			get
			{
				return _id;
			}
			set
			{
				_id = value;
				this.StateTracker.CheckOriginalValue("ID", value);
				OnPrimaryKeyValueChanged(value);
			}
		}

		public object PrimaryKeyValue
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

		private bool _isNew;
		public bool IsNew
		{
			get
			{
				return _isNew;
			}
			set
			{
				_isNew = value;
			}
		}

		private bool _hasChanges;
		public bool HasChanges
		{
			get
			{
				return _hasChanges;
			}
			set
			{
				_hasChanges = value;
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
				this.StateTracker.CheckOriginalValue("Title", value);
			}
		}

		private string _authorID = null;
		public string AuthorID
		{
			get
			{
				return _authorID;
			}
			set
			{
				_authorID = value;
				this.StateTracker.CheckOriginalValue("AuthorID", value);
			}
		}

		public override Author Author
		{
			get
			{
				if (base.Author == null && this.AuthorID != null)
				{
					base.Author = this.StateTracker.LoadItem<Author>(this.AuthorID, "Author");
				}
				return base.Author;
			}
			set
			{
				if (base.Author != null)
				{
					IDynamicProxy authorProxy = (IDynamicProxy)base.Author;
					authorProxy.PrimaryKeyValueChanged -= AuthorProxy_PrimaryKeyValueChanged;
				}
				base.Author = value;
				if (value != null)
				{
					this.StateTracker.AddLoadedItem("Author");
					IDynamicProxy authorProxy = (IDynamicProxy)value;
					SetAuthorID(authorProxy);
					authorProxy.PrimaryKeyValueChanged += AuthorProxy_PrimaryKeyValueChanged;
				}
				else
				{
					this.AuthorID = null;
				}
			}
		}

		private void SetAuthorID(IDynamicProxy value)
		{
			this.AuthorID = (string)value.PrimaryKeyValue;
		}

		private void AuthorProxy_PrimaryKeyValueChanged(object sender, PrimaryKeyValueChangedEventArgs e)
		{
			IDynamicProxy authorProxy = (IDynamicProxy)sender;
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
				this.StateTracker.CheckOriginalValue("Price", value);
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
				this.StateTracker.CheckOriginalValue("Bool", value);
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
				this.StateTracker.CheckOriginalValue("BoolNullable", value);
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
				this.StateTracker.CheckOriginalValue("DateTime", value);
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
				this.StateTracker.CheckOriginalValue("DateTimeNullable", value);
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
				this.StateTracker.CheckOriginalValue("Decimal", value);
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
				this.StateTracker.CheckOriginalValue("DecimalNullable", value);
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
				this.StateTracker.CheckOriginalValue("Double", value);
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
				this.StateTracker.CheckOriginalValue("DoubleNullable", value);
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
				this.StateTracker.CheckOriginalValue("Short", value);
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
				this.StateTracker.CheckOriginalValue("ShortNullable", value);
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
				this.StateTracker.CheckOriginalValue("Int", value);
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
				this.StateTracker.CheckOriginalValue("IntNullable", value);
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
				this.StateTracker.CheckOriginalValue("Long", value);
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
				this.StateTracker.CheckOriginalValue("LongNullable", value);
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
				this.StateTracker.CheckOriginalValue("Byte", value);
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
				this.StateTracker.CheckOriginalValue("ByteNullable", value);
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
				this.StateTracker.CheckOriginalValue("Guid", value);
			}
		}

		public bool IsValid
		{
			get
			{
				return this.StateTracker.IsValid;
			}
		}

		public IList<ValidationError> ValidationErrors
		{
			get
			{
				return this.StateTracker.ValidationErrors;
			}
		}

		public BookProxy()
		{
			this.StateTracker.IsLoading = true;

			if (!this.StateTracker.SetFields.Contains("Price"))
			{
				this.Price = 10;
			}

			this.ResetOriginalValues();

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

			if (((object)a == null) || ((object)b == null))
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
			PrimaryKeyValueChangedEventHandler changed = PrimaryKeyValueChanged;
			if (changed != null)
			{
				changed(this, new PrimaryKeyValueChangedEventArgs(value));
			}
		}

		public void ResetOriginalValues()
		{
			this.StateTracker.OriginalValues["Title"] = this.Title;
			this.StateTracker.OriginalValues["AuthorID"] = this.AuthorID;
			this.StateTracker.OriginalValues["FirstName"] = this.Price;
		}

		public void SetValuesFromReader(DbDataReader source)
		{
			this.StateTracker.IsLoading = true;

			for (int i = 0; i < source.FieldCount; i++)
			{
				switch (source.GetName(i).ToUpperInvariant())
				{
					case "ID":
					{
						this.ID = source.GetInt64(i);
						this.StateTracker.ChangedFields.Remove("ID");
						break;
					}
					case "AUTHORID":
					{
						this.AuthorID = source.GetString(i);
						this.StateTracker.ChangedFields.Remove("AUTHORID");
						break;
					}
					case "PRICE":
					{
						this.Price = source.GetDecimal(i);
						this.StateTracker.ChangedFields.Remove("PRICE");
						break;
					}
					case "BOOL":
					{
						this.Bool = source.GetBoolean(i);
						this.StateTracker.ChangedFields.Remove("BOOL");
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
						this.StateTracker.ChangedFields.Remove("BOOLNULLABLE");
						break;
					}
					case "INT":
					{
						this.Int = source.GetInt32(i);
						this.StateTracker.ChangedFields.Remove("INT");
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
						this.StateTracker.ChangedFields.Remove("INTNULLABLE");
						break;
					}
					default:
					{
						break;
					}
				}
			}

			this.ResetOriginalValues();

			this.StateTracker.IsLoading = false;
		}

		public void SetValuesFromBag(IValueBag bag)
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

			this.ResetOriginalValues();

			this.StateTracker.IsLoading = false;
		}

		public IValueBag GetBagFromValues()
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
