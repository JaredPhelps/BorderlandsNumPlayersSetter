using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;

namespace BorderlandsNumPlayersSetter
{
    public class MemoryPage
    {
        public MemoryPage(MEMORY_BASIC_INFORMATION mbi) { BaseAddress = mbi.BaseAddress; RegionSize = mbi.RegionSize; }
        public void Add(MEMORY_BASIC_INFORMATION mbi) { RegionSize += mbi.RegionSize; }
        public IntPtr BaseAddress { get; set; }
        public uint RegionSize { get; set; }
    }
    class ProcessMemoryMapper
    {
        public static List<MemoryPage> CondensePages(List<MEMORY_BASIC_INFORMATION> pages, int maxPageSize)
        {
            List<MemoryPage> newPages = new List<MemoryPage>();
            MemoryPage mp = null;
            int i = 0;
            while (i < pages.Count)
            {
                while (i < pages.Count && !ProcessMemoryMapper.PageAccessible(pages[i]))
                {
                    if (mp != null)
                    {
                        newPages.Add(mp);
                        mp = null;
                    }
                    i++;
                }
                while (i < pages.Count && ProcessMemoryMapper.PageAccessible(pages[i]))
                {
                    if (mp == null)
                    {
                        mp = new MemoryPage(pages[i]);
                    }
                    else if (mp.RegionSize + pages[i].RegionSize > maxPageSize)
                    {
                        newPages.Add(mp);
                        mp = null;
                        break;
                    }
                    else
                    {
                        mp.Add(pages[i]);
                    }
                    i++;
                }
            }
            if (mp != null)
                newPages.Add(mp);
            return newPages;
        }

        public static List<MEMORY_BASIC_INFORMATION> GetMemoryMap(Process appProcess)
        {
            // Get the SYSTEM_INFO to determine the maximum possible
            // memory for the app.
            SYSTEM_INFO si = new SYSTEM_INFO();
            GetSystemInfo(ref si);

            IntPtr hReadProcHandle = IntPtr.Zero;
            IntPtr hToken = IntPtr.Zero;
            List<MEMORY_BASIC_INFORMATION> memoryPages = new List<MEMORY_BASIC_INFORMATION>();

            try
            {
                // Open the Process Handle and set it for DEBUG/READ access
                hReadProcHandle = OpenProcessForDebug(appProcess, out hToken);

                uint startMem = 0x0;

                // Loop through until you get to the end of the application
                // memory
                while (startMem < si.lpMaximumApplicationAddress)
                {
                    // Determine the Page info
                    MEMORY_BASIC_INFORMATION mbi = new MEMORY_BASIC_INFORMATION();
                    int size = VirtualQueryEx(appProcess.Handle, (IntPtr)startMem, ref mbi, Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)));
                    memoryPages.Add(mbi);

                    // Go to next page
                    startMem = (UInt32)mbi.BaseAddress + mbi.RegionSize;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR:{0}{1}{0}MORE INFO:{0}{2}", Environment.NewLine, ex.Message, ex);
            }
            finally
            {
                if (hToken != IntPtr.Zero)
                    CloseHandle(hToken);

                if (hReadProcHandle != IntPtr.Zero)
                    CloseHandle(hReadProcHandle);
            }
            return memoryPages;
        }

        public static bool PageAccessible(MEMORY_BASIC_INFORMATION mbi)
        {
            if (mbi.AllocationProtect == MEMORY_PROTECT.PAGE_UNKNOWN) return false;
            else if (mbi.Protect == MEMORY_PROTECT.PAGE_UNKNOWN) return false;
            else if ((mbi.AllocationProtect & MEMORY_PROTECT.PAGE_GUARD) == MEMORY_PROTECT.PAGE_GUARD) return false;
            else if ((mbi.Protect & MEMORY_PROTECT.PAGE_GUARD) == MEMORY_PROTECT.PAGE_GUARD) return false;
            else if ((mbi.AllocationProtect & MEMORY_PROTECT.PAGE_NOACCESS) == MEMORY_PROTECT.PAGE_NOACCESS) return false;
            else if ((mbi.Protect & MEMORY_PROTECT.PAGE_NOACCESS) == MEMORY_PROTECT.PAGE_NOACCESS) return false;

            return true;
        }

        static IntPtr OpenProcessForDebug(Process process, out IntPtr hToken)
        {
            IntPtr hProc = IntPtr.Zero;
            hToken = IntPtr.Zero;

            try
            {
                // Open the Process for READ
                hProc = OpenProcess(PROCESS_VM_READ, false, process.Id);

                // Get the security token for the process
                if (!OpenProcessToken(process.Handle, TOKEN_QUERY | TOKEN_ADJUST_PRIVILEGES, out hToken))
                {
                    int lastError = Marshal.GetLastWin32Error();
                    throw new ApplicationException(string.Format("OpenProcessToken returned Win32 Error {0}", lastError));
                }

                LUID luid;

                // Determine the LUID for the Debug privilege
                if (!LookupPrivilegeValue(null, SE_DEBUG_NAME, out luid))
                {
                    int lastError = Marshal.GetLastWin32Error();
                    throw new ApplicationException(string.Format("LookupPrivilegeValue returned Win32 Error {0}", lastError));
                }

                TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES();
                tp.PrivilegeCount = 1;
                tp.Privileges.Luid = luid;
                tp.Privileges.Attributes = SE_PRIVILEGE_ENABLED;

                // Set DEBUG=ENABLED in security token
                if (!AdjustTokenPrivileges(hToken, false, ref tp, Marshal.SizeOf(tp), IntPtr.Zero, IntPtr.Zero))
                {
                    int lastError = Marshal.GetLastWin32Error();
                    throw new ApplicationException(string.Format("AdjustTokenPrivileges returned Win32 Error {0}", lastError));
                }

                return hProc;
            }
            catch (Exception ex)
            {
                if (hToken != IntPtr.Zero)
                {
                    CloseHandle(hToken);
                    hToken = IntPtr.Zero;
                }

                if (hProc != IntPtr.Zero)
                    CloseHandle(hProc);

                throw ex;
            }
        }

