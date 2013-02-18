﻿using System;

namespace Watsonia.Data.Tests.Models
{
	// Used for checking delete and insert
	public class Crud
	{
		public virtual long ID
		{
			get;
			set;
		}

		public virtual string Name
		{
			get;
			set;
		}
	}
}