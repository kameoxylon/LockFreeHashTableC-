using System;
namespace SXM
{
	/// <summary>
	/// Thrown when computation should shut down gracefully.
	/// </summary>
	public class GracefulException : Exception
	{
		private string cause;
		public GracefulException()	{}
		public GracefulException(string s)
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
