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

namespace SXM
{
	/// <summary>
	/// Summary description for Snapshots.
	/// </summary>
	public class Snapshots: CollectionBase
	{	
		private static int INITIAL_SIZE = 64;
		private int size;
		private int next;
		private Pair[] items;

		/// <summary>
		/// Create empty snapshot set.
		/// </summary>
		public Snapshots()
		{
			size = INITIAL_SIZE;
			items = new Pair[INITIAL_SIZE];
			next = 0;
		}

		public bool Add(STMObject stmObject, ICloneable snapshot) 
		{
			// overflow?
			if (next == size) 
			{
				Pair[] newItems = new Pair[2 * size];
				items.CopyTo(newItems, 0);
				items = newItems;
				size = 2 * size;
			}
			items[next++] = new Pair(stmObject, snapshot);
			return true;
		}
		
		/// <summary>
		/// Empty out this snasphot set.
		/// </summary>
		public new void Clear()
		{
			next = 0;
		}

		/// <summary>
		/// Get an enumerator for this Read Set.
		/// </summary>
		/// <returns></returns>
		public new MyEnumerator GetEnumerator() 
		{
			return new MyEnumerator(this);
		}
		/// <summary>
		/// Inner class for enumerating over transactions.
		/// </summary>
		public class MyEnumerator 
		{
			int nIndex;
			Snapshots collection;
			public MyEnumerator(Snapshots coll) 
			{
				collection = coll;
				nIndex = -1;
			}

			public bool MoveNext() 
			{
				nIndex++;
				return(nIndex < collection.next);
			}

			public Pair Current 
			{
				get 
				{
					return(collection.items[nIndex]);
				}
			}
		}
		public class Pair 
		{
			public STMObject stmObject;
			public ICloneable snapshot;
			public Pair(STMObject stmObject, ICloneable snapshot) 
			{
				this.stmObject = stmObject;
				this.snapshot = snapshot;
			}
		}

	}
}
