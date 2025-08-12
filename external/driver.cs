using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Diagnostics;

public static class Driver
{
    #region Constantes e Definições do Driver

    private const string DrvName = "\\\\.\\YOUDRIVERNAMEHERENIGGAWOWWWW";

    private const uint FILE_DEVICE_UNKNOWN = 0x00000022;
    private const uint METHOD_BUFFERED = 0;
    private const uint FILE_SPECIAL_ACCESS = 0;

    // Função para gerar IOCTLs
    private static uint CtlCode(uint deviceType, uint function, uint method, uint access)
    {
        return ((deviceType) << 16) | ((access) << 14) | ((function) << 2) | (method);
    }

    private static readonly uint REQ_RW = CtlCode(FILE_DEVICE_UNKNOWN, 0xnigga, METHOD_BUFFERED, FILE_SPECIAL_ACCESS);
    private static readonly uint REQ_BASE = CtlCode(FILE_DEVICE_UNKNOWN, 0xnigga, METHOD_BUFFERED, FILE_SPECIAL_ACCESS);
    private static readonly uint REQ_CR3 = CtlCode(FILE_DEVICE_UNKNOWN, 0xnigga, METHOD_BUFFERED, FILE_SPECIAL_ACCESS);
    private static readonly uint REQ_MOUSE = CtlCode(FILE_DEVICE_UNKNOWN, 0xnigga, METHOD_BUFFERED, FILE_SPECIAL_ACCESS);
    private static readonly uint REQ_PEB = CtlCode(FILE_DEVICE_UNKNOWN, 0xnigga, METHOD_BUFFERED, FILE_SPECIAL_ACCESS);

    #endregion

    #region Estruturas para Comunicação com o Driver

