using System;
using System.ComponentModel.DataAnnotations;

namespace Watsonia.Data.Tests.Models
{
	public class Order
	{
		//public virtual Customer Customer
		//{
		//    get;
		//    set;
		//}
		[Display(Name = "Number of items")]
		public virtual int NumberOfItems
		{
			get;
			set;
		}

		[StringLength(500)]
		[Display(Name = "Delivery notes")]
		public virtual string DeliveryNotes
		{
			get;
			set;
		}

		[Display(Name = "Date required")]
		public virtual DateTime? DateRequired
		{
			get;
			set;
		}

		[Display(Name = "Number of items to order")]
		public virtual int? NumberOfItemsToOrder
		{
			get;
			set;
		}
	}
}