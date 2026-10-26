using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data;
using Watsonia.Data.EventArgs;

namespace Watsonia.Data.TestPerformance.Entities
{
	public class PostProxy : Post, IDynamicProxy
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

		public override string Text
		{
			get
			{
				return base.Text;
			}
			set
			{
				base.Text = value;
				this.StateTracker.SetFieldValue(nameof(Text), value);
			}
		}

		public override DateTime DateCreated
		{
			get
			{
				return base.DateCreated;
			}
			set
			{
				base.DateCreated = value;
				this.StateTracker.SetFieldValue(nameof(DateCreated), value);
			}
		}

		public override DateTime DateModified
		{
			get
			{
				return base.DateModified;
			}
			set
			{
				base.DateModified = value;
				this.StateTracker.SetFieldValue(nameof(DateModified), value);
			}
		}

		public string __TableName { get; } = "Post";

		public string __PrimaryKeyColumnName { get; } = "ID";

		public Dictionary<string, ColumnMapping> __ColumnMappings { get; } = new Dictionary<string, ColumnMapping>() {
			{ "ID", new ColumnMapping() { Name = "ID", TypeName = "long" } },
			{ "TEXT", new ColumnMapping() { Name = "Text", TypeName = "string" } },
			{ "DATECREATED", new ColumnMapping() { Name = "DateCreated", TypeName = "DateTime" } },
			{ "DATEMODIFIED", new ColumnMapping() { Name = "DateModified", TypeName = "DateTime" } },
		};

		public PostProxy()
		{
			this.StateTracker.IsLoading = true;

			if (this.Text == null)
			{
				this.Text = "";
			}

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
				case "TEXT":
				{
					return this.Text;
				}
				case "DATECREATED":
				{
					return this.DateCreated;
				}
				case "DATEMODIFIED":
				{
					return this.DateModified;
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
				case "TEXT":
				{
					this.Text = (string)value;
					break;
				}
				case "DATECREATED":
				{
					this.DateCreated = (DateTime)value;
					break;
				}
				case "DATEMODIFIED":
				{
					this.DateModified = (DateTime)value;
					break;
				}
			}

			throw new ArgumentException(name);
		}

		public void __SetOriginalValues()
		{
			this.StateTracker.OriginalValues["ID"] = this.ID;
			this.StateTracker.OriginalValues["Text"] = this.Text;
			this.StateTracker.OriginalValues["DateCreated"] = this.DateCreated;
			this.StateTracker.OriginalValues["DateModified"] = this.DateModified;
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
					case "TEXT":
					{
						this.Text = source.GetString(i);
						break;
					}
					case "DATECREATED":
					{
						this.DateCreated = source.GetDateTime(i);
						break;
					}
					case "DATEMODIFIED":
					{
						this.DateModified = source.GetDateTime(i);
						break;
					}
				}
			}

			this.__SetOriginalValues();

			this.StateTracker.IsLoading = false;
		}

		public IValueBag __GetBagFromValues()
		{
			var postBag = new PostValueBag();
			postBag.ID = this.ID;
			postBag.Text = this.Text;
			postBag.DateCreated = this.DateCreated;
			postBag.DateModified = this.DateModified;
			return postBag;
		}

		public void __SetValuesFromBag(IValueBag bag)
		{
			this.StateTracker.IsLoading = true;

			var postBag = (PostValueBag)bag;
			this.ID = postBag.ID;
			this.Text = postBag.Text;
			this.DateCreated = postBag.DateCreated;
			this.DateModified = postBag.DateModified;

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

		public static bool operator ==(PostProxy a, PostProxy b)
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

		public static bool operator !=(PostProxy a, PostProxy b)
		{
			return !(a == b);
		}
	}
}
