/*
 * John Lambert
 * Network Routing Algorithm (via path through graph)
 *------------------------------------------------------------------
 * The overall time complexity of the algorithm is O( ( |V| + |E| ) * log( |V| ) ) time.
 * I execute deleteMin |V| times, and we do decreaseKey |E| times.
 * Making the queue required doing O(1) work, exactly, |V| times.
 * 
 * In my descriptions of my deleteMin, decreaseKey, and insertNodeThreeTuple      * functions, ie the functions that govern how I access and change my binary heap, * I show that each function has at most O( log n) time complexity, where n is the * number of * elements present in the binary heap.

Each one of these three algorithms takes O(log n) time since we sift down: if parent is bigger than either child, we swap it with the smaller child, and repeat. The number of swaps is at most the height of the tree, the binary heap.
 *
 * I employ the following data structures in my implementation of Dijkstras via
 * use of a binary heap:
 *
         // node #  index in priority queue    PRIORITY QUEUE
        //     1     [ 4 ]                      1 [ item with lowest priority]
        //     2     [ 1 ]                      2 [ Child1 ]
        //     3     [ 5 ]                      3 [ Child2 ]
        //     4     [ 3 ]                      4 [ ... ] 
        //     5     [ 2 ]                      .
        //     6     [ 6 ]                      .
*              .        .
*
* We use a struct to hold the relevant information about each node in the directed graph.
* My space complexity for the binary heap is about O(n) because I only have a number of
* arrays of length n to store all of my data.  Yes, the adjacency list seems O( n * n),
* but it is not because it is really O( 3 * n). We will write that as O( n).
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

namespace NetworkRouting
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void generateButton_Click(object sender, EventArgs e)
        {
            int randomSeed = int.Parse(randomSeedBox.Text);
            int size = int.Parse(sizeBox.Text);

            Random rand = new Random(randomSeed);
            seedUsedLabel.Text = "Random Seed Used: " + randomSeed.ToString();

            this.adjacencyList = generateAdjacencyList(size, rand);
            List<PointF> points = generatePoints(size, rand);
            resetImageToPoints(points);
            this.points = points;
            startNodeIndex = -1;
            stopNodeIndex = -1;
            sourceNodeBox.Text = "";
            targetNodeBox.Text = "";
        }

        // Generates the distance matrix.  Values of -1 indicate a missing edge.  Loopbacks are at a cost of 0.
        private const int MIN_WEIGHT = 1;
        private const int MAX_WEIGHT = 100;
        private const double PROBABILITY_OF_DELETION = 0.35;
        private const int NUMBER_OF_ADJACENT_POINTS = 3;

        private List<HashSet<int>> generateAdjacencyList(int size, Random rand)
        {
            List<HashSet<int>> adjacencyList = new List<HashSet<int>>();

            for (int i = 0; i < size; i++)
            {
                HashSet<int> adjacentPoints = new HashSet<int>();
                while (adjacentPoints.Count < 3)
                {
                    int point = rand.Next(size);
                    if (point != i) adjacentPoints.Add(point);
                }
                adjacencyList.Add(adjacentPoints);
            }

            return adjacencyList;
        }

        private List<PointF> generatePoints(int size, Random rand)
        {
            List<PointF> points = new List<PointF>();
            for (int i = 0; i < size; i++)
            {
                points.Add(new PointF((float)(rand.NextDouble() * pictureBox.Width), (float)(rand.NextDouble() * pictureBox.Height)));
            }
            return points;
        }

        private void resetImageToPoints(List<PointF> points)
        {
            pictureBox.Image = new Bitmap(pictureBox.Width, pictureBox.Height);
            Graphics graphics = Graphics.FromImage(pictureBox.Image);
            foreach (PointF point in points)
            {
                graphics.DrawEllipse(new Pen(Color.Blue), point.X, point.Y, 2, 2);
            }

            this.graphics = graphics;
            pictureBox.Invalidate();
        }

        // These variables are instantiated after the "Generate" button is clicked
        private List<PointF> points = new List<PointF>();
        private Graphics graphics;
        private List<HashSet<int>> adjacencyList;

        private struct NodeThreeTuple
        {
            public double dist;
            public int nodeNumber;
            public int prevNode;
        }

        private NodeThreeTuple[] pQueueAsBinaryHeap;
        private NodeThreeTuple[] pQueueAsBinaryHeapSinglePath;
        private int currentPQueueCount;
        private int[] pointerArray;
        private int[] pointerArraySinglePath;
        private double dijkstraOnePathTime;
        private NodeThreeTuple[] dequeuedItemsSinglePath;
        private NodeThreeTuple[] dequeuedItems;

        // Use this to generate routing tables for every node

        /*
         * This function find the least cost path from the source to the destination node 
         * using both All-paths and One-path.  
         * We draw the shortest path starting from the source node and following all 
         * intermediate nodes until the destination node. 
         * Note that "startNodeIndex" is my start point , "stopNodeIndex" is my stop points
        */
        private void solveButton_Click(object sender, EventArgs e)
        {
            singlePathAlgorithm(e);
            dequeuedItems = new NodeThreeTuple[points.Count];      

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            makePriorityQueue(); //Make Binary Heap (O( |v| ) to insert everything
            decreaseKey(startNodeIndex, 0); // set the startNodeIndex's cost to be zero
            pQueueAsBinaryHeap[pointerArray[startNodeIndex]].prevNode = -999999; // THE BEGINNING of linked list
            // add all of the node-tuples to the binary heap, all with cost = Double.MAX_VALUE


            while (!priorityQueueIsEmpty())
            {
                NodeThreeTuple newlyDequeuedItem = deleteMin();   // delete the root of the binary heap (the node-tuple with lowest cost) 
                dequeuedItems[newlyDequeuedItem.nodeNumber] = newlyDequeuedItem;
                HashSet<int> nodesThatStemFromThisNode = adjacencyList[newlyDequeuedItem.nodeNumber];
                // set previous
                // bubble it down(Replace it with smallest child if child’s cost is less than its own cost) (see array structure of binary heap in top - right of board, and its representation as tree on right-middle). Draw a line between previous and this node , with associated cost.

                foreach (int outgoingEdgeToThisNode in nodesThatStemFromThisNode)  // FOR THAT NODE, Loop across every one of its outdegree - edges 

                {
                    if (pointerArray[outgoingEdgeToThisNode] == -1) continue; // we've already dequeued this item before
                    double storedPathCostToThisNextNode = pQueueAsBinaryHeap[pointerArray[outgoingEdgeToThisNode]].dist;
                    double squaredXdirection = Math.Pow((points[outgoingEdgeToThisNode].X - points[newlyDequeuedItem.nodeNumber].X), 2);
                    double squaredYdirection = Math.Pow((points[outgoingEdgeToThisNode].Y - points[newlyDequeuedItem.nodeNumber].Y), 2);
                    double edgeLengthFromDequeuedToNext = Math.Sqrt(squaredXdirection + squaredYdirection);

                    if (storedPathCostToThisNextNode > (newlyDequeuedItem.dist + (edgeLengthFromDequeuedToNext)))
                    {
                        decreaseKey(outgoingEdgeToThisNode, (newlyDequeuedItem.dist + (edgeLengthFromDequeuedToNext)));   // make this that node's new distance

                        // set previous to the dequeued node
                        pQueueAsBinaryHeap[pointerArray[outgoingEdgeToThisNode]].prevNode = newlyDequeuedItem.nodeNumber;
                    }
                }

                //See if we can find a shorter distance to 
                // IF WE find a shorter distance, decreaseKey by indexing pointer array to find position in binary heap
                // replacing the cost in the node-tuple struct for that node
            }

            int currentNodesPrevious = -1;
            int currentNodeInLinkedList = stopNodeIndex;
            currentNodesPrevious = dequeuedItems[currentNodeInLinkedList].prevNode;
            while (currentNodesPrevious != -999999)         // LOOP AT MOST |V| times
            {
                // draw the line
                Pen blackPen = new Pen(Color.Black, 2);
                graphics.DrawLine(blackPen, points[currentNodeInLinkedList], points[currentNodesPrevious]);
                drawCostAsAString(e, points[currentNodeInLinkedList], points[currentNodesPrevious] );
                Refresh();
                currentNodeInLinkedList = currentNodesPrevious; // traverse linked list
                currentNodesPrevious = dequeuedItems[currentNodeInLinkedList].prevNode; // O(1) operations to index into an array
            }
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}.{1:0000}", ts.Seconds, ts.Milliseconds );
            this.allTimeBox.Text = elapsedTime;

            double percentDiffInTimes = ( (ts.TotalMilliseconds- dijkstraOnePathTime ) / ts.TotalMilliseconds );
            percentDiffInTimes *= 100;
            this.differenceBox.Text = percentDiffInTimes.ToString("#####.####");
        }


        // O(1) complesxity to draw each edge.
        private void drawCostAsAString(EventArgs e, PointF pointOne, PointF pointTwo)
        {
            double squaredXdirection = Math.Pow((pointOne.X - pointTwo.X), 2);
            double squaredYdirection = Math.Pow((pointOne.Y - pointTwo.Y), 2);
            double edgeLengthBetweenTwoPoints = Math.Sqrt(squaredXdirection + squaredYdirection);

            // Create string to draw.
            String drawString = edgeLengthBetweenTwoPoints.ToString("#####.##");

            // Create font and brush.
            Font drawFont = new Font("Arial", 12);
            SolidBrush drawBrush = new SolidBrush(Color.Black);

            // Create point for upper-left corner of drawing.
            PointF drawPoint = new PointF( (pointOne.X + pointTwo.X)/ 2, (pointOne.Y + pointTwo.Y)/ 2);

            // Set format of string.
            StringFormat drawFormat = new StringFormat();

            // Draw string to screen.
            this.graphics.DrawString(drawString, drawFont, drawBrush, drawPoint, drawFormat);
    }

    /*
    * Create a binary-heap priority queue
    * We put all nodes in with the exact same priority right now
    * This is O( |V| ) because we do O(1) insertions for every node in the graph
    */
    private void makePriorityQueue()
    {
        pQueueAsBinaryHeap = new NodeThreeTuple[points.Count + 1];
        pointerArray = new int[points.Count];
        currentPQueueCount = 0; // Count = 0 since nothing is in there yet

        for (int i = 0; i < points.Count; i++)
        {
            NodeThreeTuple currentNode;
            currentNode.nodeNumber = i;
            currentNode.dist = Double.MaxValue;
            currentNode.prevNode = -1;

            pQueueAsBinaryHeap[i + 1] = currentNode;
            pointerArray[i] = (i + 1); // update pointer array
            currentPQueueCount++;
        }
    }



        /*
        * Removes and returns the highest priority value.  If multiple
        * entries in the queue have the same priority, those values are
        * dequeued in the same order in which they were enqueued.
        *
        * This function removes and returns the highest priority value.
        *
        * If there is only one element in the priority queue, then we will save the name of
        * that element, and decrement count so that the program acts as if the array were empty.
        *
        * Place the leaf node-tuple (From lowest level, farthest to the right) as THE NEW ROOT
        *
        * In every other case, we move the last item in the array to the first index (We bubble that element up).
        * Now, we will need to bubble it down to its correct position in the array.  We will keep track of this element
        * and we'll keep track of the children it is attached to. We will compare them over and over again, making sure
        * that the children have larger priority than the parent.  Once we have passed over the entire array, we stop.
        *
        * This takes O(log n) time since we sift down: if parent is bigger than either child, we swap
        * it with the smaller child, and repeat.  The number of swaps is at most the height of the tree,
        * which is the floor function of log 2 (n), when there are n elements in the binary heap.
        */
        private NodeThreeTuple deleteMin()
    {

        // DELETE ROOT (OR MORE EXACTLY, WE WILL REWRITE OVER IT LATER, SO REMEMBER WHAT ELEMENT WE ARE DEQUEUEING)
        NodeThreeTuple myElementToBeDequeued = pQueueAsBinaryHeap[1];
        if (currentPQueueCount == 1)
        {
            currentPQueueCount--;

            //update pointer array
            return myElementToBeDequeued;
        }
        NodeThreeTuple tempParentHolder;
        NodeThreeTuple tempChildHolder;

        int nodeNumberOfDequeued = myElementToBeDequeued.nodeNumber;
        pointerArray[nodeNumberOfDequeued] = -1; // this guy is now absent from pQueue 

        //BUBBLE EVERYTHING UP
        //PUT ELEMENT FROM BOTTOM ROW, LEFTMOST, ON THE TOP (When looking at tree)
        //PUT ELEMENT IN LAST ARRAY POSITION IN 1ST INDEX (when looking at the array)
        pQueueAsBinaryHeap[1] = pQueueAsBinaryHeap[currentPQueueCount]; //OR COUNT

        int nodeNumberOfNewRoot = pQueueAsBinaryHeap[1].nodeNumber;
        pointerArray[nodeNumberOfNewRoot] = 1;
        int parentIndex = 1;
        int childIndex1 = (parentIndex) * 2;
        int childIndex2 = ((parentIndex) * 2) + 1;
        currentPQueueCount--;
        if (childIndex2 <= currentPQueueCount)
        {
            if ((pQueueAsBinaryHeap[parentIndex].dist == pQueueAsBinaryHeap[childIndex1].dist) &&
                (pQueueAsBinaryHeap[parentIndex].dist == pQueueAsBinaryHeap[childIndex2].dist))
            {
                return myElementToBeDequeued; // No need to do anything, everything is good
            }
        }

        while (parentIndex < currentPQueueCount) // WE GO UNTIL WE ARE AT A LEAF (THE LEAF BECOMES THE PARENT, AND VIA COUNT WE SEE, STOP!)
        {
            //THERE ARE 3 POSSIBLE CASES
            //I HAVE NO CHILDREN
            if ((childIndex1 > currentPQueueCount))
            {
                break;
            }
            //I HAVE 1 CHILD
            if ((childIndex2 > currentPQueueCount))
            {
                if (pQueueAsBinaryHeap[childIndex1].dist < pQueueAsBinaryHeap[parentIndex].dist)
                {
                    tempParentHolder = pQueueAsBinaryHeap[parentIndex];
                    tempChildHolder = pQueueAsBinaryHeap[childIndex1];
                    pQueueAsBinaryHeap[parentIndex] = tempChildHolder;
                    pQueueAsBinaryHeap[childIndex1] = tempParentHolder;

                    // update the pointer array if we bubbled the root down
                    pointerArray[pQueueAsBinaryHeap[parentIndex].nodeNumber] = parentIndex;
                    pointerArray[pQueueAsBinaryHeap[childIndex1].nodeNumber] = childIndex1;
                    }
                else
                {
                    break;
                }
            }

            //I HAVE TWO CHILDREN
            //TAKE SMALLER OF TWO CHILDREN IF ITS GREATER THAN ITS CHILDREN
            //CHECK TO SEE IF PARENT, CHILD ON RIGHT OR CHILD ON LEFT IS LARGER
            else if( (pQueueAsBinaryHeap[childIndex1].dist < pQueueAsBinaryHeap[parentIndex].dist) && (pQueueAsBinaryHeap[childIndex1].dist < pQueueAsBinaryHeap[childIndex2].dist)  ) // CHILD 1 is the smaller of the two children
            {   
                tempParentHolder = pQueueAsBinaryHeap[parentIndex];  //bubble child up one level
                tempChildHolder = pQueueAsBinaryHeap[childIndex1];   //swap with its parent
                pQueueAsBinaryHeap[parentIndex] = tempChildHolder;
                pQueueAsBinaryHeap[childIndex1] = tempParentHolder;

                pointerArray[pQueueAsBinaryHeap[parentIndex].nodeNumber] = parentIndex;   // update pointer array
                pointerArray[pQueueAsBinaryHeap[childIndex1].nodeNumber] = childIndex1;
                
                //UPDATE PARENT FOR CHILD
                parentIndex = childIndex1;
                childIndex1 = (parentIndex) * 2;
                childIndex2 = ((parentIndex) * 2) + 1;
            }
            else if ( (pQueueAsBinaryHeap[childIndex2].dist < pQueueAsBinaryHeap[parentIndex].dist) && (pQueueAsBinaryHeap[childIndex2].dist < pQueueAsBinaryHeap[childIndex1].dist) )  // child2 is the smaller of the two children
            {
                tempParentHolder = pQueueAsBinaryHeap[parentIndex];   //bubble child up one level
                tempChildHolder = pQueueAsBinaryHeap[childIndex2];   //swap with its parent
                pQueueAsBinaryHeap[parentIndex] = tempChildHolder;
                pQueueAsBinaryHeap[childIndex2] = tempParentHolder;

                pointerArray[pQueueAsBinaryHeap[parentIndex].nodeNumber] = parentIndex;   // update pointer array
                pointerArray[pQueueAsBinaryHeap[childIndex2].nodeNumber] = childIndex2;
                
                //UPDATE PARENT FOR CHILD
                parentIndex = childIndex2;
                childIndex1 = (parentIndex) * 2;
                childIndex2 = ((parentIndex) * 2) + 1;
            }
            else // if all of the children are greater in size than the parent, we can quit now and return the dequeued item
            {
                break;
            }
        }
        return myElementToBeDequeued;
    }




    /*
     * This function indexes into the binary heap, decreases the distance value
     * of a node, and that bubbles up that element as far as necessary.
     * The function has time complexity O( log n ) because we perform at most
     * one swap at each level of the tree, and the binary tree has log n levels,
     * where n is the number of elements contained inside of it.
    */
    private void decreaseKey(int nodeNumber, double newDist)
    {
        int positionInPQueue = pointerArray[nodeNumber]; // index into pointer array to find 
        pQueueAsBinaryHeap[positionInPQueue].dist = newDist;
        // Bubble Up potentially if cost is less than the parent
        // Bubble up all the way to the root if necessary

        int currentIndex = positionInPQueue;
        NodeThreeTuple tempParentHolder;
        NodeThreeTuple tempChildHolder;

        while (pQueueAsBinaryHeap[currentIndex].dist < pQueueAsBinaryHeap[currentIndex / 2].dist)
        {
            tempChildHolder = pQueueAsBinaryHeap[currentIndex];
            tempParentHolder = pQueueAsBinaryHeap[currentIndex / 2];

            //swap them, ie update child index to be that of the parent
            pQueueAsBinaryHeap[currentIndex / 2] = tempChildHolder;
            pQueueAsBinaryHeap[currentIndex] = tempParentHolder;

            // update the pointer arrays
            pointerArray[nodeNumber] = (currentIndex / 2);

            int nodeNumberOfMovedObject = tempParentHolder.nodeNumber;
            pointerArray[nodeNumberOfMovedObject] = currentIndex;

            currentIndex = (currentIndex / 2);
            if (currentIndex == 1) break;
        }
    }

    /*
    * Returns true if the priority queue contains no elements.
    */
    private bool priorityQueueIsEmpty()
    {
        return (currentPQueueCount == 0);
    }


        /*
         * Dijkstra's algorithm with a twist (with an Insert node and goal node).
         * We put in the start node, put in its children, while!= goal node, follow traditional Dijkstra algorithm above.
        */
        private void singlePathAlgorithm(EventArgs e)
    {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            dequeuedItemsSinglePath = new NodeThreeTuple[points.Count];
            pointerArraySinglePath = new int[points.Count];
            for ( int i = 0; i < points.Count; i++ )
            {
                pointerArraySinglePath[i] = -100; // MARK THESE AS NEVER HAVING BEEN SEEN BEFORE
            }

            pQueueAsBinaryHeapSinglePath = new NodeThreeTuple[points.Count + 1]; 
            // THE CAPACITY OF THE PriorityQueue is points.Count() + 1, since we start at index = 1.

            currentPQueueCount = 0; // there is nothing in there yet

            // INSERT ONLY ONE POINT INTO THE PRIORITY QUEUE

            // set all as -100 if they have never been on queue
            // once they've been on the queue, we mark them as -1

            NodeThreeTuple startNode;
            startNode.nodeNumber = startNodeIndex;
            startNode.dist = 0;
            startNode.prevNode = -999999; // THIS BACKPOINTER VALUE IS ARBITARY AND DENOTES THAT WE ARE AT THE BEGINNING OF THE LINKED LIST

            insertNodeThreeTuple(startNode);
            decreaseKeySinglePath(startNodeIndex, 0); // set the startNodeIndex's cost to be zero

            while (!priorityQueueIsEmpty())
            {
                NodeThreeTuple newlyDequeuedItem = deleteMinSinglePath();
                dequeuedItemsSinglePath[newlyDequeuedItem.nodeNumber] = newlyDequeuedItem;

                if (newlyDequeuedItem.nodeNumber == stopNodeIndex) break;
                HashSet<int> nodesThatStemFromThisNode = adjacencyList[newlyDequeuedItem.nodeNumber];

                // set previous
                // delete the root of the binary heap (the node-tuple with lowest cost) 
                // Place the leaf node-tuple (From lowest level, farthest to the right) as THE NEW ROOT
                // bubble it down(Replace it with smallest child if child’s cost is less than its own cost) (see array structure of binary heap in top - right of board, and its representation as tree on right-middle). Draw a line between previous and this node , with associated cost.

                foreach (int outgoingEdgeToThisNode in nodesThatStemFromThisNode)
                {
                    if (pointerArraySinglePath[outgoingEdgeToThisNode] == -1) continue; // we've already dequeued this item before
                    
                    if (pointerArraySinglePath[outgoingEdgeToThisNode] == -100) // node never before seen, not in priority queue yet
                    {
                        // insert it
                        NodeThreeTuple neverBeforeSeenNode;

                        double squaredXdirection = Math.Pow((points[outgoingEdgeToThisNode].X - points[newlyDequeuedItem.nodeNumber].X), 2);
                        double squaredYdirection = Math.Pow((points[outgoingEdgeToThisNode].Y - points[newlyDequeuedItem.nodeNumber].Y), 2);
                        double edgeLengthFromDequeuedToNext = Math.Sqrt(squaredXdirection + squaredYdirection);

                        neverBeforeSeenNode.nodeNumber = outgoingEdgeToThisNode;
                        neverBeforeSeenNode.prevNode = newlyDequeuedItem.nodeNumber;
                        neverBeforeSeenNode.dist = (newlyDequeuedItem.dist + (edgeLengthFromDequeuedToNext));
                        insertNodeThreeTuple(neverBeforeSeenNode);
                    }
                    else
                    {
                        double storedPathCostToThisNextNode = pQueueAsBinaryHeapSinglePath[pointerArraySinglePath[outgoingEdgeToThisNode]].dist;
                        double squaredXdirection = Math.Pow((points[outgoingEdgeToThisNode].X - points[newlyDequeuedItem.nodeNumber].X), 2);
                        double squaredYdirection = Math.Pow((points[outgoingEdgeToThisNode].Y - points[newlyDequeuedItem.nodeNumber].Y), 2);
                        double edgeLengthFromDequeuedToNext = Math.Sqrt(squaredXdirection + squaredYdirection);
                        if (storedPathCostToThisNextNode > (newlyDequeuedItem.dist + (edgeLengthFromDequeuedToNext)))
                        {
                            // make this the new distance
                            // decreaseKey with this new distance
                            decreaseKeySinglePath(outgoingEdgeToThisNode, (newlyDequeuedItem.dist + (edgeLengthFromDequeuedToNext)));

                            // set previous to the dequeued node
                            pQueueAsBinaryHeapSinglePath[pointerArraySinglePath[outgoingEdgeToThisNode]].prevNode = newlyDequeuedItem.nodeNumber;
                        }
                    }
                }

                // FOR THAT NODE, Loop across every one of its outdegree - edges 
                //See if we can find a shorter distance to 
                // IF WE find a shorter distance, decreaseKey by indexing pointer array to find position in binary heap
                // replacing the cost in the node-tuple struct for that node
            }
            this.pathCostBox.Text = dequeuedItemsSinglePath[stopNodeIndex].dist.ToString("####.######");
            int currentNodesPrevious = -1;
            int currentNodeInLinkedList = stopNodeIndex;
            currentNodesPrevious = dequeuedItemsSinglePath[currentNodeInLinkedList].prevNode;
            while (currentNodesPrevious != -999999)
            {
                // draw the line
                Pen blackPen = new Pen(Color.Black, 2);
                graphics.DrawLine(blackPen, points[currentNodeInLinkedList], points[currentNodesPrevious]);
                drawCostAsAString(e, points[currentNodeInLinkedList], points[currentNodesPrevious]);
                Refresh();
                currentNodeInLinkedList = currentNodesPrevious;
                currentNodesPrevious = dequeuedItemsSinglePath[currentNodeInLinkedList].prevNode;

            }

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:000}.{1:0000}", ts.Seconds, ts.Milliseconds); //Milliseconds/10
            Console.WriteLine("RunTime " + elapsedTime);
            this.oneTimeBox.Text = elapsedTime;
            dijkstraOnePathTime = ts.TotalMilliseconds;
        }



        
        /*
         * Complexity is identical to deleteMin for All paths (identical to other function,
         * except for fact that we operate on difference data structures in this function).
        */
        private NodeThreeTuple deleteMinSinglePath()
        {
           // Substitute the root with a different value
            NodeThreeTuple myElementToBeDequeued = pQueueAsBinaryHeapSinglePath[1];
            if (currentPQueueCount == 1)
            {
                pointerArraySinglePath[myElementToBeDequeued.nodeNumber ] = -1; // this guy is now absent from pQueue 
                currentPQueueCount--;
                return myElementToBeDequeued;
            }
            NodeThreeTuple tempParentHolder;
            NodeThreeTuple tempChildHolder;

            int nodeNumberOfDequeued = myElementToBeDequeued.nodeNumber;
            pointerArraySinglePath[nodeNumberOfDequeued] = -1; // this guy is now absent from pQueue 

            //BUBBLE EVERYTHING UP
            //PUT ELEMENT FROM BOTTOM ROW, LEFTMOST, ON THE TOP (When looking at tree)
            //PUT ELEMENT IN LAST ARRAY POSITION IN 1ST INDEX (when looking at the array)
            pQueueAsBinaryHeapSinglePath[1] = pQueueAsBinaryHeapSinglePath[currentPQueueCount];

            int nodeNumberOfNewRoot = pQueueAsBinaryHeapSinglePath[1].nodeNumber;
            pointerArraySinglePath[nodeNumberOfNewRoot] = 1;
            int parentIndex = 1;
            int childIndex1 = (parentIndex) * 2;
            int childIndex2 = ((parentIndex) * 2) + 1;
            currentPQueueCount--;
            if (childIndex2 <= currentPQueueCount)
            {
                // if they are all the same cost --- NEED TO INCLUDE AN IF STATEMENT, WE ONLY DO THIS IF WE KNOW THERE ARE 2 CHILDREN, OR 1 CHILD
                if ((pQueueAsBinaryHeapSinglePath[parentIndex].dist == pQueueAsBinaryHeapSinglePath[childIndex1].dist) &&
                    (pQueueAsBinaryHeapSinglePath[parentIndex].dist == pQueueAsBinaryHeapSinglePath[childIndex2].dist))
                {
                    return myElementToBeDequeued; // No need to do anything, everything is good
                }
            }

            //CHECK TO SEE IF PARENT, CHILD ON RIGHT OR CHILD ON LEFT IS LARGER

            while (parentIndex < currentPQueueCount) // WE GO UNTIL WE ARE AT A LEAF (THE LEAF BECOMES THE PARENT, AND VIA COUNT WE SEE, STOP!)
            {
                //WHILE PARENT INDEX IS LESS THAN MAXIMUM NUMBER OF ELEMENTS

                //THERE ARE 3 POSSIBLE CASES
                //I HAVE NO CHILDREN
                if ((childIndex1 > currentPQueueCount)) // && (childIndex2 > currentPQueueCount) ) // second check here is redundant
                {
                    break;
                }

                //I HAVE 1 CHILD
                if ((childIndex2 > currentPQueueCount))
                {
                    if (pQueueAsBinaryHeapSinglePath[childIndex1].dist < pQueueAsBinaryHeapSinglePath[parentIndex].dist)
                    {
                        tempParentHolder = pQueueAsBinaryHeapSinglePath[parentIndex];
                        tempChildHolder = pQueueAsBinaryHeapSinglePath[childIndex1];
                        pQueueAsBinaryHeapSinglePath[parentIndex] = tempChildHolder;
                        pQueueAsBinaryHeapSinglePath[childIndex1] = tempParentHolder;

                        // update the pointer array if we bubbled the root down
                        pointerArraySinglePath[pQueueAsBinaryHeapSinglePath[parentIndex].nodeNumber] = parentIndex;
                        pointerArraySinglePath[pQueueAsBinaryHeapSinglePath[childIndex1].nodeNumber] = childIndex1;
                    }
                    else
                    {
                        break;
                    }
                }

                //I HAVE TWO CHILDREN
                //TAKE SMALLER OF TWO CHILDREN IF ITS GREATER THAN ITS CHILDREN
                else if ((pQueueAsBinaryHeapSinglePath[childIndex1].dist < pQueueAsBinaryHeapSinglePath[parentIndex].dist) &&
                    (pQueueAsBinaryHeapSinglePath[childIndex1].dist < pQueueAsBinaryHeapSinglePath[childIndex2].dist)) 
                {
                    // CHILD 1 is the smaller of the two children
                    tempParentHolder = pQueueAsBinaryHeapSinglePath[parentIndex];  //bubble child up one level
                    tempChildHolder = pQueueAsBinaryHeapSinglePath[childIndex1];   //swap with its parent
                    pQueueAsBinaryHeapSinglePath[parentIndex] = tempChildHolder;
                    pQueueAsBinaryHeapSinglePath[childIndex1] = tempParentHolder;

                    pointerArraySinglePath[pQueueAsBinaryHeapSinglePath[parentIndex].nodeNumber] = parentIndex;   // update pointer array
                    pointerArraySinglePath[pQueueAsBinaryHeapSinglePath[childIndex1].nodeNumber] = childIndex1;

                    //UPDATE PARENT FOR CHILD
                    parentIndex = childIndex1;
                    childIndex1 = (parentIndex) * 2;
                    childIndex2 = ((parentIndex) * 2) + 1;
                }
                else if ((pQueueAsBinaryHeapSinglePath[childIndex2].dist < pQueueAsBinaryHeapSinglePath[parentIndex].dist) && 
                    (pQueueAsBinaryHeapSinglePath[childIndex2].dist < pQueueAsBinaryHeapSinglePath[childIndex1].dist))  
                {
                    // child2 is the smaller of the two children
                    tempParentHolder = pQueueAsBinaryHeapSinglePath[parentIndex];   //bubble child up one level
                    tempChildHolder = pQueueAsBinaryHeapSinglePath[childIndex2];   //swap with its parent
                    pQueueAsBinaryHeapSinglePath[parentIndex] = tempChildHolder;
                    pQueueAsBinaryHeapSinglePath[childIndex2] = tempParentHolder;

                    pointerArraySinglePath[pQueueAsBinaryHeapSinglePath[parentIndex].nodeNumber] = parentIndex;   // update pointer array
                    pointerArraySinglePath[pQueueAsBinaryHeapSinglePath[childIndex2].nodeNumber] = childIndex2;

                    //UPDATE PARENT FOR CHILD
                    parentIndex = childIndex2;
                    childIndex1 = (parentIndex) * 2;
                    childIndex2 = ((parentIndex) * 2) + 1;
                }
                else // if all of the children are greater in size than the parent, we can quit now and return the dequeued item
                {
                    break;
                }
            }
            return myElementToBeDequeued;
        }



