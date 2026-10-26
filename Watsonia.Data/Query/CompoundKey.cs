// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;

namespace Watsonia.Data.Query
{
    internal sealed class CompoundKey : IEquatable<CompoundKey>, IEnumerable<object>, IEnumerable
    {
        private object[] _values;
        private int _hc;

        public CompoundKey(params object[] values)
        {
            _values = values;
            for (int i = 0, n = values.Length; i < n; i++)
            {
                object value = values[i];
                if (value != null)
                {
                    _hc ^= (value.GetHashCode() + i);
                }
            }
        }

        public override int GetHashCode()
        {
            return _hc;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public bool Equals(CompoundKey other)
        {
			if (other == null || other._values.Length != _values.Length)
			{
				return false;
			}
            for (int i = 0, n = other._values.Length; i < n; i++)
            {
				if (!object.Equals(this._values[i], other._values[i]))
				{
					return false;
				}
            }
            return true;
        }

        public IEnumerator<object> GetEnumerator()
        {
            return ((IEnumerable<object>)_values).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}