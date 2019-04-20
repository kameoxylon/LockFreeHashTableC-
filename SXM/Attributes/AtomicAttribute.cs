using System;

namespace SXM
{
	/// <summary>
	/// Indicates that this class is intended to shared among transactions.
	/// </summary>
	/// 
	[AttributeUsage(AttributeTargets.Class)]
	public class AtomicAttribute: Attribute
	{
		public AtomicAttribute()
		{
		}
	}
}
