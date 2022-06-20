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
            // Here is a change made on the test-github-pr-diff-source1 branch
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
