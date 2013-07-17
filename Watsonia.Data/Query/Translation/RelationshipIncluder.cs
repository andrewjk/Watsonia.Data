﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Watsonia.Data.Query;
using Watsonia.Data.Query.Expressions;

namespace Watsonia.Data.Query.Translation
{
    /// <summary>
    /// Adds relationship to query results depending on policy
    /// </summary>
    internal sealed class RelationshipIncluder : DbExpressionVisitor
    {
        QueryMapper mapper;
        ScopedDictionary<MemberInfo, bool> includeScope = new ScopedDictionary<MemberInfo, bool>(null);

        private RelationshipIncluder(QueryMapper mapper)
        {
            this.mapper = mapper;
        }

        public static Expression Include(QueryMapper mapper, Expression expression)
        {
            return new RelationshipIncluder(mapper).Visit(expression);
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            Expression projector = this.Visit(proj.Projector);
            return this.UpdateProjection(proj, proj.SelectExpression, projector, proj.Aggregator);
        }

        protected override Expression VisitEntity(EntityExpression entity)
        {
            var save = this.includeScope;
            this.includeScope = new ScopedDictionary<MemberInfo,bool>(this.includeScope);
            try
            {
                if (this.mapper.HasIncludedMembers(entity))
                {
                    entity = this.mapper.IncludeMembers(
                        entity,
                        m =>
                        {
                            if (this.includeScope.ContainsKey(m))
                            {
                                return false;
                            }
							//if (this.policy.IsIncluded(m))
							//{
							//	this.includeScope.Add(m, true);
							//	return true;
							//}
                            return false;
                        });
                }
                return base.VisitEntity(entity);
            }
            finally
            {
                this.includeScope = save;
            }
        }
    }
}