using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Watsonia.Data.DataAnnotations;

namespace Watsonia.Data.Tests.Entities
{
	public class HasChanges
	{
		public virtual bool Bool { get; set; }

		public virtual bool? BoolNullable { get; set; }

		public virtual int Int { get; set; }

		public virtual int? IntNullable { get; set; }

		public virtual decimal Decimal { get; set; }

		public virtual decimal? DecimalNullable { get; set; }

		[DefaultDateTimeValue(1900, 1, 1)]
		public virtual DateTime DateTime { get; set; }

		public virtual DateTime? DateTimeNullable { get; set; }

		public virtual CardinalDirection Direction { get; set; }

		public virtual string String { get; set; }

		[DefaultValue(5.5)]
		public virtual decimal DecimalWithDefault { get; set; }

		// TODO: Test this
		public virtual HasChangesRelated Related { get; set; }
	}
}
