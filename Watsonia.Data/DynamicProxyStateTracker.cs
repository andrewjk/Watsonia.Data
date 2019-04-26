using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Watsonia.QueryBuilder;

namespace Watsonia.Data
{
	/// <summary>
	/// Tracks the state of a dynamic proxy as its properties are changed or accessed.
	/// </summary>
	public class DynamicProxyStateTracker
	{
		private int? _oldHashCode;

		// TODO: Investigate creating a typed class to hold original values rather than having to use ChangeType

		/// <summary>
		/// Gets or sets the database.
		/// </summary>
		/// <value>
		/// The database.
		/// </value>
		internal Database Database { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is currently being loaded from the database.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is loading; otherwise, <c>false</c>.
		/// </value>
		public bool IsLoading { get; set; }

		/// <summary>
		/// Gets or sets the item that this DynamicProxyStateTracker is tracking.
		/// </summary>
		/// <value>
		/// The item.
		/// </value>
		public IDynamicProxy Item { get; set; }

		/// <summary>
		/// Gets the original values of the item, which correspond to the values stored in the database.
		/// </summary>
		/// <value>
		/// The original values.
		/// </value>
		public Dictionary<string, object> OriginalValues { get; } = new Dictionary<string, object>();

		/// <summary>
		/// Gets the changed fields, which are used when saving this item to the database.
		/// </summary>
		/// <value>
		/// The changed fields.
		/// </value>
		public List<string> ChangedFields { get; } = new List<string>();

		/// <summary>
		/// Gets the fields with values that have been set.
		/// </summary>
		/// <value>
		/// The set fields.
		/// </value>
		public List<string> SetFields { get; } = new List<string>();

		/// <summary>
		/// Gets the loaded collections, which are saved with this item.
		/// </summary>
		/// <value>
		/// The loaded collections.
		/// </value>
		public List<string> LoadedCollections { get; } = new List<string>();

		/// <summary>
		/// Gets the saved collection IDs, which are used to determine whether items in those collections
		/// should be inserted into or deleted from the database.
		/// </summary>
		/// <value>
		/// The saved collection IDs.
		/// </value>
		internal Dictionary<string, List<object>> SavedCollectionIDs { get; } = new Dictionary<string, List<object>>();

		/// <summary>
		/// Gets the loaded items, which may be saved with this item.
		/// </summary>
		/// <value>
		/// The loaded items.
		/// </value>
		public List<string> LoadedItems { get; } = new List<string>();
		
		/// <summary>
		/// Gets a value indicating whether this instance is valid.
		/// </summary>
		/// <value>
		///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
		/// </value>
		public bool IsValid
		{
			get
			{
				return this.Validate();
			}
		}

		/// <summary>
		/// Gets the validation errors that apply to this instance.
		/// </summary>
		/// <value>
		/// The validation errors.
		/// </value>
		public List<ValidationError> ValidationErrors { get; } = new List<ValidationError>();

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
				var collection = new List<T>();
				SetCollection(propertyName, (IList)collection);
				return collection;
			}
			else
			{
				var tableType = typeof(T);
				var tableName = this.Database.Configuration.GetTableName(tableType);
				var foreignKeyColumnName = this.Database.Configuration.GetForeignKeyColumnName(tableType, this.Item.GetType().BaseType);
				var select = Select.From(tableName).Where(foreignKeyColumnName, SqlOperator.Equals, this.Item.PrimaryKeyValue);
				var collection = this.Database.LoadCollection<T>(select);
				SetCollection(propertyName, (IList)collection);
				return collection;
			}
		}

