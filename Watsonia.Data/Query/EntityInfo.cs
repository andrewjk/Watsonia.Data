using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Query
{
	public class EntityInfo
	{
		public object Instance
		{
			get;
			private set;
		}

		public MappingEntity Mapping
		{
			get;
			private set;
		}

		public EntityInfo(object instance, MappingEntity mapping)
		{
			this.Instance = instance;
			this.Mapping = mapping;
		}
	}
}
