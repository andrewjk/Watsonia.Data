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
	public class PlayerValueBag : IValueBag
	{
		public long ID { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public DateTime DateOfBirth { get; set; }
		public long TeamsID { get; set; }
		public long? TeamID { get; set; }
	}
}
