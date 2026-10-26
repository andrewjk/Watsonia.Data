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
	public class PlayerProxy : Player, IDynamicProxy
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

		public override DateTime DateOfBirth
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

		public override Team Team
		{
			get
			{
				if (base.Team == null && this.TeamID != null)
				{
					base.Team = this.StateTracker.LoadItem<Team>(this.TeamID.Value, nameof(Team));
				}
				return base.Team;
			}
			set
			{
				if (base.Team != null)
				{
					var teamProxy = (IDynamicProxy)base.Team;
					teamProxy.__PrimaryKeyValueChanged -= TeamProxy_PrimaryKeyValueChanged;
				}
				base.Team = value;
				if (value != null)
				{
					this.StateTracker.AddLoadedItem(nameof(Team));
					var teamProxy = (IDynamicProxy)value;
					this.TeamID = (long?)teamProxy.__PrimaryKeyValue;
					teamProxy.__PrimaryKeyValueChanged += TeamProxy_PrimaryKeyValueChanged;
				}
				else
				{
					this.TeamID = null;
				}
			}
		}

		private void TeamProxy_PrimaryKeyValueChanged(object sender, PrimaryKeyValueChangedEventArgs e)
		{
			var teamProxy = (IDynamicProxy)sender;
			this.TeamID = (long?)teamProxy.__PrimaryKeyValue;
		}

		public override long TeamsID
		{
			get
			{
				return base.TeamsID;
			}
			set
			{
				base.TeamsID = value;
				this.StateTracker.SetFieldValue(nameof(TeamsID), value);
			}
		}

		private long? _teamid;
		public long? TeamID
		{
			get
			{
				return _teamid;
			}
			set
			{
				_teamid = value;
				this.StateTracker.SetFieldValue(nameof(TeamID), value);
			}
		}

		public string __TableName { get; } = "Player";

		public string __PrimaryKeyColumnName { get; } = "ID";

		public Dictionary<string, ColumnMapping> __ColumnMappings { get; } = new Dictionary<string, ColumnMapping>() {
			{ "ID", new ColumnMapping() { Name = "ID", TypeName = "long" } },
			{ "FIRSTNAME", new ColumnMapping() { Name = "FirstName", TypeName = "string" } },
			{ "LASTNAME", new ColumnMapping() { Name = "LastName", TypeName = "string" } },
			{ "DATEOFBIRTH", new ColumnMapping() { Name = "DateOfBirth", TypeName = "DateTime" } },
			{ "TEAM", new ColumnMapping() { Name = "Team", TypeName = "Team", IsRelatedItem = true } },
			{ "TEAMSID", new ColumnMapping() { Name = "TeamsID", TypeName = "long" } },
			{ "TEAMID", new ColumnMapping() { Name = "TeamID", TypeName = "long?" } },
		};

		public PlayerProxy()
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
				case "FIRSTNAME":
				{
					return this.FirstName;
				}
				case "LASTNAME":
				{
					return this.LastName;
				}
				case "DATEOFBIRTH":
				{
					return this.DateOfBirth;
				}
				case "TEAMSID":
				{
					return this.TeamsID;
				}
				case "TEAMID":
				{
					return this.TeamID;
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
				case "FIRSTNAME":
				{
					this.FirstName = (string)value;
					break;
				}
				case "LASTNAME":
				{
					this.LastName = (string)value;
					break;
				}
				case "DATEOFBIRTH":
				{
					this.DateOfBirth = (DateTime)value;
					break;
				}
				case "TEAMSID":
				{
					this.TeamsID = (long)value;
					break;
				}
				case "TEAMID":
				{
					this.TeamID = (long?)value;
					break;
				}
			}

			throw new ArgumentException(name);
		}

		public void __SetOriginalValues()
		{
			this.StateTracker.OriginalValues["ID"] = this.ID;
			this.StateTracker.OriginalValues["FirstName"] = this.FirstName;
			this.StateTracker.OriginalValues["LastName"] = this.LastName;
			this.StateTracker.OriginalValues["DateOfBirth"] = this.DateOfBirth;
			this.StateTracker.OriginalValues["Team"] = this.Team;
			this.StateTracker.OriginalValues["TeamsID"] = this.TeamsID;
			this.StateTracker.OriginalValues["TeamID"] = this.TeamID;
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
					case "DATEOFBIRTH":
					{
						this.DateOfBirth = source.GetDateTime(i);
						break;
					}
					case "TEAMSID":
					{
						this.TeamsID = source.GetInt64(i);
						break;
					}
					case "TEAMID":
					{
						this.TeamID = source.GetInt64(i);
						break;
					}
				}
			}

			this.__SetOriginalValues();

			this.StateTracker.IsLoading = false;
		}

		public IValueBag __GetBagFromValues()
		{
			var playerBag = new PlayerValueBag();
			playerBag.ID = this.ID;
			playerBag.FirstName = this.FirstName;
			playerBag.LastName = this.LastName;
			playerBag.DateOfBirth = this.DateOfBirth;
			playerBag.TeamsID = this.TeamsID;
			playerBag.TeamID = this.TeamID;
			return playerBag;
		}

		public void __SetValuesFromBag(IValueBag bag)
		{
			this.StateTracker.IsLoading = true;

			var playerBag = (PlayerValueBag)bag;
			this.ID = playerBag.ID;
			this.FirstName = playerBag.FirstName;
			this.LastName = playerBag.LastName;
			this.DateOfBirth = playerBag.DateOfBirth;
			this.TeamsID = playerBag.TeamsID;
			this.TeamID = playerBag.TeamID;

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

		public static bool operator ==(PlayerProxy a, PlayerProxy b)
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

		public static bool operator !=(PlayerProxy a, PlayerProxy b)
		{
			return !(a == b);
		}
	}
}
