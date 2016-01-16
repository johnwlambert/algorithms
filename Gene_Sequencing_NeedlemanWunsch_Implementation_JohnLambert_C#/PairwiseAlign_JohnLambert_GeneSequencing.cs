/*
* John Lambert
 * Gene Sequencing
 * 
 * Time complexity of Needleman - Wunsch is O(n ^ 2) since we do several calculations 
 * for each of n^2 cells. This is true for the Extraction and for the Scoring algorithm.
 *
 * The standard Needleman-Wunsch algorithm takes two taxa (taxa1 and taxa2).
 * We set the top row to ascending multiples of five.
 * We set the left column to ascending multiples of five.
 * Then we use dynamic programming to work from the top-left corner
 * down to the bottom-right corner.
 * 
 * To determine the cheapest alignment cost, we determine for every cell
 * of a m x n grid (Where taxa1 is length m, taxa2 is length n), whether
 * it would be cheaper to substitute a letter (from upper left cell on diagonal),
 * insert a hyphen to taxa1 (from left cell), or insert a hyphen to taxa2 (from top cell).
 * 
 * By doing so, we cover all of the possibilities.  The algorithm halts when the bottom-right
 * cell has been filled in. We return the distance to that cell.
*/


using System;
using System.Collections.Generic;
using System.Text;

namespace GeneticsLab
{
    class PairWiseAlign
    {

        /// <summary>
        /// Align only 5000 characters in each sequence.
        /// </summary>
        private int MaxCharactersToAlign = 5000;

        public string DELINEATOR_BETWEEN_ALIGNED_TAXA = "%%%";


        /// <summary>
        /// this is the function you implement.
        /// </summary>
        /// <param name="sequenceA">the first sequence</param>
        /// <param name="sequenceB">the second sequence, may have length not equal to the length of the first seq.</param>
        /// <param name="resultTableSoFar">the table of alignment results that has been generated so far using pair-wise alignment</param>
        /// <param name="rowInTable">this particular alignment problem will occupy a cell in this row the result table.</param>
        /// <param name="columnInTable">this particular alignment will occupy a cell in this column of the result table.</param>
        /// <returns>the alignment score for sequenceA and sequenceB.  The calling function places the result in entry rowInTable,columnInTable
        /// of the ResultTable</returns>
        /// 


        public struct CharNode // contains info for each cell in editDistance grid
        {
            public int distanceToHere;
            public int backpointerRowVal;
            public int backpointerColVal;
        }


        /*
        * Call the 2-Array low-space-complexity SCORING algorithm for every pair of taxa.
        */
        public int Align(GeneSequence sequenceA, GeneSequence sequenceB, ResultTable resultTableSoFar, int rowInTable, int columnInTable)
        {
            if (rowInTable == columnInTable) return 0;
            if (rowInTable > columnInTable) {
                double resultFromUpperTriangle = resultTableSoFar.GetCell( columnInTable, rowInTable);
                return (int)resultFromUpperTriangle;
            }
            return scoringAlgorithmWithoutExtraction(sequenceA, sequenceB);
        }



