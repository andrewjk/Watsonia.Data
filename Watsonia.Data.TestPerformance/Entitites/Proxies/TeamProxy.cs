using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data;
using Watsonia.Data.EventArgs;

namespace Watsonia.Data.TestPerformance.Entities
{
	public class TeamProxy : Team, IDynamicProxy
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

		public override DateTime FoundingDate
		{
			get
			{
				return base.FoundingDate;
			}
			set
			{
				base.FoundingDate = value;
				this.StateTracker.SetFieldValue(nameof(FoundingDate), value);
			}
		}

		public override Sport Sport
		{
			get
			{
				if (base.Sport == null && this.SportID != null)
				{
					base.Sport = this.StateTracker.LoadItem<Sport>(this.SportID.Value, nameof(Sport));
				}
				return base.Sport;
			}
			set
			{
				if (base.Sport != null)
				{
					var sportProxy = (IDynamicProxy)base.Sport;
					sportProxy.__PrimaryKeyValueChanged -= SportProxy_PrimaryKeyValueChanged;
				}
				base.Sport = value;
				if (value != null)
				{
					this.StateTracker.AddLoadedItem(nameof(Sport));
					var sportProxy = (IDynamicProxy)value;
					this.SportID = (long?)sportProxy.__PrimaryKeyValue;
					sportProxy.__PrimaryKeyValueChanged += SportProxy_PrimaryKeyValueChanged;
				}
				else
				{
					this.SportID = null;
				}
			}
		}

		private void SportProxy_PrimaryKeyValueChanged(object sender, PrimaryKeyValueChangedEventArgs e)
		{
			var sportProxy = (IDynamicProxy)sender;
			this.SportID = (long?)sportProxy.__PrimaryKeyValue;
		}

		public override long SportsID
		{
			get
			{
				return base.SportsID;
			}
			set
			{
				base.SportsID = value;
				this.StateTracker.SetFieldValue(nameof(SportsID), value);
			}
		}

		public override ICollection<Player> Players
		{
			get
			{
				if (base.Players == null)
				{
					base.Players = this.StateTracker.LoadCollection<Player>(nameof(Players));
				}
				return base.Players;
			}
			set
			{
				base.Players = value;
				this.StateTracker.AddLoadedCollection(nameof(Players));
			}
		}

		private long? _sportid;
		public long? SportID
		{
			get
			{
				return _sportid;
			}
			set
			{
				_sportid = value;
				this.StateTracker.SetFieldValue(nameof(SportID), value);
			}
		}

		public string __TableName { get; } = "Team";

		public string __PrimaryKeyColumnName { get; } = "ID";

		public Dictionary<string, ColumnMapping> __ColumnMappings { get; } = new Dictionary<string, ColumnMapping>() {
			{ "ID", new ColumnMapping() { Name = "ID", TypeName = "long" } },
			{ "NAME", new ColumnMapping() { Name = "Name", TypeName = "string" } },
			{ "FOUNDINGDATE", new ColumnMapping() { Name = "FoundingDate", TypeName = "DateTime" } },
			{ "SPORT", new ColumnMapping() { Name = "Sport", TypeName = "Sport", IsRelatedItem = true } },
			{ "SPORTSID", new ColumnMapping() { Name = "SportsID", TypeName = "long" } },
			{ "PLAYERS", new ColumnMapping() { Name = "Players", TypeName = "ICollection<Player>", IsRelatedCollection = true, CollectionTypeName = "Player" } },
			{ "SPORTID", new ColumnMapping() { Name = "SportID", TypeName = "long?" } },
		};

		public TeamProxy()
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
				case "FOUNDINGDATE":
				{
					return this.FoundingDate;
				}
				case "SPORTSID":
				{
					return this.SportsID;
				}
				case "SPORTID":
				{
					return this.SportID;
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
				case "FOUNDINGDATE":
				{
					this.FoundingDate = (DateTime)value;
					break;
				}
				case "SPORTSID":
				{
					this.SportsID = (long)value;
					break;
				}
				case "SPORTID":
				{
					this.SportID = (long?)value;
					break;
				}
			}

			throw new ArgumentException(name);
		}

		public void __SetOriginalValues()
		{
			this.StateTracker.OriginalValues["ID"] = this.ID;
			this.StateTracker.OriginalValues["Name"] = this.Name;
			this.StateTracker.OriginalValues["FoundingDate"] = this.FoundingDate;
			this.StateTracker.OriginalValues["Sport"] = this.Sport;
			this.StateTracker.OriginalValues["SportsID"] = this.SportsID;
			this.StateTracker.OriginalValues["Players"] = this.Players;
			this.StateTracker.OriginalValues["SportID"] = this.SportID;
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
					case "FOUNDINGDATE":
					{
						this.FoundingDate = source.GetDateTime(i);
						break;
					}
					case "SPORTSID":
					{
						this.SportsID = source.GetInt64(i);
						break;
					}
					case "SPORTID":
					{
						this.SportID = source.GetInt64(i);
						break;
					}
				}
			}

			this.__SetOriginalValues();

			this.StateTracker.IsLoading = false;
		}

		public IValueBag __GetBagFromValues()
		{
			var teamBag = new TeamValueBag();
			teamBag.ID = this.ID;
			teamBag.Name = this.Name;
			teamBag.FoundingDate = this.FoundingDate;
			teamBag.SportsID = this.SportsID;
			teamBag.SportID = this.SportID;
			return teamBag;
		}

		public void __SetValuesFromBag(IValueBag bag)
		{
			this.StateTracker.IsLoading = true;

			var teamBag = (TeamValueBag)bag;
			this.ID = teamBag.ID;
			this.Name = teamBag.Name;
			this.FoundingDate = teamBag.FoundingDate;
			this.SportsID = teamBag.SportsID;
			this.SportID = teamBag.SportID;

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

		public static bool operator ==(TeamProxy a, TeamProxy b)
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

		public static bool operator !=(TeamProxy a, TeamProxy b)
		{
			return !(a == b);
		}
	}
}
