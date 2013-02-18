using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IQToolkit.Data.Common;
using IQToolkit.Data.Mapping;
using System.Linq.Expressions;

namespace Watsonia.Data.Linq
{
	/// <summary>
	/// Maps entities to their objects in the database.
	/// </summary>
	internal class EntityMapping : ImplicitMapping
	{
		private Database _database;

		/// <summary>
		/// Initializes a new instance of the <see cref="EntityMapping" /> class.
		/// </summary>
		/// <param name="database">The database.</param>
		public EntityMapping(Database database)
		{
			_database = database;
		}

		/// <summary>
		/// Gets a sequence of all the mapped members for the supplied entity.
		/// </summary>
		/// <param name="entity">The entity to get the mapped members for.</param>
		/// <returns></returns>
		public override IEnumerable<MemberInfo> GetMappedMembers(MappingEntity entity)
		{
			return _database.Configuration.PropertiesToMap(entity.EntityType);
		}

		/// <summary>
		/// Gets the key members on this side of an association.
		/// </summary>
		/// <param name="entity">The entity on this side of the association.</param>
		/// <param name="member">The member that represents the association.</param>
		/// <returns></returns>
		public override IEnumerable<MemberInfo> GetAssociationKeyMembers(MappingEntity entity, MemberInfo member)
		{
			// On this side it's the (probably dynamically created) foreign key member e.g. for Order.Customer
			// it might be Order.CustomerID

			// Convert the type to a dynamic proxy type if it's not already
			Type proxyType;
			if (typeof(IDynamicProxy).IsAssignableFrom(entity.ElementType))
			{
				proxyType = entity.ElementType;
			}
			else
			{
				proxyType = DynamicProxyFactory.GetDynamicProxyType(entity.ElementType, _database);
			}

			// Get the property
			// TODO: Should probably implement IsAssociation and make sure it's a property
			string propertyName = _database.Configuration.GetForeignKeyColumnName((PropertyInfo)member);
			MemberInfo property = proxyType.GetProperty(propertyName);
			if (property == null)
			{
				System.Diagnostics.Debugger.Break();
			}

			yield return property;
		}

		/// <summary>
		/// Gets the key members on the other side of an association.
		/// </summary>
		/// <param name="entity">The entity on this side of the association.</param>
		/// <param name="member">The member that represents the association.</param>
		/// <returns></returns>
		public override IEnumerable<MemberInfo> GetAssociationRelatedKeyMembers(MappingEntity entity, MemberInfo member)
		{
			// On the other side it's the ID e.g. for Order.Customer it would be Customer.ID
			MemberInfo property = member.ReflectedType.GetProperty("ID");
			yield return property;
		}

		/// <summary>
		/// Gets the name of the corresponding database table for the supplied entity.
		/// </summary>
		/// <param name="entity">The entity that corresponds to the table.</param>
		/// <returns></returns>
		public override string GetTableName(MappingEntity entity)
		{
			if (typeof(IDynamicProxy).IsAssignableFrom(entity.EntityType))
			{
				return _database.Configuration.GetTableName(entity.EntityType.BaseType);
			}
			else
			{
				return _database.Configuration.GetTableName(entity.EntityType);
			}
		}

		/// <summary>
		/// Gets the name of the corresponding table column for the supplied entity and member.
		/// </summary>
		/// <param name="entity">The entity that corresponds to the column's table.</param>
		/// <param name="member">The member that corresponds to the column.</param>
		/// <returns></returns>
		public override string GetColumnName(MappingEntity entity, MemberInfo member)
		{
			// TODO: Should probably implement ShouldMap and make sure it's a property
			return _database.Configuration.GetColumnName((PropertyInfo)member);
		}

		/// <summary>
		/// Creates the mapper.
		/// </summary>
		/// <param name="translator">The translator.</param>
		/// <returns></returns>
		public override QueryMapper CreateMapper(QueryTranslator translator)
		{
			return new EntityMapper(this, translator);
		}

		/// <summary>
		/// Gets the entity.
		/// </summary>
		/// <param name="elementType">Type of the element.</param>
		/// <param name="tableId">The table id.</param>
		/// <returns></returns>
		public override MappingEntity GetEntity(Type elementType, string tableId)
		{
			if (tableId == null)
			{
				tableId = this.GetTableId(elementType);
			}
			return new EntityMappingEntity(elementType, tableId);
		}
	}
}
