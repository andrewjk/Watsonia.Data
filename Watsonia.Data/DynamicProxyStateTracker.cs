using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Watsonia.Data
{
	/// <summary>
	/// Tracks the state of a dynamic proxy as its properties are changed or accessed.
	/// </summary>
	public class DynamicProxyStateTracker
	{
		private int? _oldHashCode;
		private readonly List<string> _changedFields = new List<string>();
		private readonly List<string> _loadedCollections = new List<string>();
		private readonly Dictionary<string, List<object>> _savedCollectionIDs = new Dictionary<string, List<object>>();
		private readonly List<string> _loadedItems = new List<string>();
		private readonly List<ValidationError> _errors = new List<ValidationError>();

		/// <summary>
		/// Gets or sets the database.
		/// </summary>
		/// <value>
		/// The database.
		/// </value>
		internal Database Database
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this item needs to be loaded from the database.
		/// </summary>
		/// <value>
		///   <c>true</c> if this item needs to be loaded; otherwise, <c>false</c>.
		/// </value>
		public bool NeedsLoad
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is currently being loaded from the database.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is loading; otherwise, <c>false</c>.
		/// </value>
		public bool IsLoading
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the item.
		/// </summary>
		/// <value>
		/// The item.
		/// </value>
		public IDynamicProxy Item
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the changed fields, which are used when saving this item to the database.
		/// </summary>
		/// <value>
		/// The changed fields.
		/// </value>
		public List<string> ChangedFields
		{
			get
			{
				return _changedFields;
			}
		}

		/// <summary>
		/// Gets the loaded collections, which are saved with this item.
		/// </summary>
		/// <value>
		/// The loaded collections.
		/// </value>
		public List<string> LoadedCollections
		{
			get
			{
				return _loadedCollections;
			}
		}

		/// <summary>
		/// Gets the saved collection IDs, which are used to determine whether items in those collections should be inserted
		/// into or deleted from the database.
		/// </summary>
		/// <value>
		/// The saved collection IDs.
		/// </value>
		internal Dictionary<string, List<object>> SavedCollectionIDs
		{
			get
			{
				return _savedCollectionIDs;
			}
		}

		/// <summary>
		/// Gets the loaded items, which may be saved with this item.
		/// </summary>
		/// <value>
		/// The loaded items.
		/// </value>
		public List<string> LoadedItems
		{
			get
			{
				return _loadedItems;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to validate all fields.
		/// </summary>
		/// <value>
		///   <c>true</c> if fields should be validated; otherwise, <c>false</c>.
		/// </value>
		private bool ValidateAllFields
		{
			get;
			set;
		}

		/// <summary>
		/// Gets any validation errors.
		/// </summary>
		/// <value>
		/// The errors.
		/// </value>
		public List<ValidationError> Errors
		{
			get
			{
				return _errors;
			}
		}

		/// <summary>
		/// Gets or sets an action to call when a property's value is changing.
		/// </summary>
		/// <value>
		/// The action.
		/// </value>
		public Action<string> OnPropertyChanging
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets an action to call when a property's value has changed.
		/// </summary>
		/// <value>
		/// The action.
		/// </value>
		public Action<string> OnPropertyChanged
		{
			get;
			set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DynamicProxyStateTracker"/> class.
		/// </summary>
		public DynamicProxyStateTracker()
		{
		}

		/// <summary>
		/// Loads the related collection of child items.
		/// </summary>
		/// <typeparam name="T">The type of items in the collection.</typeparam>
		/// <param name="propertyName">The name of the property containing the collection.</param>
		/// <returns>The loaded collection.</returns>
		public IList<T> LoadCollection<T>(string propertyName)
		{
			if (this.Item.IsNew)
			{
				SetCollection(propertyName, new List<object>());
				return new ObservableCollection<T>();
			}
			else
			{
				Type tableType = typeof(T);
				string tableName = this.Database.Configuration.GetTableName(tableType);
				string foreignKeyColumnName = this.Database.Configuration.GetForeignKeyColumnName(tableType, this.Item.GetType().BaseType);
				var select = Select.From(tableName).Where(foreignKeyColumnName, SqlOperator.Equals, this.Item.PrimaryKeyValue);
				IList<T> collection = this.Database.LoadCollection<T>(select);
				SetCollection(propertyName, (IList)collection);
				return collection;
			}
		}

		/// <summary>
		/// Loads the related collection of child items.
		/// </summary>
		/// <typeparam name="T">The type of items in the collection.</typeparam>
		/// <param name="propertyName">The name of the property containing the collection.</param>
		/// <returns>The loaded collection.</returns>
		public void SetCollection(string propertyName, IList collection)
		{
			AddLoadedCollection(propertyName);

			if (collection.Count > 0)
			{
				// TODO: Less reflection!  Less casting!
				foreach (PropertyInfo property in collection[0].GetType().GetProperties())
				{
					if (property.PropertyType.IsAssignableFrom(this.Item.GetType()) &&
						property.PropertyType != typeof(object))
					{
						foreach (object item in collection)
						{
							((IDynamicProxy)item).StateTracker.IsLoading = true;
							property.SetValue(item, this.Item, null);
							((IDynamicProxy)item).StateTracker.IsLoading = false;
						}
					}
				}
			}

			this.SavedCollectionIDs.Add(propertyName, new List<object>());
			for (int i = 0; i < collection.Count; i++)
			{
				this.SavedCollectionIDs[propertyName].Add(((IDynamicProxy)collection[i]).PrimaryKeyValue);
			}
		}

		// TODO: Pass in T1, T2 for the type of the id?
		/// <summary>
		/// Loads the related item.
		/// </summary>
		/// <typeparam name="T">The type of item.</typeparam>
		/// <param name="id">The id of the item.</param>
		/// <param name="propertyName">The name of the property containing the item.</param>
		/// <returns>The loaded item.</returns>
		public T LoadItem<T>(long id, string propertyName)
		{
			AddLoadedItem(propertyName);
			if (id == Convert.ToInt64(this.Database.Configuration.GetPrimaryKeyNewItemValue(typeof(T))))
			{
				return this.Database.Create<T>();
			}
			else
			{
				return this.Database.Load<T>(id);
			}
		}

		/// <summary>
		/// Loads the related item.
		/// </summary>
		/// <typeparam name="T">The type of item.</typeparam>
		/// <param name="id">The id of the item.</param>
		/// <param name="propertyName">The name of the property containing the item.</param>
		/// <returns>The loaded item.</returns>
		public T LoadItem<T>(string id, string propertyName)
		{
			AddLoadedItem(propertyName);
			if (id != null && id.Equals(this.Database.Configuration.GetPrimaryKeyNewItemValue(typeof(T))))
			{
				return this.Database.Create<T>();
			}
			else
			{
				return this.Database.Load<T>(id);
			}
		}

		/// <summary>
		/// Sets the value of a property by ref.
		/// </summary>
		/// <typeparam name="T">The property type.</typeparam>
		/// <param name="propertyValueField">The field containing the property value.</param>
		/// <param name="newValue">The new value.</param>
		/// <param name="propertyName">The name of the property.</param>
		/// <param name="onChanging">An action to call when the value is changing or null if no action should be called.</param>
		/// <param name="onChanged">An action to call when the value has changed or null if no action should be called.</param>
		public void SetPropertyValueByRef<T>(ref T propertyValueField, T newValue, string propertyName, Action<T> onChanging, Action onChanged)
		{
			// TODO: Something like this would be much nicer but doesn't quite work:
			//SetPropertyValue(propertyValue, newValue, propertyName, delegate(T value) { propertyValue = newValue; }, onChanging, onChanged);
			if (!EqualityComparer<T>.Default.Equals(propertyValueField, newValue))
			{
				if (onChanging != null)
				{
					onChanging(newValue);
				}
				this.OnPropertyChanging(propertyName);
				propertyValueField = newValue;
				if (onChanged != null)
				{
					onChanged();
				}
				this.OnPropertyChanged(propertyName);
				if (!this.IsLoading && !this.ChangedFields.Contains(propertyName))
				{
					this.ChangedFields.Add(propertyName);
				}
				this.Item.HasChanges = true;
			}
		}

		/// <summary>
		/// Sets the value of a property by calling a function.
		/// </summary>
		/// <typeparam name="T">The property type.</typeparam>
		/// <param name="oldValue">The old value.</param>
		/// <param name="newValue">The new value.</param>
		/// <param name="propertyName">The name of the property.</param>
		/// <param name="setValue">The action to call to set the property's value.</param>
		/// <param name="onChanging">An action to call when the value is changing or null if no action should be called.</param>
		/// <param name="onChanged">An action to call when the value has changed or null if no action should be called.</param>
		public void SetPropertyValueWithFunction<T>(T oldValue, T newValue, string propertyName, Action<T> setValue, Action<T> onChanging, Action onChanged)
		{
			// This is the method that gets called when an item is set.  If the item is set to null
			// but hasn't been loaded, we still need to do all of the changing stuff to ensure that
			// it is cleared.  This means we will have extra notification events but can't be avoided
			if (!EqualityComparer<T>.Default.Equals(oldValue, newValue) || newValue == null)
			{
				if (onChanging != null)
				{
					onChanging(newValue);
				}
				this.OnPropertyChanging(propertyName);
				setValue(newValue);
				if (onChanged != null)
				{
					onChanged();
				}
				this.OnPropertyChanged(propertyName);
				if (!this.IsLoading && !this.ChangedFields.Contains(propertyName))
				{
					this.ChangedFields.Add(propertyName);
				}
				this.Item.HasChanges = true;
			}
		}

		/// <summary>
		/// Adds a loaded collection to the list.
		/// </summary>
		/// <param name="propertyName">The name of the property containing the collection.</param>
		public void AddLoadedCollection(string propertyName)
		{
			if (!this.LoadedCollections.Contains(propertyName))
			{
				this.LoadedCollections.Add(propertyName);
			}
		}

		/// <summary>
		/// Adds a loaded item to the list.
		/// </summary>
		/// <param name="propertyName">The name of the property containing the item.</param>
		public void AddLoadedItem(string propertyName)
		{
			if (!this.LoadedItems.Contains(propertyName))
			{
				this.LoadedItems.Add(propertyName);
			}
		}

		public void BeginEdit()
		{
			throw new NotImplementedException();
		}

		public void CancelEdit()
		{
			throw new NotImplementedException();
		}

		public void EndEdit()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Checks whether this item is in a valid state.
		/// </summary>
		public void Validate()
		{
			this.ValidateAllFields = true;
			this.Errors.Clear();
			foreach (PropertyInfo prop in this.Item.GetType().GetProperties())
			{
				// This will have the side effect of filling out the Errors collection:
				GetError(prop);
			}
		}

		/// <summary>
		/// Gets the error text for all properties.
		/// </summary>
		/// <returns>A string containing the error text or the empty string if there are no errors.</returns>
		public string GetErrorText()
		{
			this.Validate();
			return string.Join(Environment.NewLine,
				Array.ConvertAll<ValidationError, string>(this.Errors.ToArray(), e => e.ErrorMessage));
		}

		/// <summary>
		/// Gets the error text for the property with the supplied name.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns>A string containing the error text or the empty string if there are no errors.</returns>
		public string GetErrorText(string propertyName)
		{
			ValidationError error = GetError(propertyName);
			return (error != null) ? error.ErrorMessage : "";
		}

		private string GetErrorText(PropertyInfo prop)
		{
			ValidationError error = GetError(prop);
			return (error != null) ? error.ErrorMessage : "";
		}

		/// <summary>
		/// Gets the error for the property with the supplied name.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns>An error or null if there are no errors.</returns>
		public ValidationError GetError(string propertyName)
		{
			PropertyInfo prop = this.Item.GetType().GetProperty(propertyName);
			return GetError(prop);
		}

		private ValidationError GetError(PropertyInfo prop)
		{
			// Don't check errors if we're in the middle of loading the item from the database
			if (this.IsLoading)
			{
				return null;
			}

			// Don't check errors if the user hasn't yet called Validate() and the property hasn't changed
			if (!this.ValidateAllFields && !this.ChangedFields.Contains(prop.Name))
			{
				return null;
			}

			this.Errors.RemoveAll(e => e.PropertyName == prop.Name);

			// NOTE: Interestingly, prop.GetCustomAttributes doesn't actually search the inheritance
			// chain whereas Attribute.GetCustomAttributes does
			foreach (object attribute in Attribute.GetCustomAttributes(prop, typeof(ValidationAttribute), true))
			{
				ValidationAttribute validation = (ValidationAttribute)attribute;
				object value = prop.GetValue(this.Item, null);
				if (!validation.IsValid(value))
				{
					string errorName = attribute.GetType().Name.Replace("Attribute", "");
					string errorMessage = validation.FormatErrorMessage(PropertyName(prop));
					ValidationError newError = new ValidationError(prop.Name, errorName, errorMessage);

					this.Errors.Add(newError);

					// Just return the first error rather than bombarding the user with a bunch of them
					return newError;
				}
			}

			return null;
		}

		private string PropertyName(PropertyInfo prop)
		{
			// Check for a DisplayAttribute or a DisplayNameAttribute on the property
			// If neither are found, just return the property's name
			object[] attributes = Attribute.GetCustomAttributes(prop, true);
			foreach (object att in attributes)
			{
				if (att is DisplayAttribute)
				{
					return ((DisplayAttribute)att).Name;
				}
				else if (att is DisplayNameAttribute)
				{
					return ((DisplayNameAttribute)att).DisplayName;
				}
			}
			return prop.Name;
		}

		/// <summary>
		/// Gets the item hash code.
		/// </summary>
		/// <returns></returns>
		public int GetItemHashCode()
		{
			// Once we have a hash code we'll never change it
			if (_oldHashCode.HasValue)
			{
				return _oldHashCode.Value;
			}

			// When this instance is new we use the base GetHashCode() and remember it so that an instance can
			// never change its hash code
			if (this.Item.IsNew)
			{
				_oldHashCode = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this.Item);
				return _oldHashCode.Value;
			}
			else
			{
				return this.Item.PrimaryKeyValue.GetHashCode();
			}
		}

		/// <summary>
		/// Compares this item with another item for equality.
		/// </summary>
		/// <param name="obj">The other item.</param>
		/// <returns>True if this item is equal to the other; otherwise, false.</returns>
		public bool ItemEquals(object obj)
		{
			var other = obj as IDynamicProxy;
			if (other == null)
			{
				return false;
			}
			else if (this.Item.IsNew && other.IsNew)
			{
				return ReferenceEquals(this.Item, other);
			}
			else if (ReferenceEquals(this.Item, other))
			{
				return true;
			}
			else if (this.Item.GetType() == other.StateTracker.Item.GetType())
			{
				if (this.Item == null)
				{
					return (other.StateTracker.Item == null);
				}
				else
				{
					return (this.Item.PrimaryKeyValue.Equals(other.PrimaryKeyValue));
				}
			}
			else
			{
				return false;
			}
		}
	}
}
