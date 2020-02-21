using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Collections.Generic;
using Watsonia.Data.DataAnnotations;

namespace Watsonia.Data.Tests.DynamicProxy.Entities
{
	public class Invalid
	{
		[Required]
		[Display(Name = "Required string")]
		public virtual string RequiredString { get; set; }

		[Required]
		[Display(Name = "Required nullable")]
		public virtual int? RequiredNullable { get; set; }

		[StringLength(10)]
		[Display(Name = "Short string")]
		public virtual string ShortString { get; set; }

		[RegularExpression(@"^\d{4}$")]
		[DisplayName("Invalid post code")]
		public virtual string InvalidPostCode { get; set; }

		[CustomValidation(typeof(Invalid), "ValidateEmailAddress")]
		[DisplayName("Email address")]
		public virtual string EmailAddress { get; set; }

		[CustomValidation(typeof(Invalid), "ValidateEmailAddress")]
		[DisplayName("Confirm email address")]
		public virtual string ConfirmEmailAddress { get; set; }

		public static ValidationResult ValidateEmailAddress(string emailAddress)
		{
			if (emailAddress.Contains("@"))
			{
				return null;
			}
			else
			{
				return new ValidationResult("The {0} field is no good.");
			}
		}

		[Cascade]
		public virtual List<InvalidChild> InvalidChildren { get; set; }
	}
}
