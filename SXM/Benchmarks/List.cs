using System;
using SXM;
using System.Collections;

namespace SXM
{
	/// <summary>
	/// Linked-list integer set benchmark
	/// </summary>
	public class List: SXM.IntSetBenchmark
	{
		#region members
		Node root;
		#endregion
		#region constructors
		public List() 
		{
			int size = 1;
			this.factory = new XObjectFactory(typeof(Node));
			this.root = (Node)factory.Create(Int32.MinValue);
			this.root.Next   = (Node)factory.Create(Int32.MaxValue);
			Random random = new Random(this.GetHashCode());
			while (size < INITIAL_SIZE) 
			{
				if ((bool)Insert(random.Next()))
					size++;
			}
			// initialize transaction objects
			insertXStart = new XStart(this.Insert);
			removeXStart = new XStart(this.Remove);
			containsXStart = new XStart(this.Contains);
		}
		#endregion

		#region IntSetBenchmark overrides
		/// <summary>
		/// Add a value to the set.
		/// </summary>
		/// <param name="v">Value to add.</param>
		/// <returns>Whether that value was not already there.</returns>
		public override object Insert(params object[] _v) 
		{
			int v = (int)_v[0];
			Node newNode = (Node)factory.Create(v);

			Neighborhood hood = Find(v);
			if (hood.currNode != null) 
			{
				return false;
			} 
			else 
			{
				Node prevNode = hood.prevNode;
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
		public override object Contains(params object[] _v) 
		{
			int v = (int)_v[0];
			Neighborhood hood = Find(v);
			return hood.currNode != null;
		}  

		/// <summary>
		/// Remove a value from the set.
		/// </summary>
		/// <param name="v">Value to remove.</param>
		/// <returns>Whether that value was not already there.</returns>
		public override object Remove(params object[] _v) 
		{
			int v = (int)_v[0];
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
			return new List.Enumerator(this.root);
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
		List.Neighborhood Find(int v) 
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
      
			public Node(int value) 
			{
				this.value = value;
			}
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