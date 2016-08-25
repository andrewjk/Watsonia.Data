using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Watsonia.Data.Tests.DynamicProxy
{
	public class Defaults
	{
		[DefaultValue(true)]
		public virtual bool Bool { get; set; }

		[DefaultValue(10)]
		public virtual int Int { get; set; }

		[DefaultValue(12)]
		public virtual int? NullableInt { get; set; }

		[DefaultValue(20)]
		public virtual long Long { get; set; }

		[DefaultValue(30)]
		public virtual decimal Decimal { get; set; }

		[DefaultValue("Hi")]
		public virtual string String { get; set; }

		public virtual string EmptyString { get; set; }

		[DefaultDateTimeValue(1900, 1, 1)]
		public virtual DateTime Date { get; set; }
	}
}
