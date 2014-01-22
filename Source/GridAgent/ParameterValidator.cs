using System;

namespace GridAgent
{
	/// <summary>
	/// Utility class for validating method parameters.
	/// </summary>
	public static class ParameterValidator
	{
		/// <summary>
		/// Ensures the specified value is not null.
		/// </summary>
		/// <typeparam name="T">The type of the value.</typeparam>
		/// <param name="value">The value to test.</param>
		/// <param name="parameterName">Name of the parameter.</param>
		/// <returns>The specified value.</returns>
		/// <exception cref="ArgumentNullException">Occurs if the specified value 
		/// is <code>null</code>.</exception>
		public static T EnsureNotNull<T>(T value, string parameterName) where T : class
		{
			if (value == null)
			{
				throw new ArgumentNullException(parameterName);
			}

			return value;
		}

		/// <summary>
		/// Ensures the specified value is not <code>null</code> or empty (a zero length string).
		/// </summary>
		/// <param name="value">The value to test.</param>
		/// <param name="parameterName">Name of the parameter.</param>
		/// <returns>The specified value.</returns>
		/// <exception cref="ArgumentNullException">Occurs if the specified value 
		/// is <code>null</code> or empty (a zero length string).</exception>
		public static string EnsureNotNullOrEmpty(string value, string parameterName)
		{
			if (value == null)
			{
				throw new ArgumentNullException(parameterName);
			}

			if (value.Length < 1)
			{
				throw new ArgumentException("Parameter should not be an empty string.", parameterName);
			}

			return value;
		}
	}
}
