using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
		private readonly Dictionary<string, object> _originalValues = new Dictionary<string, object>();
		private readonly List<string> _changedFields = new List<string>();
		private readonly List<string> _loadedCollections = new List<string>();
		private readonly Dictionary<string, List<object>> _savedCollectionIDs = new Dictionary<string, List<object>>();
		private readonly List<string> _loadedItems = new List<string>();
		private readonly List<ValidationError> _errors = new List<ValidationError>();
		private readonly Stack<DataChange> _undoStack = new Stack<DataChange>();
		private readonly Stack<DataChange> _redoStack = new Stack<DataChange>();

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
		/// Gets the original values of the item, which correspond to the values stored in the database.
		/// </summary>
		/// <value>
		/// The original values.
		/// </value>
		public Dictionary<string, object> OriginalValues
		{
			get
			{
				return _originalValues;
			}
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
		public List<ValidationError> ValidationErrors
		{
			get
			{
				return _errors;
			}
		}

		private Stack<DataChange> UndoStack
		{
			get
			{
				return _undoStack;
			}
		}

		private Stack<DataChange> RedoStack
		{
			get
			{
				return _redoStack;
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
				IList<T> collection = new ObservableCollection<T>();
				SetCollection(propertyName, (IList)collection);
				return collection;
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
				// TODO: Less reflection!  Less casting!  Less looping!
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

			if (typeof(INotifyCollectionChanged).IsAssignableFrom(collection.GetType()))
			{
				INotifyCollectionChanged notifyingCollection = (INotifyCollectionChanged)collection;
				notifyingCollection.CollectionChanged += (sender, e) =>
					{
						if (!this.IsLoading)
						{
							if (e.Action == NotifyCollectionChangedAction.Add)
							{
								this.UndoStack.Push(new DataChange(ChangeType.CollectionAdd, this.Item, propertyName, null, e.NewItems[0], this.Item.HasChanges));
							}
							else if (e.Action == NotifyCollectionChangedAction.Remove)
							{
								this.UndoStack.Push(new DataChange(ChangeType.CollectionRemove, this.Item, propertyName, e.OldItems[0], null, this.Item.HasChanges));
							}
						}
					};
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
				T oldValue = propertyValueField;
				propertyValueField = newValue;
				if (onChanged != null)
				{
					onChanged();
				}
				this.OnPropertyChanged(propertyName);
				if (!this.IsLoading)
				{
					if (!this.ChangedFields.Contains(propertyName))
					{
						this.ChangedFields.Add(propertyName);
					}
					this.UndoStack.Push(new DataChange(ChangeType.PropertyValue, this.Item, propertyName, oldValue, newValue, this.Item.HasChanges));
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
				if (!this.IsLoading)
				{
					if (!this.ChangedFields.Contains(propertyName))
					{
						this.ChangedFields.Add(propertyName);
					}
					this.UndoStack.Push(new DataChange(ChangeType.PropertyValue, this.Item, propertyName, oldValue, newValue, this.Item.HasChanges));
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
		///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
		public bool Validate()
		{
			this.ValidateAllFields = true;
			this.ValidationErrors.Clear();
			foreach (IDynamicProxy item in RecurseRelatedItems(this.Item))
			{
				ValidateItem(item);
			}
			return (this.ValidationErrors.Count == 0);
		}

		private void ValidateItem(IDynamicProxy item)
		{
			// First check data annotations on properties
			foreach (PropertyInfo prop in item.GetType().GetProperties())
			{
				// This will have the side effect of filling out the Errors collection:
				GetError(item, prop);
			}

			// Now check IValidatableObject methods (if that interface is implemented)
			if (typeof(IValidatableObject).IsAssignableFrom(item.GetType()))
			{
				foreach (var error in ((IValidatableObject)item).Validate(null))
				{
					object itemID = item.PrimaryKeyValue;
					string itemName = item.GetType().BaseType.Name;
					string propertyName = string.Join(", ", error.MemberNames);
					ValidationError newError = new ValidationError(itemID, itemName, propertyName, "Error", error.ErrorMessage);
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
				Array.ConvertAll<ValidationError, string>(this.ValidationErrors.ToArray(), e => e.ErrorMessage));
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
			ValidationError error = GetError(this.Item, prop);
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
			return GetError(this.Item, prop);
		}

		private ValidationError GetError(IDynamicProxy item, PropertyInfo prop)
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

			// Remove the errors for this item
			object itemID = item.PrimaryKeyValue;
			string itemName = item.GetType().BaseType.Name;
			string propertyName = prop.Name;
			this.ValidationErrors.RemoveAll(e =>
				e.ItemID == itemID &&
				e.ItemName == itemName &&
				e.PropertyName == propertyName);

			// NOTE: Interestingly, prop.GetCustomAttributes doesn't actually search the inheritance
			// chain whereas Attribute.GetCustomAttributes does
			foreach (object attribute in Attribute.GetCustomAttributes(prop, typeof(ValidationAttribute), true))
			{
				ValidationAttribute validation = (ValidationAttribute)attribute;
				object value = prop.GetValue(item, null);
				if (!validation.IsValid(value))
				{
					string errorName = attribute.GetType().Name.Replace("Attribute", "");
					string errorMessage = validation.FormatErrorMessage(PropertyName(prop));
					ValidationError newError = new ValidationError(itemID, itemName, prop.Name, errorName, errorMessage);

					this.ValidationErrors.Add(newError);

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
		/// Determines whether this item has changes that can be undone.
		/// </summary>
		/// <returns>
		///   <c>true</c> if this instance has changes that can be undone; otherwise, <c>false</c>.
		/// </returns>
		public bool CanUndo()
		{
			return RecurseRelatedItems(this.Item).Any(i => i.StateTracker.UndoStack.Count > 0);
		}

		/// <summary>
		/// Builds the queue of changes that can be undone, with the most recent first.
		/// </summary>
		/// <returns></returns>
		public IList<DataChange> BuildUndoQueue()
		{
			List<DataChange> queue = new List<DataChange>();
			foreach (var item in RecurseRelatedItems(this.Item))
			{
				queue.AddRange(item.StateTracker.UndoStack.ToList());
			}
			queue.Sort();
			return queue;
		}

		/// <summary>
		/// Undoes the last change.
		/// </summary>
		public void Undo()
		{
			// We can't know which is the most recent undo operation until we've built and sorted the
			// whole lot
			IList<DataChange> allChanges = BuildUndoQueue();
			if (allChanges.Count > 0)
			{
				DataChange change = allChanges[0];
				change.Proxy.StateTracker.UndoStack.Pop();
				change.Proxy.StateTracker.RedoStack.Push(change);
				change.Undo();
			}
		}

		/// <summary>
		/// Determines whether this item has changes that can be redone.
		/// </summary>
		/// <returns>
		///   <c>true</c> if this instance has changes that can be redone; otherwise, <c>false</c>.
		/// </returns>
		public bool CanRedo()
		{
			return RecurseRelatedItems(this.Item).Any(i => i.StateTracker.RedoStack.Count > 0);
		}

		/// <summary>
		/// Builds the queue of changes that can be redone, with the most recent first.
		/// </summary>
		/// <returns></returns>
		public IList<DataChange> BuildRedoQueue()
		{
			List<DataChange> queue = new List<DataChange>();
			foreach (var item in RecurseRelatedItems(this.Item))
			{
				queue.AddRange(item.StateTracker.RedoStack.ToList());
			}
			queue.Sort();
			return queue;
		}

		/// <summary>
		/// Redoes the last undone change.
		/// </summary>
		public void Redo()
		{
			// We can't know which is the most recent undo operation until we've built and sorted the
			// whole lot
			IList<DataChange> allChanges = BuildRedoQueue();
			if (allChanges.Count > 0)
			{
				DataChange change = allChanges[0];
				change.Proxy.StateTracker.RedoStack.Pop();
				change.Proxy.StateTracker.UndoStack.Push(change);
				change.Redo();
			}
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

		private IEnumerable<IDynamicProxy> RecurseRelatedItems(IDynamicProxy item)
		{
			yield return item;

			// Recurse through loaded collections
			foreach (string collectionPropertyName in item.StateTracker.LoadedCollections)
			{
				PropertyInfo property = item.GetType().GetProperty(collectionPropertyName,
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
				foreach (var child in (IList)property.GetValue(item, null))
				{
					yield return (IDynamicProxy)child;
				}
			}

			// Recurse through loaded items
			foreach (string itemPropertyName in item.StateTracker.LoadedItems)
			{
				PropertyInfo property = item.GetType().GetProperty(itemPropertyName,
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
				IDynamicProxy relatedItem = (IDynamicProxy)property.GetValue(item, null);
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