        const UInt32 PROCESS_VM_READ = 0x0010;
        const UInt32 STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        const UInt32 SYNCHRONIZE = 0x00100000;
        const UInt32 PROCESS_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0xFFF;
        const Int32 TOKEN_QUERY = 0x0008;
        const Int32 TOKEN_ADJUST_PRIVILEGES = 0x20;
        const string SE_DEBUG_NAME = "SeDebugPrivilege";
        const Int32 SE_PRIVILEGE_ENABLED = 0x00000002;

        [DllImport("Kernel32.dll")]
        static extern void GetSystemInfo(ref SYSTEM_INFO systemInfo);
        // void GetSystemInfo( LPSYSTEM_INFO lpSystemInfo );

        [DllImport("Kernel32.dll")]
        static extern Int32 VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, ref MEMORY_BASIC_INFORMATION buffer, Int32 dwLength);
        // SIZE_T VirtualQueryEx( HANDLE hProcess, LPCVOID lpAddress, PMEMORY_BASIC_INFORMATION lpBuffer, SIZE_T dwLength );

        [DllImport("Kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, UInt32 size, ref IntPtr lpNumberOfBytesRead);
        // BOOL ReadProcessMemory( HANDLE hProcess, LPCVOID lpBaseAddress, LPVOID lpBuffer, SIZE_T nSize, SIZE_T* lpNumberOfBytesRead );

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, bool bInheritHandle, Int32 dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern Int32 CloseHandle(IntPtr hObject);

        [DllImport("Advapi32.dll")]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, Int32 DesiredAccess, out IntPtr TokenHandle);
        // BOOL OpenProcessToken( HANDLE ProcessHandle, DWORD DesiredAccess, PHANDLE TokenHandle );

        [DllImport("Advapi32.dll", EntryPoint = "LookupPrivilegeValueA", CharSet = CharSet.Ansi)]
        public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);
        // BOOL LookupPrivilegeValue( LPCTSTR lpSystemName, LPCTSTR lpName, PLUID lpLuid );

        [DllImport("Advapi32.dll")]
        public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, [MarshalAs(UnmanagedType.Bool)]bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, Int32 BufferLength, IntPtr PreviousState, IntPtr ReturnLength);
        // BOOL AdjustTokenPrivileges( HANDLE TokenHandle, BOOL DisableAllPrivileges, PTOKEN_PRIVILEGES NewState, DWORD BufferLength, PTOKEN_PRIVILEGES PreviousState, PDWORD ReturnLength );
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SYSTEM_INFO
    {
        public Int32 dwOemId;
        public Int32 dwPageSize;
        public UInt32 lpMinimumApplicationAddress;
        public UInt32 lpMaximumApplicationAddress;
        public IntPtr dwActiveProcessorMask;
        public Int32 dwNumberOfProcessors;
        public Int32 dwProcessorType;
        public Int32 dwAllocationGranularity;
        public Int16 wProcessorLevel;
        public Int16 wProcessorRevision;
    }

    [Flags]
    public enum MEMORY_STATE : int
    {
        COMMIT = 0x1000,
        FREE = 0x10000,
        RESERVE = 0x2000
    }

    [Flags]
    public enum MEMORY_TYPE : int
    {
        IMAGE = 0x1000000,
        MAPPED = 0x40000,
        PRIVATE = 0x20000
    }

    [Flags]
    public enum MEMORY_PROTECT : int
    {
        PAGE_UNKNOWN = 0x0,
        PAGE_EXECUTE = 0x10,
        PAGE_EXECUTE_READ = 0x20,
        PAGE_EXECUTE_READWRITE = 0x40,
        PAGE_EXECUTE_WRITECOPY = 0x80,
        PAGE_NOACCESS = 0x01,
        PAGE_READONLY = 0x02,
        PAGE_READWRITE = 0x04,
        PAGE_WRITECOPY = 0x08,
        PAGE_GUARD = 0x100,
        PAGE_NOCACHE = 0x200,
        PAGE_WRITECOMBINE = 0x400
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MEMORY_BASIC_INFORMATION
    {
        public IntPtr BaseAddress;
        public IntPtr AllocationBase;
        public MEMORY_PROTECT AllocationProtect;
        public UInt32 RegionSize;
        public MEMORY_STATE State;
        public MEMORY_PROTECT Protect;
        public MEMORY_TYPE Type;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct LUID
    {
        public Int32 LowPart;
        public Int32 HighPart;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct TOKEN_PRIVILEGES
    {
        public Int32 PrivilegeCount;
        public LUID_AND_ATTRIBUTES Privileges;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct LUID_AND_ATTRIBUTES
    {
        public LUID Luid;
        public Int32 Attributes;
    }
}
