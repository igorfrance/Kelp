namespace Kelp.Extensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	/// <summary>
	/// Defines extension methods for integers.
	/// </summary>
	public static class Int32Extensions
	{
		/// <summary>
		/// Returns <c>true</c> if the string equals any one of the supplied values.
		/// </summary>
		/// <param name="subject">The string subject being tested.</param>
		/// <param name="values">The values to test for.</param>
		/// <returns><c>true</c> if the string contains any one of the supplied values; otherwise <c>false</c>.</returns>
		public static bool EqualsAnyOf(this int subject, params int[] values)
		{
			if (values == null || values.Length == 0)
				return false;

			foreach (int test in values)
			{
				if (subject == test)
					return true;
			}

			return false;

		}
	}
}
