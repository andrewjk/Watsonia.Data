using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Query
{
	public class MappingEntity
	{
		public Type EntityType
		{
			get;
			private set;
		}

		public string EntityId
		{
			get;
			private set;
		}

		public MappingEntity(Type elementType, string entityID)
		{
			this.EntityType = elementType;
			this.EntityId = entityID;
		}

		public override string ToString()
		{
			return this.EntityId;
		}
	}
}
