using System;

namespace SXM
{
	/// <summary>
	/// Thrown by user code when to block transaction
	/// </summary>
	public class RetryException : Exception
	{
		public RetryException()	{}
	}
}
