using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data
{
	public class MappedProcedureParameter
	{
		public string Name { get; set; }

		public Type ParameterType { get; set; }

		public int MaxLength { get; set; }

		public MappedProcedureParameter(string name, Type parameterType)
		{
			this.Name = name;
			this.ParameterType = parameterType;
		}

		public MappedProcedureParameter(string name, Type parameterType, int maxLength)
		{
			this.Name = name;
			this.ParameterType = parameterType;
			this.MaxLength = MaxLength;
		}

		public override string ToString()
		{
			return this.Name;
		}
	}
}
