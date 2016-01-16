/*
* John Lambert
* TSP Branch and Bound
* 
* I implement Branch and Bound.  This is an improved State Space Search. I use a priority queue
* so that the state with the best (lowest) bound can be chosen when it is time to expand a new state.
* We use a bounding scheme (reduced cost matrix) and use a state space search that "drills deeper"
* leading to finding complete paths which will update the BSSF sooner and lead to earlier pruning.
*
* We do not choose an initial city to start at; we just have the initial reduced cost matrix
* tied to the initial search state.  The search tree will be a binary tree with the decision
* based on whether a particular edge is in the final solution or out of it. 
* Include edge (i,j) Exclude edge (i,j).  Note that the left (include) child reduces our
* subsequent options to paths of length n-1 while the right (exclude) child still requires us
* to find a path of length n, which cannot include edge (i,j).
*
* We want to include the best edges (usually very short edges between cities). Solutions
* including the good edges usually are better and have low lower bounds. Solutions which
* exclude that good edge are usually worse and have higher lower bounds. b(exclude) - b(include) can 
* find the edge which maximizes that difference.
*
* We hope this leads to "digging deep" down the include side to get new BSSFs, while leading
* to quick pruning down the exclude side.

* We select one of the up to n edges in our graph by selecting from those in our current
* reduced cost matrix – still n^2
*
* Including 0 cost paths tends to keep include bounds low, while excluding 0 cost
* paths tends to increase exclude bounds, (and always at least one 0 cost path available
* in every row not yet travelled from), thus we will only consider 0 cost paths – 
* closer to O(n) edges to consider. 
* Select the edge which maximizes b(Se) – b(Si) (b is bound function).
* 
* How do we find the best edge to chose? Examine the candidate (0 cost) edges.
* We can just try them all (O(n)) to see which maximizes b(Se) – b(Si).
* 
* Note that after setting Se(i,j) = INFINITY that
* b(Se) = b(Sparent) + min(rowi) + min(columnj)
* We reduce both states Se and Si and put them in the priority queue (as long
* as their bound < BSSF) and then take next lowest state off the queue.
*
* How many nodes do we expand? O(b^n) where b is the average state branching factor,
* (average state branching factor <= 2 ) and n is the depth of solutions.
*
* Time complexity: O( n^3 * b^n ). Since each node expansion tests n possible edges and 
* for each alternative does a cost reduction which is O(n^2).
* Note, however, that we hope that this extra computation time will be made up for by 
* less overall states needing to be considered.
*
* Space complexity: O( n^2 * b^n ) since we store one reduced cost matrix ( n^2) for 
* each node in the queue and the queue is the frontier of the search tree with is O( b^n).
*/



using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;

namespace TSP
{

    class ProblemAndSolver
    {

        private class TSPSolution
        {
            /// <summary>
            /// we use the representation [cityB,cityA,cityC] 
            /// to mean that cityB is the first city in the solution, cityA is the second, cityC is the third 
            /// and the edge from cityC to cityB is the final edge in the path.  
            /// You are, of course, free to use a different representation if it would be more convenient or efficient 
            /// for your node data structure and search algorithm. 
            /// </summary>
            public ArrayList
                Route;

            public TSPSolution(ArrayList iroute)
            {
                Route = new ArrayList(iroute);
            }



            /// <summary>
            /// Compute the cost of the current route.  
            /// Note: This does not check that the route is complete.
            /// It assumes that the route passes from the last city back to the first city. 
            /// </summary>
            /// <returns></returns>
            public double costOfRoute()
            {
                // go through each edge in the route and add up the cost. 
                int x;
                City here;
                double cost = 0D;

                for (x = 0; x < Route.Count - 1; x++)
                {
                    here = Route[x] as City;
                    cost += here.costToGetTo(Route[x + 1] as City);
                }

                // go from the last city to the first. 
                here = Route[Route.Count - 1] as City;
                cost += here.costToGetTo(Route[0] as City);
                return cost;
            }
        }

        #region Private members 

        /// <summary>
        /// Default number of cities (unused -- to set defaults, change the values in the GUI form)
        /// </summary>
        // (This is no longer used -- to set default values, edit the form directly.  Open Form1.cs,
        // click on the Problem Size text box, go to the Properties window (lower right corner), 
        // and change the "Text" value.)
        private const int DEFAULT_SIZE = 25;

        private const int CITY_ICON_SIZE = 5;

        // For normal and hard modes:
        // hard mode only
        private const double FRACTION_OF_PATHS_TO_REMOVE = 0.20;

