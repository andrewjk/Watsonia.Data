using System;
using System.Data.Common;

namespace Watsonia.Data.Query
{
	/// <summary>
	/// A <see cref="FieldReader"/> that reads from a <see cref="DbDataReader"/>.
	/// </summary>
	internal class DbFieldReader : FieldReader
	{
		private readonly DbDataReader _reader;

		public DbFieldReader(DbDataReader reader)
		{
			_reader = reader;
			Init();
		}

		protected override int FieldCount
		{
			get
			{
				return _reader.FieldCount;
			}
		}

		protected override Type GetFieldType(int ordinal)
		{
			return _reader.GetFieldType(ordinal);
		}

		protected override bool IsDBNull(int ordinal)
		{
			return _reader.IsDBNull(ordinal);
		}

		protected override T GetValue<T>(int ordinal)
		{
			var value = _reader.GetValue(ordinal);
			if (value is T typedValue)
			{
				return typedValue;
			}

			var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
			return (T)TypeHelper.ChangeType(value, targetType);
		}

		protected override byte GetByte(int ordinal)
		{
			return _reader.GetByte(ordinal);
		}

		protected override char GetChar(int ordinal)
		{
			return _reader.GetChar(ordinal);
		}

		protected override DateTime GetDateTime(int ordinal)
		{
			return _reader.GetDateTime(ordinal);
		}

		protected override decimal GetDecimal(int ordinal)
		{
			return _reader.GetDecimal(ordinal);
		}

		protected override double GetDouble(int ordinal)
		{
			return _reader.GetDouble(ordinal);
		}

		protected override float GetSingle(int ordinal)
		{
			return _reader.GetFloat(ordinal);
		}

		protected override Guid GetGuid(int ordinal)
		{
			return _reader.GetGuid(ordinal);
		}

		protected override short GetInt16(int ordinal)
		{
			return _reader.GetInt16(ordinal);
		}

		protected override int GetInt32(int ordinal)
		{
			return _reader.GetInt32(ordinal);
		}

		protected override long GetInt64(int ordinal)
		{
			return _reader.GetInt64(ordinal);
		}

		protected override string GetString(int ordinal)
		{
			return _reader.GetString(ordinal);
		}
	}
}
