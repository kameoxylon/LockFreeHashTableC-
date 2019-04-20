using System;

namespace SXM
{
	/// <summary>
	/// Thrown when proxy factory cannot continue
	/// </summary>
	public class ProxyException : Exception
	{
		private string cause;
		public ProxyException(Type type, string format, params object[] args)
		{
			string header = "TYPE ERROR:\nXObjectFactory could not create proxy for class {0}\n";
			cause = String.Format(header, type);
			cause += String.Format(format, args);
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
