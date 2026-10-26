using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data;
using Watsonia.Data.EventArgs;

namespace Watsonia.Data.TestPerformance.Entities
{
	public class TeamValueBag : IValueBag
	{
		public long ID { get; set; }
		public string Name { get; set; }
		public DateTime FoundingDate { get; set; }
		public long SportsID { get; set; }
		public long? SportID { get; set; }
	}
}
