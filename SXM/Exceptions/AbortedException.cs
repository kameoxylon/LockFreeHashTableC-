using System;

namespace SXM
{
	/// <summary>
	/// Thrown when transaction discovers it has been aborted.
	/// </summary>
	public class AbortedException : Exception
	{
		private string cause;
		public AbortedException()	{}
		public AbortedException(string s)
		{
			cause = s;
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
