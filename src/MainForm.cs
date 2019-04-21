using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

namespace BorderlandsNumPlayersSetter
{
    public partial class MainForm : Form
    {
        private GameInformation CurrentGame { get; set; }
        private uint? valLocation = null;
        private int maxMemoryLocation = int.MaxValue;
        private int minMemoryLocation = 0;
        MemoryScanAsync scanner = new MemoryScanAsync();
        private Thread scannerThread;
        public MainForm()
        {
            InitializeComponent();
            scanner.ScanCanceled += new MemoryScanAsync.ScanCanceledEventHandler(scanner_ScanCanceled);
            scanner.ScanCompleted += new MemoryScanAsync.ScanCompletedEventHandler(scanner_ScanCompleted);
            scanner.ScanProgressChanged += new MemoryScanAsync.ScanProgressedEventHandler(scanner_ScanProgressChanged);
        }

        void scanner_ScanProgressChanged(object sender, ScanProgressChangedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new BorderlandsNumPlayersSetter.MemoryScanAsync.ScanProgressedEventHandler(scanner_ScanProgressChanged), new[] { sender, e });
                return;
            }
            this.progressBar1.Value = e.Progress;
        }

        void scanner_ScanCompleted(object sender, ScanCompletedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new BorderlandsNumPlayersSetter.MemoryScanAsync.ScanCompletedEventHandler(scanner_ScanCompleted), new[] { sender, e });
                return;
            }
            if (e.MemoryAddresses == null || e.MemoryAddresses.Length == 0)
            {
                this.valLocation = null;
                MessageBox.Show("# Players memory location not found.  Please make sure you've started the game, selected a character, and that you are currently the only person in your game.");
                this.lblProgress.Text = "Player value not found.";
                this.valLocation = null;
                this.slider.Enabled = false;
                this.btnCancel.Enabled = false;
                this.btnScan.Enabled = true;
                this.progressBar1.Value = 0;
                return;
            }
            this.lblProgress.Text = "Player value found at " + e.MemoryAddresses[0].ToString("X");
            this.valLocation = e.MemoryAddresses[0];
            this.slider.Enabled = true;
            this.btnCancel.Enabled = false;
            this.btnScan.Enabled = false;
            this.progressBar1.Value = 0;
            Properties.Settings.Default.LastFindLocation = this.valLocation.Value;
            Properties.Settings.Default.Save();
        }

        void scanner_ScanCanceled(object sender, ScanCanceledEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new BorderlandsNumPlayersSetter.MemoryScanAsync.ScanCanceledEventHandler(scanner_ScanCanceled), new[] { sender, e });
                return;
            }
            if (e.Exception != null)
                MessageBox.Show("An error occured while searching for the location value.  " + e.Exception.ToString());

            this.lblProgress.Text = "Search Canceled.";
            this.slider.Enabled = false;
            this.btnCancel.Enabled = false;
            this.btnScan.Enabled = true;
            this.progressBar1.Value = 0;
        }

        private void slider_Scroll(object sender, EventArgs e)
        {
            //check to make sure the player location is the same.
            SetNumPlayers();
        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            this.slider.Enabled = false;
            this.btnCancel.Enabled = true;
            this.btnScan.Enabled = false;
            this.progressBar1.Value = 0;
            this.slider.Value = 1;

            this.lblProgress.Text = "Progress:";
            FindPlayerLocation();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            scanner.Cancel = true;
        }

        private Process GetBorderlandsProcess()
        {
            Process process = null;
            foreach (GameInformation g in GameInformation.Games)
            {
                Process[] processes = Process.GetProcessesByName(g.ProcessName);
                if (processes != null && processes.Length > 0)
                {
                    process = processes[0];
                    this.CurrentGame = g;
                    break;
                }
            }
            if (process == null)
            {
                MessageBox.Show("Borderlands/Borderlands2 process not found.  Make sure you've started the game.");
            }
            return process;
        }

        private void FindPlayerLocation()
        {
            ProcessMemoryReader pr = new ProcessMemoryReader();
            pr.ReadProcess = GetBorderlandsProcess();//don't open this one, the thread will open and close it.
            if (pr.ReadProcess == null)
            {
                this.valLocation = null;
                return;
            }
            MemoryScanParameters parms = new MemoryScanParameters(CurrentGame.ValToFind, Properties.Settings.Default.LastFindLocation, minMemoryLocation, maxMemoryLocation, pr);
            ParameterizedThreadStart pts = new ParameterizedThreadStart(scanner.ScanMemoryNew);
            scannerThread = new Thread(pts);
            scannerThread.Start(parms);
        }

        private void WriteValueToMemory(ProcessMemoryReader pr, byte numPlayers)
        {
            byte[] newValue = new byte[] { numPlayers };
            foreach (int offset in this.CurrentGame.ValToChangeOffset)
            {
                int numBytesWritten;
                pr.WriteProcessMemory((IntPtr)(this.valLocation.Value + offset), newValue, out numBytesWritten);
            }
        }

        private bool NumPlayersIsGood()
        {
            ProcessMemoryReader pr = new ProcessMemoryReader();
            pr.ReadProcess = GetBorderlandsProcess();
            if (pr.ReadProcess == null)
                return false;
            pr.OpenProcess();
            int bytesRead;
            byte[] valToTest = pr.ReadProcessMemory((IntPtr)this.valLocation.Value, (uint)this.CurrentGame.ValToFind.Length, out bytesRead);
            if (MemoryScanAsync.ArrayEqual(this.CurrentGame.ValToFindWildcard, valToTest))
            {
                pr.CloseHandle();
                return true;
            }
            else
            {
                pr.CloseHandle();
                return false;
            }
        }

        private void SetNumPlayers()
        {
            ProcessMemoryReader pr = new ProcessMemoryReader();
            pr.ReadProcess = GetBorderlandsProcess();
            if (pr.ReadProcess == null)
                return;
            pr.OpenProcess();
            if (NumPlayersIsGood())
            {
                WriteValueToMemory(pr, (byte)this.slider.Value);
            }
            else
            {
                MessageBox.Show("Player value has changed.  Please rescan.");
                this.slider.Value = 1;
                this.slider.Enabled = false;
                this.btnCancel.Enabled = false;
                this.btnScan.Enabled = true;
                this.progressBar1.Value = 0;
            }
            this.lblNumPlayers.Text = "# Players: " + this.slider.Value.ToString();
            pr.CloseHandle();
        }
    }
}
