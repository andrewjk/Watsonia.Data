using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data;
using Watsonia.Data.EventArgs;

namespace Watsonia.Data.TestPerformance.Entities
{
	public class SportProxy : Sport, IDynamicProxy
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

		public override string Name
		{
			get
			{
				return base.Name;
			}
			set
			{
				base.Name = value;
				this.StateTracker.SetFieldValue(nameof(Name), value);
			}
		}

		public override ICollection<Team> Teams
		{
			get
			{
				if (base.Teams == null)
				{
					base.Teams = this.StateTracker.LoadCollection<Team>(nameof(Teams));
				}
				return base.Teams;
			}
			set
			{
				base.Teams = value;
				this.StateTracker.AddLoadedCollection(nameof(Teams));
			}
		}

		public string __TableName { get; } = "Sport";

		public string __PrimaryKeyColumnName { get; } = "ID";

		public Dictionary<string, ColumnMapping> __ColumnMappings { get; } = new Dictionary<string, ColumnMapping>() {
			{ "ID", new ColumnMapping() { Name = "ID", TypeName = "long" } },
			{ "NAME", new ColumnMapping() { Name = "Name", TypeName = "string" } },
			{ "TEAMS", new ColumnMapping() { Name = "Teams", TypeName = "ICollection<Team>", IsRelatedCollection = true, CollectionTypeName = "Team" } },
		};

		public SportProxy()
		{
			this.StateTracker.IsLoading = true;

			if (this.Name == null)
			{
				this.Name = "";
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
				case "NAME":
				{
					return this.Name;
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
				case "NAME":
				{
					this.Name = (string)value;
					break;
				}
			}

			throw new ArgumentException(name);
		}

		public void __SetOriginalValues()
		{
			this.StateTracker.OriginalValues["ID"] = this.ID;
			this.StateTracker.OriginalValues["Name"] = this.Name;
			this.StateTracker.OriginalValues["Teams"] = this.Teams;
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
					case "NAME":
					{
						this.Name = source.GetString(i);
						break;
					}
				}
			}

			this.__SetOriginalValues();

			this.StateTracker.IsLoading = false;
		}

		public IValueBag __GetBagFromValues()
		{
			var sportBag = new SportValueBag();
			sportBag.ID = this.ID;
			sportBag.Name = this.Name;
			return sportBag;
		}

		public void __SetValuesFromBag(IValueBag bag)
		{
			this.StateTracker.IsLoading = true;

			var sportBag = (SportValueBag)bag;
			this.ID = sportBag.ID;
			this.Name = sportBag.Name;

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

		public static bool operator ==(SportProxy a, SportProxy b)
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

		public static bool operator !=(SportProxy a, SportProxy b)
		{
			return !(a == b);
		}
	}
}