        /*
         * My Scoring Algorithm: O(n) space complexity because we fill out one row at a time, 
         * only need to keep two arrays around, plus a third temporary array , 
         * fill to row end, and move down to next row,
         * until get to bottom row. Each array is equal to length of taxa B.
         * Switch pointers so that contents of old array
         * are overwritten as if it is now the new array. Use Needleman - Wunsch, just without
         * the big array of arrays. Swap pointers to arrays at the end of each row.
         * 
         * We know that the time complexity of the SCORING algorithm is that of the
         * standard Needleman - Wunsch: O(n^2) since we do several operations 
         * for each of n^2 cells. In this case, "n" is the length of a general
         * gene sequence. The 10-20 operations that we perform for each cell act
         * as a constant factor in front of n^2.
        */
        public int scoringAlgorithmWithoutExtraction( GeneSequence sequenceA, GeneSequence sequenceB )
        {
            int seqALengthToUse = MaxCharactersToAlign; // we only align the first 5000 characters
            int seqBLengthToUse = MaxCharactersToAlign;
            if (sequenceA.Sequence.Length < MaxCharactersToAlign)
            {
                seqALengthToUse = sequenceA.Sequence.Length;
            }
            if (sequenceB.Sequence.Length < MaxCharactersToAlign)
            {
                seqBLengthToUse = sequenceB.Sequence.Length;
            }
            CharNode[] editDistPrevTopArray = new CharNode[ seqBLengthToUse + 1]; // O(n) space complexity, length of taxa B
            CharNode[] editDistCurrBottomArray = new CharNode[ seqBLengthToUse + 1]; // O(n) space complexity, length of taxa B
            CharNode[] temp = new CharNode[ seqBLengthToUse + 1]; // O(n) space complexity, length of taxa B

            for (int j = 0; j < seqBLengthToUse + 1; j++) // O(n) time complexity, loop over length of taxa B
            {
                CharNode nodeAlongTopHorizontal = editDistPrevTopArray[j];
                nodeAlongTopHorizontal.distanceToHere = (j * 5);
                editDistPrevTopArray[j] = nodeAlongTopHorizontal;
            }
            for (int i = 1; i < seqALengthToUse + 1; i++) // O(n^2) time complexity, double nested-for-loop
            {
                CharNode nodeAlongLeftVertical = editDistCurrBottomArray[0]; // far left column's dist = multiple of 5
                nodeAlongLeftVertical.distanceToHere = (i * 5);
                editDistCurrBottomArray[0] = nodeAlongLeftVertical;
                for (int j = 1; j < seqBLengthToUse + 1; j++) // this is the second for loop inside the double-nested for-loop
                {
                    int minimumDistBetweenCharsHere = int.MaxValue;
                    int distFromTopCell = editDistPrevTopArray[j].distanceToHere + 5; // THIS IS THE NEW VALUE THAT WE PUT AT i,j
                    int distFromLeftCell = editDistCurrBottomArray[j - 1].distanceToHere + 5;  // THIS IS THE NEW VALUE THAT WE PUT AT i,j
                    if (distFromTopCell <= distFromLeftCell)
                    {
                        editDistCurrBottomArray[ j ].backpointerRowVal = i - 1;
                        editDistCurrBottomArray[ j ].backpointerColVal = j;
                    }
                    else
                    {
                        editDistCurrBottomArray[ j ].backpointerRowVal = i;
                        editDistCurrBottomArray[ j ].backpointerColVal = j - 1;
                    }
                    minimumDistBetweenCharsHere = Math.Min(distFromTopCell, distFromLeftCell);
                    int diffBetweenIAndJ = 1;
                    if (sequenceA.Sequence.Substring(i - 1, 1) == sequenceB.Sequence.Substring(j - 1, 1))
                    {
                        diffBetweenIAndJ = -3;
                    }
                    int distFromTopLeftDiagCell = editDistPrevTopArray[ j - 1].distanceToHere + diffBetweenIAndJ;
                    if (distFromTopLeftDiagCell < minimumDistBetweenCharsHere) // then we change it. otherwise, already correct
                    {
                        editDistCurrBottomArray[ j ].backpointerRowVal = i - 1;
                        editDistCurrBottomArray[ j ].backpointerColVal = j - 1;
                    }
                    minimumDistBetweenCharsHere = Math.Min( minimumDistBetweenCharsHere, distFromTopLeftDiagCell );
                    editDistCurrBottomArray[ j ].distanceToHere = minimumDistBetweenCharsHere;
                }
                temp = editDistPrevTopArray;
                editDistPrevTopArray = editDistCurrBottomArray;
                editDistCurrBottomArray = temp;
            }
            return editDistPrevTopArray[ seqBLengthToUse ].distanceToHere; // this is edit distance at last row,last col
        }



        /*
         * My extraction algorithm uses the Standard Needleman-Wunsch Algorithm. We use 
           high-space complexity with a
         * m x n grid of CharNode structs, where m is the length of taxa A, and n is the length of taxa B.
         * Thus the extraction algorithm uses O(n^2) space because need to keep whole grid of CharNode structs. 
         * Each CharNode struct has a saved backpointer and an edit distance value.
         *
         * We know that the time complexity of the EXTRACTION algorithm is that of the
         * standard Needleman - Wunsch: O(n^2) since we do several operations 
         * for each of n^2 cells. In this case, "n" is the length of a general
         * gene sequence. The 10-20 operations that we perform for each cell act
         * as a constant factor in front of n^2.
         * Time complexity is equivalent to that of the scoring algorithm.
        */
        public string extractionAlgorithm(GeneSequence sequenceA, GeneSequence sequenceB )
        {
            int seqALengthToUse = 100;
            int seqBLengthToUse = 100;
            if (sequenceA.Sequence.Length < 100)
            {
                seqALengthToUse = sequenceA.Sequence.Length;
            }
            if (sequenceB.Sequence.Length < 100)
            {
                seqBLengthToUse = sequenceB.Sequence.Length;
            }
            CharNode[,] editDistanceGrid = new CharNode[seqALengthToUse + 1, seqBLengthToUse + 1]; // O(n^2) space
            for (int i = 0; i < seqALengthToUse + 1; i++) // O(n) time complexity with for-loop
            {
                CharNode nodeAlongLeftVertical = editDistanceGrid[i, 0];
                nodeAlongLeftVertical.distanceToHere = (i * 5);
                editDistanceGrid[i, 0] = nodeAlongLeftVertical;
            }

            for (int j = 0; j < seqBLengthToUse + 1; j++) // O(n) time complexity with for-loop
            {
                CharNode nodeAlongTopHorizontal = editDistanceGrid[0, j];
                nodeAlongTopHorizontal.distanceToHere = (j * 5);
                editDistanceGrid[0, j] = nodeAlongTopHorizontal;
            }

            for (int i = 1; i < seqALengthToUse + 1; i++) // O(n^2) time complexity with double-nested for-loop
            {
                for (int j = 1; j < seqBLengthToUse + 1; j++) // second for-loop within double-nested for-loop
                {
                    int minimumDistBetweenCharsHere = int.MaxValue;
                    int distFromTopCell = editDistanceGrid[i - 1, j].distanceToHere + 5; // We will place an edit dist. value at (i,j)
                    int distFromLeftCell = editDistanceGrid[i, j - 1].distanceToHere + 5; 
                    if (distFromTopCell <= distFromLeftCell)
                    {
                        editDistanceGrid[i, j].backpointerRowVal = i - 1;
                        editDistanceGrid[i, j].backpointerColVal = j;
                    }
                    else
                    {
                        editDistanceGrid[i, j].backpointerRowVal = i;
                        editDistanceGrid[i, j].backpointerColVal = j - 1;
                    }
                    minimumDistBetweenCharsHere = Math.Min(distFromTopCell, distFromLeftCell);
                    int diffBetweenIAndJ = 1;
                    if (sequenceA.Sequence.Substring(i - 1, 1) == sequenceB.Sequence.Substring(j - 1, 1))
                    {
                        diffBetweenIAndJ = -3;
                    }
                    int distFromTopLeftDiagCell = editDistanceGrid[i - 1, j - 1].distanceToHere + diffBetweenIAndJ;
                    if (distFromTopLeftDiagCell < minimumDistBetweenCharsHere) // then we change it. otherwise, already correct
                    {
                        editDistanceGrid[i, j].backpointerRowVal = i - 1;
                        editDistanceGrid[i, j].backpointerColVal = j - 1;
                    }
                    minimumDistBetweenCharsHere = Math.Min(minimumDistBetweenCharsHere, distFromTopLeftDiagCell);
                    editDistanceGrid[i, j].distanceToHere = minimumDistBetweenCharsHere;
                }
            }

            return executeBacktrace(editDistanceGrid, sequenceA, sequenceB, seqALengthToUse, seqBLengthToUse);
        }