		/// <summary>
		/// Sets the related collection of child items.
		/// </summary>
		/// <param name="propertyName">The name of the property containing the collection.</param>
		/// <param name="collection">The collection.</param>
		public void SetCollection(string propertyName, IList collection)
		{
			AddLoadedCollection(propertyName);

			// For each item in the collection, set the property that refers to this item
			// E.g. if the user is setting the Books collection for an Author, set the value of the
			// Author property for each Book
			if (collection.Count > 0)
			{
				// TODO: Less reflection!  Less casting!  Less looping!
				// TODO: Need to only be setting the one property that points to this item when there
				// are two properties that point to different items e.g. in LearningUserNotes
				// Should come from ChildParentMapping?
				foreach (var property in collection[0].GetType().GetProperties())
				{
					if (property.PropertyType.IsAssignableFrom(this.Item.GetType()) &&
						property.PropertyType != typeof(object))
					{
						foreach (var item in collection)
						{
							var proxyItem = (IDynamicProxy)item;
							proxyItem.StateTracker.IsLoading = true;
							property.SetValue(item, this.Item, null);
							proxyItem.StateTracker.IsLoading = false;
						}
					}
				}
			}

			this.SavedCollectionIDs.Add(propertyName, new List<object>());
			for (var i = 0; i < collection.Count; i++)
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

		// TODO: That shouldn't be public
		public void CheckOriginalValue<T>(string propertyName, T newValue)
		{
			if (this.IsLoading)
			{
				return;
			}

			// Check whether the original values contains the key
			// If it doesn't it must be a related item (e.g. Book), which will be covered when
			// the ID value is set (e.g. BookID)
			// OR this property is being set from within the constructor (where the original
			// values haven't been created yet) or from SetValuesFromReader (where the original
			// values have been cleared out in preparation for new ones)
			if (this.OriginalValues.ContainsKey(propertyName))
			{
				// If the property is being set to its original value, clear it out of the changed fields
				// Otherwise, add it to the changed fields
				var originalValue = (T)TypeHelper.ChangeType(this.OriginalValues[propertyName], typeof(T));
				if (EqualityComparer<T>.Default.Equals(originalValue, newValue))
				{
					if (this.ChangedFields.Contains(propertyName))
					{
						this.ChangedFields.Remove(propertyName);
					}
				}
				else
				{
					if (!this.ChangedFields.Contains(propertyName))
					{
						this.ChangedFields.Add(propertyName);
					}
				}
				this.Item.HasChanges = (this.ChangedFields.Count > 0);
			}

			// It's been changed now, so it should be validated
			if (!this.SetFields.Contains(propertyName))
			{
				this.SetFields.Add(propertyName);
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

		/// <summary>
		/// Checks whether this item is in a valid state.
		/// </summary>
		///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
		public bool Validate()
		{
			this.ValidationErrors.Clear();
			foreach (var item in this.RecurseRelatedItems(this.Item))
			{
				ValidateItem(item);
			}
			return (this.ValidationErrors.Count == 0);
		}

		/// <summary>
		/// Checks whether the supplied item is in a valid state.
		/// </summary>
		/// <param name="item">The item.</param>
		private void ValidateItem(IDynamicProxy item)
		{
			// First check data annotations on properties
			foreach (var property in item.GetType().GetProperties())
			{
				// This will have the side effect of filling out the Errors collection:
				GetError(item, property);
			}

			// Now check IValidatableObject methods (if that interface is implemented)
			if (typeof(IValidatableObject).IsAssignableFrom(item.GetType()))
			{
				foreach (var error in ((IValidatableObject)item).Validate(null))
				{
					var itemID = item.PrimaryKeyValue;
					var itemName = item.GetType().BaseType.Name;
					var propertyName = string.Join(", ", error.MemberNames);
					var newError = new ValidationError(itemID, itemName, propertyName, "Error", error.ErrorMessage);
					this.ValidationErrors.Add(newError);
				}
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
				Array.ConvertAll(this.ValidationErrors.ToArray(), e => e.ErrorMessage));
		}

		/// <summary>
		/// Gets the error text for the property with the supplied name.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns>A string containing the error text or the empty string if there are no errors.</returns>
		public string GetErrorText(string propertyName)
		{
			var error = GetError(propertyName);
			return (error != null) ? error.ErrorMessage : "";
		}

		/// <summary>
		/// Gets the error for the property with the supplied name.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns>An error or null if there are no errors.</returns>
		public ValidationError GetError(string propertyName)
		{
			var property = this.Item.GetType().GetProperty(propertyName);
			return GetError(this.Item, property);
		}

		/// <summary>
		/// Gets the error for the property on the supplied item.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="property">The property.</param>
		/// <returns>An error or null if there are no errors.</returns>
		private ValidationError GetError(IDynamicProxy item, PropertyInfo property)
		{
			// Don't check errors if we're in the middle of loading the item from the database
			if (this.IsLoading)
			{
				return null;
			}

			// Remove the errors for this item
			var itemID = item.PrimaryKeyValue;
			var itemName = item.GetType().BaseType.Name;
			var propertyName = property.Name;
			this.ValidationErrors.RemoveAll(e =>
				e.ItemID == itemID &&
				e.ItemName == itemName &&
				e.PropertyName == propertyName);

			// NOTE: Interestingly, prop.GetCustomAttributes doesn't actually search the inheritance
			// chain whereas Attribute.GetCustomAttributes does
			foreach (object attribute in Attribute.GetCustomAttributes(property, typeof(ValidationAttribute), true))
			{
				var validation = (ValidationAttribute)attribute;
				var value = property.GetValue(item, null);
				if (!validation.IsValid(value))
				{
					var errorName = attribute.GetType().Name.Replace("Attribute", "");
					var errorMessage = validation.FormatErrorMessage(PropertyName(property));
					var newError = new ValidationError(itemID, itemName, property.Name, errorName, errorMessage);

					this.ValidationErrors.Add(newError);

					// Just return the first error rather than bombarding the user with a bunch of them
					return newError;
				}
			}

			return null;
		}

		/// <summary>
		/// Gets the name of the property for displaying to the user, from a DisplayAttribute, DisplayNameAttribute
		/// or its name, in that order.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns>The name of the property for displaying to the user.</returns>
		private string PropertyName(PropertyInfo property)
		{
			// Check for a DisplayAttribute or a DisplayNameAttribute on the property
			// If neither are found, just return the property's name
			object[] attributes = Attribute.GetCustomAttributes(property, true);
			foreach (var att in attributes)
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
			return property.Name;
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
			if (!(obj is IDynamicProxy other))
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

		/// <summary>
		/// Iterates through all related collections and items in this item.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <returns>A yielded enumerator for all related collections and items.</returns>
		private IEnumerable<IDynamicProxy> RecurseRelatedItems(IDynamicProxy item)
		{
			yield return item;

			// Recurse through loaded collections
			foreach (var collectionPropertyName in item.StateTracker.LoadedCollections)
			{
				var property = item.GetType().GetProperty(collectionPropertyName,
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
				foreach (var child in (IList)property.GetValue(item, null))
				{
					yield return (IDynamicProxy)child;
				}
			}

			// Recurse through loaded items
			foreach (var itemPropertyName in item.StateTracker.LoadedItems)
			{
				var property = item.GetType().GetProperty(itemPropertyName,
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
				var relatedItem = (IDynamicProxy)property.GetValue(item, null);
				if (relatedItem != null)
				{
					if (this.Database.Configuration.ShouldCascadeInternal(property))
					{
						yield return (IDynamicProxy)property.GetValue(item, null);
					}
				}
			}
		}
	}
}
