using System;

namespace SXM
{
	/// <summary>
	/// Indicates that this method does not modify any transactional objects.
	/// </summary>
	/// 
	[AttributeUsage(AttributeTargets.Method)]
	public class ReaderAttribute: Attribute
	{
		public ReaderAttribute()
		{
		}
	}
}
