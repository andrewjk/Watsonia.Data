using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Reference
{
	public sealed class BookValueBag : IValueBag
	{
		public string Title { get; set; }
		public long? AuthorID { get; set; }
		public decimal Price { get; set; }

		public bool Bool { get; set; }
		public bool? BoolNullable { get; set; }
		public DateTime DateTime { get; set; }
		public DateTime? DateTimeNullable { get; set; }
		public decimal Decimal { get; set; }
		public decimal? DecimalNullable { get; set; }
		public double Double { get; set; }
		public double? DoubleNullable { get; set; }
		public short Short { get; set; }
		public short? ShortNullable { get; set; }
		public int Int { get; set; }
		public int? IntNullable { get; set; }
		public long Long { get; set; }
		public long? LongNullable { get; set; }
		public byte Byte { get; set; }
		public byte? ByteNullable { get; set; }
		public Guid Guid { get; set; }
	}
}
