using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Query
{
	public class QueryCommandParameter
	{
		public string Name
		{
			get;
			private set;
		}

		public Type Type
		{
			get;
			private set;
		}

		public Type ParameterType
		{
			get;
			private set;
		}

		public QueryCommandParameter(string name, Type type, Type parameterType)
		{
			this.Name = name;
			this.Type = type;
			this.ParameterType = parameterType;
		}
	}
}