        /*
         * I execute the backtrace by adding one character at a time to the beginning
         * of a string (not concatenating to the end, as is usually the case when editing 
         * strings).  I use a while loop to move from the bottom-right cell in the grid to 
         * traverse all the way until the origin (the upper-left hand cell of grid).  If 
         * the backpointer was from the cell directly to the left, we add a hyphen into taxa1. 
         * If the backpointer was to the cell directly above, we add a hyphen into taxa2.  
         * If the backpointer was to the backpointer in the upper-left cell, we overlay the 
         * words directly on top of each other (this is considered substitution).
         *
         * This function has O(2n) Time complexity, because we traverse the width
         * of the grid, and also the height of the grid, each once, as we move
         * from the bottom-right corner to the top-left corner.
         *
         * Space complexity of this function is O(n) since we only maintain
         * strings that are length n, where n is the length of a gene sequence.
         * 
         * We need to display these aligned strings in text boxes that the user will see.
         * We do so by returning them as one long string, packaged together and
         * delineated via a delineator. For example, the aligned strings:
         * 
         * POLYNOM-IAL
         * EXPONENTIAL
         *
         * Would be returned to the function that will parse them and place them into
         * their respective textboxes via the following encoding:
         *
         * POLYNOM-IAL%%%EXPONENTIAL
        */
        public string executeBacktrace(CharNode[,] editDistanceGrid, GeneSequence sequenceA, GeneSequence sequenceB, int seqALengthToUse, int seqBLengthToUse)
        {
            String editedSequenceA = "";
            String editedSequenceB = "";
            int currentRow = seqALengthToUse;
            int currentCol = seqBLengthToUse;
            int rowBackptr = 0;
            int colBackptr = 0;
            while (currentRow != 0 && currentCol != 0) // O(2n) complexity since we are just traversing width + height of grid
            {
                colBackptr = editDistanceGrid[currentRow, currentCol].backpointerColVal;
                rowBackptr = editDistanceGrid[currentRow, currentCol].backpointerRowVal;
                if ((rowBackptr == (currentRow - 1)) && (colBackptr == (currentCol - 1))) // Backpointer to Diagonal
                {
                    editedSequenceA = (sequenceA.Sequence.Substring(currentRow - 1, 1) + editedSequenceA);
                    editedSequenceB = (sequenceB.Sequence.Substring(currentCol - 1, 1) + editedSequenceB);
                }
                else if ((rowBackptr == (currentRow)) && (colBackptr == (currentCol - 1))) // Backpointer to the left
                {
                    editedSequenceA = ("-" + editedSequenceA);
                    editedSequenceB = (sequenceB.Sequence.Substring(currentCol - 1, 1) + editedSequenceB);
                }
                else // Backpointer to the right
                {
                    editedSequenceA = (sequenceA.Sequence.Substring(currentRow - 1, 1) + editedSequenceA);
                    editedSequenceB = ("-" + editedSequenceB);
                }
                currentRow = rowBackptr;
                currentCol = colBackptr;
            }
            return ( editedSequenceA + DELINEATOR_BETWEEN_ALIGNED_TAXA + editedSequenceB); // return aligned strings, packaged together
        }
    }
}
