using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Reference
{
	public class Book
	{
		public virtual string Title { get; set; }

		public virtual Author Author { get; set; }

		[DefaultValue(10)]
		public virtual decimal Price { get; set; }

		public virtual bool Bool { get; set; }
		public virtual bool? BoolNullable { get; set; }
		public virtual DateTime DateTime { get; set; }
		public virtual DateTime? DateTimeNullable { get; set; }
		public virtual decimal Decimal { get; set; }
		public virtual decimal? DecimalNullable { get; set; }
		public virtual double Double { get; set; }
		public virtual double? DoubleNullable { get; set; }
		public virtual short Short { get; set; }
		public virtual short? ShortNullable { get; set; }
		public virtual int Int { get; set; }
		public virtual int? IntNullable { get; set; }
		public virtual long Long { get; set; }
		public virtual long? LongNullable { get; set; }
		public virtual byte Byte { get; set; }
		public virtual byte? ByteNullable { get; set; }
		public virtual Guid Guid { get; set; }
	}
}
