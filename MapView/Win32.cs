/// ------------------------------------------------------------
/// Copyright (c) 2002-2008 Antonio Cisternino (cisterni@di.unipi.it)
/// 
/// File: Win32.cs
/// Author: Antonio Cisternino (cisterni@di.unipi.it)
/// 
/// The use and distribution terms for this software are 
/// contained in the file named license.txt, which can be found 
/// in the root of this distribution.
/// By using this software in any fashion, you are agreeing to 
/// be bound by the terms of this license.
///
/// You must not remove this notice, or any other, from this
/// software.
/// ------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace MappedView {
  public enum FileProtection {
    PAGE_NOACCESS = 0x01,
    PAGE_READONLY = 0x02,
    PAGE_READWRITE = 0x04,
    PAGE_WRITECOPY = 0x08,
    PAGE_EXECUTE = 0x10,
    PAGE_EXECUTE_READ = 0x20,
    PAGE_EXECUTE_READWRITE = 0x40,
    PAGE_EXECUTE_WRITECOPY = 0x80,
    PAGE_GUARD = 0x100,
    PAGE_NOCACHE = 0x200,
    PAGE_WRITECOMBINE = 0x400,
    SEC_FILE = 0x800000,
    SEC_IMAGE = 0x1000000,
    SEC_RESERVE = 0x4000000,
    SEC_COMMIT = 0x8000000,
    SEC_NOCACHE = 0x10000000
  }
  public enum FileMapAccess {
    FILE_MAP_COPY = 0x0001,
    FILE_MAP_WRITE = 0x0002,
    FILE_MAP_READ = 0x0004,
    FILE_MAP_ALL_ACCESS = 0x000F001F
  }
  public unsafe struct SystemInfo {
    public short wProcessorArchitecture;
    public short wReserved;
    public int   dwPageSize;
    public void* lpMinimumApplicationAddress;
    public void* lpMaximumApplicationAddress;
    public int   dwActiveProcessorMask;
    public int   dwNumberOfProcessors;
    public int   dwProcessorType;
    public int   dwAllocationGranularity;
    public short wProcessorLevel;
    public short wProcessorRevision;
  }

  public unsafe class Win32Mapping {
    public static readonly int AllocationGranularity = 0;
    public static readonly long MaskBase = 0;
    public static readonly long MaskOffset = 0;
    
    static Win32Mapping() {
      SystemInfo info = new SystemInfo();
      unsafe { GetSystemInfo(&info); }
      // Assume a power of 2!
      AllocationGranularity = (int)Math.Log(info.dwAllocationGranularity, 2);
      MaskBase = (1 << AllocationGranularity) - 1;
      MaskOffset = ~MaskBase;
    }
    
    /// <summary>
    /// The CreateFileMapping function creates or opens a named or unnamed file-mapping 
    /// object for the specified file.
    /// </summary>
    /// <param name="hFile">[in] Handle to the file from which to create a mapping object. 
    /// The file must be opened with an access mode compatible with the protection flags 
    /// specified by the flProtect parameter. It is recommended, though not required, 
    /// that files you intend to map be opened for exclusive access. 
    /// If hFile is INVALID_HANDLE_VALUE, the calling process must also specify a mapping 
    /// object size in the dwMaximumSizeHigh and dwMaximumSizeLow parameters. In this case, 
    /// CreateFileMapping creates a file-mapping object of the specified size backed by the 
    /// operating-system paging file rather than by a named file in the file system. 
    /// The file-mapping object can be shared through duplication, through inheritance, 
    /// or by name. The initial contents of the pages in the file-mapping object are zero.
    /// </param>
    /// <param name="lpAttributes">[in] Pointer to a SECURITY_ATTRIBUTES structure that 
    /// determines whether the returned handle can be inherited by child processes. If 
    /// lpAttributes is NULL, the handle cannot be inherited. 
    /// Windows NT/2000/XP: The lpSecurityDescriptor member of the structure specifies a 
    /// security descriptor for the new file-mapping object. If lpAttributes is NULL,
    /// the file-mapping object gets a default security descriptor.
    /// </param>
    /// <param name="flProtect">[in] Protection desired for the file view, when the file 
    /// is mapped. This parameter can be one of the following values. 
    /// <list type="">
    /// <item>PAGE_READONLY Gives read-only access to the committed region of pages. An 
    /// attempt to write to or execute the committed region results in an access violation. 
    /// The file specified by the hFile parameter must have been created with GENERIC_READ
    /// access.</item>
    /// <item>PAGE_READWRITE Gives read/write access to the committed region of pages. 
    /// The file specified by hFile must have been created with GENERIC_READ and 
    /// GENERIC_WRITE access.</item>
    /// <item>PAGE_WRITECOPY Gives copy on write access to the committed region of pages. 
    /// The files specified by the hFile parameter must have been created with GENERIC_READ 
    /// and GENERIC_WRITE access.</item>
    /// </list>  
    /// In addition, an application can specify certain section attributes by combining 
    /// (using the bitwise OR operator) one or more of the following section attribute 
    /// values with one of the preceding page protection values.
    /// <list type="">
    /// <item>SEC_COMMIT Allocates physical storage in memory or in the paging file on disk 
    /// for all pages of a section. This is the default setting.</item>
    /// <item>SEC_IMAGE Windows NT/2000/XP: The file specified for a section's file mapping 
    /// is an executable image file. Because the mapping information and file protection 
    /// are taken from the image file, no other attributes are valid with SEC_IMAGE.</item>
    /// <item>SEC_NOCACHE All pages of a section are to be set as noncacheable. This 
    /// attribute is intended for architectures requiring various locking structures to be 
    /// in memory that is never fetched into the processor's. On 80x86 and MIPS machines, 
    /// using the cache for these structures only slows down the performance as the 
    /// hardware keeps the caches coherent. Some device drivers require noncached data so 
    /// that programs can write through to the physical memory. SEC_NOCACHE requires 
    /// either the SEC_RESERVE or SEC_COMMIT to also be set.</item>
    /// <item>SEC_RESERVE Reserves all pages of a section without allocating physical 
    /// storage. The reserved range of pages cannot be used by any other allocation 
    /// operations until it is released. Reserved pages can be committed in subsequent 
    /// calls to the VirtualAlloc function. This attribute is valid only if the hFile 
    /// parameter is INVALID_HANDLE_VALUE; that is, a file-mapping object backed by 
    /// the operating system paging file.</item>
    /// </list></param>
    /// <param name="dwMaximumSizeHigh">[in] High-order DWORD of the maximum size of the 
    /// file-mapping object.</param>
    /// <param name="dwMaximumSizeLow">[in] Low-order DWORD of the maximum size of the 
    /// file-mapping object. If this parameter and dwMaximumSizeHigh are zero, the 
    /// maximum size of the file-mapping object is equal to the current size of the file 
    /// identified by hFile.
    /// An attempt to map a file with a length of zero in this manner fails with an error 
    /// code of ERROR_FILE_INVALID. Applications should test for files with a length of 
    /// zero and reject such files.</param>
    /// <param name="lpName">[in] Pointer to a null-terminated string specifying the name 
    /// of the mapping object. 
    /// If this parameter matches the name of an existing named mapping object, the 
    /// function requests access to the mapping object with the protection specified by 
    /// flProtect.
    /// If this parameter is NULL, the mapping object is created without a name.
    /// If lpName matches the name of an existing event, semaphore, mutex, waitable timer, 
    /// or job object, the function fails and the GetLastError function returns 
    /// ERROR_INVALID_HANDLE. This occurs because these objects share the same name space.
    /// Terminal Services: The name can have a "Global\" or "Local\" prefix to explicitly 
    /// create the object in the global or session name space. The remainder of the name 
    /// can contain any character except the backslash character (\). For more 
    /// information, see Kernel Object Name Spaces. 
    /// Windows XP: Fast user switching is implemented using Terminal Services sessions. 
    /// The first user to log on uses session 0, the next user to log on uses session 1, 
    /// and so on. Kernel object names must follow the guidelines outlined for Terminal 
    /// Services so that applications can support multiple users. 
    /// Windows 2000: If Terminal Services is not running, the "Global\" and "Local\" 
    /// prefixes are ignored. The remainder of the name can contain any character except 
    /// the backslash character. 
    /// Windows NT 4.0 and earlier: The name can contain any character except the backslash 
    /// character. 
    /// Windows 95/98/Me: The name can contain any character except the backslash character. 
    /// The empty string ("") is a valid object name.
    /// </param>
    /// <returns>If the function succeeds, the return value is a handle to the file-mapping 
    /// object. If the object existed before the function call, the function returns a 
    /// handle to the existing object (with its current size, not the specified size) and 
    /// GetLastError returns ERROR_ALREADY_EXISTS. 
    /// If the function fails, the return value is NULL. To get extended error information, 
    /// call GetLastError. 
    /// </returns>
    /// <remarks>After a file-mapping object has been created, the size of the file must 
    /// not exceed the size of the file-mapping object; if it does, not all of the file's 
    /// contents will be available for sharing. 
    /// If an application specifies a size for the file-mapping object that is larger than 
    /// the size of the actual named file on disk, the file on disk is grown to match the 
    /// specified size of the file-mapping object. If the file cannot be grown, this 
    /// results in a failure to create the file-mapping object. GetLastError will return 
    /// ERROR_DISK_FULL.
    /// The handle that CreateFileMapping returns has full access to the new file-mapping 
    /// object. It can be used with any function that requires a handle to a file-mapping 
    /// object. File-mapping objects can be shared either through process creation, through 
    /// handle duplication, or by name. For information on duplicating handles, see 
    /// DuplicateHandle. For information on opening a file-mapping object by name, see 
    /// OpenFileMapping. 
    /// Windows 95/98/Me: File handles that have been used to create file-mapping objects 
    /// must not be used in subsequent calls to file I/O functions, such as ReadFile and 
    /// WriteFile. In general, if a file handle has been used in a successful call to the 
    /// CreateFileMapping function, do not use that handle unless you first close the 
    /// corresponding file-mapping object.
    /// Creating a file-mapping object creates the potential for mapping a view of the 
    /// file but does not map the view. The MapViewOfFile and MapViewOfFileEx functions map 
    /// a view of a file into a process's address space. 
    /// With one important exception, file views derived from a single file-mapping object 
    /// are coherent, or identical, at a given time. If multiple processes have handles of 
    /// the same file-mapping object, they see a coherent view of the data when they map a 
    /// view of the file. 
    /// The exception has to do with remote files. Although CreateFileMapping works with 
    /// remote files, it does not keep them coherent. For example, if two computers both 
    /// map a file as writable, and both change the same page, each computer will only see 
    /// its own writes to the page. When the data gets updated on the disk, it is not 
    /// merged. 
    /// A mapped file and a file accessed by means of the input and output (I/O) functions 
    /// (ReadFile and WriteFile) are not necessarily coherent. 
    /// To fully close a file-mapping object, an application must unmap all mapped views 
    /// of the file-mapping object by calling UnmapViewOfFile, and close the file-mapping 
    /// object handle by calling CloseHandle. The order in which these functions are 
    /// called does not matter. The call to UnmapViewOfFile is necessary because mapped 
    /// views of a file-mapping object maintain internal open handles to the object, and a 
    /// file-mapping object will not close until all open handles to it are closed. 
    /// Note  Terminal Services sessions can use shared memory blocks to transfer data 
    /// between processes spawned by those sessions. If you do this, keep in mind that 
    /// shared memory cannot be used in situations where both of the following conditions 
    /// exist: 
    /// <list type="">
    /// <item>All of the processes using the shared memory block were not spawned by one 
    /// session.</item> 
    /// <item>All of the sessions share the same user logon credential.</item>
    /// </list>
    /// Note  To guard against an access violation, use structured exception handling to 
    /// protect any code that writes to or reads from a memory mapped view. For more 
    /// information, see Reading and Writing.
    /// Windows 95/98/Me: CreateFileMappingW is supported by the Microsoft Layer for 
    /// Unicode. To use this, you must add certain files to your application, as outlined 
    /// in Microsoft Layer for Unicode on Windows 95/98/Me Systems.
    /// </remarks>
    [DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
    public static unsafe extern IntPtr CreateFileMapping(
      IntPtr hFile,                    // handle to file
      void* lpAttributes,              // security
      FileProtection flProtect,        // protection
      int dwMaximumSizeHigh,           // high-order DWORD of size
      int dwMaximumSizeLow,            // low-order DWORD of size
      string lpName                    // object name
      );
    /// <summary>
    /// The FlushViewOfFile function writes to the disk a byte range within a mapped view of 
    /// a file.
    /// </summary>
    /// <param name="lpBaseAddress">[in] Pointer to the base address of the byte range to be 
    /// flushed to the disk representation of the mapped file.</param>
    /// <param name="dwNumberOfBytesToFlush">[in] Specifies the number of bytes to flush. If 
    /// dwNumberOfBytesToFlush is zero, the file is flushed from the base address to the 
    /// end of the mapping.</param>
    /// <returns>If the function succeeds, the return value is nonzero.
    /// If the function fails, the return value is zero. To get extended error information, 
    /// call GetLastError.
    /// </returns>
    /// <remarks>
    /// Flushing a range of a mapped view causes any dirty pages within that range to be 
    /// written to the disk. Dirty pages are those whose contents have changed since the 
    /// file view was mapped.
    /// When flushing a memory-mapped file over a network, FlushViewOfFile guarantees that 
    /// the data has been written from the local computer, but not that the data resides 
    /// on the remote computer. The server can cache the data on the remote side. 
    /// Therefore, FlushViewOfFile can return before the data has been physically written 
    /// to disk. However, you can cause FlushViewOfFile to return only when the physical 
    /// write is complete by specifying the FILE_FLAG_WRITE_THROUGH flag when you open 
    /// the file with the CreateFile function.
    /// </remarks>
    [DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
    public static unsafe extern bool FlushViewOfFile(
      void* lpBaseAddress,         // starting address
      int dwNumberOfBytesToFlush   // number of bytes in range
      );
    /// <summary>
    /// The MapViewOfFile function maps a view of a file into the address space of the 
    /// calling process. 
    /// To specify a suggested base address, use the MapViewOfFileEx function.
    /// </summary>
    /// <param name="hFileMappingObject">[in] Handle to an open handle of a file-mapping 
    /// object. The CreateFileMapping and OpenFileMapping functions return this handle.
    /// </param>
    /// <param name="dwDesiredAccess">[in] Specifies the type of access to the file view 
    /// and, therefore, the protection of the pages mapped by the file. This parameter can 
    /// be one of the following values.
    /// <list type=""> 
    /// <item>FILE_MAP_WRITE Read/write access. The hFileMappingObject parameter must have 
    /// been created with PAGE_READWRITE protection. A read/write view of the file is 
    /// mapped.</item> 
    /// <item>FILE_MAP_READ Read-only access. The hFileMappingObject parameter must have 
    /// been created with PAGE_READWRITE or PAGE_READONLY protection. A read-only view of 
    /// the file is mapped. </item>
    /// <item>FILE_MAP_ALL_ACCESS Same as FILE_MAP_WRITE.</item>
    /// <item>FILE_MAP_COPY Copy on write access. If you create the map with PAGE_WRITECOPY 
    /// and the view with FILE_MAP_COPY, you will receive a view to file. If you write to 
    /// it, the pages are automatically swappable and the modifications you make will not 
    /// go to the original data file. </item>
    /// </list>
    /// Windows 95/98/Me: You must pass PAGE_WRITECOPY to CreateFileMapping; otherwise, an 
    /// error will be returned.
    /// If you share the mapping between multiple processes using DuplicateHandle or 
    /// OpenFileMapping and one process writes to a view, the modification is propagated 
    /// to the other process. The original file does not change.
    /// Windows NT/2000/XP: There is no restriction as to how the hFileMappingObject parameter 
    /// must be created. Copy on write is valid for any type of view.
    /// If you share the mapping between multiple processes using DuplicateHandle or 
    /// OpenFileMapping and one process writes to a view, the modification is not 
    /// propagated to the other process. The original file does not change.
    /// </param>
    /// <param name="dwFileOffsetHigh">[in] Specifies the high-order DWORD of the file offset 
    /// where mapping is to begin.</param>
    /// <param name="dwFileOffsetLow">[in] Specifies the low-order DWORD of the file offset 
    /// where mapping is to begin. The combination of the high and low offsets must specify 
    /// an offset within the file that matches the system's memory allocation granularity, 
    /// or the function fails. That is, the offset must be a multiple of the allocation 
    /// granularity. Use the GetSystemInfo function, which fills in the members of a 
    /// SYSTEM_INFO structure, to obtain the system's memory allocation granularity.
    /// </param>
    /// <param name="dwNumberOfBytesToMap">[in] Specifies the number of bytes of the file to 
    /// map. If dwNumberOfBytesToMap is zero, the entire file is mapped.</param>
    /// <returns>If the function succeeds, the return value is the starting address of the 
    /// mapped view.
    /// If the function fails, the return value is NULL. To get extended error information, 
    /// call GetLastError.
    /// </returns>
    /// <remarks>
    /// Mapping a file makes the specified portion of the file visible in the address space 
    /// of the calling process. 
    /// Multiple views of a file (or a file-mapping object and its mapped file) are said to 
    /// be "coherent" if they contain identical data at a specified time. This occurs if 
    /// the file views are derived from the same file-mapping object. A process can 
    /// duplicate a file-mapping object handle into another process by using the 
    /// DuplicateHandle function, or another process can open a file-mapping object by name 
    /// by using the OpenFileMapping function. 
    /// A mapped view of a file is not guaranteed to be coherent with a file being accessed 
    /// by the ReadFile or WriteFile function: 
    /// Windows 95/98/Me: MapViewOfFile may require the swapfile to grow. If the swapfile 
    /// cannot grow, the function fails. 
    /// Windows NT/2000/XP: If the file-mapping object is backed by the paging file 
    /// (hFile is INVALID_HANDLE_VALUE), the paging file must be large enough to hold the 
    /// entire mapping. If it is not, MapViewOfFile fails. 
    /// Note  To guard against an access violation, use structured exception handling to 
    /// protect any code that writes to or reads from a memory mapped view. For more 
    /// information, see Reading and Writing.
    /// </remarks>
    [DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
    public static unsafe extern void* MapViewOfFile(
      IntPtr hFileMappingObject,     // handle to file-mapping object
      FileMapAccess dwDesiredAccess, // access mode
      int dwFileOffsetHigh,          // high-order DWORD of offset
      int dwFileOffsetLow,           // low-order DWORD of offset
      int dwNumberOfBytesToMap       // number of bytes to map
      );
    /// <summary>
    /// The MapViewOfFileEx function maps a view of a file into the address space of the 
    /// calling process. This extended function allows the calling process to specify a 
    /// suggested memory address for the mapped view.
    /// </summary>
    /// <param name="hFileMappingObject">[in] Handle to an open handle to a file-mapping 
    /// object. The CreateFileMapping and OpenFileMapping functions return this handle.
    /// </param>
    /// <param name="dwDesiredAccess">[in] Specifies the type of access to the file-mapping 
    /// object and, therefore, the page protection of the pages mapped by the file. This 
    /// parameter can be one of the following values.
    /// <list type="">
    /// <item>FILE_MAP_WRITE Read-and-write access. The hFileMappingObject parameter must 
    /// have been created with PAGE_READWRITE protection. A read/write view of the file is 
    /// mapped. </item>
    /// <item>FILE_MAP_READ Read-only access. The hFileMappingObject parameter must have 
    /// been created with PAGE_READWRITE or PAGE_READONLY protection. A read-only view of 
    /// the file is mapped. </item>
    /// <item>FILE_MAP_ALL_ACCESS Same as FILE_MAP_WRITE. </item>
    /// <item>FILE_MAP_COPY Copy on write access. If you create the map with 
    /// PAGE_WRITECOPY and the view with FILE_MAP_COPY, you will receive a view to the 
    /// file. If you write to it, the pages are automatically swappable and the 
    /// modifications you make will not go to the original data file. </item>
    /// </list>
    /// Windows 95/98/Me: You must pass PAGE_WRITECOPY to CreateFileMapping; otherwise, an 
    /// error will be returned.
    /// If you share the mapping between multiple processes using DuplicateHandle or 
    /// OpenFileMapping and one process writes to a view, the modification is propagated 
    /// to the other process. The original file does not change.
    /// Windows NT/2000/XP: There is no restriction as to how the hFileMappingObject 
    /// parameter must be created. Copy on write is valid for any type of view. 
    /// If you share the mapping between multiple processes using DuplicateHandle or 
    /// OpenFileMapping and one process writes to a view, the modification is not 
    /// propagated to the other process. The original file does not change.
    /// </param>
    /// <param name="dwFileOffsetHigh">[in] Specifies the high-order DWORD of the 
    /// file offset where mapping is to begin.</param>
    /// <param name="dwFileOffsetLow">[in] Specifies the low-order DWORD of the file 
    /// offset where mapping is to begin. The combination of the high and low offsets must 
    /// specify an offset within the file that matches the system's memory allocation 
    /// granularity, or the function fails. That is, the offset must be a multiple of the 
    /// allocation granularity. Use the GetSystemInfo function, which fills in the members 
    /// of a SYSTEM_INFO structure, to obtain the system's memory allocation granularity.
    /// </param>
    /// <param name="dwNumberOfBytesToMap">[in] Specifies the number of bytes of the file 
    /// to map. If dwNumberOfBytesToMap is zero, the entire file is mapped.</param>
    /// <param name="lpBaseAddress">[in] Pointer to the memory address in the calling 
    /// process's address space where mapping should begin. This must be a multiple of the 
    /// system's memory allocation granularity, or the function fails. Use the 
    /// GetSystemInfo function, which fills in the members of a SYSTEM_INFO structure, to 
    /// obtain the system's memory allocation granularity. If there is not enough address 
    /// space at the specified address, the function fails. 
    /// If lpBaseAddress is NULL, the operating system chooses the mapping address. In this 
    /// case, this function is equivalent to the MapViewOfFile function.
    /// </param>
    /// <returns>If the function succeeds, the return value is the starting address of the 
    /// mapped view.
    /// If the function fails, the return value is NULL. To get extended error information, 
    /// call GetLastError. 
    /// </returns>
    /// <remarks>
    /// Mapping a file makes the specified portion of the file visible in the address space 
    /// of the calling process. 
    /// If a suggested mapping address is supplied, the file is mapped at the specified 
    /// address (rounded down to the nearest 64K boundary) if there is enough address space 
    /// at the specified address. If there is not, the function fails.
    /// Typically, the suggested address is used to specify that a file should be mapped at the same address in multiple processes. This requires the region of address space to be available in all involved processes. No other memory allocation, including use of the VirtualAlloc function to reserve memory, can take place in the region used for mapping: 
    /// Windows 95/98/Me: If the lpBaseAddress parameter specifies a base offset, the 
    /// function succeeds only if the same memory region is available for the memory mapped 
    /// file in all other 32-bit processes. 
    /// Windows NT/2000/XP: If the lpBaseAddress parameter specifies a base offset, the 
    /// function succeeds if the given memory region is not already in use by the calling 
    /// process. the system does not guarantee that the same memory region is available for 
    /// the memory mapped file in other 32-bit processes. 
    /// Multiple views of a file (or a file-mapping object and its mapped file) are said 
    /// to be "coherent" if they contain identical data at a specified time. This occurs 
    /// if the file views are derived from the same file-mapping object. A process can 
    /// duplicate a file-mapping object handle into another process by using the 
    /// DuplicateHandle function, or another process can open a file-mapping object by 
    /// name by using the OpenFileMapping function. 
    /// A mapped view of a file is not guaranteed to be coherent with a file being 
    /// accessed by the ReadFile or WriteFile function. 
    /// Note  To guard against an access violation, use structured exception handling to 
    /// protect any code that writes to or reads from a memory mapped view. For more 
    /// information, see Reading and Writing.
    /// </remarks>
    [DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
    public static unsafe extern void* MapViewOfFileEx(
      int hFileMappingObject,        // handle to file-mapping object
      FileMapAccess dwDesiredAccess, // access mode
      int dwFileOffsetHigh,          // high-order DWORD of offset
      int dwFileOffsetLow,           // low-order DWORD of offset
      int dwNumberOfBytesToMap,      // number of bytes to map
      void* lpBaseAddress            // starting address
      );
    /// <summary>
    /// The OpenFileMapping function opens a named file-mapping object. 
    /// </summary>
    /// <param name="dwDesiredAccess">[in] Specifies the access to the file-mapping object.
    /// Windows NT/2000/XP: This access is checked against any security descriptor on the 
    /// target file-mapping object.
    /// Windows 95/98/Me: Security descriptors on file-mapping objects are not supported.
    /// This parameter can be one of the following values.
    /// <list type="">
    /// <item>FILE_MAP_WRITE Read-write access. The target file-mapping object must have been 
    /// created with PAGE_READWRITE or PAGE_WRITE protection. Allows a read-write view of 
    /// the file to be mapped. </item>
    /// <item>FILE_MAP_READ Read-only access. The target file-mapping object must have been 
    /// created with PAGE_READWRITE or PAGE_READ protection. Allows a read-only view of the 
    /// file to be mapped. </item>
    /// <item>FILE_MAP_ALL_ACCESS All access. The target file-mapping object must have been 
    /// created with PAGE_READWRITE protection. Allows a read-write view of the file to be 
    /// mapped. </item>
    /// <item>FILE_MAP_COPY Copy-on-write access. The target file-mapping object must have 
    /// been created with PAGE_WRITECOPY protection. Allows a copy-on-write view of the 
    /// file to be mapped. </item>
    /// </list></param>
    /// <param name="bInheritHandle">[in] Specifies whether the returned handle is to be 
    /// inherited by a new process during process creation. A value of TRUE indicates that 
    /// the new process inherits the handle.</param>
    /// <param name="lpName">[in] Pointer to a string that names the file-mapping object 
    /// to be opened. If there is an open handle to a file-mapping object by this name and 
    /// the security descriptor on the mapping object does not conflict with the 
    /// dwDesiredAccess parameter, the open operation succeeds. 
    /// Terminal Services: The name can have a "Global\" or "Local\" prefix to explicitly 
    /// open an object in the global or session name space. The remainder of the name can 
    /// contain any character except the backslash character (\). For more information, see 
    /// Kernel Object Name Spaces. 
    /// Windows XP: Fast user switching is implemented using Terminal Services sessions. 
    /// The first user to log on uses session 0, the next user to log on uses session 1, 
    /// and so on. Kernel object names must follow the guidelines outlined for Terminal 
    /// Services so that applications can support multiple users. 
    /// Windows 2000: If Terminal Services is not running, the "Global\" and "Local\" 
    /// prefixes are ignored. The remainder of the name can contain any character except 
    /// the backslash character. 
    /// Windows NT 4.0 and earlier: The name can contain any character except the 
    /// backslash character. 
    /// Windows 95/98/Me: The name can contain any character except the backslash 
    /// character. The empty string ("") is a valid object name.
    /// </param>
    /// <returns>If the function succeeds, the return value is an open handle to the 
    /// specified file-mapping object.
    /// If the function fails, the return value is NULL. To get extended error information, 
    /// call GetLastError. 
    /// </returns>
    /// <remarks>
    /// The handle that OpenFileMapping returns can be used with any function that requires 
    /// a handle to a file-mapping object. 
    /// Windows 95/98/Me: OpenFileMappingW is supported by the Microsoft Layer for Unicode. 
    /// To use this, you must add certain files to your application, as outlined in 
    /// Microsoft Layer for Unicode on Windows 95/98/Me Systems.
    /// </remarks>
    [DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
    public static unsafe extern IntPtr OpenFileMapping(
      FileMapAccess dwDesiredAccess,  // access mode
      bool bInheritHandle,            // inherit flag
      string lpName                   // object name
      );
    /// <summary>
    /// The UnmapViewOfFile function unmaps a mapped view of a file from the calling 
    /// process's address space.
    /// </summary>
    /// <param name="lpBaseAddress">[in] Pointer to the base address of the mapped view of a 
    /// file that is to be unmapped. This value must be identical to the value returned by 
    /// a previous call to the MapViewOfFile or MapViewOfFileEx function.</param>
    /// <returns>If the function succeeds, the return value is nonzero, and all dirty pages 
    /// within the specified range are written "lazily" to disk. 
    /// If the function fails, the return value is zero. To get extended error information,
    /// call GetLastError. 
    /// </returns>
    /// <remarks>
    /// Although an application may close the file handle used to create a file-mapping 
    /// object, the system holds the corresponding file open until the last view of the 
    /// file is unmapped: 
    /// Windows 95/98/Me: Files for which the last view has not yet been unmapped are held 
    /// open with the same sharing restrictions as the original file handle. 
    /// Windows NT/2000/XP: Files for which the last view has not yet been unmapped are 
    /// held open with no sharing restrictions.
    /// </remarks>
    [DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
    public static unsafe extern bool UnmapViewOfFile(
      void* lpBaseAddress   // starting address
      );
    [DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
    public static unsafe extern void GetSystemInfo(SystemInfo* lpInfo);

    [DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
    public static extern bool CloseHandle(IntPtr h);
  }
}