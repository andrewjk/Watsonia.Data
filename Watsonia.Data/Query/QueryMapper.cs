using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.Query.Expressions;
using Watsonia.Data.Query.Translation;

namespace Watsonia.Data.Query
{
	public class QueryMapper
	{
		public QueryMapping Mapping
		{
			get;
			private set;
		}

		public QueryTranslator Translator
		{
			get;
			private set;
		}

		public QueryMapper(QueryMapping mapping, QueryTranslator translator)
        {
            this.Mapping = mapping;
            this.Translator = translator;
        }

		/// <summary>
		/// Get a query expression that selects all entities from a table
		/// </summary>
		/// <param name="rowType"></param>
		/// <returns></returns>
		public ProjectionExpression GetQueryExpression(MappingEntity entity)
		{
			var tableAlias = new TableAlias();
			var selectAlias = new TableAlias();
			var table = new TableExpression(tableAlias, entity, this.Mapping.GetTableName(entity));

			Expression projector = this.GetEntityExpression(table, entity);
			var pc = ColumnProjector.ProjectColumns(projector, null, selectAlias, tableAlias);

			var proj = new ProjectionExpression(
				new SelectExpression(selectAlias, pc.Columns, table, null),
				pc.Projector);

			return proj;
		}

		/// <summary>
		/// Gets an expression that constructs an entity instance relative to a root.
		/// The root is most often a TableExpression, but may be any other experssion such as
		/// a ConstantExpression.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="entity"></param>
		/// <returns></returns>
		public EntityExpression GetEntityExpression(Expression root, MappingEntity entity)
		{
			// must be some complex type constructed from multiple columns
			var assignments = new List<EntityAssignment>();
			foreach (MemberInfo mi in this.Mapping.GetMappedMembers(entity))
			{
				if (!this.Mapping.IsAssociationRelationship(entity, mi))
				{
					Expression me = this.GetMemberExpression(root, entity, mi);
					if (me != null)
					{
						assignments.Add(new EntityAssignment(mi, me));
					}
				}
			}

			return new EntityExpression(entity, BuildEntityExpression(entity, assignments));
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
		protected Expression BuildEntityExpression(MappingEntity entity, IList<EntityAssignment> assignments)
		{
			NewExpression newExpression;

			ConstructorInfo[] cons = entity.EntityType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			bool hasNoArgConstructor = cons.Any(c => c.GetParameters().Length == 0);

			if (!hasNoArgConstructor)
			{
				throw new InvalidOperationException(string.Format("Cannot construct type '{0}' as it doesn't have an empty constructor.", entity.EntityType));
			}
			else
			{
				newExpression = Expression.New(entity.EntityType);
			}

			Expression result;
			if (assignments.Count > 0)
			{
				if (entity.EntityType.IsInterface)
				{
					assignments = this.MapAssignments(assignments, entity.EntityType).ToList();
				}
				result = Expression.MemberInit(newExpression, (MemberBinding[])assignments.Select(a => Expression.Bind(a.Member, a.Expression)).ToArray());
			}
			else
			{
				result = newExpression;
			}

			if (entity.EntityType != entity.EntityType)
			{
				result = Expression.Convert(result, entity.EntityType);
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

		protected ConstructorBindResult BindConstructor(ConstructorInfo cons, IList<EntityAssignment> assignments)
		{
			var ps = cons.GetParameters();
			var args = new Expression[ps.Length];
			var mis = new MemberInfo[ps.Length];
			HashSet<EntityAssignment> members = new HashSet<EntityAssignment>(assignments);
			HashSet<EntityAssignment> used = new HashSet<EntityAssignment>();

			for (int i = 0, n = ps.Length; i < n; i++)
			{
				ParameterInfo p = ps[i];
				var assignment = members.FirstOrDefault(a =>
					p.Name == a.Member.Name
					&& p.ParameterType.IsAssignableFrom(a.Expression.Type));
				if (assignment == null)
				{
					assignment = members.FirstOrDefault(a =>
						string.Compare(p.Name, a.Member.Name, true) == 0
						&& p.ParameterType.IsAssignableFrom(a.Expression.Type));
				}
				if (assignment != null)
				{
					args[i] = assignment.Expression;
					if (mis != null)
						mis[i] = assignment.Member;
					used.Add(assignment);
				}
				else
				{
					MemberInfo[] mems = cons.DeclaringType.GetMember(p.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
					if (mems != null && mems.Length > 0)
					{
						args[i] = Expression.Constant(TypeHelper.GetDefault(p.ParameterType), p.ParameterType);
						mis[i] = mems[0];
					}
					else
					{
						// unknown parameter, does not match any member
						return null;
					}
				}
			}

			members.ExceptWith(used);

			return new ConstructorBindResult(Expression.New(cons, args, mis), members);
		}

		/// <summary>
		/// Get an expression for a mapped property relative to a root expression. 
		/// The root is either a TableExpression or an expression defining an entity instance.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="entity"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		public Expression GetMemberExpression(Expression root, MappingEntity entity, MemberInfo member)
		{
			if (this.Mapping.IsAssociationRelationship(entity, member))
			{
				MappingEntity relatedEntity = this.Mapping.GetRelatedEntity(entity, member);
				ProjectionExpression projection = this.GetQueryExpression(relatedEntity);

				// make where clause for joining back to 'root'
				var declaredTypeMembers = this.Mapping.GetAssociationKeyMembers(entity, member).ToList();
				var associatedMembers = this.Mapping.GetAssociationRelatedKeyMembers(entity, member).ToList();

				Expression where = null;
				for (int i = 0, n = associatedMembers.Count; i < n; i++)
				{
					Expression equal =
						this.GetMemberExpression(projection.Projector, relatedEntity, associatedMembers[i]).Equal(
							this.GetMemberExpression(root, entity, declaredTypeMembers[i]));
					where = (where != null) ? where.And(equal) : equal;
				}

				TableAlias newAlias = new TableAlias();
				var pc = ColumnProjector.ProjectColumns(projection.Projector, null, newAlias, projection.SelectExpression.Alias);

				LambdaExpression aggregator = Aggregator.GetAggregator(TypeHelper.GetMemberType(member), typeof(IEnumerable<>).MakeGenericType(pc.Projector.Type));
				var result = new ProjectionExpression(
					new SelectExpression(newAlias, pc.Columns, projection.SelectExpression, where),
					pc.Projector, aggregator);

				return result;
			}
			else
			{
				AliasedExpression aliasedRoot = root as AliasedExpression;
				if (aliasedRoot != null && this.Mapping.IsColumn(entity, member))
				{
					return new ColumnExpression(TypeHelper.GetMemberType(member), aliasedRoot.Alias, this.Mapping.GetColumnName(entity, member));
				}
				return QueryBinder.BindMember(root, member);
			}
		}

		/// <summary>
		/// Get an expression that represents the insert operation for the specified instance.
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="instance">The instance to insert.</param>
		/// <param name="selector">A lambda expression that computes a return value from the operation.</param>
		/// <returns></returns>
		public Expression GetInsertExpression(MappingEntity entity, Expression instance, LambdaExpression selector)
		{
			var tableAlias = new TableAlias();
			var table = new TableExpression(tableAlias, entity, this.Mapping.GetTableName(entity));
			var assignments = this.GetColumnAssignments(table, instance, entity, (e, m) => !this.Mapping.IsGenerated(e, m));

			if (selector != null)
			{
				return new BlockCommand(
					new InsertCommand(table, assignments),
					this.GetInsertResult(entity, instance, selector, null));
			}

			return new InsertCommand(table, assignments);
		}

		private IEnumerable<ColumnAssignment> GetColumnAssignments(Expression table, Expression instance, MappingEntity entity, Func<MappingEntity, MemberInfo, bool> fnIncludeColumn)
		{
			foreach (var m in this.Mapping.GetMappedMembers(entity))
			{
				if (this.Mapping.IsColumn(entity, m) && fnIncludeColumn(entity, m))
				{
					yield return new ColumnAssignment(
						(ColumnExpression)this.GetMemberExpression(table, entity, m),
						Expression.MakeMemberAccess(instance, m));
				}
			}
		}

		protected Expression GetInsertResult(MappingEntity entity, Expression instance, LambdaExpression selector, Dictionary<MemberInfo, Expression> map)
		{
			var tableAlias = new TableAlias();
			var tex = new TableExpression(tableAlias, entity, this.Mapping.GetTableName(entity));
			var aggregator = Aggregator.GetAggregator(selector.Body.Type, typeof(IEnumerable<>).MakeGenericType(selector.Body.Type));

			Expression where;
			DeclarationCommand genIdCommand = null;
			var generatedIds = this.Mapping.GetMappedMembers(entity).Where(m => this.Mapping.IsPrimaryKey(entity, m) && this.Mapping.IsGenerated(entity, m)).ToList();
			if (generatedIds.Count > 0)
			{
				if (map == null || !generatedIds.Any(m => map.ContainsKey(m)))
				{
					var localMap = new Dictionary<MemberInfo, Expression>();
					genIdCommand = this.GetGeneratedIdCommand(entity, generatedIds.ToList(), localMap);
					map = localMap;
				}

				// is this just a retrieval of one generated id member?
				var mex = selector.Body as MemberExpression;
				if (mex != null && this.Mapping.IsPrimaryKey(entity, mex.Member) && this.Mapping.IsGenerated(entity, mex.Member))
				{
					if (genIdCommand != null)
					{
						// just use the select from the genIdCommand
						return new ProjectionExpression(
							genIdCommand.Source,
							new ColumnExpression(mex.Type, genIdCommand.Source.Alias, genIdCommand.Source.Columns[0].Name),
							aggregator);
					}
					else
					{
						TableAlias alias = new TableAlias();
						var colType = TypeHelper.GetMemberType(mex.Member);
						return new ProjectionExpression(
							new SelectExpression(alias, new[] { new ColumnDeclaration("", map[mex.Member], colType) }, null, null),
							new ColumnExpression(TypeHelper.GetMemberType(mex.Member), alias, ""),
							aggregator);
					}
				}

				where = generatedIds.Select((m, i) =>
					this.GetMemberExpression(tex, entity, m).Equal(map[m])
					).Aggregate((x, y) => x.And(y));
			}
			else
			{
				where = this.GetIdentityCheck(tex, entity, instance);
			}

			Expression typeProjector = this.GetEntityExpression(tex, entity);
			Expression selection = DbExpressionReplacer.Replace(selector.Body, selector.Parameters[0], typeProjector);
			TableAlias newAlias = new TableAlias();
			var pc = ColumnProjector.ProjectColumns(selection, null, newAlias, tableAlias);
			var pe = new ProjectionExpression(
				new SelectExpression(newAlias, pc.Columns, tex, where),
				pc.Projector,
				aggregator);

			if (genIdCommand != null)
			{
				return new BlockCommand(genIdCommand, pe);
			}
			return pe;
		}

		protected DeclarationCommand GetGeneratedIdCommand(MappingEntity entity, List<MemberInfo> members, Dictionary<MemberInfo, Expression> map)
		{
			var columns = new List<ColumnDeclaration>();
			var decls = new List<VariableDeclaration>();
			var alias = new TableAlias();
			foreach (var member in members)
			{
				Expression genId = GetGeneratedIdExpression(member);
				var name = member.Name;
				var colType = TypeHelper.GetMemberType(member);
				columns.Add(new ColumnDeclaration(member.Name, genId, colType));
				decls.Add(new VariableDeclaration(member.Name, colType, new ColumnExpression(genId.Type, alias, member.Name)));
				if (map != null)
				{
					var vex = new VariableExpression(member.Name, colType);
					map.Add(member, vex);
				}
			}
			var select = new SelectExpression(alias, columns, null, null);
			return new DeclarationCommand(decls, select);
		}

		// TODO: It should never be this
		private Expression GetGeneratedIdExpression(MemberInfo member)
		{
			throw new NotImplementedException();
		}

		protected Expression GetIdentityCheck(Expression root, MappingEntity entity, Expression instance)
		{
			return this.Mapping.GetMappedMembers(entity)
			.Where(m => this.Mapping.IsPrimaryKey(entity, m))
			.Select(m => this.GetMemberExpression(root, entity, m).Equal(Expression.MakeMemberAccess(instance, m)))
			.Aggregate((x, y) => x.And(y));
		}

		protected Expression GetEntityExistsTest(MappingEntity entity, Expression instance)
		{
			ProjectionExpression tq = this.GetQueryExpression(entity);
			Expression where = this.GetIdentityCheck(tq.SelectExpression, entity, instance);
			return new ExistsExpression(new SelectExpression(new TableAlias(), null, tq.SelectExpression, where));
		}

		protected Expression GetEntityStateTest(MappingEntity entity, Expression instance, LambdaExpression updateCheck)
		{
			ProjectionExpression tq = this.GetQueryExpression(entity);
			Expression where = this.GetIdentityCheck(tq.SelectExpression, entity, instance);
			Expression check = DbExpressionReplacer.Replace(updateCheck.Body, updateCheck.Parameters[0], tq.Projector);
			where = where.And(check);
			return new ExistsExpression(new SelectExpression(new TableAlias(), null, tq.SelectExpression, where));
		}

		/// <summary>
		/// Get an expression that represents the update operation for the specified instance.
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="instance"></param>
		/// <param name="updateCheck"></param>
		/// <param name="selector"></param>
		/// <param name="else"></param>
		/// <returns></returns>
		public Expression GetUpdateExpression(MappingEntity entity, Expression instance, LambdaExpression updateCheck, LambdaExpression selector, Expression @else)
		{
			var tableAlias = new TableAlias();
			var table = new TableExpression(tableAlias, entity, this.Mapping.GetTableName(entity));

			var where = this.GetIdentityCheck(table, entity, instance);
			if (updateCheck != null)
			{
				Expression typeProjector = this.GetEntityExpression(table, entity);
				Expression pred = DbExpressionReplacer.Replace(updateCheck.Body, updateCheck.Parameters[0], typeProjector);
				where = where.And(pred);
			}

			var assignments = this.GetColumnAssignments(table, instance, entity, (e, m) => this.Mapping.IsUpdatable(e, m));

			Expression update = new UpdateCommand(table, where, assignments);

			if (selector != null)
			{
				return new BlockCommand(
					update,
					new IfCommand(
						GetRowsAffectedExpression(update).GreaterThan(Expression.Constant(0)),
						this.GetUpdateResult(entity, instance, selector),
						@else));
			}
			else if (@else != null)
			{
				return new BlockCommand(
					update,
					new IfCommand(
						GetRowsAffectedExpression(update).LessThanOrEqual(Expression.Constant(0)),
						@else,
						null));
			}
			else
			{
				return update;
			}
		}

		// TODO: It should never be this
		private Expression GetRowsAffectedExpression(Expression command)
		{
			return new FunctionExpression(typeof(int), "@@ROWCOUNT", null);
		}

		protected Expression GetUpdateResult(MappingEntity entity, Expression instance, LambdaExpression selector)
		{
			var tq = this.GetQueryExpression(entity);
			Expression where = this.GetIdentityCheck(tq.SelectExpression, entity, instance);
			Expression selection = DbExpressionReplacer.Replace(selector.Body, selector.Parameters[0], tq.Projector);
			TableAlias newAlias = new TableAlias();
			var pc = ColumnProjector.ProjectColumns(selection, null, newAlias, tq.SelectExpression.Alias);
			return new ProjectionExpression(
				new SelectExpression(newAlias, pc.Columns, tq.SelectExpression, where),
				pc.Projector,
				Aggregator.GetAggregator(selector.Body.Type, typeof(IEnumerable<>).MakeGenericType(selector.Body.Type)));
		}

		/// <summary>
		/// Get an expression that represents the insert-or-update operation for the specified instance.
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="instance"></param>
		/// <param name="updateCheck"></param>
		/// <param name="resultSelector"></param>
		/// <returns></returns>
		public Expression GetInsertOrUpdateExpression(MappingEntity entity, Expression instance, LambdaExpression updateCheck, LambdaExpression resultSelector)
		{
			if (updateCheck != null)
			{
				Expression insert = this.GetInsertExpression(entity, instance, resultSelector);
				Expression update = this.GetUpdateExpression(entity, instance, updateCheck, resultSelector, null);
				var check = this.GetEntityExistsTest(entity, instance);
				return new IfCommand(check, update, insert);
			}
			else
			{
				Expression insert = this.GetInsertExpression(entity, instance, resultSelector);
				Expression update = this.GetUpdateExpression(entity, instance, updateCheck, resultSelector, insert);
				return update;
			}
		}

		/// <summary>
		/// Get an expression that represents the delete operation for the specified instance.
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="instance"></param>
		/// <param name="deleteCheck"></param>
		/// <returns></returns>
		public Expression GetDeleteExpression(MappingEntity entity, Expression instance, LambdaExpression deleteCheck)
		{
			TableExpression table = new TableExpression(new TableAlias(), entity, this.Mapping.GetTableName(entity));
			Expression where = null;

			if (instance != null)
			{
				where = this.GetIdentityCheck(table, entity, instance);
			}

			if (deleteCheck != null)
			{
				Expression row = this.GetEntityExpression(table, entity);
				Expression pred = DbExpressionReplacer.Replace(deleteCheck.Body, deleteCheck.Parameters[0], row);
				where = (where != null) ? where.And(pred) : pred;
			}

			return new DeleteCommand(table, where);
		}

		/// <summary>
		/// Recreate the type projection with the additional members included
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="fnIsIncluded"></param>
		/// <returns></returns>
		public EntityExpression IncludeMembers(EntityExpression entity, Func<MemberInfo, bool> fnIsIncluded)
		{
			var assignments = this.GetAssignments(entity.Expression).ToDictionary(ma => ma.Member.Name);
			bool anyAdded = false;
			foreach (var mi in this.Mapping.GetMappedMembers(entity.Entity))
			{
				EntityAssignment ea;
				bool okayToInclude = !assignments.TryGetValue(mi.Name, out ea) || IsNullRelationshipAssignment(entity.Entity, ea);
				if (okayToInclude && fnIsIncluded(mi))
				{
					ea = new EntityAssignment(mi, this.GetMemberExpression(entity.Expression, entity.Entity, mi));
					assignments[mi.Name] = ea;
					anyAdded = true;
				}
			}
			if (anyAdded)
			{
				return new EntityExpression(entity.Entity, this.BuildEntityExpression(entity.Entity, assignments.Values.ToList()));
			}
			return entity;
		}

		private bool IsNullRelationshipAssignment(MappingEntity entity, EntityAssignment assignment)
		{
			if (this.Mapping.IsRelationship(entity, assignment.Member))
			{
				var cex = assignment.Expression as ConstantExpression;
				if (cex != null && cex.Value == null)
					return true;
			}
			return false;
		}

		private IEnumerable<EntityAssignment> GetAssignments(Expression newOrMemberInit)
		{
			var assignments = new List<EntityAssignment>();
			var minit = newOrMemberInit as MemberInitExpression;
			if (minit != null)
			{
				assignments.AddRange(minit.Bindings.OfType<MemberAssignment>().Select(a => new EntityAssignment(a.Member, a.Expression)));
				newOrMemberInit = minit.NewExpression;
			}
			var nex = newOrMemberInit as NewExpression;
			if (nex != null && nex.Members != null)
			{
				assignments.AddRange(
					Enumerable.Range(0, nex.Arguments.Count)
							  .Where(i => nex.Members[i] != null)
							  .Select(i => new EntityAssignment(nex.Members[i], nex.Arguments[i])));
			}
			return assignments;
		}

		/// <summary>
		/// </summary>
		/// <returns></returns>
		public bool HasIncludedMembers(EntityExpression entity)
		{
			////var policy = this.Translator.Police.Policy;
			////foreach (var mi in this.Mapping.GetMappedMembers(entity.Entity))
			////{
			////	if (policy.IsIncluded(mi))
			////		return true;
			////}
			return false;
		}

		/// <summary>
		/// Apply mapping to a sub query expression
		/// </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		public Expression ApplyMapping(Expression expression)
		{
			return QueryBinder.Bind(this, expression);
		}

		/// <summary>
		/// Apply mapping translations to this expression
		/// </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		public Expression Translate(Database database, Expression expression)
		{
			//// Replace types with dynamic proxies so that they contain all of the necessary fields for joining etc
			//expression = QueryTypeReplacer.Replace(database, expression);
	
			// Convert references to LINQ operators into query specific nodes
			expression = QueryBinder.Bind(this, expression);

			// Move aggregate computations so they occur in same select as group-by
			expression = AggregateRewriter.Rewrite(expression);

			// Do reduction so duplicate associations are likely to be clumped together
			expression = UnusedColumnRemover.Remove(expression);
			expression = RedundantColumnRemover.Remove(expression);
			expression = RedundantSubqueryRemover.Remove(expression);
			expression = RedundantJoinRemover.Remove(expression);

			// Convert references to association properties into correlated queries
			var bound = RelationshipBinder.Bind(this, expression);
			if (bound != expression)
			{
				expression = bound;
				// Clean up after ourselves! (multiple references to same association property)
				expression = RedundantColumnRemover.Remove(expression);
				expression = RedundantJoinRemover.Remove(expression);
			}

			// Rewrite comparision checks between entities and multi-valued constructs
			expression = ComparisonRewriter.Rewrite(this.Mapping, expression);

			return expression;
		}
	}
}
