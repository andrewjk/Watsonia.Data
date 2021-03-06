﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Mapping
{
	public class MappedParameter
	{
		public string Name { get; }

		public Type ParameterType { get; }

		public int MaxLength { get; }

		public MappedParameter(string name, Type parameterType)
		{
			this.Name = name;
			this.ParameterType = parameterType;
		}

		public MappedParameter(string name, Type parameterType, int maxLength)
		{
			this.Name = name;
			this.ParameterType = parameterType;
			this.MaxLength = maxLength;
		}

		public override string ToString()
		{
			return this.Name;
		}
	}
}
