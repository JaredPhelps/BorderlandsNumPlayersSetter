using System;
using System.Collections.Generic;
using System.Text;

namespace BorderlandsNumPlayersSetter
{
    #region EventArgs classes
    public class ScanProgressChangedEventArgs : EventArgs
    {
        public ScanProgressChangedEventArgs(int Progress)
        {
            progress = Progress;
        }
        private int progress;
        public int Progress
        {
            set
            {
                progress = value;
            }
            get
            {
                return progress;
            }
        }
    }

    public class ScanCompletedEventArgs : EventArgs
    {
        public ScanCompletedEventArgs(uint[] MemoryAddresses)
        {
            memoryAddresses = MemoryAddresses;
        }
        private uint[] memoryAddresses;
        public uint[] MemoryAddresses
        {
            set
            {
                memoryAddresses = value;
            }
            get
            {
                return memoryAddresses;
            }
        }
    }

    public class ScanCanceledEventArgs : EventArgs
    {
        public Exception Exception { get; set; }
        public ScanCanceledEventArgs()
        {
        }
    }
    #endregion

    public class MemoryScanParameters
    {
        public MemoryScanParameters(byte[] valToFind, uint startLocation, int minLocation, int maxLocation, ProcessMemoryReader reader)
        {
            ValToFind = valToFind;
            StartLocation = startLocation;
            MinLocation = minLocation;
            MaxLocation = maxLocation;
            Reader = reader;
        }
        public byte[] ValToFind { get; set; }
        public uint StartLocation { get; set; }
        public int MinLocation { get; set; }
        public int MaxLocation { get; set; }
        public ProcessMemoryReader Reader { get; set; }
    }

    public class MemoryScanAsync
    {
        const int PAGE_SIZE = 4096;
        #region Delegate and Event objects
        //Delegate and Event objects for raising the ScanProgressChanged event.
        public delegate void ScanProgressedEventHandler(object sender, ScanProgressChangedEventArgs e);
        public event ScanProgressedEventHandler ScanProgressChanged;

        //Delegate and Event objects for raising the ScanCompleted event.
        public delegate void ScanCompletedEventHandler(object sender, ScanCompletedEventArgs e);
        public event ScanCompletedEventHandler ScanCompleted;

        //Delegate and Event objects for raising the ScanCanceled event.
        public delegate void ScanCanceledEventHandler(object sender, ScanCanceledEventArgs e);
        public event ScanCanceledEventHandler ScanCanceled;
        public volatile bool Cancel = false;
        #endregion

        public MemoryScanParameters Parameters { get; set; }

        public void ScanMemoryNew(object memoryScanParameters)
        {
            ProcessMemoryReader pr = null;
            try
            {
                MemoryScanParameters parameters = (MemoryScanParameters)memoryScanParameters;
                Parameters = parameters;
                byte[] valToFind = parameters.ValToFind;
                int maxMemoryLocation = parameters.MaxLocation;
                int minMemoryLocation = parameters.MinLocation;
                uint startLocation = parameters.StartLocation;
                bm = new BoyerMoore(valToFind);
                pr = parameters.Reader;
                List<MEMORY_BASIC_INFORMATION> pages = ProcessMemoryMapper.GetMemoryMap(pr.ReadProcess);
                List<MemoryPage> memPages = ProcessMemoryMapper.CondensePages(pages, 4096 * 4);
                int r = memPages.Count / 2;
                for (int i = 0; i < memPages.Count; i++)
                {
                    uint low = (uint)memPages[i].BaseAddress;
                    uint high = low + memPages[i].RegionSize;
                    if (startLocation >= low && startLocation < high)
                    {
                        r = i;
                    }
                }
                int l = r - 1;

                int bytesRead = 0;
                int lastPercentage = 0;

                Decimal onePercentAmount = memPages.Count / 100.0M;

                pr.OpenProcess();
                while (r < memPages.Count || l >= 0)
                {
                    if (Cancel)
                    {
                        this.ScanCanceled(this, new ScanCanceledEventArgs());
                        Cancel = false;
                        return;
                    }

                    if ((r - l) / (Decimal)memPages.Count > lastPercentage / 100M)
                    {
                        lastPercentage++;
                        this.ScanProgressChanged(this, new ScanProgressChangedEventArgs(lastPercentage));
                    }

                    if (r < memPages.Count)
                    {
                        MemoryPage mbi = memPages[r];
                        byte[] valToTest = pr.ReadProcessMemory(mbi.BaseAddress, mbi.RegionSize, out bytesRead);
                        if (bytesRead == 0)
                        {
                            r++;
                            continue;
                        }
                        int foundIndex = ArrayIndexOf(valToTest, valToFind);
                        if (foundIndex >= 0)
                        {
                            this.ScanCompleted(this, new ScanCompletedEventArgs(new [] { (uint)mbi.BaseAddress + (uint)foundIndex }));
                            return;
                        }
                    }
                    if (l >=0)
                    {
                        MemoryPage mbi = memPages[l];
                        byte[] valToTest = pr.ReadProcessMemory(mbi.BaseAddress, mbi.RegionSize, out bytesRead);
                        if (bytesRead == 0)
                        {
                            l--;
                            continue;
                        }
                        int foundIndex = ArrayIndexOf(valToTest, valToFind);
                        if (foundIndex >= 0)
                        {
                            this.ScanCompleted(this, new ScanCompletedEventArgs(new[] { (uint)mbi.BaseAddress + (uint)foundIndex }));
                            return;
                        }
                    }
                    if (r < memPages.Count)
                        r++;
                    if (l >= 0)
                        l--;
                }

                this.ScanCompleted(this, new ScanCompletedEventArgs(null));
            }
            catch (Exception ex)
            {
                this.ScanCanceled(this, new ScanCanceledEventArgs() { Exception = ex });
            }
            finally
            {
                pr.CloseHandle();
            }
        }

        private BoyerMoore bm = null;
        public int ArrayIndexOf(byte[] searchIn, byte[] searchFor)
        {
            if (searchIn == null || searchFor == null || searchFor.Length > searchIn.Length)
                return -1;
            return bm.BoyerMooreMatch(searchIn);
        }


        public static bool ArrayEqual(byte?[] b1, byte[] b2)
        {
            if (b1 == null || b2 == null || b1.Length != b2.Length)
                return false;
            for (int i = 0; i < b1.Length; i++)
            {
                if (b1[i] == null && b2[i] > 0 && b2[i] <= 4)
                    continue;
                if (b1[i] != b2[i])
                    return false;
            }
            return true;
        }
    }
}
