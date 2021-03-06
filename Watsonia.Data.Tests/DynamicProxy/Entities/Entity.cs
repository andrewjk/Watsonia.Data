﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Tests.DynamicProxy.Entities
{
	// Ignore these warnings because GetHashCode and Equals will be overridden with DynamicProxyFactory
#pragma warning disable 660, 661
	public class Entity
#pragma warning restore 660, 661
	{
		public static bool operator ==(Entity a, Entity b)
		{
			if (object.ReferenceEquals(a, b))
			{
				return true;
			}

			if (a is null || b is null)
			{
				return false;
			}

			return a.Equals(b);
		}

		public static bool operator !=(Entity a, Entity b)
		{
			return !(a == b);
		}
	}
}
