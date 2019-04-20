using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;

namespace SXM
{
	/// <summary>
	// A transactional object encapsulates an ICloneable object. 
	/// </summary>
	public struct SynchState
	{
		object start;
		
		public SynchState(ICloneable obj)
		{
			start = new Locator(obj);
		}

		/// <summary>
		/// Open object with intention to modify.
		/// </summary>
		/// <returns>Private version of object</returns>
		public ICloneable OpenWrite() 
		{
			XState me  = XAction.XState;       // my transaction
			Locator oldLocator = (Locator)this.start;
			XState writer = oldLocator.writer;
			if (writer == me) 
			{
				return oldLocator.newObject;
			}

			IContentionManager manager = XAction.Manager; // my manager
        
			// allocate successor
			Locator newLocator = new Locator();
			newLocator.writer = me;
			ICloneable oldVersion = null;
			ICloneable newVersion = null;
			while (true) 
			{
			retry:
				// read locator
				switch (writer.State) 
				{
					case XStates.ACTIVE:
						// abort or wait?
						manager.ResolveConflict(me, writer);
						goto retry;			// try again
					case XStates.COMMITTED:
						oldVersion = newLocator.oldObject = oldLocator.newObject;
						break;
					case XStates.ABORTED:
						oldVersion = newLocator.oldObject = oldLocator.oldObject;
						break;
					default:
						throw new PanicException("Unexpected transaction state: {0}", writer.State);
				}
				switch (me.State) 
				{
					case XStates.ABORTED:
						throw new AbortedException();
					case XStates.COMMITTED:
						return oldVersion;
					case XStates.ACTIVE:
						// check for read conflicts
						bool readConflict = false;
						foreach (XState reader in oldLocator) 
						{
							if (reader.State == XStates.ACTIVE && reader != me)
							{
								manager.ResolveConflict(me, reader);
								readConflict = true;
							}	
							if (readConflict) 
							{
								goto retry;
							}
						}
						// no conflict
						newVersion = newLocator.newObject = (ICloneable)oldVersion.Clone();
						// try to install
						if ((Locator)(Interlocked.CompareExchange(
							ref start,
							newLocator,
							oldLocator)) == oldLocator)
						{
							return newVersion;
						}
						break;
					default:
						throw new PanicException("Unknown transaction state: {0}", me.State);
				}
				oldLocator = (Locator)this.start;
				writer = oldLocator.writer;
			}
		}

		/// <summary>
		/// Open object with intention to read.
		/// </summary>
		/// <returns>Shared version of object</returns>
		public ICloneable OpenRead() 
		{        
			XState me = XAction.XState;  // my transaction
			Locator oldLocator = (Locator)this.start;
			XState writer = oldLocator.writer;
			if (writer == me) 
			{
				return oldLocator.newObject;
			}
			foreach (XState reader in oldLocator) 
			{
				if (reader == me)
				{
					return oldLocator.newObject;
				}
			}
			IContentionManager manager = XAction.Manager; // my manager
        
			// allocate successor
			Locator newLocator = new Locator();
			newLocator.writer = XState.COMMITTED;
			while (true) 
			{
			retry:
				// read locator
				ICloneable version = null;
				switch (writer.State) 
				{
					case XStates.ACTIVE:
						// abort or wait?
						manager.ResolveConflict(me, writer);
						goto retry;			// try again
					case XStates.COMMITTED:
						version = oldLocator.newObject;
						break;
					case XStates.ABORTED:
						version = oldLocator.oldObject;
						break;
					default:
						throw new PanicException("Unknown transaction state: {0}", writer.State);
				}
				switch (me.State) 
				{
					case XStates.ABORTED:
						throw new AbortedException();
					case XStates.COMMITTED:
						return version;
					case XStates.ACTIVE:
						newLocator.newObject = version;
						newLocator.reader = me;
						if (oldLocator.reader != null) 
						{
							newLocator.prevReader = oldLocator;
						} 
						else 
						{
							newLocator.prevReader = null;
						}
						if (Interlocked.CompareExchange(
							ref start,
							newLocator,
							oldLocator) == oldLocator)
						{
							return version;
						}
						break;
					default:
						throw new PanicException("Unknown transaction state: {0}", me.State);
				}
				 oldLocator = (Locator)this.start;
				 writer = oldLocator.writer;
			}
		}


		bool ConflictsWithReader(XState me, ICollection readers)
		{
			foreach (XState reader in readers) 
			{
				if (reader.State == XStates.ACTIVE && reader != me) 
					return true;
			}
			return false;
		}
	}
}
