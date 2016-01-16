using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;


namespace GeneticsLab
{
    public partial class MainForm : Form
    {
        ResultTable m_resultTable;
        GeneSequence[] m_sequences;
        const int NUMBER_OF_SEQUENCES = 10;
        const string GENOME_FILE = "genomes.txt";

        public string DELINEATOR_BETWEEN_ALIGNED_TAXA = "%%%";


        public MainForm()
        {
            InitializeComponent();

            statusMessage.Text = "Loading Database...";

            // load database here

            try
            {
                m_sequences = loadFile("../../" + GENOME_FILE);
            }
            catch (FileNotFoundException e)
            {
                try // Failed, try one level down...
                {
                    m_sequences = loadFile("../" + GENOME_FILE);
                }
                catch (FileNotFoundException e2)
                {
                    // Failed, try same folder
                    m_sequences = loadFile(GENOME_FILE);
                }
            }

            m_resultTable = new ResultTable(this.dataGridViewResults, NUMBER_OF_SEQUENCES);

            statusMessage.Text = "Loaded Database.";

        }

        private GeneSequence[] loadFile(string fileName)
        {
            StreamReader reader = new StreamReader(fileName);
            string input = "";

            try
            {
                input = reader.ReadToEnd();
            }
            catch
            {
                Console.WriteLine("Error Parsing File...");
                return null;
            }
            finally
            {
                reader.Close();
            }

            GeneSequence[] temp = new GeneSequence[NUMBER_OF_SEQUENCES];
            string[] inputLines = input.Split('\r');

            for (int i = 0; i < NUMBER_OF_SEQUENCES; i++)
            {
                string[] line = inputLines[i].Replace("\n","").Split('#');
                temp[i] = new GeneSequence(line[0], line[1]);
            }
            return temp;
        }



        /*
        * This function fills my grid of 10 taxa by 10 taxa. We call our scoring
        * algorithm on each pair.
        */
        private void fillMatrix()
        {
            PairWiseAlign processor = new PairWiseAlign();
            for (int x = 0; x < NUMBER_OF_SEQUENCES; ++x)
            {
                for (int y = 0; y < NUMBER_OF_SEQUENCES; ++y)
                {
                    m_resultTable.SetCell(x, y, processor.Align(m_sequences[x], m_sequences[y],m_resultTable,x,y));
                }
            }
        }

        private void processButton_Click(object sender, EventArgs e)
        {
            statusMessage.Text = "Processing...";
            Stopwatch timer = new Stopwatch();
            timer.Start();
                   fillMatrix();
            timer.Stop();
            statusMessage.Text = "Done.  Time taken: " + timer.Elapsed;
        }



        /*
        * When I click on a cell in the 10x10 grid of 10 taxa
        * by 10 taxa, my extraction algorithm is executed on the 
        * two taxa that correspond to that row and column index in 
        * the grid.
        * 
        * Thus we pass in two arguments to our extraction algorithm: 
        * a GeneSequence sequenceA, and a GeneSequence sequenceB.
        *
        * My extraction algorithm returns the two aligned strings
        * as one string, separated by a delineator. I separate the
        * two strings and display them into two different text boxes.
        *
        * I use a font that prints each character as the same width.
        */
        private void dataGridViewResults_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            PairWiseAlign processor = new PairWiseAlign();
            string alignedStrings = processor.extractionAlgorithm(m_sequences[e.RowIndex], m_sequences[e.ColumnIndex] );
            string alignedSequenceA = ""; // Will be O(n) space complexity
            string alignedSequenceB = "";  // Will be O(n) space complexity
            bool onSecondString = false;
            for( int i = 1; i < alignedStrings.Length + 1 ; i++ ) // O(n) time complexity
            {
                if( onSecondString == false )
                {
                    if (alignedStrings.Substring(i - 1, 3) == DELINEATOR_BETWEEN_ALIGNED_TAXA )
                    {
                        onSecondString = true;
                        i = (i + 3);
                    }
                }
                if( onSecondString == false)
                {
                    alignedSequenceA += alignedStrings.Substring(i - 1, 1);
                } else
                {
                    alignedSequenceB += alignedStrings.Substring(i - 1, 1);
                }
            }
            sequenceANowAligned.Font = new Font(FontFamily.GenericMonospace, sequenceANowAligned.Font.Size);
            sequenceBNowAligned.Font = new Font(FontFamily.GenericMonospace, sequenceBNowAligned.Font.Size);
            sequenceANowAligned.Text = alignedSequenceA;  // Print out the newly aligned sequences
            sequenceBNowAligned.Text = alignedSequenceB; 
        }

}
}
