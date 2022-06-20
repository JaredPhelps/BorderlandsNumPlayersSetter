using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace BorderlandsNumPlayersSetter
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Here is a change in master that should conflict with the test branches
            // Here is a change made on the test-github-pr-diff-source1 branch
            // Here is a change made on the test-github-pr-diff-source2 branch, which was based on the source1 branch
            // Here is a change made during merge conflict resolution
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
