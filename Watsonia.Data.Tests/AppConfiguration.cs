using System;
using System.Collections.Generic;
using System.Text;

namespace Watsonia.Data.Tests
{
	// HACK: How are you supposed to have config settings in .NET Core now?
	internal static class AppConfiguration
    {
		private static string _connectionString;
		public static string ConnectionString
		{
			get
			{
				if (string.IsNullOrEmpty(_connectionString))
				{
					string fileName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\App.secret.config";
					_connectionString = System.IO.File.ReadAllLines(fileName)[0];
				}
				return _connectionString;
			}
		}
    }
}
