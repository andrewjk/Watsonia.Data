using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.EventArgs;

namespace Watsonia.Data
{
	public delegate void PrimaryKeyValueChangedEventHandler(object sender, PrimaryKeyValueChangedEventArgs e);
}