        /// <summary>
        /// the cities in the current problem.
        /// </summary>
        private City[] Cities;
        /// <summary>
        /// a route through the current problem, useful as a temporary variable. 
        /// </summary>
        private ArrayList Route;
        /// <summary>
        /// best solution so far. 
        /// </summary>
        private TSPSolution bssf;

        private double myGreedyBSSF;

        // the difference of these two variables is the number of states pruned
        private int numStatesPruned;
        private int numStatesActuallyCreated;


        private HashSet<int> sizesOfPQueueThroughoutExecution;
        /// <summary>
        /// how to color various things. 
        /// </summary>
        private Brush cityBrushStartStyle;
        private Brush cityBrushStyle;
        private Pen routePenStyle;


        /// <summary>
        /// keep track of the seed value so that the same sequence of problems can be 
        /// regenerated next time the generator is run. 
        /// </summary>
        private int _seed;
        /// <summary>
        /// number of cities to include in a problem. 
        /// </summary>
        private int _size;

        /// <summary>
        /// Difficulty level
        /// </summary>
        private HardMode.Modes _mode;

        /// <summary>
        /// random number generator. 
        /// </summary>
        private Random rnd;
        #endregion

        #region Public members
        public int Size
        {
            get { return _size; }
        }

        public int Seed
        {
            get { return _seed; }
        }

        /*
        * This struct allows us to return 2 arguments when we call our function
        * to maximize lowerBoundExclude - lowerBoundInclude.
        */
        public struct ResultOfSplitOnThisEdge
        {
            public double excludeBoundMinusIncludeBound;
            public TSPState includeState;
            public TSPState excludeState;
        }
        #endregion

        #region Constructors
        public ProblemAndSolver()
        {
            this._seed = 1; 
            rnd = new Random(1);
            this._size = DEFAULT_SIZE;

            this.resetData();
        }

        public ProblemAndSolver(int seed)
        {
            this._seed = seed;
            rnd = new Random(seed);
            this._size = DEFAULT_SIZE;

            this.resetData();
        }

