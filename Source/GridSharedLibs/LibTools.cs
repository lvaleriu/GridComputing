#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using mscoree;

#endregion

namespace GridSharedLibs
{
    public enum PEnum
    {
        PE32 = 0x10B, // - PE32  format.
        PE64 = 0x200, // - PE32+ format.
    }

    public static class LibTools
    {
        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            // Check if the target directory exists, if not, create it.
            if (!Directory.Exists(target.FullName))
            {
                Directory.CreateDirectory(target.FullName);
            }

            // Copy each file into it’s new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        public static IList<AppDomain> GetAppDomains()
        {
            IList<AppDomain> iList = new List<AppDomain>();
            IntPtr enumHandle = IntPtr.Zero;
            ICorRuntimeHost host = new CorRuntimeHost();
            try
            {
                host.EnumDomains(out enumHandle);
                while (true)
                {
                    object domain;
                    host.NextDomain(enumHandle, out domain);
                    if (domain == null) break;
                    var appDomain = (AppDomain) domain;
                    iList.Add(appDomain);
                }
                return iList;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
            finally
            {
                host.CloseEnum(enumHandle);
                Marshal.ReleaseComObject(host);
            }
        }

        public static bool Is32Bit(string dllLocation)
        {
            return GetPeArchitecture(dllLocation) == (ushort) PEnum.PE32;
        }

        private static ushort GetPeArchitecture(string filePath)
        {
            //Module.GetPEKind(PortableExecutableKinds.Required32Bit, )

            ushort architecture = 0;
            try
            {
                using (var fStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (var bReader = new BinaryReader(fStream))
                    {
                        if (bReader.ReadUInt16() == 23117) //check the MZ signature
                        {
                            fStream.Seek(0x3A, SeekOrigin.Current); //seek to e_lfanew.
                            fStream.Seek(bReader.ReadUInt32(), SeekOrigin.Begin); //seek to the start of the NT header.
                            if (bReader.ReadUInt32() == 17744) //check the PE\0\0 signature.
                            {
                                fStream.Seek(20, SeekOrigin.Current); //seek past the file header,
                                architecture = bReader.ReadUInt16(); //read the magic number of the optional header.
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                /* TODO: Any exception handling you want to do, personally I just take 0 as a sign of failure */
            }
            //if architecture returns 0, there has been an error.
            return architecture;
        }

        private static MachineType GetDllMachineType(string dllPath)
        {
            //see http://www.microsoft.com/whdc/system/platform/firmware/PECOFF.mspx
            //offset to PE header is always at 0x3C
            //PE header starts with "PE\0\0" =  0x50 0x45 0x00 0x00
            //followed by 2-byte machine type field (see document above for enum)
            using (var fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read))
            {
                using (var br = new BinaryReader(fs))
                {
                    fs.Seek(0x3c, SeekOrigin.Begin);
                    Int32 peOffset = br.ReadInt32();
                    fs.Seek(peOffset, SeekOrigin.Begin);
                    UInt32 peHead = br.ReadUInt32();
                    if (peHead != 0x00004550) // "PE\0\0", little-endian
                        throw new Exception("Can't find PE header");
                    var machineType = (MachineType) br.ReadUInt16();
                    return machineType;
                }
            }
        }

        public static ImageFileMachine GetDllMachineType2(string dllPath, out PortableExecutableKinds peKind)
        {
            Assembly assembly = Assembly.ReflectionOnlyLoadFrom(dllPath);
            ImageFileMachine imageFileMachine;
            assembly.ManifestModule.GetPEKind(out peKind, out imageFileMachine);

            return imageFileMachine;
        }

        public static bool IsUnmanaged(string dllPath, out MachineType machineType)
        {
            bool m_IsManaged = false;

            // Create a byte array to hold the information.
            var data = new byte[4096];

            // Open the file
            var fileInfo = new FileInfo(dllPath);
            FileStream fin = fileInfo.Open(FileMode.Open, FileAccess.Read);

            // Put in the first 4k of the file into our byte array
            int iRead = fin.Read(data, 0, 4096);

            // Flush any buffered data and close (we don't need to read anymore)
            fin.Flush();
            fin.Close();

            unsafe
            {
                // The fixed statement prevents relocation of a variable 
                // by the garbage collector. It pins the location of the Data object 
                // in memory so that they will not be moved. The objects will 
                // be unpinned when the fixed block completes. In other words,
                // The gosh darn efficient garbage collector is always working 
                // and always moving stuff around. We need to tell him to keep 
                // off our property while we're working with it. 
                fixed (byte* p_Data = data)
                {
                    // Get the first 64 bytes and turn it into a IMAGE_DOS_HEADER
                    var idh = (IMAGE_DOS_HEADER*) p_Data;

                    // Now that we have the DOS header, we can get the offset
                    // (e_lfanew) add it to the original address (p_Data) and 
                    // squeeze those bytes into a IMAGE_NT_HEADERS32 structure
                    // (I'll talk about the 64 bit stuff in a bit
                    var inhs = (IMAGE_NT_HEADERS32*) (idh->e_lfanew + p_Data);

                    // Now that we have the NT_HEADERS, let's get what kind of
                    // machine it's build for. I cast it into my enum MachineType.
                    machineType = (MachineType) inhs->FileHeader.Machine;

                    // Here, I used the OptionalHeader.Magic. It tells you whether
                    // the assembly is PE32 (0x10b) or PE32+ (0x20b). 
                    // PE32+ just means 64-bit. So, instead of checking if it is 
                    // an X64 or Itanium, I just check if it's a PE32+.
                    if (inhs->OptionalHeader.Magic == 0x20b)
                    {
                        // If it is a PE32+, I want to be sure I get the correct Optional
                        // Header. I cast it as an IMAGE_NT_HEADERS64 pointer.
                        // All you have to do is check the size!
                        if (((IMAGE_NT_HEADERS64*) inhs)->OptionalHeader.DataDirectory.Size > 0)
                            m_IsManaged = true;
                    }
                    else
                    {
                        if (inhs->OptionalHeader.DataDirectory.Size > 0)
                            m_IsManaged = true;
                    }
                }
            }

            return m_IsManaged;
        }

        // returns true if the dll is 64-bit, false if 32-bit, and null if unknown
        public static bool? UnmanagedDllIs64Bit(string dllPath)
        {
            switch (GetDllMachineType(dllPath))
            {
                case MachineType.IMAGE_FILE_MACHINE_AMD64:
                case MachineType.IMAGE_FILE_MACHINE_IA64:
                    return true;
                case MachineType.IMAGE_FILE_MACHINE_I386:
                    return false;
                default:
                    return null;
            }
        }
    }

    public struct IMAGE_DATA_DIRECTORY
    {
        public uint Size;
        public uint VirtualAddress;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct IMAGE_OPTIONAL_HEADER32
    {
        [FieldOffset(0)] public ushort Magic;
        [FieldOffset(208)] public IMAGE_DATA_DIRECTORY DataDirectory;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct IMAGE_OPTIONAL_HEADER64
    {
        [FieldOffset(0)] public ushort Magic;
        [FieldOffset(224)] public IMAGE_DATA_DIRECTORY DataDirectory;
    }

    public struct IMAGE_FILE_HEADER
    {
        public ushort Characteristics;
        public ushort Machine;
        public ushort NumberOfSections;
        public ulong NumberOfSymbols;
        public ulong PointerToSymbolTable;
        public ushort SizeOfOptionalHeader;
        public ulong TimeDateStamp;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct IMAGE_NT_HEADERS32
    {
        [FieldOffset(0)] public uint Signature;
        [FieldOffset(4)] public IMAGE_FILE_HEADER FileHeader;
        [FieldOffset(24)] public IMAGE_OPTIONAL_HEADER32 OptionalHeader;
    }

    public struct IMAGE_NT_HEADERS64
    {
        public IMAGE_FILE_HEADER FileHeader;
        public IMAGE_OPTIONAL_HEADER64 OptionalHeader;
        public uint Signature;
    }


    [StructLayout(LayoutKind.Explicit)]
    public struct IMAGE_DOS_HEADER
    {
        [FieldOffset(60)] public int e_lfanew;
    }

    public enum MachineType : ushort
    {
        IMAGE_FILE_MACHINE_UNKNOWN = 0x0,
        IMAGE_FILE_MACHINE_AM33 = 0x1d3,
        IMAGE_FILE_MACHINE_AMD64 = 0x8664,
        IMAGE_FILE_MACHINE_ARM = 0x1c0,
        IMAGE_FILE_MACHINE_EBC = 0xebc,
        IMAGE_FILE_MACHINE_I386 = 0x14c,
        IMAGE_FILE_MACHINE_IA64 = 0x200,
        IMAGE_FILE_MACHINE_M32R = 0x9041,
        IMAGE_FILE_MACHINE_MIPS16 = 0x266,
        IMAGE_FILE_MACHINE_MIPSFPU = 0x366,
        IMAGE_FILE_MACHINE_MIPSFPU16 = 0x466,
        IMAGE_FILE_MACHINE_POWERPC = 0x1f0,
        IMAGE_FILE_MACHINE_POWERPCFP = 0x1f1,
        IMAGE_FILE_MACHINE_R4000 = 0x166,
        IMAGE_FILE_MACHINE_SH3 = 0x1a2,
        IMAGE_FILE_MACHINE_SH3DSP = 0x1a3,
        IMAGE_FILE_MACHINE_SH4 = 0x1a6,
        IMAGE_FILE_MACHINE_SH5 = 0x1a8,
        IMAGE_FILE_MACHINE_THUMB = 0x1c2,
        IMAGE_FILE_MACHINE_WCEMIPSV2 = 0x169,
    }
}