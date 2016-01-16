        public void solveProblem()
        {
            // run our farthest insertion
            farthestInsertionAlgorithm();
        }


        /*
                * 
                */
        public void farthestInsertionAlgorithm()
        {
            //for (int i = 0; i < Cities.Length; i++)
            //{
            //    Console.WriteLine("City located at (" + Cities[i].X + "," + Cities[i].Y + ")");
            //    for (int j = 0; j < Cities.Length; j++)
            //    {
            //        Console.WriteLine("Cost to get to this city is: " + Cities[i].costToGetTo(Cities[j]));
            //    }
            //}
            Stopwatch s = new Stopwatch();
            s.Start();

            ArrayList currentTourIndices = initializeTour();
            while (currentTourIndices.Count < Cities.Length)
            {
                int selectedCity = selectFarthestFromClosestCityToIt(currentTourIndices);
                currentTourIndices = insertWhereverEdgeLengthIsMinimized(currentTourIndices, selectedCity);
            }
            ArrayList routeOfCityObjects = new ArrayList();
            for (int cityIndex = 0; cityIndex < currentTourIndices.Count ; cityIndex++)
            {
                routeOfCityObjects.Add(Cities[ (int)currentTourIndices[cityIndex] ] );
            }
            bssf = new TSPSolution(routeOfCityObjects ); //  bssf is the route that will be drawn by the Draw method. 
            Console.WriteLine("This Many Milliseconds to reach solution" + s.ElapsedMilliseconds);
            s.Stop();
            TimeSpan ts = s.Elapsed;
            Program.MainForm.tbElapsedTime.Text = String.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            // update the cost of the tour. 
            Program.MainForm.tbCostOfTour.Text = " " + bssf.costOfRoute(); // this automatically will find the greedy bssf path's cost
            Program.MainForm.Invalidate();  // do a refresh. 
        }

        /*
         * Start with a partial tour with just one city i, randomly chosen;
         * find the city j for which c ij (distance or cost from i to j) is minimum
         * and build the partial tour (i, j).
        */
        public ArrayList initializeTour( )
        {
            ArrayList currentTourIndices = new ArrayList();
            // randomly choose a city i, and make it a partial tour by adding it
            //currentTourIndices.Add(rnd.Next(0, Cities.Length)); // we add a random city from the Cities array
            currentTourIndices.Add(0);
            currentTourIndices = findAndAddCityFarthestFromInitialNode( currentTourIndices );
            return currentTourIndices;
        }


        /*
         *
        */
        public ArrayList findAndAddCityFarthestFromInitialNode(ArrayList currentTourIndices )
        {
            // Find the next city that will be added to the tour
            double farthestDistance  = Double.MinValue;
            int indexOfNodeWithFarthestDistance = -1;

            for (int i = 0; i < Cities.Length; i++)
            {
                if (!currentTourIndices.Contains(i))
                {
                    // find closest node to this city
                    double currentDistance = Cities[(int)currentTourIndices[0]].costToGetTo( Cities[i] );
                    if (currentDistance > farthestDistance )
                    {
                        farthestDistance = currentDistance;
                        indexOfNodeWithFarthestDistance = i;  // closestNodeIndex;
                    }
                }
            }
            currentTourIndices.Add( indexOfNodeWithFarthestDistance ); // add index of city from cities that should be added next
            return currentTourIndices;
        }


        /*
         * Find cities k and j (j belonging to the partial tour and k not belonging) for which min_kj{c_kj} is maximized
         *
         * Given a sub-tour, find node r not in the sub-tour farthest from any node in the sub-tour; i.e. with maximal c_rj
        */
        public int selectFarthestFromClosestCityToIt(ArrayList currentTourIndices) { // Find the next city that will be added to the tour
            
            double maxDistFromTour = Double.MinValue;
            int indexOfFarthestNodeFromTour = -1;
            for (int i = 0; i < Cities.Length; i++)
            {
                if (!currentTourIndices.Contains(i)) // then not on tour,  find closest dist to this city
                {
                    double closestDistBetweenNodeOnAndThisNodeOff = Double.MaxValue;

                    for (int j = 0; j < currentTourIndices.Count; j++) // find the shortest distance to this node
                    {
                        double currentDist = Cities[(int)currentTourIndices[j]].costToGetTo( Cities[i] );
                        if (currentDist < closestDistBetweenNodeOnAndThisNodeOff)
                        {
                            closestDistBetweenNodeOnAndThisNodeOff = currentDist;
                        }
                    }

                    if (closestDistBetweenNodeOnAndThisNodeOff > maxDistFromTour) // if that shortest distance greater than shortest distances to other nodes, then 
                    {
                        maxDistFromTour = closestDistBetweenNodeOnAndThisNodeOff;
                        indexOfFarthestNodeFromTour = i;
                    }
                }
            }
            return indexOfFarthestNodeFromTour ; // this is the index of city from cities that should be added next
        }



        /*
         * Find the edge (i,j), belonging to the partial tour, that minimizes c_ik + c_kj - c_ij. Insert k between i and j.
         *
         * Find the arc (i,j) in the sub-tour which minimizes c_ir + c_rj - c_ij. Insert r between i and j.
        */
        public ArrayList insertWhereverEdgeLengthIsMinimized(ArrayList currentTourIndices, int selectedCity)
        {
            int bestCityIIndex = -1;
            int bestCityJIndex = -1;
            double cheapestExpressionDist = Double.MaxValue;

            int cityIIndex = -1;
            int cityJIndex = -1;
            double expressionDist = Double.MaxValue;
            //Loop over every edge
            for (int cityIndex = 0; cityIndex < (currentTourIndices.Count - 1); cityIndex++)
            {
                cityIIndex = (int)currentTourIndices[cityIndex]; // current position in tour
                cityJIndex = (int)currentTourIndices[cityIndex + 1]; // next position in tour, cover every edge this way
                expressionDist = Cities[cityIIndex].costToGetTo( Cities[selectedCity] ) + Cities[selectedCity].costToGetTo(Cities[cityJIndex]) - Cities[cityIIndex].costToGetTo(Cities[cityJIndex]);
                if (expressionDist < cheapestExpressionDist)
                {
                    bestCityIIndex = cityIndex;
                    bestCityJIndex = cityIndex + 1;
                    cheapestExpressionDist = expressionDist;
                }
            }
            // then check the edge that was missed during the for loop
            cityIIndex = (int)currentTourIndices[currentTourIndices.Count - 1]; // last position in tour
            cityJIndex = (int)currentTourIndices[0]; // first position in tour
            expressionDist = Cities[cityIIndex].costToGetTo( Cities[selectedCity] ) + Cities[selectedCity].costToGetTo(Cities[cityJIndex]) - Cities[cityIIndex].costToGetTo(Cities[cityJIndex]);
            if ( expressionDist < cheapestExpressionDist)
            {
                bestCityIIndex = currentTourIndices.Count - 1;
                bestCityJIndex = 0; // then we add it add the end in this special case; else, we add it as listed below
            }
            // do I have to try it backwards now too? NO THATS IMPOSSIBLE, WE ONLY HAVE GUARANTEED DIRECTION ONE WAY
            //currentTourIndices.Add(-1);

            //for( int i = bestCityI + 1; i < currentTourIndices.Count - 1 ; i++ )
            //{
            //    currentTourIndices[i+1%currentTourIndices.Count] = currentTourIndices[ i ];
            //}
            currentTourIndices.Insert( bestCityIIndex + 1, selectedCity); // should insert in between those two cities
            return currentTourIndices;
        }

