using System;

namespace Watsonia.Data.Tests.DatabaseModels
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