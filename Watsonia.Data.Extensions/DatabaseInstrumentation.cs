using System;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Watsonia.Data.EventArgs;

namespace Watsonia.Data.Extensions
{
	/// <summary>
	/// Wires a database up to logging and OpenTelemetry-compatible tracing.
	/// </summary>
	/// <remarks>
	/// Command executions produce <see cref="Activity"/> spans on the "Watsonia.Data" source.
	/// The integration is implemented over the database's existing events, so the core library
	/// keeps no dependency on Microsoft.Extensions.
	/// </remarks>
	public sealed class DatabaseInstrumentation : IDisposable
	{
		/// <summary>
		/// The activity source for database operations.
		/// </summary>
		public static readonly ActivitySource ActivitySource = new ActivitySource("Watsonia.Data");

		private readonly Watsonia.Data.Database _database;
		private readonly ILogger _logger;
		private readonly ConditionalWeakTable<DbCommand, Activity> _activities = new ConditionalWeakTable<DbCommand, Activity>();
		private bool _disposed;

		private DatabaseInstrumentation(Watsonia.Data.Database database, ILogger logger)
		{
			_database = database;
			_logger = logger;

			_database.BeforeExecuteCommand += this.OnBeforeExecuteCommand;
			_database.AfterExecuteCommand += this.OnAfterExecuteCommand;
			_database.BeforeExecute += this.OnBeforeExecute;
			_database.AfterExecute += this.OnAfterExecute;
			_database.BeforeUpdateDatabase += this.OnBeforeUpdateDatabase;
			_database.AfterUpdateDatabase += this.OnAfterUpdateDatabase;
			_database.AfterSave += this.OnAfterSave;
		}

		/// <summary>
		/// Attaches logging and tracing to the supplied database.
		/// </summary>
		/// <param name="database">The database.</param>
		/// <param name="loggerFactory">The logger factory (optional).</param>
		/// <returns>The instrumentation, which should be disposed when the database is no longer used.</returns>
		public static DatabaseInstrumentation Attach(Watsonia.Data.Database database, ILoggerFactory loggerFactory = null)
		{
			if (database == null)
			{
				throw new ArgumentNullException(nameof(database));
			}

			var logger = loggerFactory != null
				? loggerFactory.CreateLogger("Watsonia.Data")
				: (ILogger)NullLogger.Instance;

			return new DatabaseInstrumentation(database, logger);
		}

		private void OnBeforeExecuteCommand(object sender, System.EventArgs e)
		{
			if (e is CommandEventArgs args && args.Command != null)
			{
				var activity = ActivitySource.StartActivity("db.command", ActivityKind.Client);
				if (activity != null)
				{
					activity.SetTag("db.system", "watsonia.data");
					if (!string.IsNullOrEmpty(args.Command.CommandText))
					{
						activity.SetTag("db.statement", args.Command.CommandText);
					}

					_activities.Add(args.Command, activity);
				}

				_logger.LogDebug("Executing: {CommandText}", args.Command.CommandText);
			}
		}

		private void OnAfterExecuteCommand(object sender, System.EventArgs e)
		{
			if (e is CommandEventArgs args && args.Command != null)
			{
				if (_activities.TryGetValue(args.Command, out var activity))
				{
					activity.Dispose();
					_activities.Remove(args.Command);
				}
			}
		}

		private void OnBeforeExecute(object sender, System.EventArgs e)
		{
			if (e is StatementEventArgs args && args.Statement != null)
			{
				_logger.LogDebug("Executing statement: {Statement}", args.Statement);
			}
		}

		private void OnAfterExecute(object sender, System.EventArgs e)
		{
		}

		private void OnBeforeUpdateDatabase(object sender, System.EventArgs e)
		{
			_logger.LogInformation("Updating database schema");
		}

		private void OnAfterUpdateDatabase(object sender, System.EventArgs e)
		{
			_logger.LogInformation("Database schema updated");
		}

		private void OnAfterSave(object sender, System.EventArgs e)
		{
			_logger.LogDebug("Saved item");
		}

		/// <summary>
		/// Stops logging and tracing for the database.
		/// </summary>
		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_database.BeforeExecuteCommand -= this.OnBeforeExecuteCommand;
			_database.AfterExecuteCommand -= this.OnAfterExecuteCommand;
			_database.BeforeExecute -= this.OnBeforeExecute;
			_database.AfterExecute -= this.OnAfterExecute;
			_database.BeforeUpdateDatabase -= this.OnBeforeUpdateDatabase;
			_database.AfterUpdateDatabase -= this.OnAfterUpdateDatabase;
			_database.AfterSave -= this.OnAfterSave;
		}
	}
}
