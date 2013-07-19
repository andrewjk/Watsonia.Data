using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data;

namespace Watsonia.Data.Query
{
	/// <summary>
	/// Defines mapping information and rules for the query provider.
	/// </summary>
	internal class QueryMapping
	{
		/// <summary>
		/// Gets or sets the database to use for configuration and creating dynamic proxies.
		/// </summary>
		/// <value>
		/// The database.
		/// </value>
		private Database Database
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the configuration options used for mapping to and accessing the database.</param>
		/// </summary>
		/// <value>
		/// The configuration.
		/// </value>
		private DatabaseConfiguration Configuration
		{
			get;
			set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="EntityMapping" /> class.
		/// </summary>
		/// <param name="database">The database.</param>
		public QueryMapping(Database database)
		{
			// TODO: Shouldn't be passing the database around
			this.Database = database;
			this.Configuration = database.Configuration;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="EntityMapping" /> class.
		/// </summary>
		/// <param name="database">The database.</param>
		public QueryMapping(DatabaseConfiguration configuration)
		{
			this.Configuration = configuration;
		}

		/// <summary>
		/// Determines the entity Id based on the type of the entity alone
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public string GetTableId(Type type)
		{
			return SplitWords(type.Name);
		}

		/// <summary>
		/// Get the meta entity directly corresponding to the CLR type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public MappingEntity GetEntity(Type type)
		{
			return this.GetEntity(type, this.GetTableId(type));
		}

		/// <summary>
		/// Get the meta entity that maps between the CLR type 'entityType' and the database table, yet
		/// is represented publicly as 'elementType'.
		/// </summary>
		/// <param name="elementType"></param>
		/// <param name="entityID"></param>
		/// <returns></returns>
		public MappingEntity GetEntity(Type elementType, string tableId)
		{
			if (tableId == null)
				tableId = this.GetTableId(elementType);
			return new MappingEntity(elementType, tableId);
		}

		/// <summary>
		/// Get the meta entity represented by the IQueryable context member
		/// </summary>
		/// <param name="contextMember"></param>
		/// <returns></returns>
		public MappingEntity GetEntity(MemberInfo contextMember)
		{
			Type elementType = TypeHelper.GetElementType(TypeHelper.GetMemberType(contextMember));
			return this.GetEntity(elementType);
		}

		/// <summary>
		/// Gets a sequence of all the mapped members for the supplied entity.
		/// </summary>
		/// <param name="entity">The entity to get the mapped members for.</param>
		/// <returns></returns>
		public IEnumerable<MemberInfo> GetMappedMembers(MappingEntity entity)
		{
			return this.Configuration.PropertiesToMap(entity.EntityType);
		}

		public bool IsPrimaryKey(MappingEntity entity, MemberInfo member)
		{
			// Customers has CustomerID, Orders has OrderID, etc
			if (this.IsColumn(entity, member))
			{
				// TODO: ? Type declaringType = typeof(IDynamicProxy).IsAssignableFrom(member.DeclaringType) ? member.DeclaringType.BaseType : member.DeclaringType;
				string name = NameWithoutTrailingDigits(member.Name);
				return member.Name.EndsWith("ID") && member.DeclaringType.Name.StartsWith(member.Name.Substring(0, member.Name.Length - 2));
			}
			return false;
		}

		private string NameWithoutTrailingDigits(string name)
		{
			int n = name.Length - 1;
			while (n >= 0 && char.IsDigit(name[n]))
			{
				n--;
			}
			if (n < name.Length - 1)
			{
				return name.Substring(0, n);
			}
			return name;
		}

		public IEnumerable<MemberInfo> GetPrimaryKeyMembers(MappingEntity entity)
		{
			return this.GetMappedMembers(entity).Where(m => this.IsPrimaryKey(entity, m));
		}

		/// <summary>
		/// Determines if a property is mapped as a relationship
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		public bool IsRelationship(MappingEntity entity, MemberInfo member)
		{
			return this.IsAssociationRelationship(entity, member);
		}

		/// <summary>
		/// Deterimines is a property is mapped onto a column or relationship
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public bool IsMapped(MappingEntity entity, MemberInfo member)
		{
			return true;
		}

		/// <summary>
		/// Determines if a property is mapped onto a column
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public bool IsColumn(MappingEntity entity, MemberInfo member)
		{
			return IsScalar(TypeHelper.GetMemberType(member));
		}

		private bool IsScalar(Type type)
		{
			type = TypeHelper.GetNonNullableType(type);
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Empty:
				case TypeCode.DBNull:
				{
					return false;
				}
				case TypeCode.Object:
				{
					return
						type == typeof(DateTimeOffset) ||
						type == typeof(TimeSpan) ||
						type == typeof(Guid) ||
						type == typeof(byte[]);
				}
				default:
				{
					return true;
				}
			}
		}

		/// <summary>
		/// The type declaration for the column in the provider's syntax
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="member"></param>
		/// <returns>a string representing the type declaration or null</returns>
		public string GetColumnDbType(MappingEntity entity, MemberInfo member)
		{
			return null;
		}

		/// <summary>
		/// Determines if a relationship property refers to a single entity (as opposed to a collection.)
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public bool IsSingletonRelationship(MappingEntity entity, MemberInfo member)
		{
			if (!this.IsRelationship(entity, member))
			{
				return false;
			}

			Type ieType = TypeHelper.FindIEnumerable(TypeHelper.GetMemberType(member));
			return ieType == null;
		}

		/// <summary>
		/// Determines whether a given expression can be executed locally. 
		/// (It contains no parts that should be translated to the target environment.)
		/// </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		public bool CanBeEvaluatedLocally(Expression expression)
		{
			// any operation on a query can't be done locally
			ConstantExpression cex = expression as ConstantExpression;
			if (cex != null)
			{
				IQueryable query = cex.Value as IQueryable;
				if (query != null && query.Provider == this)
				{
					return false;
				}
			}

			MethodCallExpression mc = expression as MethodCallExpression;
			if (mc != null &&
				(mc.Method.DeclaringType == typeof(Enumerable) ||
				 mc.Method.DeclaringType == typeof(Queryable) /*||
				 mc.Method.DeclaringType == typeof(Updatable)*/)
				 )
			{
				return false;
			}

			if (expression.NodeType == ExpressionType.Convert &&
				expression.Type == typeof(object))
			{
				return true;
			}

			return expression.NodeType != ExpressionType.Parameter &&
				   expression.NodeType != ExpressionType.Lambda;
		}

		/// <summary>
		/// Determines if a property is computed after insert or update
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public bool IsComputed(MappingEntity entity, MemberInfo member)
		{
			return false;
		}

		/// <summary>
		/// Determines if a property is generated on the server during insert
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public bool IsGenerated(MappingEntity entity, MemberInfo member)
		{
			return false;
		}

		/// <summary>
		/// Determines if a property can be part of an update operation
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		public bool IsUpdatable(MappingEntity entity, MemberInfo member)
		{
			return !this.IsPrimaryKey(entity, member) && !this.IsComputed(entity, member);
		}

		/// <summary>
		/// The type of the entity on the other side of the relationship
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public MappingEntity GetRelatedEntity(MappingEntity entity, MemberInfo member)
		{
			Type relatedType = TypeHelper.GetElementType(TypeHelper.GetMemberType(member));
			return this.GetEntity(relatedType);
		}

		/// <summary>
		/// Determines if the property is an assocation relationship.
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		public bool IsAssociationRelationship(MappingEntity entity, MemberInfo member)
		{
			if (IsMapped(entity, member) && !IsColumn(entity, member))
			{
				Type otherType = TypeHelper.GetElementType(TypeHelper.GetMemberType(member));
				return !this.IsScalar(otherType);
			}
			return false;
		}

		/// <summary>
		/// Gets the key members on this side of an association.
		/// </summary>
		/// <param name="entity">The entity on this side of the association.</param>
		/// <param name="member">The member that represents the association.</param>
		/// <returns></returns>
		public IEnumerable<MemberInfo> GetAssociationKeyMembers(MappingEntity entity, MemberInfo member)
		{
			// On this side it's the (probably dynamically created) foreign key member e.g. for Order.Customer
			// it might be Order.CustomerID

			// Convert the type to a dynamic proxy type if it's not already
			Type proxyType;
			if (typeof(IDynamicProxy).IsAssignableFrom(entity.EntityType))
			{
				proxyType = entity.EntityType;
			}
			else
			{
				proxyType = DynamicProxyFactory.GetDynamicProxyType(entity.EntityType, this.Database);
			}

			// Get the property
			// TODO: Should probably implement IsAssociation and make sure it's a property
			string propertyName = this.Configuration.GetForeignKeyColumnName((PropertyInfo)member);
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
		public IEnumerable<MemberInfo> GetAssociationRelatedKeyMembers(MappingEntity entity, MemberInfo member)
		{
			// On the other side it's the ID e.g. for Order.Customer it would be Customer.ID
			MemberInfo property = member.ReflectedType.GetProperty("ID");

			// TODO: Except that the property might not exist as we create things like ID automatically
			// in DynamicProxyFactory
			if (property == null)
			{
				//property = new DummyPropertyInfo("ID", member.ReflectedType, typeof(long));
			}

			yield return property;
		}

		private void GetAssociationKeys(MappingEntity entity, MemberInfo member, out List<MemberInfo> keyMembers, out List<MemberInfo> relatedKeyMembers)
		{
			MappingEntity entity2 = GetRelatedEntity(entity, member);

			// find all members in common (same name)
			var map1 = this.GetMappedMembers(entity).Where(m => this.IsColumn(entity, m)).ToDictionary(m => m.Name);
			var map2 = this.GetMappedMembers(entity2).Where(m => this.IsColumn(entity2, m)).ToDictionary(m => m.Name);
			var commonNames = map1.Keys.Intersect(map2.Keys).OrderBy(k => k);
			keyMembers = new List<MemberInfo>();
			relatedKeyMembers = new List<MemberInfo>();
			foreach (string name in commonNames)
			{
				keyMembers.Add(map1[name]);
				relatedKeyMembers.Add(map2[name]);
			}
		}

		public bool IsRelationshipSource(MappingEntity entity, MemberInfo member)
		{
			if (IsAssociationRelationship(entity, member))
			{
				if (typeof(IEnumerable).IsAssignableFrom(TypeHelper.GetMemberType(member)))
				{
					return false;
				}

				// is source of relationship if relatedKeyMembers are the related entity's primary keys
				MappingEntity entity2 = GetRelatedEntity(entity, member);
				var relatedPKs = new HashSet<string>(this.GetPrimaryKeyMembers(entity2).Select(m => m.Name));
				var relatedKeyMembers = new HashSet<string>(this.GetAssociationRelatedKeyMembers(entity, member).Select(m => m.Name));
				return relatedPKs.IsSubsetOf(relatedKeyMembers) && relatedKeyMembers.IsSubsetOf(relatedPKs);
			}
			return false;
		}

		public bool IsRelationshipTarget(MappingEntity entity, MemberInfo member)
		{
			if (IsAssociationRelationship(entity, member))
			{
				if (typeof(IEnumerable).IsAssignableFrom(TypeHelper.GetMemberType(member)))
				{
					return true;
				}

				// is target of relationship if the assoctions keys are the same as this entities primary key
				var pks = new HashSet<string>(this.GetPrimaryKeyMembers(entity).Select(m => m.Name));
				var keys = new HashSet<string>(this.GetAssociationKeyMembers(entity, member).Select(m => m.Name));
				return keys.IsSubsetOf(pks) && pks.IsSubsetOf(keys);
			}
			return false;
		}

		/// <summary>
		/// Gets the name of the corresponding database table for the supplied entity.
		/// </summary>
		/// <param name="entity">The entity that corresponds to the table.</param>
		/// <returns></returns>
		public string GetTableName(MappingEntity entity)
		{
			if (typeof(IDynamicProxy).IsAssignableFrom(entity.EntityType))
			{
				return this.Configuration.GetTableName(entity.EntityType.BaseType);
			}
			else
			{
				return this.Configuration.GetTableName(entity.EntityType);
			}
		}

		private string SplitWords(string name)
		{
			StringBuilder sb = null;
			bool lastIsLower = char.IsLower(name[0]);
			for (int i = 0, n = name.Length; i < n; i++)
			{
				bool thisIsLower = char.IsLower(name[i]);
				if (lastIsLower && !thisIsLower)
				{
					if (sb == null)
					{
						sb = new StringBuilder();
						sb.Append(name, 0, i);
					}
					sb.Append(" ");
				}
				if (sb != null)
				{
					sb.Append(name[i]);
				}
				lastIsLower = thisIsLower;
			}
			if (sb != null)
			{
				return sb.ToString();
			}
			return name;
		}

		/// <summary>
		/// Gets the name of the corresponding table column for the supplied entity and member.
		/// </summary>
		/// <param name="entity">The entity that corresponds to the column's table.</param>
		/// <param name="member">The member that corresponds to the column.</param>
		/// <returns></returns>
		public string GetColumnName(MappingEntity entity, MemberInfo member)
		{
			// TODO: Should probably implement ShouldMap and make sure it's a property
			return this.Configuration.GetColumnName((PropertyInfo)member);
		}

		public object CloneEntity(MappingEntity entity, object instance)
		{
			var clone = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(entity.EntityType);
			foreach (var mi in this.GetMappedMembers(entity))
			{
				if (this.IsColumn(entity, mi))
				{
					mi.SetValue(clone, mi.GetValue(instance));
				}
			}
			return clone;
		}

		public bool IsModified(MappingEntity entity, object instance, object original)
		{
			foreach (var mi in this.GetMappedMembers(entity))
			{
				if (this.IsColumn(entity, mi))
				{
					if (!object.Equals(mi.GetValue(instance), mi.GetValue(original)))
						return true;
				}
			}
			return false;
		}

		public object GetPrimaryKey(MappingEntity entity, object instance)
		{
			object firstKey = null;
			List<object> keys = null;
			foreach (var mi in this.GetPrimaryKeyMembers(entity))
			{
				if (firstKey == null)
				{
					firstKey = mi.GetValue(instance);
				}
				else
				{
					if (keys == null)
					{
						keys = new List<object>();
						keys.Add(firstKey);
					}
					keys.Add(mi.GetValue(instance));
				}
			}
			if (keys != null)
			{
				return new CompoundKey(keys.ToArray());
			}
			return firstKey;
		}

		public Expression GetPrimaryKeyQuery(MappingEntity entity, Expression source, Expression[] keys)
		{
			// make predicate
			ParameterExpression p = Expression.Parameter(entity.EntityType, "p");
			Expression pred = null;
			var idMembers = this.GetPrimaryKeyMembers(entity).ToList();
			if (idMembers.Count != keys.Length)
			{
				throw new InvalidOperationException("Incorrect number of primary key values");
			}
			for (int i = 0, n = keys.Length; i < n; i++)
			{
				MemberInfo mem = idMembers[i];
				Type memberType = TypeHelper.GetMemberType(mem);
				if (keys[i] != null && TypeHelper.GetNonNullableType(keys[i].Type) != TypeHelper.GetNonNullableType(memberType))
				{
					throw new InvalidOperationException("Primary key value is wrong type");
				}
				Expression eq = Expression.MakeMemberAccess(p, mem).Equal(keys[i]);
				pred = (pred == null) ? eq : pred.And(eq);
			}
			var predLambda = Expression.Lambda(pred, p);

			return Expression.Call(typeof(Queryable), "SingleOrDefault", new Type[] { entity.EntityType }, source, predLambda);
		}

		public IEnumerable<EntityInfo> GetDependentEntities(MappingEntity entity, object instance)
		{
			foreach (var mi in this.GetMappedMembers(entity))
			{
				if (this.IsRelationship(entity, mi) && this.IsRelationshipSource(entity, mi))
				{
					MappingEntity relatedEntity = this.GetRelatedEntity(entity, mi);
					var value = mi.GetValue(instance);
					if (value != null)
					{
						var list = value as IList;
						if (list != null)
						{
							foreach (var item in list)
							{
								if (item != null)
								{
									yield return new EntityInfo(item, relatedEntity);
								}
							}
						}
						else
						{
							yield return new EntityInfo(value, relatedEntity);
						}
					}
				}
			}
		}

		public IEnumerable<EntityInfo> GetDependingEntities(MappingEntity entity, object instance)
		{
			foreach (var mi in this.GetMappedMembers(entity))
			{
				if (this.IsRelationship(entity, mi) && this.IsRelationshipTarget(entity, mi))
				{
					MappingEntity relatedEntity = this.GetRelatedEntity(entity, mi);
					var value = mi.GetValue(instance);
					if (value != null)
					{
						var list = value as IList;
						if (list != null)
						{
							foreach (var item in list)
							{
								if (item != null)
								{
									yield return new EntityInfo(item, relatedEntity);
								}
							}
						}
						else
						{
							yield return new EntityInfo(value, relatedEntity);
						}
					}
				}
			}
		}
	}
}
