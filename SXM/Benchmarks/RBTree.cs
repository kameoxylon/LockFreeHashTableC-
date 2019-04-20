using System;
using System.Threading;
using System.Collections;

namespace SXM
{
	/// <summary>
	/// Red-BLACK tree benchmark.
	/// </summary>
	public class RBTree : IntSetBenchmark
	{

		public enum Color {BLACK, RED};

		#region members
		protected RBNode root;
		//  sentinelNode is convenient way of indicating a leaf node.
		public static RBNode sentinelNode; 
		#endregion

		#region constructor
		///<remark>
		/// Initializes the tree shared by all the threads.
		/// </remark>
		public RBTree() 
		{
			// factory for creating transactional RBNodes
			this.factory = new XObjectFactory(typeof(RBNode));
			// set up the sentinel node. the sentinel node is the key to a successfull
			// implementation and for understanding the red-black tree properties.
			sentinelNode        = (RBNode)factory.Create();
			sentinelNode.Left   = null;
			sentinelNode.Right  = null;
			sentinelNode.Parent = null;
			sentinelNode.Color  = Color.BLACK;
			root                = sentinelNode;
			this.root.Value = Int32.MinValue;
			this.root.Color = Color.BLACK;
			int size = 0;
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
		/// <remarks>
		/// Inserts an element into the integer set, if it is not already present.
		/// </remarks>
		public override object Insert(params object[] _v)
		{
			int key = (int)_v[0];
			
			// traverse tree - find where node belongs
			RBNode node	= (RBNode)factory.Create();			// create new node
			RBNode temp	= root;

			while(temp != sentinelNode)
			{	// find Parent
				node.Parent	= temp;
				if ( key == temp.Value) 
				{
					return false;
				} 
				else if (key > temp.Value) 
				{
					temp = temp.Right;
				} 
				else 
				{
					temp = temp.Left;
				}
			}
			
			// setup node
			node.Value			=	key;
			node.Left           =   sentinelNode;
			node.Right          =   sentinelNode;

			// insert node into tree starting at parent's location
			if(node.Parent != null)	
			{
				if (node.Value > node.Parent.Value) 
				{
					node.Parent.Right = node;
				}
				else
					node.Parent.Left = node;
			}
			else
				root = node;					// first node added

			RestoreAfterInsert(node);           // restore red-black properities
			return true;
		}
  
		/// <remarks>
		/// Tests whether item is present.
		/// </remarks>
		/// <returns> whether the item was present</returns>
		public override object Contains(params object[] _v)
		{
			int key = (int)_v[0];
			
			RBNode node = root;     // begin at root
            
			// traverse tree until node is found
			while(node != sentinelNode)
			{
				if (key == node.Value) 
				{
					return true;
				} 
				else if (key < node.Value) 
				{
					node = node.Left;
				} 
				else 
				{
					node = node.Right;
				}
			}
			return false;			
		}
  
		/// <remarks>
		/// Remove item if present.
		/// </remarks>
		/// <returns> whether the item was removed</returns>
		public override  object Remove(params object[] _v) 
		{
			int key = (int)_v[0];
		
			// find node
			RBNode node;

			node = root;
			while(node != sentinelNode)
			{
				if (key == node.Value) 
				{
					break;
				} 
				else if (key < node.Value) 
				{
					node = node.Left;
				} 
				else 
				{
					node = node.Right;
				}
			}

			if(node == sentinelNode)
				return false;				// key not found

			Delete(node);
			return true;
		}
		#endregion

		#region helper methods and classes
		
		///<summary>
		/// GetEnumerator
		/// return an enumerator that returns the tree nodes in order
		///<summary>
		public override IEnumerator GetEnumerator()
		{
			// elements is simply a generic name to refer to the 
			// data objects the nodes contain
			return new Enumerator(root);      
		}
		
		public override void Validate() 
		{
			base.Validate();
			if (sentinelNode != root && Color.BLACK != root.Color)
				Console.WriteLine("Tree root is wrong color!");
			int blackNodes = CountBlackNodes(root);
			RecursiveValidate(root, blackNodes, 0);
		}

		/// <summary>
		/// Deleta a node
		/// </summary>
		/// <param name="z">node to be deleted</param>
		private void Delete(RBNode z)
		{
			// A node to be deleted will be: 
			//		1. a leaf with no children
			//		2. have one child
			//		3. have two children
			// If the deleted node is red, the red black properties still hold.
			// If the deleted node is black, the tree needs rebalancing

			RBNode x = (RBNode)factory.Create();	// work node to contain the replacement node
			RBNode y;					// work node 

			// find the replacement node (the successor to x) - the node one with 
			// at *most* one child. 
			if(z.Left == sentinelNode || z.Right == sentinelNode) 
				y = z;						// node has sentinel as a child
			else 
			{
				// z has two children, find replacement node which will 
				// be the leftmost node greater than z
				y = z.Right;				        // traverse right subtree	
				while(y.Left != sentinelNode)		// to find next node in sequence
					y = y.Left;
			}

			// at this point, y contains the replacement node. it's content will be copied 
			// to the valules in the node to be deleted

			// x (y's only child) is the node that will be linked to y's old parent. 
			if(y.Left != sentinelNode)
				x = y.Left;					
			else
				x = y.Right;					

			// replace x's parent with y's parent and
			// link x to proper subtree in parent
			// this removes y from the chain
			x.Parent = y.Parent;
			if(y.Parent != null)
				if(y == y.Parent.Left)
					y.Parent.Left = x;
				else
					y.Parent.Right = x;
			else
				root = x;			// make x the root node

			// copy the values from y (the replacement node) to the node being deleted.
			// note: this effectively deletes the node. 
			if(y != z) 
			{
				z.Value	= y.Value;
			}

			if(y.Color == Color.BLACK)
				RestoreAfterDelete(x);
		}

		///<summary>
		/// RestoreAfterDelete
		/// Deletions from red-black trees may destroy the red-black 
		/// properties. Examine the tree and restore. Rotations are normally 
		/// required to restore it
		///</summary>
		private void RestoreAfterDelete(RBNode x)
		{
			// maintain Red-Black tree balance after deleting node 			

			RBNode y;

			while(x != root && x.Color == Color.BLACK) 
			{
				if(x == x.Parent.Left)			// determine sub tree from parent
				{
					y = x.Parent.Right;			// y is x's sibling 
					if(y.Color == Color.RED) 
					{	// x is black, y is red - make both black and rotate
						y.Color			= Color.BLACK;
						x.Parent.Color	= Color.RED;
						RotateLeft(x.Parent);
						y = x.Parent.Right;
					}
					if(y.Left.Color == Color.BLACK && 
						y.Right.Color == Color.BLACK) 
					{	// children are both black
						y.Color = Color.RED;		// change parent to red
						x = x.Parent;					// move up the tree
					} 
					else 
					{
						if(y.Right.Color == Color.BLACK) 
						{
							y.Left.Color	= Color.BLACK;
							y.Color			= Color.RED;
							RotateRight(y);
							y				= x.Parent.Right;
						}
						y.Color			= x.Parent.Color;
						x.Parent.Color	= Color.BLACK;
						y.Right.Color	= Color.BLACK;
						RotateLeft(x.Parent);
						x = root;
					}
				} 
				else 
				{	// right subtree - same as code above with right and left swapped
					y = x.Parent.Left;
					if(y.Color == Color.RED) 
					{
						y.Color			= Color.BLACK;
						x.Parent.Color	= Color.RED;
						RotateRight (x.Parent);
						y = x.Parent.Left;
					}
					if(y.Right.Color == Color.BLACK && 
						y.Left.Color == Color.BLACK) 
					{
						y.Color = Color.RED;
						x		= x.Parent;
					} 
					else 
					{
						if(y.Left.Color == Color.BLACK) 
						{
							y.Right.Color	= Color.BLACK;
							y.Color			= Color.RED;
							RotateLeft(y);
							y				= x.Parent.Left;
						}
						y.Color			= x.Parent.Color;
						x.Parent.Color	= Color.BLACK;
						y.Left.Color	= Color.BLACK;
						RotateRight(x.Parent);
						x = root;
					}
				}
			}
			x.Color = Color.BLACK;
		}

		private void RestoreAfterInsert(RBNode x)
		{   
			RBNode y;

			// maintain red-black tree properties after adding x
			while(x != root && x.Parent.Color == Color.RED)
			{
				// Parent node is .Colored red; 
				if(x.Parent == x.Parent.Parent.Left)	// determine traversal path			
				{										// is it on the Left or Right subtree?
					y = x.Parent.Parent.Right;			// get uncle
					if(y!= null && y.Color == Color.RED)
					{	// uncle is red; change x's Parent and uncle to black
						x.Parent.Color			= Color.BLACK;
						y.Color					= Color.BLACK;
						// grandparent must be red. Why? Every red node that is not 
						// a leaf has only black children 
						x.Parent.Parent.Color	= Color.RED;	
						x						= x.Parent.Parent;	// continue loop with grandparent
					}	
					else
					{
						// uncle is black; determine if x is greater than Parent
						if(x == x.Parent.Right) 
						{	// yes, x is greater than Parent; rotate Left
							// make x a Left child
							x = x.Parent;
							RotateLeft(x);
						}
						// no, x is less than Parent
						x.Parent.Color			= Color.BLACK;	// make Parent black
						x.Parent.Parent.Color	= Color.RED;		// make grandparent black
						RotateRight(x.Parent.Parent);					// rotate right
					}
				}
				else
				{	// x's Parent is on the Right subtree
					// this code is the same as above with "Left" and "Right" swapped
					y = x.Parent.Parent.Left;
					if(y!= null && y.Color == Color.RED)
					{
						x.Parent.Color			= Color.BLACK;
						y.Color					= Color.BLACK;
						x.Parent.Parent.Color	= Color.RED;
						x						= x.Parent.Parent;
					}
					else
					{
						if(x == x.Parent.Left)
						{
							x = x.Parent;
							RotateRight(x);
						}
						x.Parent.Color			= Color.BLACK;
						x.Parent.Parent.Color	= Color.RED;
						RotateLeft(x.Parent.Parent);
					}
				}																													
			}
			root.Color = Color.BLACK;		// root should always be black
		}

		///<summary>
		/// RotateLeft
		/// Rebalance the tree by rotating the nodes to the left
		///</summary>
		public void RotateLeft(RBNode x)
		{
			// pushing node x down and to the Left to balance the tree. x's Right child (y)
			// replaces x (since y > x), and y's Left child becomes x's Right child 
			// (since it's < y but > x).
            
			RBNode y = x.Right;			// get x's Right node, this becomes y

			// set x's Right link
			x.Right = y.Left;					// y's Left child's becomes x's Right child

			// modify parents
			if(y.Left != sentinelNode) 
				y.Left.Parent = x;				// sets y's Left Parent to x

			if(y != sentinelNode)
				y.Parent = x.Parent;			// set y's Parent to x's Parent

			if(x.Parent != null)		
			{	// determine which side of it's Parent x was on
				if(x == x.Parent.Left)			
					x.Parent.Left = y;			// set Left Parent to y
				else
					x.Parent.Right = y;			// set Right Parent to y
			} 
			else 
				root = y;						// at root, set it to y

			// link x and y 
			y.Left = x;							// put x on y's Left 
			if(x != sentinelNode)						// set y as x's Parent
				x.Parent = y;		
		}

		///<summary>
		/// RotateRight
		/// Rebalance the tree by rotating the nodes to the right
		///</summary>
		public void RotateRight(RBNode x)
		{
			// pushing node x down and to the Right to balance the tree. x's Left child (y)
			// replaces x (since x < y), and y's Right child becomes x's Left child 
			// (since it's < x but > y).
            
			RBNode y = x.Left;			// get x's Left node, this becomes y

			// set x's Right link
			x.Left = y.Right;					// y's Right child becomes x's Left child

			// modify parents
			if(y.Right != sentinelNode) 
				y.Right.Parent = x;				// sets y's Right Parent to x

			if(y != sentinelNode)
				y.Parent = x.Parent;			// set y's Parent to x's Parent

			if(x.Parent != null)				// null=root, could also have used root
			{	// determine which side of its Parent x was on
				if(x == x.Parent.Right)			
					x.Parent.Right = y;			// set Right Parent to y
				else
					x.Parent.Left = y;			// set Left Parent to y
			} 
			else 
				root = y;						// at root, set it to y

			// link x and y 
			y.Right = x;						// put x on y's Right
			if(x != sentinelNode)				// set y as x's Parent
				x.Parent = y;		
		}	

		private int CountBlackNodes(RBNode root) 
		{
			if (sentinelNode == root)
				return 0;
			int me = (root.Color == Color.BLACK) ? 1 : 0;
			RBNode left = (sentinelNode == root.Left)
				? sentinelNode 
				: root.Left;
			return me + CountBlackNodes(left);
		}

		private int Count(RBNode root) 
		{
			if (root == sentinelNode)
				return 0;
			return 1 + Count(root.Left) + Count(root.Right);

		}

		private void RecursiveValidate(RBNode root, int blackNodes, int soFar) 
		{
			// Empty sub-tree is vacuously OK
			if (sentinelNode == root)
				return;

			Color rootcolor = root.Color;
			soFar += ((Color.BLACK == rootcolor) ? 1 : 0);
			root.Marked = true;

			// Check left side
			RBNode left = root.Left;
			if (sentinelNode != left) 
			{
				if (left.Color == Color.RED && rootcolor == Color.RED) 
				{
					Console.WriteLine("Two consecutive red nodes!");
					return;
				}
				if (left.Value >= root.Value) 
				{
					Console.WriteLine("Tree values out of order!");
					return;
				}
				if (left.Marked) 
				{
					Console.WriteLine("Cycle in tree structure!");
					return;
				}
				RecursiveValidate(left, blackNodes, soFar);
			}

			// Check right side
			RBNode right = root.Right;
			if (sentinelNode != right) 
			{
				if (right.Color == Color.RED && rootcolor == Color.RED) 
				{
					Console.WriteLine("Two consecutive red nodes!");
					return;
				}
				if (right.Value <= root.Value) 
				{
					Console.WriteLine("Tree values out of order!");
					return;
				}
				if (right.Marked) 
				{
					Console.WriteLine("Cycle in tree structure!");
					return;
				}
				RecursiveValidate(right, blackNodes, soFar);
			}

			// Check black node count
			if (sentinelNode == root.Left || sentinelNode == root.Right) 
			{
				if (soFar != blackNodes) 
				{
					Console.WriteLine("Variable number of black nodes to leaves!");
					return;
				}
			}

			// Everything checks out if we get this far.
			return;
		}

		public class Enumerator: IEnumerator
		{
			#region IEnumerator Members
			private Stack stack;
			private RBNode root;
			private bool first;
            
			public Enumerator(RBNode root) 
			{
				this.root = root;
				this.stack = new Stack();
				RBNode node = root;
				while(node != sentinelNode)
				{
					stack.Push(node);
					node = node.Left;
				}
				first = true;
			}

			public void Reset()
			{
				stack.Clear();
				stack.Push(root);
			}

			public object Current
			{
				get
				{
					return ((RBNode)stack.Peek()).Value;
				}
			}

			public bool MoveNext()
			{
				// empty tree?
				if (stack.Count == 0)
					return false;
				// first call is special case
				if (first) 
				{
					first = false;
					return true;
				}
				RBNode node = (RBNode) stack.Peek();
				// If I have a right child, move there and descend left
				if (node.Right != sentinelNode) 
				{
					node = node.Right;
					while(node != sentinelNode)
					{
						stack.Push(node);
						node = node.Left;
					}
				}
				else
				{
					RBNode seen = sentinelNode;
					while (node.Right == seen) 
					{
						seen = (RBNode)stack.Pop();
						if (stack.Count == 0)
							return false;
						else
							node = (RBNode)stack.Peek();
					}
				}
				return true;
			}

			#endregion

		}
		#endregion

		#region atomic classes

		[Atomic]
		public class RBNode : ICloneable 
		{
			/** creates new tree node **/
			private int value;
			private Color color;
			private bool marked;
			private RBNode parent;
			private RBNode left;
			private RBNode right;
	

			public RBNode() 
			{
				value = 0;
				color = Color.RED;
				parent = null;
				left = null;
				right = null;
			}

			// Thse properties are the only way to access the fields

			public virtual int Value
			{
				get
				{
					return this.value;
				}
				set
				{
					this.value = value;
				}
			}

			public virtual bool Marked
			{
				get
				{
					return this.marked;
				}
				set
				{
					this.marked = value;
				}
			}

			public virtual Color Color
			{
				get
				{
					return this.color;
				}
				set
				{
					this.color = value;
				}
			}

			public virtual RBNode Parent
			{
				get
				{
					return this.parent;
				}
				set
				{
					this.parent = value;
				}
			}
			public virtual RBNode Right
			{
				get
				{
					return this.right;
				}
				set
				{
					this.right = value;
				}
			}
			public virtual RBNode Left
			{
				get
				{
					return this.left;
				}
				set
				{
					this.left = value;
				}
			}

			public object Clone() 
			{
				RBNode newNode = new RBNode();
				newNode.value = this.value;
				newNode.color = this.color;
				newNode.marked = this.marked;
				newNode.parent = this.parent;
				newNode.left = this.left;
				newNode.right = this.right;
				return newNode;
			}
		}
		#endregion
	}
}