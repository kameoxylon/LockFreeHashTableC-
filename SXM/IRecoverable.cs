using System;

namespace SXM
{
	/// <summary>
	/// Objects that can be backed up and restored
	/// </summary>
	public interface IRecoverable
	{
		/// <summary>
		/// Create a backup copy
		/// </summary>
		void Backup();
		/// <summary>
		/// Restore backup copy
		/// </summary>
		void Recover();
	}
}