/*
* Adds node to the bottom of the binary heap / tree (in the first available position), and 
* let it bubble up. That is, if it is smaller than its parent, swap the two and repeat.
* The number of swaps is at most the height of the tree, which is floor (log 2 (n) ), when
* there are n elements in the binary heap.
*    
* Lower priority numbers correspond to higher priorities,
* which means that all priority 1 elements are dequeued before any priority 2 elements.
*
* We use a start index of 1 for the array.  Insertion entails adding the given struct
* to the last spot in the array, and then bubbling it up if necessary.  We rely upon
* two key assumptions: using integer division, we can find a "child" by computing
* 2*(parent's position) or (2*parent's position)+1.  We can find a parent of any
* given child by calculating (i/2).
*
* If parent has identical priority (ie distance), we do not bubble up.
*
* 
*/
void insertNodeThreeTuple(NodeThreeTuple itemToBeInserted)
{

      // or we could just pass in the nodeNumber and create the struct inside of the function
      // NodeThreeTuple itemToBeInserted
      // itemToBeInserted.nodeNumber = 
      // itemToBeInserted.cost = costAsPriority;

      //FIRST CASE: WHEN THERE IS NOTHING IN THE ARRAY SO FAR
      if( currentPQueueCount == 0 )
      {
          pQueueAsBinaryHeapSinglePath[1] = itemToBeInserted;
          pointerArraySinglePath[itemToBeInserted.nodeNumber] = 1;
          currentPQueueCount++;
          return;
      }

      currentPQueueCount++;

      // ELSE ADD IT TO THE END
      // THEN COMPARE TO THE PARENTS, USE A WHILE LOOP, TAIL RECURSION
      // PUT CHILD (CURRENT OBJECT) AT THE LAST SPOT
      pQueueAsBinaryHeapSinglePath[currentPQueueCount] = itemToBeInserted;
      int currentIndex = currentPQueueCount;
      NodeThreeTuple tempParentHolder;
      NodeThreeTuple tempChildHolder;


            //While child's priority < parent's priority, HAVE THIS OBJECT SWITCH SPOTS WITH ITS PARENT
            while (pQueueAsBinaryHeapSinglePath[currentIndex].dist < pQueueAsBinaryHeapSinglePath[currentIndex / 2].dist)
            {
                tempChildHolder = pQueueAsBinaryHeapSinglePath[currentIndex];
                tempParentHolder = pQueueAsBinaryHeapSinglePath[currentIndex / 2];

                //swap them, ie update child index to be that of the parent
                pQueueAsBinaryHeapSinglePath[currentIndex / 2] = tempChildHolder;
                pQueueAsBinaryHeapSinglePath[currentIndex] = tempParentHolder;

                // update the pointer arrays
                pointerArraySinglePath[ itemToBeInserted.nodeNumber  ] = (currentIndex / 2);
                int nodeNumberOfMovedObject = tempParentHolder.nodeNumber;
                pointerArraySinglePath[nodeNumberOfMovedObject] = currentIndex;

                currentIndex = (currentIndex / 2);
                if (currentIndex == 1) break;
            }
            return;
        }






        /*
         * Identical to decreaseKey function for all_paths, but we use the data structures
         * that are set apart for the single_path task.
        */
        private void decreaseKeySinglePath(int nodeNumber, double newDist)
        {
            int positionInPQueue = pointerArraySinglePath[nodeNumber]; // index into pointer array to find 
            pQueueAsBinaryHeapSinglePath[positionInPQueue].dist = newDist;
            // Bubble Up potentially if cost is less than the parent
            // Bubble up all the way to the root if necessary

            int currentIndex = positionInPQueue;
            NodeThreeTuple tempParentHolder;
            NodeThreeTuple tempChildHolder;

            while (pQueueAsBinaryHeapSinglePath[currentIndex].dist < pQueueAsBinaryHeapSinglePath[currentIndex / 2].dist)
            {
                tempChildHolder = pQueueAsBinaryHeapSinglePath[currentIndex];
                tempParentHolder = pQueueAsBinaryHeapSinglePath[currentIndex / 2];

                //swap them, ie update child index to be that of the parent
                pQueueAsBinaryHeapSinglePath[currentIndex / 2] = tempChildHolder;
                pQueueAsBinaryHeapSinglePath[currentIndex] = tempParentHolder;

                // update the pointer arrays
                pointerArraySinglePath[nodeNumber] = (currentIndex / 2);

                int nodeNumberOfMovedObject = tempParentHolder.nodeNumber;
                pointerArraySinglePath[nodeNumberOfMovedObject] = currentIndex;

                currentIndex = (currentIndex / 2);
                if (currentIndex == 1) break;
            }
        }

        

    private Boolean startStopToggle = true;
    private int startNodeIndex = -1;
    private int stopNodeIndex = -1;
    private void pictureBox_MouseDown(object sender, MouseEventArgs e)
    {
        if (points.Count > 0)
        {
            Point mouseDownLocation = new Point(e.X, e.Y);
            int index = ClosestPoint(points, mouseDownLocation);
            if (startStopToggle)
            {
                startNodeIndex = index;
                sourceNodeBox.Text = "" + index;
            }
            else
            {
                stopNodeIndex = index;
                targetNodeBox.Text = "" + index;
            }
            startStopToggle = !startStopToggle;

            resetImageToPoints(points);
            paintStartStopPoints();
        }
    }

    private void paintStartStopPoints()
    {
        if (startNodeIndex > -1)
        {
            Graphics graphics = Graphics.FromImage(pictureBox.Image);
            graphics.DrawEllipse(new Pen(Color.Green, 6), points[startNodeIndex].X, points[startNodeIndex].Y, 1, 1);
            this.graphics = graphics;
            pictureBox.Invalidate();
        }

        if (stopNodeIndex > -1)
        {
            Graphics graphics = Graphics.FromImage(pictureBox.Image);
            graphics.DrawEllipse(new Pen(Color.Red, 2), points[stopNodeIndex].X - 3, points[stopNodeIndex].Y - 3, 8, 8);
            this.graphics = graphics;
            pictureBox.Invalidate();
        }
    }

    private int ClosestPoint(List<PointF> points, Point mouseDownLocation)
    {
        double minDist = double.MaxValue;
        int minIndex = 0;

        for (int i = 0; i < points.Count; i++)
        {
            double dist = Math.Sqrt(Math.Pow(points[i].X - mouseDownLocation.X, 2) + Math.Pow(points[i].Y - mouseDownLocation.Y, 2));
            if (dist < minDist)
            {
                minIndex = i;
                minDist = dist;
            }
        }

        return minIndex;
    }
}
}
