// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Watsonia.Data.Query
{
	/// <summary>
	/// Simple implementation of the IGrouping<TKey, TElement> interface
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TElement"></typeparam>
	internal sealed class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
	{
		private IEnumerable<TElement> _group;

		public TKey Key
		{
			get;
			private set;
		}

		public Grouping(TKey key, IEnumerable<TElement> group)
		{
			this.Key = key;
			this._group = group;
		}

		public IEnumerator<TElement> GetEnumerator()
		{
			if (!(_group is List<TElement>))
			{
				_group = _group.ToList();
			}
			return this._group.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this._group.GetEnumerator();
		}
	}
}