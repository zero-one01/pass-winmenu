using System;
using System.Collections.Generic;
using PassWinmenu.ExternalPrograms;

namespace PassWinmenu.Utilities
{
	public struct Option<T> : IEquatable<Option<T>>
	{
		public T Value { get; }
		public bool HasValue { get; }

		public Option(T value, bool hasValue)
		{
			Value = value;
			HasValue = hasValue;
		}

		public override bool Equals(object obj)
		{
			try
			{
				var converted = (Option<T>)obj;
				return Equals(converted);
			}
			catch (InvalidCastException)
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			var hashCode = 1816676634;
			hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(Value);
			hashCode = hashCode * -1521134295 + HasValue.GetHashCode();
			return hashCode;
		}

		public static bool operator ==(Option<T> left, Option<T> right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Option<T> left, Option<T> right)
		{
			return !(left == right);
		}

		public bool Equals(Option<T> other)
		{
			if(!HasValue && !other.HasValue)
			{
				return true;
			}
			if(HasValue && other.HasValue)
			{
				return Value.Equals(other.Value);
			}
			return false;
		}
	}

	public static class Option
	{
		public static Option<T> FromNullable<T>(T value) => new Option<T>(value, value == null);
	}
}
