// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Watsonia.Data.Query.Translation;

namespace Watsonia.Data.Query
{
	/// <summary>
	/// Defines query execution & materialization policies. 
	/// </summary>
	public class QueryTranslator
	{
		public QueryMapper Mapper
		{
			get;
			private set;
		}

		public QueryTranslator(QueryMapping mapping)
		{
			this.Mapper = mapping.CreateMapper(this);
		}

		public virtual Expression Translate(Database database, Expression expression)
		{
			//// Replace types with dynamic proxies so that they contain all of the necessary fields for joining etc
			//expression = QueryTypeReplacer.Replace(database, expression);

			// Pre-evaluate local sub-trees
			expression = PartialEvaluator.Eval(expression, this.Mapper.Mapping.CanBeEvaluatedLocally);

			// Apply mapping (binds LINQ operators too)
			expression = this.Mapper.Translate(database, expression);

			// Any policy specific translations or validations
			expression = this.PolicyTranslate(expression);

			// Any language specific translations or validations
			expression = this.LanguageTranslate(expression);

			return expression;
		}

		/// <summary>
		/// Provides policy specific query translations.  This is where choices about inclusion of related objects and how
		/// heirarchies are materialized affect the definition of the queries.
		/// </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		private Expression PolicyTranslate(Expression expression)
		{
			// TODO: What are all of these things?  Are they needed

			// Add included relationships to client projection
			var rewritten = RelationshipIncluder.Include(this.Mapper, expression);
			if (rewritten != expression)
			{
				expression = rewritten;
				expression = UnusedColumnRemover.Remove(expression);
				expression = RedundantColumnRemover.Remove(expression);
				expression = RedundantSubqueryRemover.Remove(expression);
				expression = RedundantJoinRemover.Remove(expression);
			}

			// Convert any singleton (1:1 or n:1) projections into server-side joins (cardinality is preserved)
			rewritten = SingletonProjectionRewriter.Rewrite(expression);
			if (rewritten != expression)
			{
				expression = rewritten;
				expression = UnusedColumnRemover.Remove(expression);
				expression = RedundantColumnRemover.Remove(expression);
				expression = RedundantSubqueryRemover.Remove(expression);
				expression = RedundantJoinRemover.Remove(expression);
			}

			// Convert projections into client-side joins
			rewritten = ClientJoinedProjectionRewriter.Rewrite(expression);
			if (rewritten != expression)
			{
				expression = rewritten;
				expression = UnusedColumnRemover.Remove(expression);
				expression = RedundantColumnRemover.Remove(expression);
				expression = RedundantSubqueryRemover.Remove(expression);
				expression = RedundantJoinRemover.Remove(expression);
			}

			return expression;
		}

		/// <summary>
		/// Provides language specific query translation.  Use this to apply language specific rewrites or
		/// to make assertions/validations about the query.
		/// </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		private Expression LanguageTranslate(Expression expression)
		{
			// Fix up any order-by's
			expression = OrderByRewriter.Rewrite(expression);

			// Remove redundant layers again before cross apply rewrite
			expression = UnusedColumnRemover.Remove(expression);
			expression = RedundantColumnRemover.Remove(expression);
			expression = RedundantSubqueryRemover.Remove(expression);

			// Convert cross-apply and outer-apply joins into inner & left-outer-joins if possible
			var rewritten = CrossApplyRewriter.Rewrite(expression);

			// Convert cross joins into inner joins
			rewritten = CrossJoinRewriter.Rewrite(rewritten);

			if (rewritten != expression)
			{
				expression = rewritten;
				// Do final reduction
				expression = UnusedColumnRemover.Remove(expression);
				expression = RedundantSubqueryRemover.Remove(expression);
				expression = RedundantJoinRemover.Remove(expression);
				expression = RedundantColumnRemover.Remove(expression);
			}

			// Fix up any order-by's we may have changed
			expression = OrderByRewriter.Rewrite(expression);

			return expression;
		}
	}
}