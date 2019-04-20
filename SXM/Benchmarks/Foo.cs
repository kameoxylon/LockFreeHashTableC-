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
using SXM;
using System.Collections;

namespace SXM
{
	/// <summary>
	/// Linked-list integer set benchmark
	/// </summary>
	public class Foo: SXM.IntSetBenchmark
	{
		#region members
		Node root;
		#endregion
		#region constructors
		public Foo() 
		{
			int size = 1;
			this.factory = new XObjectFactory(typeof(Node));
			this.root = (Node)factory.Create();
			this.root.Value = Int32.MinValue;
			this.root.Next   = (Node)factory.Create();
			this.root.Next.Value = Int32.MaxValue;
			Random random = new Random(this.GetHashCode());
			while (size < INITIAL_SIZE) 
			{
				if ((bool)Insert(random.Next()))
					size++;
			}
			// initialize transaction objects
			insertXaction = new Xaction.Delegate1(this.Insert);
			removeXaction = new Xaction.Delegate1(this.Remove);
			containsXaction = new Xaction.Delegate1(this.Contains);
		}
		#endregion

		#region IntSetBenchmark overrides
		/// <summary>
		/// Add a value to the set.
		/// </summary>
		/// <param name="v">Value to add.</param>
		/// <returns>Whether that value was not already there.</returns>
		public override object Insert(object _v) 
		{
			int v = (int)_v;
			Node newNode = (Node)factory.Create((int)_v);
			Node prevNode = this.root;
			Node currNode = prevNode.Next;
			while (currNode.Value < v) 
			{
				prevNode = currNode;
				currNode = prevNode.Next;
			}
			if (currNode.Value == v) 
			{
				return false;
			}
			else
			{
				newNode.Next = prevNode.Next;
				prevNode.Next = newNode;
				return true;
			}
		}
  
		/// <summary>
		/// Test whether a value is in a set.
		/// </summary>
		/// <param name="v">Value to test.</param>
		/// <returns>Whether that value is present.</returns>
		public override object Contains(object _v) 
		{
			int v = (int)_v;
			Neighborhood hood = Find(v);
			return hood.currNode != null;
		}  

		/// <summary>
		/// Remove a value from the set.
		/// </summary>
		/// <param name="v">Value to remove.</param>
		/// <returns>Whether that value was not already there.</returns>
		public override object Remove(object _v) 
		{
			int v = (int)_v;
			Neighborhood hood = Find(v);
			if (hood.currNode == null) 
			{
				return false;
			} 
			else 
			{
				Node prevNode = hood.prevNode;
				prevNode.Next = hood.currNode.Next;
				return true;
			}
		}

		/// <summary>
		/// Enumerate over values in order
		/// </summary>
		/// <returns></returns>
		public override IEnumerator GetEnumerator() 
		{
			return new Foo.Enumerator(this.root);
		}
		#endregion

		#region helper methods and classes
		/// <summary>
		/// Inner class for enumerating over transactions.
		/// </summary>
		class Enumerator: IEnumerator
		{
			Node node;
			Node root;
			public Enumerator(Node node) 
			{
				this.root = this.node = node;
			}

			public bool MoveNext() 
			{
				node = node.Next;
				return(node != null);
			}

			public object Current 
			{
				get 
				{
					return(node.Value);
				}
			}

			public void Reset() 
			{
				this.node = this.root;
			}
		}

		public class Neighborhood 
		{
			public Node prevNode;
			public Node currNode;
			public Neighborhood(Node prevNode, Node currNode) 
			{
				this.prevNode = prevNode;
				this.currNode = currNode;
			}
			public Neighborhood(Node prevNode) 
			{
				this.prevNode = prevNode;
			}
		}

		/// <summary>
		/// This method does most of the work. It finds the list entries containing
		/// the value, or, if the value is absent, the closest entries.
		/// </summary>
		/// <param name="v">Search for this value</param>
		/// <returns>Structure containing closest list entries.</returns>
		Foo.Neighborhood Find(int v) 
		{
			Node prevNode = this.root;
			Node currNode = prevNode.Next;
			while (currNode.Value < v) 
			{
				prevNode = currNode;
				currNode = prevNode.Next;
			}
			if (currNode.Value == v)
				return new Neighborhood(prevNode, currNode);
			else
				return new Neighborhood(prevNode);
		}
		#endregion

		#region Atomic classes
		[Atomic]
			public class Node
		{
			private int value;
			private Node next;
      
			public virtual int Value
			{
				get
				{
					return value;
				}
				set
				{
					this.value = value;
				}
			}
			public virtual Node Next
			{
				get
				{
					return next;
				}
				set
				{
					this.next = value;
				}
			}
		}
		#endregion
	}
}