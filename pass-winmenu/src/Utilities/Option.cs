using PassWinmenu.ExternalPrograms;

namespace PassWinmenu.Utilities
{
	public struct Option<T>
	{
		public T Value { get; }
		public bool HasValue { get; }

		private Option(T value, bool hasValue)
		{
			Value = value;
			HasValue = hasValue;
		}

		public static Option<T> FromNullable(T value) => new Option<T>(value, value == null);
	}
}
