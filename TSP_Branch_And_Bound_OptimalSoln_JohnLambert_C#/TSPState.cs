using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSP
{
    class TSPState
    {
        public double lowerBoundOfState;
        public double[,] grid;
        public int[] citiesEntered; // = new int[all nodes in graph] // this is entered
        public int[] citiesExited;// 
        public int numCitiesTraversedSoFar;
    }
}
