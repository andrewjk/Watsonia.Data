﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;

namespace Watsonia.Data.Query
{
	internal sealed class ScopedDictionary<TKey, TValue>
	{
		private readonly ScopedDictionary<TKey, TValue> _previous;
		private readonly Dictionary<TKey, TValue> _map;

		public ScopedDictionary(ScopedDictionary<TKey, TValue> previous)
		{
			this._previous = previous;
			this._map = new Dictionary<TKey, TValue>();
		}

		public ScopedDictionary(ScopedDictionary<TKey, TValue> previous, IEnumerable<KeyValuePair<TKey, TValue>> pairs)
			: this(previous)
		{
			foreach (var p in pairs)
			{
				this._map.Add(p.Key, p.Value);
			}
		}

		public void Add(TKey key, TValue value)
		{
			this._map.Add(key, value);
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			for (ScopedDictionary<TKey, TValue> scope = this; scope != null; scope = scope._previous)
			{
				if (scope._map.TryGetValue(key, out value))
				{
					return true;
				}
			}
			value = default(TValue);
			return false;
		}

		public bool ContainsKey(TKey key)
		{
			for (ScopedDictionary<TKey, TValue> scope = this; scope != null; scope = scope._previous)
			{
				if (scope._map.ContainsKey(key))
				{
					return true;
				}
			}
			return false;
		}
	}
}