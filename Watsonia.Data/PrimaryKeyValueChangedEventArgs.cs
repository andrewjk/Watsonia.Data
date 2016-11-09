using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data
{
	public sealed class PrimaryKeyValueChangedEventArgs : EventArgs
	{
		public object Value
		{
			get;
			private set;
		}

		public PrimaryKeyValueChangedEventArgs(object value)
		{
			this.Value = value;
		}
	}
}
