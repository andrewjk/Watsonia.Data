using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data
{
	/// <summary>
	/// A cache of items in the database, keyed by table name.
	/// </summary>
	/// <seealso cref="System.Collections.Concurrent.ConcurrentDictionary{System.String, Watsonia.Data.ItemCache}" />
	public class DatabaseItemCache : ConcurrentDictionary<string, ItemCache>
	{
	}
}