        public ProblemAndSolver(int seed, int size)
        {
            this._seed = seed;
            this._size = size;
            rnd = new Random(seed); 
            this.resetData();
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Reset the problem instance.
        /// </summary>
        private void resetData()
        {

            Cities = new City[_size];
            Route = new ArrayList(_size);
            bssf = null;

            if (_mode == HardMode.Modes.Easy)
            {
                for (int i = 0; i < _size; i++)
                    Cities[i] = new City(rnd.NextDouble(), rnd.NextDouble());
            }
            else // Medium and hard
            {
                for (int i = 0; i < _size; i++)
                    Cities[i] = new City(rnd.NextDouble(), rnd.NextDouble(), rnd.NextDouble() * City.MAX_ELEVATION);
            }

            HardMode mm = new HardMode(this._mode, this.rnd, Cities);
            if (_mode == HardMode.Modes.Hard)
            {
                int edgesToRemove = (int)(_size * FRACTION_OF_PATHS_TO_REMOVE);
                mm.removePaths(edgesToRemove);
            }
            City.setModeManager(mm);

            cityBrushStyle = new SolidBrush(Color.Black);
            cityBrushStartStyle = new SolidBrush(Color.Red);
            routePenStyle = new Pen(Color.Blue,1);
            routePenStyle.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// make a new problem with the given size.
        /// </summary>
        /// <param name="size">number of cities</param>
        //public void GenerateProblem(int size) // unused
        //{
        //   this.GenerateProblem(size, Modes.Normal);
        //}

        /// <summary>
        /// make a new problem with the given size.
        /// </summary>
        /// <param name="size">number of cities</param>
        public void GenerateProblem(int size, HardMode.Modes mode)
        {
            this._size = size;
            this._mode = mode;
            resetData();
        }

        /// <summary>
        /// return a copy of the cities in this problem. 
        /// </summary>
        /// <returns>array of cities</returns>
        public City[] GetCities()
        {
            City[] retCities = new City[Cities.Length];
            Array.Copy(Cities, retCities, Cities.Length);
            return retCities;
        }

        /// <summary>
        /// draw the cities in the problem.  if the bssf member is defined, then
        /// draw that too. 
        /// </summary>
        /// <param name="g">where to draw the stuff</param>
        public void Draw(Graphics g)
        {
            float width  = g.VisibleClipBounds.Width-45F;
            float height = g.VisibleClipBounds.Height-45F;
            Font labelFont = new Font("Arial", 10);

            // Draw lines
            if (bssf != null)
            {
                // make a list of points. 
                Point[] ps = new Point[bssf.Route.Count];
                int index = 0;
                foreach (City c in bssf.Route)
                {
                    if (index < bssf.Route.Count -1)
                        g.DrawString(" " + index +"("+c.costToGetTo(bssf.Route[index+1]as City)+")", labelFont, cityBrushStartStyle, new PointF((float)c.X * width + 3F, (float)c.Y * height));
                    else 
                        g.DrawString(" " + index +"("+c.costToGetTo(bssf.Route[0]as City)+")", labelFont, cityBrushStartStyle, new PointF((float)c.X * width + 3F, (float)c.Y * height));
                    ps[index++] = new Point((int)(c.X * width) + CITY_ICON_SIZE / 2, (int)(c.Y * height) + CITY_ICON_SIZE / 2);
                }

                if (ps.Length > 0)
                {
                    g.DrawLines(routePenStyle, ps);
                    g.FillEllipse(cityBrushStartStyle, (float)Cities[0].X * width - 1, (float)Cities[0].Y * height - 1, CITY_ICON_SIZE + 2, CITY_ICON_SIZE + 2);
                }

                // draw the last line. 
                g.DrawLine(routePenStyle, ps[0], ps[ps.Length - 1]);
            }

            // Draw city dots
            foreach (City c in Cities)
            {
                g.FillEllipse(cityBrushStyle, (float)c.X * width, (float)c.Y * height, CITY_ICON_SIZE, CITY_ICON_SIZE);
            }

        }

        /// <summary>
        ///  return the cost of the best solution so far. 
        /// </summary>
        /// <returns></returns>
        public double costOfBssf ()
        {
            if (bssf != null)
                return (bssf.costOfRoute());
            else
                return -1D;
        }

        /// <summary>
        ///  solve the problem.  This is the entry point for the solver when the run button is clicked
        ///   
        // I order states in the priority queue according to the lowest lower bound
        // Lower bound comes from reducing the cost matrix
        // Entered/ existed arrays have what goes to what nodes
        // Could try to drill down faster in the begnining before we process the priority queue
        //
        // Time complexity of solveProblem is that of the algorithmic time complexity,
        // O( n^3 * b^n )  where b is the average state branching factor, 
        // (average state branching factor <= 2 ) and n is the depth of solutions (at most n cities)
        // Since each node expansion tests n possible edges and for each alternative does a cost reduction which is O(n^2).
        // 
        /// </summary>
        public void solveProblem()
        {
            int counterNumBSSFUpdates = 0;
            sizesOfPQueueThroughoutExecution = new HashSet<int>();
            numStatesPruned = 0;
            numStatesActuallyCreated = 0;
            Stopwatch s = new Stopwatch();
            s.Start();
            Route = new ArrayList();
            SortedList myPQueue = new SortedList();
            myGreedyBSSF = generateBSSFViaGreedy();    // this function is O(n^2)
            double myOriginalGreedyBSSF = myGreedyBSSF;
            TSPState initialState = initializeFirstState();  // this function is O(n^2)
            initialState.lowerBoundOfState = 0;
            initialState = reduceRows(initialState); // O( n^2)
            initialState = reduceCols(initialState); // O( n^2)
            myPQueue.Add(initialState.lowerBoundOfState + (rnd.NextDouble() * 0.0000001), initialState); // O(1)
            numStatesActuallyCreated++;
            int[] optimalExitedArray = new int[ Cities.Length ];
            // while less than 30 seconds has elapsed and we havent hit home yet. 
            while ( (s.Elapsed < TimeSpan.FromSeconds(30)) )
            {  // O( b^n), where b is branching factor <=2, and n is number of cities in Rudrata cycle/ TSP Problem
                //Console.WriteLine( "Time is: " + String.Format("{0:00}.{1:00}", s.Elapsed.Seconds, s.Elapsed.Milliseconds / 10));
                if (myPQueue.Count == 0) break;
                if ( ((TSPState)myPQueue.GetByIndex(0)).lowerBoundOfState > myGreedyBSSF) {
                    iterateOverQueueContentsAndSumWhatWillNeverBeDequeued( myPQueue);
                    break;
                }
                if (myPQueue.Count == 0) break;
                if ( (((TSPState)myPQueue.GetByIndex(0)).numCitiesTraversedSoFar == (Cities.Length - 1)) )
                {
                    if(((TSPState)myPQueue.GetByIndex(0)).lowerBoundOfState < myGreedyBSSF)
                    {
                        counterNumBSSFUpdates++;
                        optimalExitedArray = ((TSPState)myPQueue.GetByIndex(0)).citiesExited;  // save the Route
                        myGreedyBSSF = ((TSPState)myPQueue.GetByIndex(0)).lowerBoundOfState;  // save the BSSF
                        Console.WriteLine("New BSSF is: " + myGreedyBSSF);
                        myPQueue.RemoveAt(0);
                    }
                }
                if ( (((TSPState)myPQueue.GetByIndex(0)).lowerBoundOfState > myGreedyBSSF)) 
                {
                    iterateOverQueueContentsAndSumWhatWillNeverBeDequeued(myPQueue);
                    break;
                }
                while(((TSPState)myPQueue.GetByIndex(0)).numCitiesTraversedSoFar == (Cities.Length - 1) )
                {
                    if ((((TSPState)myPQueue.GetByIndex(0)).lowerBoundOfState > myGreedyBSSF))
                    {
                        numStatesPruned++;
                    } else
                    {
                        counterNumBSSFUpdates++;
                        optimalExitedArray = ((TSPState)myPQueue.GetByIndex(0)).citiesExited; // save the Route
                        myGreedyBSSF = ((TSPState)myPQueue.GetByIndex(0)).lowerBoundOfState;  // save the BSSF
                        Console.WriteLine("New BSSF is: " + myGreedyBSSF);
                    }
                    myPQueue.RemoveAt(0);
                    if (myPQueue.Count == 0) break;
                }
                if (myPQueue.Count == 0) break;
                myPQueue = iterateOverReducedCostMatrix(myPQueue);  // O(n^3)
                sizesOfPQueueThroughoutExecution.Add(myPQueue.Count);  
            }
            if( s.Elapsed.Seconds > 30)
            {
                numStatesPruned += myPQueue.Count;  // we weren't able to dequeue everything
            }
            ArrayList branchAndBoundRoute = new ArrayList(); // could do this loop here, or we could just do it as we go along
            if( myPQueue.Count != 0)
            {
                if (((TSPState)myPQueue.GetByIndex(0)).numCitiesTraversedSoFar == (Cities.Length - 1))
                {
                    if (((TSPState)myPQueue.GetByIndex(0)).lowerBoundOfState < myGreedyBSSF)
                    {
                        counterNumBSSFUpdates++;
                        optimalExitedArray = ((TSPState)myPQueue.GetByIndex(0)).citiesExited; // save the Route
                        myGreedyBSSF = ((TSPState)myPQueue.GetByIndex(0)).lowerBoundOfState;  // save the BSSF
                        Console.WriteLine("New BSSF is: " + myGreedyBSSF);
                    }
                }
            }
            
            if( myGreedyBSSF == myOriginalGreedyBSSF )
            {
                branchAndBoundRoute = Route; // go with greedily-found route instead of branch-and-bound-found path
            } else
            {
                branchAndBoundRoute = calculateRouteFromExitedMatrix(optimalExitedArray, myPQueue);
            }
            bssf = new TSPSolution(branchAndBoundRoute); //  bssf is the route that will be drawn by the Draw method. 
            Console.WriteLine( s.ElapsedMilliseconds );
            s.Stop();
            TimeSpan ts = s.Elapsed;
            Program.MainForm.tbElapsedTime.Text = String.Format("{0:00}.{1:00}", ts.Seconds, ts.Milliseconds / 10);
            // update the cost of the tour. 
            Program.MainForm.tbCostOfTour.Text = " " + bssf.costOfRoute(); // this automatically will find the greedy bssf path's cost
            Program.MainForm.Invalidate();  // do a refresh. 
            Console.WriteLine("Success. Num States Actually Created: " + numStatesActuallyCreated + ". Num States Pruned: " +
                numStatesPruned + "." );
            int maxNumStatesInPQueuePipeline = calculateMaxOfNumStatesInPQueue(); // this is O(b^n) time complexity
            Console.WriteLine("Max number of states in PQueue at any time: " + maxNumStatesInPQueuePipeline);
            Console.WriteLine("Number of BSSF Updates was: " + counterNumBSSFUpdates);
        }



        /*
         * We start at one city
         * go to the next closest city
         * continue until get home
         * can't repeat cities
         * if you can't get home, start over
         *
         * Time complexity: O(n^2) because of double nested for-loop
         *
         * How much does it cost to get from this city to the destination?
         * We call the function: "double costToGetTo(City destination)"
        */
        public double generateBSSFViaGreedy()
        {
            double bssfDistance = 0;
            HashSet<City> alreadyVisitedCities = new HashSet<City>(); // add them to the set. if size = n-1, then we break and go to home city
            City[] myCopyOfCities = GetCities();
            City homeCity = myCopyOfCities[0];
            Route.Add(homeCity);
            alreadyVisitedCities.Add(homeCity);
            City currentCity;
            City nextCity = homeCity; //I assign this just because otherwise the compiler complains
            double distanceToClosestCity = Double.MaxValue;
            for (int cityIndex = 1; cityIndex < myCopyOfCities.Length; cityIndex++) // O(n)
            {
                if( homeCity.costToGetTo(myCopyOfCities[cityIndex]) < distanceToClosestCity )  // O(1)
                {
                    nextCity = myCopyOfCities[cityIndex];
                    distanceToClosestCity = homeCity.costToGetTo(myCopyOfCities[cityIndex]);
                }
            }
            currentCity = nextCity;
            Route.Add(nextCity);
            bssfDistance += distanceToClosestCity;
            distanceToClosestCity = Double.MaxValue;
            alreadyVisitedCities.Add(nextCity);
            while ( alreadyVisitedCities.Count < (myCopyOfCities.Length ) ) // O(n) , where n is number of cities in Rudrata cycle
            {
                for (int cityIndex = 0; cityIndex < myCopyOfCities.Length; cityIndex++) // O(n)
                {
                    if ( (currentCity.costToGetTo(myCopyOfCities[cityIndex]) < distanceToClosestCity) && !alreadyVisitedCities.Contains( myCopyOfCities[cityIndex] ) )
                    {
                        nextCity = myCopyOfCities[cityIndex]; // O(1)
                        distanceToClosestCity = currentCity.costToGetTo(myCopyOfCities[cityIndex]); // O(1)
                    }
                }
                currentCity = nextCity;
                alreadyVisitedCities.Add(nextCity);
                Route.Add(nextCity);
                bssfDistance += distanceToClosestCity;
                distanceToClosestCity = Double.MaxValue;
            }
            bssfDistance += (nextCity.costToGetTo(homeCity)); // this is the final cost from the last city, to home, to complete tour
            // make a clause in case this can't be reached, regenerate everything
            return bssfDistance;
        }

        /*
         * I use the function City.costToGetTo in order to fill in each cell of the initial state's grid
         *
         *Time complexity is O(n^2)
        */
        public TSPState initializeFirstState()
        {
            City[] myCopyOfCities = GetCities();
            TSPState initialState = new TSPState();
            initialState.numCitiesTraversedSoFar = 0;
            double[,] initialGrid = new double[myCopyOfCities.Length, myCopyOfCities.Length];
            initialState.grid = initialGrid;
            int[] entered = new int[Cities.Length];
            for (int cityIndex = 0; cityIndex < Cities.Length; cityIndex++) // O(n)
            {
                entered[cityIndex] = -1;
            }
            initialState.citiesEntered = entered;
            int[] exited = new int[Cities.Length];
            for (int cityIndex = 0; cityIndex < Cities.Length; cityIndex++) // O(n)
            {
                exited[cityIndex] = -1;
            }
            initialState.citiesExited = exited;
            for ( int rowIndex = 0; rowIndex < myCopyOfCities.Length; rowIndex++ ) // O(n)
            {
                for ( int colIndex = 0; colIndex < myCopyOfCities.Length; colIndex++ ) // O(n)
                {
                    initialState.grid[rowIndex, colIndex] = myCopyOfCities[rowIndex].costToGetTo(myCopyOfCities[colIndex]);
                    if( rowIndex == colIndex)
                    {
                        initialState.grid[rowIndex, colIndex] = Double.MaxValue; // when i=j, always set i,i to be INFINITY (can't go back to yourself)
                    }
                }
            }
            return initialState;
        }




        /*
         * We find the reduced cost matrix, starting with our state's matrix/grid.
         * We make new include/exclude states for all of the zeros inside of the 
         * grid, and we will add the one to the PQueue that has the largest ( lowerBoundExclude - lowerBoundInclude );
         *
         * I call a function that implements Martinez' code segment to cut off premature cycles.
         *
         * The time complexity of this algorithm looks like it would be O(n^4). That is not the case, however.
         * Yes, we do call a function that has complexity O(n^2) within our double nested for loop.
         * But we only call that function if the cell in that grid has value equal to zero. This will not
         * happen more than n times (except in an extremely, extremely rare case). This is because
         * usually each row will only have one zero value cost in it, after it is reduced (occasionally two).
         * And there are n rows, so this function has time complexity O(n^3).
        */
        public SortedList iterateOverReducedCostMatrix(SortedList myPQueue)
        {
            TSPState state = (TSPState)myPQueue.GetByIndex(0);
            myPQueue.RemoveAt(0);
            City[] myCopyOfCities = GetCities();
            ResultOfSplitOnThisEdge currentResultOfSplitOnThisEdge = new ResultOfSplitOnThisEdge();
            currentResultOfSplitOnThisEdge.excludeBoundMinusIncludeBound = 0;
            double previousMaximumExcludeBoundMinusIncludeBound = Double.MinValue;
            int rowOfEdgeToSplitOn = -1;
            int colOfEdgeToSplitOn = -1;
            TSPState excludeStateSplitOnCorrectEdge = new TSPState();
            TSPState includeStateSplitOnCorrectEdge = new TSPState();
            for (int rowIndex = 0; rowIndex < myCopyOfCities.Length; rowIndex++) // O(n)
            {
                for (int colIndex = 0; colIndex < myCopyOfCities.Length; colIndex++) // O(n)
                {
                    if( state.grid[rowIndex, colIndex] == 0) // if it is a zero inside of the grid, then...
                    {
                        currentResultOfSplitOnThisEdge = calculateDifferenceExcludeMinusInclude( rowIndex, colIndex, state, currentResultOfSplitOnThisEdge );
                        // we call function that has O(n^2) complexity
                        if( currentResultOfSplitOnThisEdge.excludeBoundMinusIncludeBound > previousMaximumExcludeBoundMinusIncludeBound )
                        {
                            rowOfEdgeToSplitOn = rowIndex;
                            colOfEdgeToSplitOn = colIndex;
                            previousMaximumExcludeBoundMinusIncludeBound = currentResultOfSplitOnThisEdge.excludeBoundMinusIncludeBound;
                            excludeStateSplitOnCorrectEdge = currentResultOfSplitOnThisEdge.excludeState;
                            includeStateSplitOnCorrectEdge = currentResultOfSplitOnThisEdge.includeState;
                        }
                    }
                }
            }
            if( (rowOfEdgeToSplitOn == -1) || (colOfEdgeToSplitOn == -1) ){
                return myPQueue;
            }
            numStatesActuallyCreated += 2; // 2 states were generated
            includeStateSplitOnCorrectEdge.numCitiesTraversedSoFar = (state.numCitiesTraversedSoFar + 1 );
            excludeStateSplitOnCorrectEdge.numCitiesTraversedSoFar = state.numCitiesTraversedSoFar;
            includeStateSplitOnCorrectEdge = preventPrematureCycles(includeStateSplitOnCorrectEdge, rowOfEdgeToSplitOn, colOfEdgeToSplitOn);
            if( includeStateSplitOnCorrectEdge.lowerBoundOfState < myGreedyBSSF)
            {
                double newKey = includeStateSplitOnCorrectEdge.lowerBoundOfState + (rnd.NextDouble() * 0.000001);
                if (!myPQueue.ContainsKey(newKey))
                {
                    myPQueue.Add(newKey, includeStateSplitOnCorrectEdge);
                }
                else
                {
                    myPQueue.Add(newKey + (rnd.NextDouble() * 0.000001), includeStateSplitOnCorrectEdge); // artificially change key by a tiny bit, if a duplicate is found
                }
            } else
            {
                numStatesPruned++; // state never put on the queue
            }
            if( excludeStateSplitOnCorrectEdge.lowerBoundOfState < myGreedyBSSF )
            {
                double newKey = excludeStateSplitOnCorrectEdge.lowerBoundOfState + (rnd.NextDouble() * 0.000001);
                if( !myPQueue.ContainsKey(newKey))
                {
                    myPQueue.Add(newKey, excludeStateSplitOnCorrectEdge);
                } else
                {
                    myPQueue.Add(newKey + (rnd.NextDouble() * 0.000001), excludeStateSplitOnCorrectEdge); // artificially change key by a tiny bit, if a duplicate is found
                }
            } else
            {
                numStatesPruned++; // state never put on the queue
            }
            return myPQueue;
        }






        /*
         * I traverse the rows of the state grid.
         * I find min value in row, and subsequently
         * subtract the min value from each index of row.
         *
         * Time complexity of this function is O(n^2) because of double nested for-loop
        */
        public TSPState reduceRows( TSPState state )
        {
            for ( int rowIndex = 0; rowIndex < state.grid.GetLength(0); rowIndex++ ) // O(n)
            {
                double minValueInRow = Double.MaxValue;
                for( int colIndex = 0; colIndex < state.grid.GetLength(0); colIndex++ ) // O(n)
                {
                    if( state.grid[rowIndex, colIndex] < minValueInRow )
                    {
                        minValueInRow = state.grid[rowIndex, colIndex];
                    }
                }
                if( minValueInRow != Double.MaxValue )
                {
                    for (int colIndex = 0; colIndex < state.grid.GetLength(0); colIndex++) // O(n)
                    {
                        state.grid[rowIndex, colIndex] -= minValueInRow;
                    }
                    state.lowerBoundOfState += minValueInRow;
                }
            }
            return state;
        }




        /*
         * I find the min value in each column, and then I
         * subtract that min value each cell in that column
         *
         * Time complexity of this function is O(n^2) because of double nested for-loop
        */
        public TSPState reduceCols( TSPState state )
        {
            for (int colIndex = 0; colIndex < state.grid.GetLength(0); colIndex++)    // O(n)
            {
                double minValueInCol = Double.MaxValue;
                for (int rowIndex = 0; rowIndex < state.grid.GetLength(0); rowIndex++) // O(n)
                {
                    if (state.grid[rowIndex, colIndex] < minValueInCol)
                    {
                        minValueInCol = state.grid[rowIndex, colIndex];
                    }
                }
                if( minValueInCol != Double.MaxValue)
                {
                    for (int rowIndex = 0; rowIndex < state.grid.GetLength(0); rowIndex++) // O(n)
                    {
                        state.grid[rowIndex, colIndex] -= minValueInCol;
                    }
                    state.lowerBoundOfState += minValueInCol;
                }
            }
            return state;
        }

        /*
         * This function finds the difference between the lower bound of the child exclude state,
         * and the lower bound of the child include state. We return that difference to the caller 
         * of this function, along with the actual child include and exclude states (all bundled
         * up inside of a struct).
         *
         * Time complexity of this function is O(n^2)
        */
        public ResultOfSplitOnThisEdge calculateDifferenceExcludeMinusInclude( int chosenRow, int chosenCol, TSPState state, 
            ResultOfSplitOnThisEdge currentOptimalRowCol )
        {
            TSPState includeState = new TSPState();
            includeState = deepCopyOfParentsEnteredExitedArrays( state, includeState );
            TSPState excludeState = new TSPState();
            excludeState = deepCopyOfParentsEnteredExitedArrays( state, excludeState );
            double[,] includeGrid = new double[Cities.Length, Cities.Length];
            double[,] excludeGrid = new double[Cities.Length, Cities.Length];
            for (int rowIndex = 0; rowIndex < Cities.Length; rowIndex++)  // O(n)
            {
                for (int colIndex = 0; colIndex < Cities.Length; colIndex++) // O(n)
                {
                    includeGrid[rowIndex, colIndex] = state.grid[rowIndex, colIndex];
                    excludeGrid[rowIndex, colIndex] = state.grid[rowIndex, colIndex];
                }
            }
            for (int rowIndex = 0; rowIndex < Cities.Length; rowIndex++) // O(n)
            {
                includeGrid[rowIndex, chosenCol] = Double.MaxValue; // make all cells in that row = INFINITY
            }
            for (int colIndex = 0; colIndex < Cities.Length; colIndex++) // O(n)
            {
                includeGrid[chosenRow, colIndex] = Double.MaxValue; // make all cells in that col = INFINITY
            }
            excludeGrid[chosenRow, chosenCol] = Double.MaxValue;
            includeState.grid = includeGrid;
            excludeState.grid = excludeGrid;
            includeState.lowerBoundOfState = state.lowerBoundOfState;
            includeState = reduceRows(includeState); // this function is O(n^2) time complexity
            includeState = reduceCols(includeState); // this function is O(n^2) time complexity
            excludeState.lowerBoundOfState = state.lowerBoundOfState;
            excludeState = reduceRows(excludeState); // this function is O(n^2) time complexity
            excludeState = reduceCols(excludeState); // this function is O(n^2) time complexity
            currentOptimalRowCol.excludeState = excludeState;
            currentOptimalRowCol.includeState = includeState;
            currentOptimalRowCol.excludeBoundMinusIncludeBound = (excludeState.lowerBoundOfState - includeState.lowerBoundOfState);
            return currentOptimalRowCol;
        }



        /*
         * Each of the two children need a copy of the parent's entered and exit arrays.
         * Since the parent and both of the children work independently of each other,
         * we need to implement a deep copy.
         *
         * Time complexity of this function is O(n)
        */
        TSPState deepCopyOfParentsEnteredExitedArrays( TSPState parentState, TSPState childState )
        {
            int[] entered = new int[Cities.Length];
            for (int cityIndex = 0; cityIndex < Cities.Length; cityIndex++) // O(n)
            {
                entered[cityIndex] = parentState.citiesEntered[cityIndex];
            }
            childState.citiesEntered = entered;
            int[] exited = new int[Cities.Length];
            for (int cityIndex = 0; cityIndex < Cities.Length; cityIndex++) // O(n)
            {
                exited[cityIndex] = parentState.citiesExited[cityIndex];
            }
            childState.citiesExited = exited;
            return childState;
        }




        /*
         * If you add edge (i,j) then you need to set to infinity (delete) some edges that are 
         * subsequently impossible and might lead to a premature cycle.  The arrays "entered"
         * and "exited" are initialized to -1 before processing is started, and updated as
         * processing continues.  Other premature cycles are prevented by our matrix updates.
         *
         * Time complexity of this function is O(n)
        */
        public TSPState preventPrematureCycles (TSPState state, int i, int j)
        {   
            state.citiesEntered[j] = i;
            state.citiesExited[i] = j;
            int start = i;
            int end = j;
            // the new edge may be part of a part of a partial solution. Go to the end of that solution
            while ( state.citiesExited[end] != -1 ) // could be up to O(n)
            {
                end = state.citiesExited[end];
            }
            // similarly, go to the start of the new partial solution
            while( state.citiesEntered[start] != -1) // could be up to O(n)
            {
                start = state.citiesEntered[start];
            }
            //delete the edges that would make partial cycles, unless we're ready to finish the tour
            if( state.numCitiesTraversedSoFar < Cities.Length - 1) 
            {
                while ( start != j ) //  O(n)
                {
                    state.grid[end, start] = Double.MaxValue;
                    state.grid[j, start] = Double.MaxValue;
                    start = state.citiesExited[start];
                }
            }
            return state;
        }


        /*
         * Once we have solved the problem, we are left with an exited array. This exited array
         * can tell us about every single edge that was added to the route.  Every single
         * index in the exited array tells us which city we went to next, except for one index,
         * which will store "-1." This shows that the very last city has not been exited yet 
         * (eventually it will exit to the very first city we start at, completing the Rudrata cycle).
         * We try to make a Route that has length equal to the size of the "Cities" array.  If we
         * can make such a Route, traversing from city, to city, to city, and the length is what
         * we want, we know that we started at the right first city. If not, we keep trying
         * to start at every other city, until we achieve what we desire, and we exit the loop.
         *
         * Time complexity of this function is O(n^2) because of double nested for-loop
        */
        public ArrayList calculateRouteFromExitedMatrix( int[] exitedArray , SortedList myPQueue)
        {
            ArrayList newRoute = new ArrayList();
            for (int cityIndex = 0; cityIndex < Cities.Length; cityIndex++) // O(n), for all of the cities
            {
                newRoute = new ArrayList();
                int currentCity = cityIndex;
                while ( currentCity != -1) // while we don't reach -1, O(n)
                {
                    newRoute.Add(Cities[currentCity]);
                    currentCity = exitedArray[currentCity];
                }
                if ( newRoute.Count == Cities.Length )
                {
                    break;  // then this is the correct one, return it
                }
            }
            return newRoute; // the draw method automatically connects the last element to the first
        }

        /*
         * We need to find the max value in a HashSet.
         * This max value corresponds to the largest number
         * of states in the PQueue at any one given moment.
         *
         * The size of the PQueue is checked (b^n) / 2 times, where b is the branch factor <=2,
         * and n is the number of cities in the Rudrata cycle (we check every iteration, or in other
         * words, every time after an include and exclude state are added to the PQueue). 
         *
         * Thus, iterating over this HashSet sizesOfPQueueThroughoutExecution is up to 
         * O(b^n) time complexity, which is subsumed within algorithmic O(b^n * n^3) complexity.
        */
        public int calculateMaxOfNumStatesInPQueue()
        {
            int largestValFound = int.MinValue;
            foreach ( int pQueueSize in sizesOfPQueueThroughoutExecution)
            {
                if( pQueueSize > largestValFound)
                {
                    largestValFound = pQueueSize;
                }
            }
            return largestValFound;
        }



        /*
         * When we count up the number of pruned states, we must include
         * those items in the queue that never got dequeued.
        */
        public void iterateOverQueueContentsAndSumWhatWillNeverBeDequeued( SortedList myPQueue )
        {
            for( int i = 0; i < myPQueue.Count; i++ )
            {
                if( ((TSPState)myPQueue.GetByIndex(i)).lowerBoundOfState > myGreedyBSSF )
                {
                    numStatesPruned += (myPQueue.Count - i); // we know that from here on out, everything's lower bound is larger than BSSF
                    break;
                }
            }
        }
        #endregion
    }
}
