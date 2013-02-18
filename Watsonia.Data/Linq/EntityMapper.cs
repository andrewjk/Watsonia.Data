using IQToolkit.Data.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Watsonia.Data.Linq
{
	/// <summary>
	/// Maps entities to their objects in the database.
	/// </summary>
	internal class EntityMapper : BasicMapper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EntityMapper" /> class.
		/// </summary>
		/// <param name="mapping">The mapping.</param>
		/// <param name="translator">The translator.</param>
		public EntityMapper(BasicMapping mapping, QueryTranslator translator)
			: base(mapping, translator)
		{
		}

		/// <summary>
		/// Builds the entity expression.
		/// </summary>
		/// <remarks>
		/// The base version of this method looks for a constructor that takes the same number of parameters as the number
		/// of read-only members.  We don't want to do that; we want to just invoke the empty constructor.
		/// </remarks>
		/// <param name="entity">The entity.</param>
		/// <param name="assignments">The assignments.</param>
		/// <returns></returns>
		/// <exception cref="System.InvalidOperationException"></exception>
		protected override Expression BuildEntityExpression(MappingEntity entity, IList<EntityAssignment> assignments)
		{
			NewExpression newExpression;

			ConstructorInfo[] cons = entity.EntityType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			bool hasNoArgConstructor = cons.Any(c => c.GetParameters().Length == 0);

			if (!hasNoArgConstructor)
			{
				throw new InvalidOperationException(string.Format("Cannot construct type '{0}' as it doesn't have an empty constructor.", entity.ElementType));
			}
			else
			{
				newExpression = Expression.New(entity.EntityType);
			}

			Expression result;
			if (assignments.Count > 0)
			{
				if (entity.ElementType.IsInterface)
				{
					assignments = this.MapAssignments(assignments, entity.EntityType).ToList();
				}
				result = Expression.MemberInit(newExpression, (MemberBinding[])assignments.Select(a => Expression.Bind(a.Member, a.Expression)).ToArray());
			}
			else
			{
				result = newExpression;
			}

			if (entity.ElementType != entity.EntityType)
			{
				result = Expression.Convert(result, entity.ElementType);
			}

			return result;
		}

		private IEnumerable<EntityAssignment> MapAssignments(IEnumerable<EntityAssignment> assignments, Type entityType)
		{
			foreach (var assign in assignments)
			{
				MemberInfo[] members = entityType.GetMember(assign.Member.Name, BindingFlags.Instance | BindingFlags.Public);
				if (members != null && members.Length > 0)
				{
					yield return new EntityAssignment(members[0], assign.Expression);
				}
				else
				{
					yield return assign;
				}
			}
		}
	}
}
