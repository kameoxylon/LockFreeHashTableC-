using System;

namespace SXM
{
	/// <summary>
	/// Thrown when internal system error discovered
	/// </summary>
	public class PanicException : Exception
	{
		private string cause;
		public PanicException(string format, params object[] args)
		{
			cause = String.Format(format, args);
		}

		public string Cause
		{
			get
			{
				return cause;
			}
		}
	}
}