    [StructLayout(LayoutKind.Sequential)]
    private struct PebRequest
    {
        public uint ProcessId;
        public IntPtr PebAddress; // Usando IntPtr para ponteiros
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RwRequest
    {
        public int ProcessId;
        public ulong Address;
        public ulong Buffer;
        public ulong Size;
        [MarshalAs(UnmanagedType.Bool)]
        public bool Write;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BaRequest
    {
        public int ProcessId;
        public IntPtr Address; // Ponteiro para receber o endereço base
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DtbRequest
    {
        public int ProcessId;
        public IntPtr Operation; // Ponteiro para um booleano
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseRequest
    {
        public long X;
        public long Y;
        public ushort ButtonFlags;
    }

    #endregion

    #region P/Invoke - Importações da API do Windows

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFileW(
        [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool DeviceIoControl(
        IntPtr hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        IntPtr lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    [DllImport("kernel32.dll")]
    private static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll")]
    private static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESSENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }

    private const uint TH32CS_SNAPPROCESS = 0x00000002;
    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint OPEN_EXISTING = 3;
    public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);


    #endregion

    #region Variáveis de Classe

    private static IntPtr _driverHandle;
    private static int _processId;

    #endregion

    #region Métodos do Driver

    public static bool Init()
    {
        _driverHandle = CreateFileW(DrvName, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        if (_driverHandle == IntPtr.Zero || _driverHandle == INVALID_HANDLE_VALUE)
        {
            return false;
        }
        return true;
    }

    private static void ReadPhysical(ulong address, IntPtr buffer, uint size)
    {
        var request = new RwRequest
        {
            Address = address,
            Buffer = (ulong)buffer.ToInt64(),
            Size = size,
            ProcessId = _processId,
            Write = false
        };

        IntPtr pRequest = Marshal.AllocHGlobal(Marshal.SizeOf<RwRequest>());
        try
        {
            Marshal.StructureToPtr(request, pRequest, false);
            DeviceIoControl(_driverHandle, REQ_RW, pRequest, (uint)Marshal.SizeOf<RwRequest>(), IntPtr.Zero, 0, out _, IntPtr.Zero);
        }
        finally
        {
            Marshal.FreeHGlobal(pRequest);
        }
    }

    private static void WritePhysical(ulong address, IntPtr buffer, uint size)
    {
        var request = new RwRequest
        {
            Address = address,
            Buffer = (ulong)buffer.ToInt64(),
            Size = size,
            ProcessId = _processId,
            Write = true
        };

        IntPtr pRequest = Marshal.AllocHGlobal(Marshal.SizeOf<RwRequest>());
        try
        {
            Marshal.StructureToPtr(request, pRequest, false);
            DeviceIoControl(_driverHandle, REQ_RW, pRequest, (uint)Marshal.SizeOf<RwRequest>(), IntPtr.Zero, 0, out _, IntPtr.Zero);
        }
        finally
        {
            Marshal.FreeHGlobal(pRequest);
        }
    }

    public static IntPtr GetBaseAddress()
    {
        IntPtr imageAddress = IntPtr.Zero;
        IntPtr pImageAddress = Marshal.AllocHGlobal(IntPtr.Size);
        Marshal.WriteIntPtr(pImageAddress, IntPtr.Zero);

        var request = new BaRequest
        {
            ProcessId = _processId,
            Address = pImageAddress
        };

        IntPtr pRequest = Marshal.AllocHGlobal(Marshal.SizeOf<BaRequest>());
        try
        {
            Marshal.StructureToPtr(request, pRequest, false);
            DeviceIoControl(_driverHandle, REQ_BASE, pRequest, (uint)Marshal.SizeOf<BaRequest>(), IntPtr.Zero, 0, out _, IntPtr.Zero);
            imageAddress = Marshal.ReadIntPtr(pImageAddress);
        }
        finally
        {
            Marshal.FreeHGlobal(pRequest);
            Marshal.FreeHGlobal(pImageAddress);
        }
        return imageAddress;
    }

    public static IntPtr GetPebAddress()
    {
        var request = new PebRequest { ProcessId = (uint)_processId };
        uint bytesReturned = 0;

        IntPtr pRequest = Marshal.AllocHGlobal(Marshal.SizeOf<PebRequest>());
        try
        {
            Marshal.StructureToPtr(request, pRequest, false);
            bool success = DeviceIoControl(_driverHandle, REQ_PEB, pRequest, (uint)Marshal.SizeOf<PebRequest>(), pRequest, (uint)Marshal.SizeOf<PebRequest>(), out bytesReturned, IntPtr.Zero);

            if (!success)
            {
                // Lança uma exceção se a chamada falhar
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            request = Marshal.PtrToStructure<PebRequest>(pRequest);
        }
        finally
        {
            Marshal.FreeHGlobal(pRequest);
        }
        return request.PebAddress;
    }

    public static bool CR3()
    {
        bool result = false;
        IntPtr pResult = Marshal.AllocHGlobal(Marshal.SizeOf<bool>());

        var request = new DtbRequest
        {
            ProcessId = _processId,
            Operation = pResult
        };

        IntPtr pRequest = Marshal.AllocHGlobal(Marshal.SizeOf<DtbRequest>());
        try
        {
            Marshal.StructureToPtr(request, pRequest, false);
            DeviceIoControl(_driverHandle, REQ_CR3, pRequest, (uint)Marshal.SizeOf<DtbRequest>(), IntPtr.Zero, 0, out _, IntPtr.Zero);
            result = Marshal.ReadByte(pResult) != 0;
        }
        finally
        {
            Marshal.FreeHGlobal(pRequest);
            Marshal.FreeHGlobal(pResult);
        }
        return result;
    }

    public static void MoveMouse(long x, long y, ushort buttonFlags)
    {
        var request = new MouseRequest
        {
            X = x,
            Y = y,
            ButtonFlags = buttonFlags
        };

        IntPtr pRequest = Marshal.AllocHGlobal(Marshal.SizeOf<MouseRequest>());
        try
        {
            Marshal.StructureToPtr(request, pRequest, false);
            DeviceIoControl(_driverHandle, REQ_MOUSE, pRequest, (uint)Marshal.SizeOf<MouseRequest>(), pRequest, (uint)Marshal.SizeOf<MouseRequest>(), out _, IntPtr.Zero);
        }
        finally
        {
            Marshal.FreeHGlobal(pRequest);
        }
    }

    public static int GetProcessId(string processName)
    {
        IntPtr hSnap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (hSnap == INVALID_HANDLE_VALUE)
        {
            return 0;
        }

        PROCESSENTRY32 pt = new PROCESSENTRY32();
        pt.dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32>();

        if (Process32First(hSnap, ref pt))
        {
            do
            {
                if (pt.szExeFile.Equals(processName, StringComparison.OrdinalIgnoreCase))
                {
                    CloseHandle(hSnap);
                    _processId = (int)pt.th32ProcessID;
                    return _processId;
                }
            } while (Process32Next(hSnap, ref pt));
        }

        CloseHandle(hSnap);
        return 0;
    }

    public static T Read<T>(ulong address) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        IntPtr buffer = Marshal.AllocHGlobal(size);
        try
        {
            ReadPhysical(address, buffer, (uint)size);
            return Marshal.PtrToStructure<T>(buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static void Write<T>(ulong address, T value) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        IntPtr buffer = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(value, buffer, false);
            WritePhysical(address, buffer, (uint)size);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static bool IsValidPointer(IntPtr address)
    {
        ulong addr = (ulong)address.ToInt64();
        if (addr <= 0x400000 ||
            addr == 0xCCCCCCCCCCCCCCCC ||
            addr == 0UL ||
            addr > 0x7FFFFFFFFFFFFFFF)
        {
            return false;
        }
        return true;
    }

    #endregion

}

