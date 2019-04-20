using System;

namespace SXM
{
	/// <summary>
	/// Indicates that this method may modify transactional objects.
	/// </summary>
	/// 
	[AttributeUsage(AttributeTargets.Method)]
	public class WriterAttribute: Attribute
	{
		public WriterAttribute()
		{
		}
	}
}
