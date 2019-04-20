using System;
using System.Collections;
using System.Diagnostics;

namespace SXM
{
	/// <summary>
	/// Keeps track of old and new object versions,
	/// along with latest accessing transaction(s).
	/// </summary>
	public class Locator: IEnumerable
	{
		// Transaction that wrote this version, if any
		public XState writer;

		// Transaction that read this version, if any
		public XState reader;

		// previous reader, if any
		public Locator prevReader;

		// Value of object if creating transaction aborts.
		public ICloneable oldObject;

		// Value of object if creating transaction commits.
		public ICloneable newObject;

		/// <summary>
		/// Principal constructor.
		/// </summary>
		public Locator()
		{
			this.writer    = XState.COMMITTED;
			this.newObject = null;
			this.oldObject = null;
		}

		public Locator(ICloneable obj) : this()
		{
			this.newObject = obj;
		}

		public override string ToString() 
		{
			return "Locator[" + writer.ToString() + "]";
		}
		#region IEnumerable Members

		public IEnumerator GetEnumerator()
		{
			return new Enumerator(this);
		}

		#endregion
		private class Enumerator : IEnumerator
		{
			#region IEnumerator Members
			Locator start;
			Locator current;
			Locator next;

			public Enumerator(Locator locator) 
			{
				if (locator.reader != null) 
				{
					start = locator;
				}
				next = start;
			}

			public void Reset()
			{
				current = null;
				next = start;
			}

			public object Current
			{
				get
				{
					return current.reader;
				}
			}

			public bool MoveNext()
			{
				current = next;
				if (next != null) 
				{
					next = next.prevReader;
				}
				// splice out inactive readers
				while (next != null && next.reader.State != XStates.ACTIVE) 
				{
					next = next.prevReader;
				}
				// benevolent side-effect
				if (current != null) 
				{
					current.prevReader = next;
				}
				return current != null;
			}

			#endregion

		}

	}
}