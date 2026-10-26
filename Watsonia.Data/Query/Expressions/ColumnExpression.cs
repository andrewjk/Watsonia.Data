// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Query.Expressions
{
	/// <summary>
	/// A custom expression node that represents a reference to a column in a SQL query
	/// </summary>
	internal sealed class ColumnExpression : DbExpression, IEquatable<ColumnExpression>
	{
		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.Column; }
		}

		public TableAlias Alias
		{
			get;
			private set;
		}

		public string Name
		{
			get;
			private set;
		}

		public ColumnExpression(Type type, TableAlias alias, string name)
			: base(type)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			this.Alias = alias;
			this.Name = name;
		}

		public override string ToString()
		{
			return this.Alias.ToString() + ".C(" + this.Name + ")";
		}

		public override int GetHashCode()
		{
			return this.Alias.GetHashCode() + this.Name.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as ColumnExpression);
		}

		public bool Equals(ColumnExpression other)
		{
			return other != null
				&& ((object)this) == (object)other
				 || (this.Alias == other.Alias && this.Name == other.Name);
		}
	}
}
