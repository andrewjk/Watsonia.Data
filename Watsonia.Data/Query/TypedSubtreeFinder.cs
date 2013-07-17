﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Watsonia.Data.Query
{
    /// <summary>
    /// Finds the first sub-expression that is of a specified type
    /// </summary>
    internal sealed class TypedSubtreeFinder : ExpressionVisitor
    {
        private Expression root;
        private Type type;

        private TypedSubtreeFinder(Type type)
        {
            this.type = type;
        }

        public static Expression Find(Expression expression, Type type)
        {
            TypedSubtreeFinder finder = new TypedSubtreeFinder(type);
            finder.Visit(expression);
            return finder.root;
        }

        protected override Expression Visit(Expression exp)
        {
            Expression result = base.Visit(exp);

            // Remember the first sub-expression that produces an IQueryable
            if (this.root == null && result != null)
            {
				if (this.type.IsAssignableFrom(result.Type))
				{
					this.root = result;
				}
            }

            return result;
        }
    }
}