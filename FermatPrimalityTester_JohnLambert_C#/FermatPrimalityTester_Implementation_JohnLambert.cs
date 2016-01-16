/*
 John Lambert
 A Fermat Primality Tester
 September 14, 2015

 We implement a primality test that utilizes a modular exponentiation function.
 
 The time complexity of the overall algorithm is O(k*n^3), or if k is assumed
 to be small, O(n^3).  Why? Because we perform k executions of the modular
 exponentiation function. The modular exponentation function requires multiplication
 of z*z, or x*z*z.  That requires O(n^2) or O(2*(n^2)) for the multiplications.
 In addition, we perform division log(y) times when we find
 the result mod N. If we have an odd intermediate result, we find the result mod N
 in two steps. So that would be O(2*(n^2)) in worst case scenario.
 We perform these multiplications and divisions n times, where n is the length
 of the bit string y. In other words, n = log(y).
 We see overall time complexity of the algorithm is O(n^3).

 The time complexity of accessing my data structure is a drop in the bucket.
 I decided to use the HashSet instead of an array of integers for the storage
 of my integer bases. An array is obviously much slower: O(n) vs. O(1) for the HashSet. 
 O(1) complexity is disregarded since O(1) is lower order than O(n^3).

 Space complexity of the algorithm is O(k).  It would be O(1) if we did not have
 to store the bases to ensure sampling without replacement. The space complexity
 of the algorithm is that of the HashSet, since this is the only data structure
 we use: O(k).

 We disregard the Carmichael numbers in our check of the number N's primality.
 I considered implementing my own modular arithmetic function, but in the end
 decided to opt for the libary function mod "%" instead of using:

      while (oddYResult > modulus) {
          oddYResult -= modulus;
      }
 
 In order to calculate the probability of a number's primality, we use the
 the following equation: p = 1 - (1 / 2^k ). Clearly, if the number passes
 the Fermat Primality Test for more and more bases, we can be more and more
 confident of its primality.
*/

using System;
using System.Windows.Forms;
using System.Collections.Generic;


namespace PrimalityTester
{
    public partial class initalizeGUIWindow : Form
    {
        public initalizeGUIWindow() {
            InitializeComponent();
        }

        /*
         This method is called when the "solve" button is clicked by the user.  
         We perform some error-checking to ensure that a k value (number of bases
         to be verified) is provided by the user.

         We call our helper function that loops across these bases.

         Furthermore, if the user enters a k value greater than the integer N, 
         we set k to be N-2. This is necessary because there are only N-1 bases
         possible if sampling without replacement, and since we exclude the base 1,
         there are only N-2 possibilities for choosing a base.
        */
        private void solve_Click(object sender, EventArgs e)
        {
            if( k_value.Text.Equals("") ){
                output.Text = "You must specify a k-value";
                return;
            }
            int inputForTest = Convert.ToInt32( input.Text );
            bool isPrime = true;
            int k = Convert.ToInt32( k_value.Text ) ;
            if( k > inputForTest ) {
                k = inputForTest - 2 ;
            }
            isPrime = loopAcrossBases(k, isPrime, inputForTest);
            printOutput(isPrime, k);
        }

        /*
         We assume that the user-provided integer is prime, unless we can
         prove otherwise.  In this function, we try to prove otherwise by
         generating random bases (using a random number generator).

         Note that we choose random bases between 2 and N-1. We do not use 1
         as a base because it will always claim that the number is prime; ie
         it claims 1^3 mod 4 is congruent to 1, but we know 4 is not prime.
        */
        private bool loopAcrossBases(int k, bool isPrime, int inputForTest)
        {
            HashSet<int> randDifferentBases = new HashSet<int>();
            Random randomGenerator = new Random();
            for (int i = 0; i < k; i++) // modexp will be called k times
            {
                int randomIntegerVal = randomGenerator.Next(2, inputForTest - 1);
                while (randDifferentBases.Contains(randomIntegerVal))
                {
                    randomIntegerVal = randomGenerator.Next(2, inputForTest - 1);
                }
                randDifferentBases.Add(randomIntegerVal);
                if (ModExp(randomIntegerVal, inputForTest - 1, inputForTest) != 1)
                {
                    isPrime = false;
                    break;
                }
            }
            return isPrime;
        }

        /*
         This function prints out to the user the results of our primality testing.
        */
        private void printOutput(Boolean isPrime, int k)
        {
            if (isPrime == true)
            {
                double probability = 1 - (1 / Math.Pow(2, k));
                output.Text = input.Text + " is prime with confidence >= " + probability * 100 + " %";
            }
            else
            {
                output.Text = input.Text + " is not prime";
            }
        }

        /*
         This function calculates what x^y mod N is congruent to.  The caller of this function will
         be specifically interested in determining whether or not x^y mod N is congruent to 1. 
         Note: x is the base, y is the exponent, we call N the "modulus".
        
         We get overflow immediately for numbers much larger than 1024 because the
         machine cannot handle the length of the integer bit strings in its calculations.
         However, if we calculate the oddYResult in a series of steps, we avoid this problem:
         take z^2 mod N, and then multiply by x afterwards, and then calculate result mod N again. 
        */
        private int ModExp( int x, int y, int modulus )
        {
            if (y == 0) return 1;
            int z = 0;
            z = ModExp(x, (y / 2) , modulus );
            if ((y % 2) == 0) {
                int evenYResult = z * z;  // Multiplication is O(n^2)
                evenYResult = evenYResult % modulus;  // Division is O(n^2)
                return evenYResult;
            } else {
                int oddYResult  =  (z * z); // Multiplication is O(n^2)
                oddYResult = oddYResult % modulus; // Division is O(n^2)
                oddYResult = oddYResult * x; // Multiplication is O(n^2)
                oddYResult = oddYResult % modulus; // Division is O(n^2)
                return oddYResult;
            }
        }
    }
}
