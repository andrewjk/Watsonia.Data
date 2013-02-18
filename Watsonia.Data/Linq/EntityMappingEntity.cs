using IQToolkit.Data.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Watsonia.Data.Linq
{
	internal class EntityMappingEntity : MappingEntity
	{
		private readonly string _entityId;
		private readonly Type _type;
		private readonly List<string> _includePaths = new List<string>();

		public override string TableId
		{
			get { return _entityId; }
		}

		public override Type ElementType
		{
			get { return _type; }
		}

		public override Type EntityType
		{
			get { return _type; }
		}

		public List<string> IncludePaths
		{
			get
			{
				return _includePaths;
			}
		}

		public EntityMappingEntity(Type type, string entityId)
		{
			_entityId = entityId;
			_type = type;
		}
	}
}
