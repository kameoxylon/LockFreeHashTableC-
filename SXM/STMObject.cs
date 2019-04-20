/*
 IMPORTANT:  READ BEFORE DOWNLOADING, COPYING, INSTALLING OR USING.
 By downloading, copying, installing or using the software you agree to this license.
 If you do not agree to this license, do not download, install, copy or use the software.

 Brown University License Agreement
 Copyright (c) 2003, Brown University 
 All rights reserved.

 Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met: 
  •	Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer. 
  •	Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution. 
  •	The name of Brown University may not be used to endorse or promote products derived from this software without specific prior written permission. 
 THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE REGENTS OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;

namespace SXM
{
	/// <summary>
	// A transactional object encapsulates an ICloneable object. 
	/// </summary>
	public struct STMObject
	{
		object start;
		LocalDataStoreSlot readCache;  // thread-local read 
		LocalDataStoreSlot writeCache;  // thread-local write copy 
		
		public STMObject(ICloneable obj, LocalDataStoreSlot readCache, LocalDataStoreSlot writeCache)
		{
			start = new Locator(obj);
			this.readCache = readCache;
			this.writeCache = writeCache;
			// Do not cache objects if not in transaction!
			if (Xaction.IsActive) 
			{
				Xaction.AddToCache(readCache, obj);
				Xaction.AddToCache(writeCache, obj);
			}
		}

		/// <summary>
		/// Open object with intention to modify.
		/// </summary>
		/// <returns>Private version of object</returns>
		public ICloneable OpenWrite() 
		{
			XStatus me  = Xaction.XStatus;       // my transaction
			IContentionManager manager = Xaction.Manager; // my manager
        
			// allocate successor
			Locator newLocator = new Locator();
			newLocator.writer = me;
			ICloneable oldVersion = null;
			ICloneable newVersion = null;
			while (true) 
			{
			retry:
				// read locator
				Locator oldLocator = (Locator)this.start;
				XStatus writer = oldLocator.writer;
				switch (writer.State) 
				{
					case XState.ACTIVE:
					case XState.WAITING:
						// abort or wait?
						manager.ResolveConflict(me, writer);
						goto retry;			// try again
					case XState.COMMITTED:
						oldVersion = newLocator.oldObject = oldLocator.newObject;
						break;
					case XState.ABORTED:
						oldVersion = newLocator.oldObject = oldLocator.oldObject;
						break;
					default:
						Panic.SystemError("Unknown transaction state: {0}", me.State);
						break;	// not reached
				}
				switch (me.State) 
				{
					case XState.ABORTED:
						throw new AbortedException();
					case XState.COMMITTED:
						return oldVersion;
					case XState.ACTIVE:
						// check for read conflicts
						if (ConflictsWithReader(me, oldLocator.readers))
						{
							manager.ResolveConflict(me, oldLocator.readers);
							goto retry;
						}	
						// no conflict
						newVersion = newLocator.newObject = (ICloneable)oldVersion.Clone();
						// try to install
						if ((Locator)(Interlocked.CompareExchange(
							ref start,
							newLocator,
							oldLocator)) == oldLocator)
						{
#if DEBUG
							Xaction.myMisses++;
#endif
							Xaction.AddToCache(readCache, newVersion);
							Xaction.AddToCache(writeCache, newVersion);
							return newVersion;
						}
						break;
					default:
						Panic.SystemError("Unknown transaction state: {0}", me.State);
						break;	// not reached
				}
			}
		}

		/// <summary>
		/// Open object with intention to read.
		/// </summary>
		/// <returns>Shared version of object</returns>
		public ICloneable OpenRead() 
		{        
			XStatus me = Xaction.XStatus;  // my transaction
			IContentionManager manager = Xaction.Manager; // my manager
        
			// allocate successor
			Locator newLocator = new Locator();
			newLocator.writer = XStatus.COMMITTED;
			while (true) 
			{
			retry:
				// read locator
				Locator oldLocator = (Locator)this.start;
				XStatus writer = oldLocator.writer;
				ICloneable version = null;
				switch (writer.State) 
				{
					case XState.ACTIVE:
					case XState.WAITING:
						// abort or wait?
						manager.ResolveConflict(me, writer);
						goto retry;			// try again
					case XState.COMMITTED:
						version = oldLocator.newObject;
						break;
					case XState.ABORTED:
						version = oldLocator.oldObject;
						break;
					default:
						Panic.SystemError("Unknown transaction state: {0}", writer.State);
						break;	// not reached
				}
				switch (me.State) 
				{
					case XState.ABORTED:
						throw new AbortedException();
					case XState.COMMITTED:
						return version;
					case XState.SNAP:
						Xaction.snapshots.Add(this, version);
						return version;
					case XState.ACTIVE:
						newLocator.newObject = version;
						// copy live readers into new locator
						newLocator.readers.Clear();
						newLocator.readers.Add(me);
						foreach (XStatus t in oldLocator.readers) 
						{
							if (t.State == XState.ACTIVE)
								newLocator.readers.Add(t);
						}
						if (Interlocked.CompareExchange(
							ref start,
							newLocator,
							oldLocator) == oldLocator)
						{
#if DEBUG
							Xaction.myMisses++;
#endif
							Xaction.AddToCache(readCache, version);
							return version;
						}
						break;
					default:
						Panic.SystemError("Unknown transaction state: {0}", me.State);
						break;	// not reached
				}
			}
		}

		/// <summary>
		/// If object hasn't changed since snapshot, upgrade to read access.
		/// </summary>
		/// <returns>Shared version of object</returns>
		public bool Upgrade(ICloneable snapshot) 
		{        
			XStatus me = Xaction.XStatus;  // my transaction
			IContentionManager manager = Xaction.Manager; // my manager
        
			// allocate successor
			Locator newLocator = new Locator();
			newLocator.writer = XStatus.COMMITTED;
			while (true) 
			{
			retry:
				// read locator
				Locator oldLocator = (Locator)this.start;
				XStatus writer = oldLocator.writer;
				switch (writer.State) 
				{
					case XState.ACTIVE:
						// abort or wait?
						manager.ResolveConflict(me, writer);
						goto retry;			// try again
					case XState.COMMITTED:
						if (snapshot != oldLocator.newObject)
							return false;
						break;
					case XState.ABORTED:
						if (snapshot != oldLocator.oldObject)
							return false;
						break;
					default:
						Panic.SystemError("Unknown transaction state: {0}", me.State);
						break;	// not reached
				}
				switch (me.State) 
				{
					case XState.ABORTED:
						throw new AbortedException();
					case XState.ACTIVE:
						newLocator.newObject = snapshot;
						// copy live readers into new locator
						newLocator.readers.Clear();
						newLocator.readers.Add(me);
						foreach (XStatus t in oldLocator.readers) 
						{
							if (t.State == XState.ACTIVE)
								newLocator.readers.Add(t);
						}
						if (Interlocked.CompareExchange(
							ref start,
							newLocator,
							oldLocator) == oldLocator)
						{
#if DEBUG
							Xaction.myMisses++;
#endif
							Xaction.AddToCache(readCache, snapshot);
							return true;
						}
						break;
					default:
						Panic.SystemError("Unknown transaction state: {0}", me.State);
						break;	// not reached
				}
			}
		}

		bool ConflictsWithReader(XStatus me, ICollection readers)
		{
			foreach (XStatus reader in readers) 
			{
				if (reader.State == XState.ACTIVE && reader != me) 
					return true;
			}
			return false;
		}
	}
}
