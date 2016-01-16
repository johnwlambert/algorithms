/*
 * John Lambert
 * Convex Hull
 * October 6, 2015
 * I implement an algorithm that can find the "Convex hull" given a set
 * of points (fed in as x,y coordinates).
 *
 * Algorithmic time complexity will be O(n log(n)) because we do O(1) work
 * for n points, and we repeat this work at log(n) levels.  Furthermore,
 * sorting also adds O(n log(n)).
 * 
 * I use the sorting function from C#'s library called "Array.Sort" to sort
 * my list of points in terms of increasing x-values.  Microsoft states
 * that their library function uses different algorithms depending on
 * specific criteria: "If the partition size is fewer than 16 elements, it
 * uses an insertion sort algorithm.  If the number of partitions exceeds
 * 2 * LogN, where N is the range of the input array, it uses a Heapsort
 * algorithm. Otherwise, it uses a Quicksort algorithm."
 *
 * Yes, we know that Quicksort has worst-case scenario of O(n^2) sorting,
 * and that insertion sort has average case O(n^2). But we are working with
 * numbers on the scale of 100-1000, not less than 16. So we can assume we
 * avoid this insertion sort complexity. Generally, we will be working with
 * Quicksort, which has average-case complexity of O(n*log(n)), so we can
 * safely say that this is the time complexity added by sorting the points.
 *
 * TIME COMPLEXITY OF EACH PORTION OF MY ALGORITHM
 * Divide list in half  - O(N), look at each point once
 * Recurse left
 * Recurse right
 * Sort lists into counter-clockwise order O(N) - we look at each point once
 * Find Upper Tangent - O(N) because we look at basically 1/2 * n points
 *      (upper 1/2)
 * Find Lower Tangent - O(N) because we look at basically 1/2 * n points
 *      (lower 1/2)
 * Combining/ merging the hulls is also O(N) 
 *
 * Space complexity is O(N) since I just have a bunch of lists with n
 * elements in them, where n is the number of original points specified.
 *
 * The following recurrence relation describes the algorithm: 
 * T(n) = 2T(n/2) + O(n), where T(n) represents  the number of operations
 * required as a function of the original number of n points specified. 
 * O(n) is the complexity to combine two hulls. The size of the subtask
 * is 1/2 the number of points of its parent, and each split creates two
 * subtasks. By the Master Theorem, when we consider the equation
 * T(n) = at( n/b) + O(n^d), we see in our case that d=1 and that b = 2,
 * a = 2. Thus a/(b^d) = 2/2 = 1, so the work is constant at each level
 * of the divide and conquer algorithm. The Master Theorem once again
 * proves that the time complexity of the recurrence relation 
 * is O( (n^1) * log(n) ).
 *
 * At first the problem seems trivial, because it seemes as if one could
 * always use the greatest and least y vals for upper tangent. This is not
 * the case (have to traverse set of points clockwise or counterclockwise
 * and compare slopes to know for certain ).
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Collections;
using System.Linq;

public struct PointWithSlope
{
    public PointF specificPoint;
    public double specificSlope;

}

public struct TangentPointStruct
{
    public int upperTangentLeftPointIndex;
    public int upperTangentRightPointIndex;
    public int lowerTangentLeftPointIndex;
    public int lowerTangentRightPointIndex;
    public PointF upperTangentLeftPoint;
    public PointF upperTangentRightPoint;
    public PointF lowerTangentLeftPoint;
    public PointF lowerTangentRightPoint;
    public int rightHullIndexWithRightmostXVal;
}

namespace _2_convex_hull
{

    class ConvexHullSolver
    {
        System.Drawing.Graphics g;
        System.Windows.Forms.PictureBox pictureBoxView;

        public ConvexHullSolver(System.Drawing.Graphics g, System.Windows.Forms.PictureBox pictureBoxView)
        {
            this.g = g;
            this.pictureBoxView = pictureBoxView;
        }

        public void Refresh()
        {
            // Use this especially for debugging and whenever you want to see what you have drawn so far
            pictureBoxView.Refresh();
        }

        public void Pause(int milliseconds)
        {
            // Use this especially for debugging and to animate your algorithm slowly
            pictureBoxView.Refresh();
            System.Threading.Thread.Sleep(milliseconds);
        }


        /*
        * We take in a list of points. We run quicksort on all of the 
        * hull’s points, sorting them according to their x values, from
        * least to greatest (as if they lie on a number line).
        *
        * We then make a new list, and call a function that recursively
        * will divide and conquer for us, finally returning a pruned list
        * of points that constitute the convex hull. We use the 
        * Graphics g object that is part of the current class to draw the
        * line segments on the UI, connecting these points in a polygon
        * along the hull's border.
        */
        public void Solve(List<System.Drawing.PointF> pointList) 
        {
            Pen blackPen = new Pen(Color.Black, 3);
            Pen pointColor = new Pen(Color.FromArgb(0, 0, 0));

            // This shows calling the Sort(Comparison(T) )
            // This method treats null as the lesser of two values.
            pointList.Sort(delegate (PointF x, PointF y)
            {
                if (x.X == null && y.X == null) return 0;
                else if (x.X == null) return -1;
                else if (y.X == null) return 1;
                else return x.X.CompareTo(y.X);
            });
            List<System.Drawing.PointF> finalConvexHull = new List<System.Drawing.PointF>();
            finalConvexHull = dividePoints( pointList);  
            finalConvexHull = sortHullPointsIntoCounterClockwiseOrder(finalConvexHull);

            for (int i = finalConvexHull.Count - 1; i > 0; i--)
            {
                this.g.DrawLine(blackPen, finalConvexHull[i], finalConvexHull[i - 1]);
            }
            this.g.DrawLine(blackPen, finalConvexHull[finalConvexHull.Count - 1], finalConvexHull[0]);
            return;
        }



        /*
         * This function implements the divide-and-conquer strategy. Via
         * recursion, we split the list of points in half each time.  Our
         * base case is a list with 2 or 3 points inside of it, in which
         * case we sort them counterclockwise. We never do any merging in
         * the base case; we simply return the points upwards and onwards
         * to the parent.
         * 
         * Otherwise, we continue the recursion and keep splitting the
         * list in half, and merge these two halves. The merged version is
         * passed upward to the parent.
        */
        public List<System.Drawing.PointF> dividePoints(List<System.Drawing.PointF> pointList)
        {
            if( ( pointList.Count == 2) || (pointList.Count == 3) ) // BASE CASE
            {
                return sortHullPointsIntoCounterClockwiseOrder( pointList);
            }
            int numListElements = pointList.Count;             // OTHERWISE, repeat recursion
            int counterToHalf = 0;
            List<System.Drawing.PointF> rightList = new List<System.Drawing.PointF>();
            List<System.Drawing.PointF> leftList = new List<System.Drawing.PointF>();
            foreach (System.Drawing.PointF aPoint in pointList)
            {
                if( counterToHalf < numListElements/2 ) {
                    leftList.Add(new System.Drawing.PointF() { X = aPoint.X, Y = aPoint.Y });
                    counterToHalf++;
                }
                else {
                    rightList.Add(new System.Drawing.PointF() { X = aPoint.X, Y = aPoint.Y });
                    counterToHalf++;
                }
            }
            return mergeHulls( dividePoints(leftList), dividePoints(rightList) ); // THE MERGING HAPPENS DOWN HERE
        }


        /*
         * We find out information about the upper and lower tangents, and
         * place the info into a struct. Then our helper function places the points relevant to the convex hull into a new list.
         * We do so because we are trying to find the point farthest out and
         * above (not concave, but convex hull)
         *
         * We invert greater than and less than operators because the Graphics Window is upside down
        */
        public List<PointF> mergeHulls(List<PointF> leftList, List<System.Drawing.PointF> rightList)
        {
            Pen blackPen = new Pen(Color.Black, 3);
            List<System.Drawing.PointF> mergedList = new List<System.Drawing.PointF>();
            // always connect a set of two or three points automatically.
            int counterRightHullCurrentPoint = 0; 
            int counterLeftHullCurrentPoint = 0;


            TangentPointStruct allCurrentTangentInfo = new TangentPointStruct();
            allCurrentTangentInfo = findUpperTangent( leftList, rightList, blackPen, mergedList, counterRightHullCurrentPoint, 
                counterLeftHullCurrentPoint, allCurrentTangentInfo);

            allCurrentTangentInfo = findLowerTangent(leftList, rightList, blackPen, mergedList, counterRightHullCurrentPoint,
                counterLeftHullCurrentPoint, allCurrentTangentInfo);

            mergedList = addConvexPointsThatWillGoInCombinedConvexHull(mergedList, allCurrentTangentInfo, rightList, leftList);

            mergedList = sortHullPointsIntoCounterClockwiseOrder(mergedList);

            // LOWER TANGENT - we will add everything counterclockwise of storedLowerTangentRightPoint (until we get to max x val in right hull, but don't overlap)
            // LOWER TANGENT - we will add everything clockwise of storedLowerTangentLeftPoint( to leftmost, but don't overlap)
            return mergedList;
        }



        /*
         * Find the lower tangent.
         * On the left hull, To find lower tangent, we traverse clockwise. We place all of the useful information that we find out about the points that constitute the lower tangent INTO A STRUCT, which we return.
        */
        public TangentPointStruct findLowerTangent ( List<PointF> leftList, List<PointF> rightList, Pen blackPen, List<PointF> mergedList,
            int counterRightHullCurrentPoint, int counterLeftHullCurrentPoint, TangentPointStruct allCurrentTangentInfo)
        {
            double minXValue = -999999999999999; // magic number
            PointF storedLowerTangentRightPoint = new PointF();
            PointF storedLowerTangentLeftPoint = new PointF();
            storedLowerTangentRightPoint.X = 0;
            storedLowerTangentRightPoint.Y = 0;
            storedLowerTangentLeftPoint.X = 0;
            storedLowerTangentLeftPoint.Y = 0;
            PointF currentRightHullPoint = new PointF();
            PointF currentLeftHullPoint = new PointF();

            currentRightHullPoint.X = 9999;
            currentRightHullPoint.Y = 9999;
            currentLeftHullPoint.X = 9999;
            currentLeftHullPoint.Y = 9999;

            for (int i = 0; i < leftList.Count; i++)  // Take rightmost point on left hull.
            {
                if (leftList[i].X > minXValue)             //find the rightmost point in the left hull quickly
                {
                    counterLeftHullCurrentPoint = i;
                    minXValue = leftList[i].X;
                }
            }
            while ((storedLowerTangentRightPoint.X != currentRightHullPoint.X) &&
                (storedLowerTangentRightPoint.Y != currentRightHullPoint.Y) &&
                (storedLowerTangentLeftPoint.X != currentLeftHullPoint.X) &&
                (storedLowerTangentLeftPoint.Y != currentLeftHullPoint.Y))
            { // while loop while the tangent is changing

                // look at right side
                // FOR THE RIGHT HULL
                // Take leftmost point on right hull. To find upper tangent, we traverse clockwise. To find lower tangent,
                // we traverse counterclockwise.

                storedLowerTangentRightPoint = currentRightHullPoint;
                storedLowerTangentLeftPoint = currentLeftHullPoint;
                currentLeftHullPoint = leftList[counterLeftHullCurrentPoint];
                PointF originalLeftPoint = new PointF();
                originalLeftPoint = currentLeftHullPoint;
                currentRightHullPoint = rightList[counterRightHullCurrentPoint];


                counterLeftHullCurrentPoint--; // keep scrolling counterclockwise through the left hull's points
                counterLeftHullCurrentPoint = counterLeftHullCurrentPoint % (leftList.Count);
                if( counterLeftHullCurrentPoint < 0 )
                {
                    counterLeftHullCurrentPoint += leftList.Count;
                }
                currentLeftHullPoint = leftList[counterLeftHullCurrentPoint];

                while (findSlope(originalLeftPoint, currentRightHullPoint) > findSlope(currentLeftHullPoint, currentRightHullPoint))
                {
                    originalLeftPoint = currentLeftHullPoint; // originalLeft  = newLEFT;

                    counterLeftHullCurrentPoint--; // keep scrolling counterclockwise through the left hull's points
                    counterLeftHullCurrentPoint = counterLeftHullCurrentPoint % (leftList.Count);
                    if (counterLeftHullCurrentPoint < 0)
                    {
                        counterLeftHullCurrentPoint += leftList.Count;
                    }
                    currentLeftHullPoint = leftList[counterLeftHullCurrentPoint];
                }
                /// we've gone too far, go back one
                counterLeftHullCurrentPoint++; // keep scrolling counterclockwise through the left hull's points
                counterLeftHullCurrentPoint = counterLeftHullCurrentPoint % (leftList.Count);
                currentLeftHullPoint = leftList[counterLeftHullCurrentPoint];

                // we looked at left side, see if its changing
                // FOR THE LEFT HULL, To find upper tangent, we traverse counterclockwise.
                // Look at slope.Will it continue to decrease as we connect to point on right?  When it increases again, gone too far.
                // move over one on the left


                PointF originalRightPoint = new PointF();
                originalRightPoint = currentRightHullPoint;

                counterRightHullCurrentPoint++; // keep scrolling clockwise through the right hull's points
                counterRightHullCurrentPoint = counterRightHullCurrentPoint % (rightList.Count);
                currentRightHullPoint = rightList[counterRightHullCurrentPoint]; // remember to decrement if it was right

                // NOW RIGHT HULL, when it stops increasing (Starts to decrease)  
                while (findSlope(originalRightPoint, currentLeftHullPoint) < findSlope(currentRightHullPoint, currentLeftHullPoint))
                {
                    originalRightPoint = currentRightHullPoint;
                    counterRightHullCurrentPoint++; // keep scrolling clockwise through the right hull's points
                    counterRightHullCurrentPoint = counterRightHullCurrentPoint % (rightList.Count);
                    currentRightHullPoint = rightList[counterRightHullCurrentPoint]; 
                }


                // we've gone too far, so go back one
                counterRightHullCurrentPoint--; // keep scrolling clockwise through the right hull's points
                counterRightHullCurrentPoint = counterRightHullCurrentPoint % (rightList.Count);
                if( counterRightHullCurrentPoint < 0 )
                {
                    counterRightHullCurrentPoint += rightList.Count;
                }
                currentRightHullPoint = rightList[counterRightHullCurrentPoint]; 
            }

            double maxXValueInRightHull = -99999999;
            int rightHullIndexWithRightmostXVal = 0;
            for (int i = 0; i < rightList.Count; i++)
            {
                if (rightList[i].X > maxXValueInRightHull)
                {
                    rightHullIndexWithRightmostXVal = i;
                    maxXValueInRightHull = rightList[i].X;
                }
            }
            allCurrentTangentInfo.lowerTangentLeftPointIndex = counterLeftHullCurrentPoint;
            allCurrentTangentInfo.lowerTangentRightPointIndex = counterRightHullCurrentPoint;
            allCurrentTangentInfo.lowerTangentLeftPoint = currentLeftHullPoint;
            allCurrentTangentInfo.lowerTangentRightPoint = currentRightHullPoint;
            allCurrentTangentInfo.rightHullIndexWithRightmostXVal = rightHullIndexWithRightmostXVal;
            return allCurrentTangentInfo;
    }



        /*
         * EXACT SAME ALGORITHM AS Finding lower tangent, BUT REVERSED
         *
         * We find the upper tangent by scrolling through a point on the 
         * right and all of the left points, and then a point on the left
         * and all of the right points, etc. The slope between the points
         * will decrease / increase at zenith, depending if we are looking
         * at right or left hull specifically.                                   We place all of the useful information that we find out about the points that constitute the upper tangent INTO A STRUCT, which we return.
        */
        public TangentPointStruct findUpperTangent (List<PointF> leftList, List<PointF> rightList, Pen blackPen, List<PointF> mergedList,
            int counterRightHullCurrentPoint, int counterLeftHullCurrentPoint, TangentPointStruct allCurrentTangentInfo)
        {
            double minXValue = -999999999999999; // magic, huge negative number
            PointF storedUpperTangentRightPoint = new PointF();
            PointF storedUpperTangentLeftPoint = new PointF();
            storedUpperTangentRightPoint.X = 0;
            storedUpperTangentRightPoint.Y = 0;
            storedUpperTangentLeftPoint.X = 0;
            storedUpperTangentLeftPoint.Y = 0;
            
            PointF currentRightHullPoint = new PointF();
            PointF currentLeftHullPoint = new PointF();

            currentRightHullPoint.X = 9999;
            currentRightHullPoint.Y = 9999;
            currentLeftHullPoint.X = 9999;
            currentLeftHullPoint.Y = 9999;

            for (int i = 0; i < leftList.Count; i++)  // Take rightmost point on left hull.
            {
                if (leftList[i].X > minXValue)
                {
                    counterLeftHullCurrentPoint = i;
                    minXValue = leftList[i].X;
                }
            }
            while ((storedUpperTangentRightPoint.X != currentRightHullPoint.X) && 
                (storedUpperTangentRightPoint.Y != currentRightHullPoint.Y) &&
                (storedUpperTangentLeftPoint.X != currentLeftHullPoint.X) &&
                (storedUpperTangentLeftPoint.Y != currentLeftHullPoint.Y))
            { // WHILE THE UPPER TANGENT IS STILL CHANGING

                storedUpperTangentLeftPoint = currentLeftHullPoint;
                storedUpperTangentRightPoint = currentRightHullPoint;
                
                // look at the right hull
                // Take leftmost point on right hull. To find upper tangent, we traverse clockwise. To find lower tangent,
                // we traverse counterclockwise.
                currentLeftHullPoint = leftList[counterLeftHullCurrentPoint];
                PointF originalLeftPoint = new PointF();
                originalLeftPoint = currentLeftHullPoint;
                currentRightHullPoint = rightList[counterRightHullCurrentPoint];

                counterLeftHullCurrentPoint++; // keep scrolling counterclockwise through the left hull's points
                counterLeftHullCurrentPoint = counterLeftHullCurrentPoint % (leftList.Count);
                currentLeftHullPoint = leftList[counterLeftHullCurrentPoint];

                while (findSlope(originalLeftPoint, currentRightHullPoint) < findSlope(currentLeftHullPoint, currentRightHullPoint))
                {
                    originalLeftPoint = currentLeftHullPoint; // originalLeft  = newLEFT;
                    counterLeftHullCurrentPoint++; // keep scrolling counterclockwise through the left hull's points
                    counterLeftHullCurrentPoint = counterLeftHullCurrentPoint % (leftList.Count);
                    currentLeftHullPoint = leftList[counterLeftHullCurrentPoint];      //newLeft = originalLeft.clockwise;
                }
                /// we've gone too far, go back one
                counterLeftHullCurrentPoint--; // keep scrolling counterclockwise through the left hull's points
                counterLeftHullCurrentPoint = counterLeftHullCurrentPoint % (leftList.Count);
                if (counterLeftHullCurrentPoint < 0)
                {
                    counterLeftHullCurrentPoint += leftList.Count;
                }
                currentLeftHullPoint = leftList[counterLeftHullCurrentPoint];
                // We have just looked at the left hull, to see if its lower tangent point is changing
                // Look at slope.Will it continue to increase as we connect to point on
                // right?  When it decreases again, gone too far. Move back one step


                PointF originalRightPoint = new PointF();
                originalRightPoint = currentRightHullPoint;

                counterRightHullCurrentPoint--; // keep scrolling clockwise through the right hull's points
                counterRightHullCurrentPoint = counterRightHullCurrentPoint % (rightList.Count);
                if (counterRightHullCurrentPoint < 0)
                {
                    counterRightHullCurrentPoint += rightList.Count;
                }
                currentRightHullPoint = rightList[counterRightHullCurrentPoint];

                // Now calculate the lower tangent point for the right hull ( when the slope stops decreasing)
                while (findSlope(originalRightPoint, currentLeftHullPoint) > findSlope(currentRightHullPoint, currentLeftHullPoint))
                {
                    originalRightPoint = currentRightHullPoint;
                    counterRightHullCurrentPoint--; // keep scrolling clockwise through the right hull's points
                    if (counterRightHullCurrentPoint < 0)
                    {
                        counterRightHullCurrentPoint += rightList.Count;
                    }
                    counterRightHullCurrentPoint = counterRightHullCurrentPoint % (rightList.Count);
                    currentRightHullPoint = rightList[counterRightHullCurrentPoint];
                }


                // we've gone too far, so go back one
                counterRightHullCurrentPoint++; // keep scrolling clockwise through the right hull's points
                counterRightHullCurrentPoint = counterRightHullCurrentPoint % (rightList.Count);
                currentRightHullPoint = rightList[counterRightHullCurrentPoint]; 
            }
            double maxXValueInRightHull = -99999999;
            int rightHullIndexWithRightmostXVal = 0;
            for (int i = 0; i < rightList.Count; i++)
            {
                if (rightList[i].X > maxXValueInRightHull)
                {
                    rightHullIndexWithRightmostXVal = i;
                    maxXValueInRightHull = rightList[i].X;
                }
            }
            allCurrentTangentInfo.upperTangentLeftPointIndex = counterLeftHullCurrentPoint;
            allCurrentTangentInfo.upperTangentRightPointIndex = counterRightHullCurrentPoint;
            allCurrentTangentInfo.upperTangentLeftPoint = currentLeftHullPoint;
            allCurrentTangentInfo.upperTangentRightPoint = currentRightHullPoint;
            allCurrentTangentInfo.rightHullIndexWithRightmostXVal = rightHullIndexWithRightmostXVal;
            return allCurrentTangentInfo;
        }




        /*
        * This little helper function does the dirty work for us to find
        * the slope between two PointF objects
        */
        public double findSlope ( PointF currentRightHullPoint, PointF currentLeftHullPoint)
    {
        double slope = 0;
        slope = (currentLeftHullPoint.Y - currentRightHullPoint.Y) / (currentLeftHullPoint.X - currentRightHullPoint.X);
        return slope;

    }

        /*
        * I order clockwise from leftmost point for both right and left
        * hulls. We sort the left and right hull’s points based in
        * counterclockwise order. ie for both hulls take leftmost point,
        * find slope between itself and every other point in hull. Greatest
        * to least slope signifies counterclockwise in the upside down
        * Graphics GWindow world.  (Otherwise, in real world, least to
        * greatest slope between leftmost and other arbitrary point would
        * be counterclockwise)
        */
        public List<System.Drawing.PointF> sortHullPointsIntoCounterClockwiseOrder( List<System.Drawing.PointF> pointList )
        {
            pointList.Sort(delegate (PointF x, PointF y)
            {
                if (x.X == null && y.X == null) return 0;
                else if (x.X == null) return -1;
                else if (y.X == null) return 1;
                else return x.X.CompareTo(y.X);
            });

            PointF leftmostPoint = pointList[0];
            PointWithSlope[] arrayOfStructs = new PointWithSlope[pointList.Count];

            for( int i = 0; i< pointList.Count; i++ )
            {
                // NOTE THAT WHEN WE DIVIDE BY 0 ( DIVIDING LEFTMOST POINT VALS - LEFTMOST POINT VALS), we get NaN. This goes in highest
                // position in list. So we know leftmost element always in leftmost part of list
                double individualPointsSlope = (leftmostPoint.Y - pointList[i].Y) / (leftmostPoint.X - pointList[i].X);

                if ( (pointList[i].Y == leftmostPoint.Y) && (pointList[i].X == leftmostPoint.X)) // we can't divide by ourselves ( will get a NaN)
                {
                    individualPointsSlope = 99999999; // magic number that represents arbitrarily (almost impossible) large positive val
                }
                PointWithSlope structForEachPoint = new PointWithSlope();
                structForEachPoint.specificPoint = pointList[i];
                structForEachPoint.specificSlope = individualPointsSlope;
                    arrayOfStructs[i] = structForEachPoint;
            }
            Array.Sort(arrayOfStructs, (y, x) => x.specificSlope.CompareTo(y.specificSlope));
            List<PointF> pointsSortedCounterClockwise = new List<PointF>();
            for( int i = 0; i < arrayOfStructs.Length; i++ )
            {
                pointsSortedCounterClockwise.Insert(i, arrayOfStructs[i].specificPoint);
            }
            return pointsSortedCounterClockwise;
        }


        /*
        * This function does the work of extracting the info about the upper and lower
        * tangents from the struct that holds this info.                           This function has nested loops inside of each other. Immediately,
         * we might worry that this will jump to O(n^2) complexity, where n
         * is the number of elements being looped over. Fortunately, this is
         * not the case. We are guaranteed that we won't go all the way
         * around either hull ( we actually only touch each point once). This
         * ends up being linear time , O(n).

        * Then, we use while loops to traverse the right and left lists and to
        * add the appropriate points such that we build a convex hull at each level.
        *
        * Note that the leftmost point on left hull always goes in there. We walk counterclockwise from leftmost point to lower tangent point in left hull. We add all of the points along the way. Then we skip to lower tangent point in the right hull. We add all of the points until we reach the upper tangent point in the same right hull. Then we skip again to the left hull, continuing our counterclockwise motion, and add all of the points from the upper left tangent point up to the point before the leftmost point.
        */
        public List<PointF> addConvexPointsThatWillGoInCombinedConvexHull ( List<PointF> mergedList, TangentPointStruct allCurrentTangentInfo, List<PointF> rightList, List<PointF> leftList)
        {
            int lowerLeftTanCounter = 0;
            if ((allCurrentTangentInfo.lowerTangentLeftPoint.X == leftList[lowerLeftTanCounter].X) && (allCurrentTangentInfo.lowerTangentLeftPoint.Y == leftList[lowerLeftTanCounter].Y))
            {
                mergedList.Add(leftList[0]);
            }
            else
            {
                while ((allCurrentTangentInfo.lowerTangentLeftPoint.X != leftList[lowerLeftTanCounter].X) && (allCurrentTangentInfo.lowerTangentLeftPoint.Y != leftList[lowerLeftTanCounter].Y))
                {
                    mergedList.Add(leftList[lowerLeftTanCounter]);
                    lowerLeftTanCounter++;
                    lowerLeftTanCounter = (lowerLeftTanCounter % leftList.Count);

                }
                mergedList.Add(leftList[lowerLeftTanCounter]);
            }
            int fromLowToHighRightTanCounter = allCurrentTangentInfo.lowerTangentRightPointIndex;
            while ((allCurrentTangentInfo.upperTangentRightPoint.X != rightList[fromLowToHighRightTanCounter].X) && (allCurrentTangentInfo.upperTangentRightPoint.Y != rightList[fromLowToHighRightTanCounter].Y))
            {
                mergedList.Add(rightList[fromLowToHighRightTanCounter]);
                fromLowToHighRightTanCounter++;
                fromLowToHighRightTanCounter = (fromLowToHighRightTanCounter % rightList.Count);
            }

            mergedList.Add(rightList[fromLowToHighRightTanCounter]);

            int upperLeftTanCounter = allCurrentTangentInfo.upperTangentLeftPointIndex;
            while ((leftList[upperLeftTanCounter].X != leftList[0].X) && (leftList[upperLeftTanCounter].Y != leftList[0].Y))
            {
                mergedList.Add(leftList[upperLeftTanCounter]);
                upperLeftTanCounter++;
                upperLeftTanCounter = (upperLeftTanCounter % leftList.Count);
            }
            return mergedList;
        }
    }
}
