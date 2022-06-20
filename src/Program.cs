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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
