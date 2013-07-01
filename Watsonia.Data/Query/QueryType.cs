using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Query
{
	public class QueryType
	{
		public DbType DbType
		{
			get
			{
				return QueryTypeSystem.GetDbType(this.SqlDbType);
			}
		}

		public SqlDbType SqlDbType
		{
			get;
			private set;
		}

		public int Length
		{
			get;
			private set;
		}

		public bool NotNull
		{
			get;
			private set;
		}

		public short Precision
		{
			get;
			private set;
		}

		public short Scale
		{
			get;
			private set;
		}

		public QueryType(SqlDbType sqlDbType, bool notNull, int length, short precision, short scale)
		{
			this.SqlDbType = sqlDbType;
			this.NotNull = notNull;
			this.Length = length;
			this.Precision = precision;
			this.Scale = scale;
		}
	}
}