/// ------------------------------------------------------------
/// Copyright (c) 2002-2008 Antonio Cisternino (cisterni@di.unipi.it)
/// 
/// File: MapView.cs
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
using System.IO;
using System.Diagnostics;

namespace MappedView {
  /// <summary>
  /// This Value type represents a pointer to a mapped file.
  /// </summary>
  public struct MapPtr {
    /// <summary>
    /// Pointer to memory
    /// </summary>
    private unsafe byte* p;
    /// <summary>
    /// Base pointer to the allowed memory area
    /// </summary>
    private unsafe byte* b;
    /// <summary>
    /// First byte out of the allowed memory area
    /// </summary>
    private unsafe byte* e;

    /// <summary>
    /// Return the number of bytes between the current position and the end of
    /// the memory area referenced by the MapPtr.
    /// </summary>
    public long Length {
      // FIXME: Should be -1?
      get { unsafe { return this.e - this.p; } }
    }

    /// <summary>
    /// Construct a pointer with a restricted allowed area. The base is
    /// the given pointer whereas the end is p + sz (if not beyond the
    /// limit of p).
    /// </summary>
    /// <param name="p">Pointer to restrict</param>
    /// <param name="sz">Size of the allowed memory</param>
    public MapPtr(MapPtr p, int sz) {
      unsafe {
        Debug.Assert(p.e - p.p >= sz, "Wrong window size!");
        this.b = p.p;
        this.p = p.p;
        this.e = p.p + sz;
      }
    }

    /// <summary>
    /// Used to build a pointer that boint at the base.
    /// </summary>
    /// <param name="b">Base of the memory area</param>
    /// <param name="e">First byte out of the area</param>
    internal unsafe MapPtr(byte* b, byte* e) : this(b, b, e) {}
    
    /// <summary>
    /// Build a pointer to a memory area that begins at b and ends at e.
    /// </summary>
    /// <param name="p">Pointer to be wrapped</param>
    /// <param name="b">Begin of memory area</param>
    /// <param name="e">End of memory area</param>
    internal unsafe MapPtr(byte* p, byte* b, byte* e) { 
      this.p = p;
      this.b = b;
      this.e = e;
    }

    /// <summary>
    /// Indexer that allow to access the memory. It behaves like a C array
    /// with the exception that it checks the bounds.
    /// It is readonly.
    /// </summary>
    public byte this[int pos] {
      get {
        unsafe {
          byte* np = p + pos;
          Debug.Assert(np >= b && np < e, "Illegal access");
          return *np; 
        }
      }
    }

    /// <summary>
    /// Matches a string against the memory pointed by the 
    /// pointer. A C semantic of strings is assumed in memory.
    /// </summary>
    /// <param name="s">String to be checked</param>
    /// <returns>Return true if the string matches</returns>
    public bool Matches(string s) {
      unsafe {
        int len = Math.Min(s.Length, (int)(e - p));
        for (int i = 0; i < len; i++)
          if (s[i] != (char)*(p + i))
            return false;
      }
      return true;
    }

    /// <summary>
    /// Fast add a short as two bytes at the end of an array.
    /// </summary>
    /// <param name="dest">Array where the short should be stored</param>
    /// <param name="from">Index of the first byte to be saved</param>
    /// <param name="v">Value to store</param>
    /// <returns></returns>
    public static int AddShort(byte[] dest, int from, short v) {
      Debug.Assert(from + 2 <= dest.Length, "Wrong array size");
      unsafe {
        fixed (byte* p = &dest[from]) {
          *((short*)p) = v;
        }
      }
      return from + 2;
    }

    /// <summary>
    /// Fast add an int as four bytes at the end of an array.
    /// </summary>
    /// <param name="dest">Array where the int should be stored</param>
    /// <param name="from">Index of the first byte to be saved</param>
    /// <param name="v">Value to store</param>
    /// <returns></returns>
    public static int AddInt32(byte[] dest, int from, int v) {
      Debug.Assert(from + 4 <= dest.Length, "Wrong array size");
      unsafe {
        fixed (byte* p = &dest[from]) {
          *((int*)p) = v;
        }
      }
      return from + 4;
    }

    /// <summary>
    /// Fast add a long as eight bytes at the end of an array.
    /// </summary>
    /// <param name="dest">Array where the long should be stored</param>
    /// <param name="from">Index of the first byte to be saved</param>
    /// <param name="v">Value to store</param>
    /// <returns></returns>
    public static int AddInt64(byte[] dest, int from, long v) {
      Debug.Assert(from + 8 <= dest.Length, "Wrong array size");
      unsafe {
        fixed (byte* p = &dest[from]) {
          *((long*)p) = v;
        }
      }
      return from + 8;
    }

    /// <summary>
    /// Fast add a float as four bytes at the end of an array.
    /// </summary>
    /// <param name="dest">Array where the float should be stored</param>
    /// <param name="from">Index of the first byte to be saved</param>
    /// <param name="v">Value to store</param>
    /// <returns></returns>
    public static int AddFloat(byte[] dest, int from, float v) {
      Debug.Assert(from + 4 <= dest.Length, "Wrong array size");
      unsafe {
        fixed (byte* p = &dest[from]) {
          *((float*)p) = v;
        }
      }
      return from + 4;
    }

    /// <summary>
    /// Fast add a double as eight bytes at the end of an array.
    /// </summary>
    /// <param name="dest">Array where the double should be stored</param>
    /// <param name="from">Index of the first byte to be saved</param>
    /// <param name="v">Value to store</param>
    /// <returns></returns>
    public static int AddDouble(byte[] dest, int from, double v) {
      Debug.Assert(from + 8 <= dest.Length, "Wrong array size");
      unsafe {
        fixed (byte* p = &dest[from]) {
          *((double*)p) = v;
        }
      }
      return from + 8;
    }

    /// <summary>
    /// This method is intended to quickly convert a byte in memory into an sbyte.
    /// The format of the int is architecture dependent and little endian is assumed. This is true for 
    /// all subsequent methods.
    /// </summary>
    /// <param name="a">Array containing the bytes</param>
    /// <param name="from">index of the first byte to look for</param>
    /// <returns>The sbyte value.</returns>
    public static sbyte ToSByte(byte[] a, int from) {
      unsafe { 
        Debug.Assert(from + sizeof(sbyte) < a.Length, "Access to byte array out of range!");
        sbyte ret = 0; 
        fixed (byte *p = &a[from]) { ret = *((sbyte*)p); }
        return ret; 
      }
    }

    /// <summary>
    /// This method is intended to quickly convert a segment of bytes in memory into an int.
    /// The format of the int is architecture dependent and little endian is assumed. This is true for 
    /// all subsequent methods.
    /// </summary>
    /// <param name="a">Array containing the bytes</param>
    /// <param name="from">index of the first byte to look for</param>
    /// <returns>The integer value.</returns>
    public static int ToInt32(byte[] a, int from) {
      unsafe { 
        Debug.Assert(from + sizeof(int) < a.Length, "Access to byte array out of range!");
        int ret = 0; 
        fixed (byte *p = &a[from]) { ret = *((int*)p); }
        return ret; 
      }
    }

    /// <summary>
    /// This method is intended to quickly convert a segment of bytes in memory into an short.
    /// </summary>
    /// <param name="a">Array containing the bytes</param>
    /// <param name="from">index of the first byte to look for</param>
    /// <returns>The short value.</returns>
    public static short ToShort(byte[] a, int from) {
      unsafe { 
        Debug.Assert(from + sizeof(long) < a.Length, "Access to byte array out of range!");
        short ret = 0;
        fixed (byte *p = &a[from]) { ret = *((short*)p); }
        return ret;
      }
    }

    /// <summary>
    /// This method is intended to quickly convert a segment of bytes in memory into an long.
    /// </summary>
    /// <param name="a">Array containing the bytes</param>
    /// <param name="from">index of the first byte to look for</param>
    /// <returns>The long value.</returns>
    public static long ToInt64(byte[] a, int from) {
      unsafe { 
        Debug.Assert(from + sizeof(long) < a.Length, "Access to byte array out of range!");
        long ret = 0;
        fixed (byte *p = &a[from]) { ret = *((long*)p); }
        return ret;
      }
    }

    /// <summary>
    /// This method is intended to quickly convert a segment of bytes in memory into a double.
    /// </summary>
    /// <param name="a">Array containing the bytes</param>
    /// <param name="from">index of the first byte to look for</param>
    /// <returns>The double value.</returns>
    public static double ToDouble(byte[] a, int from) {
      unsafe { 
        Debug.Assert(from + sizeof(double) < a.Length, "Access to byte array out of range!");
        double ret = 0;
        fixed (byte *p = &a[from]) { ret = *((double*)p); }
        return ret;
      }
    }

    /// <summary>
    /// This method is intended to quickly convert a segment of bytes in memory into a float.
    /// </summary>
    /// <param name="a">Array containing the bytes</param>
    /// <param name="from">index of the first byte to look for</param>
    /// <returns>The float value.</returns>
    public static float ToFloat(byte[] a, int from) {
      unsafe { 
        Debug.Assert(from + sizeof(float) < a.Length, "Access to byte array out of range!");
        float ret = 0;
        fixed (byte *p = &a[from]) { ret = *((float*)p); }
        return ret;
      }
    }

    /// <summary>
    /// Create a pointer by adding a constant to the given pointer.
    /// The constant represents the number of bytes to shift the pointer.
    /// The end is allowed as a border value.
    /// </summary>
    /// <param name="p">Pointer to be shifted</param>
    /// <param name="i">Number of bytes to be shifted</param>
    /// <returns>The shifted pointer</returns>
    public static MapPtr operator+(MapPtr p, int i) {
      unsafe {
        Debug.Assert(p.p + i <= p.e, "Illegal access");
        return new MapPtr(p.p + i, p.b, p.e);
      }
    }

    /// <summary>
    /// Calculate the difference between two pointers.
    /// </summary>
    /// <param name="p">Start pointer</param>
    /// <param name="q">Pointer to subtract</param>
    /// <returns>The number of bytes between p and q with sign</returns>
    public static long operator-(MapPtr p, MapPtr q) {
      unsafe {
        return p.p - q.p;
      }
    }

    /// <summary>
    /// Read a byte of the memory as a sbyte value
    /// </summary>
    /// <param name="p">Pointer to the memory to be read</param>
    /// <returns>A sbyte value corresponding to the byte</returns>
    public static explicit operator sbyte(MapPtr p) {
      unsafe { return *((sbyte*)p.p); }
    }

    /// <summary>
    /// Read two bytes of the memory as a short value
    /// </summary>
    /// <param name="p">Pointer to the memory to be read</param>
    /// <returns>A short value corresponding to the two bytes</returns>
    public static explicit operator short(MapPtr p) {
      unsafe { return *((short*)p.p); }
    }

    /// <summary>
    /// Read four bytes pointed by the pointer as an integer.
    /// </summary>
    /// <param name="p">Pointer that points to the integer</param>
    /// <returns>An integer value pointer by a pointer.</returns>
    public static explicit operator int(MapPtr p) {
      unsafe { return *((int*)p.p); }
    }

    /// <summary>
    /// Read eight bytes of memory starting from the pointed location.
    /// </summary>
    /// <param name="p">Pointer to the memory</param>
    /// <returns>A long value read from the memory</returns>
    public static explicit operator long(MapPtr p) {
      unsafe { return *((long*)p.p); }
    }

    /// <summary>
    /// Read a C string starting from the specified location.
    /// </summary>
    /// <param name="p">Pointer to the location to be read</param>
    /// <returns>The string read from the memory.</returns>
    public static explicit operator string(MapPtr p) {
      System.Text.StringBuilder ret = new System.Text.StringBuilder(16);
      unsafe {
        byte* s = (byte*)p.p;
        while (*s != 0 && s < p.e) {
          ret.Append((char)*s);
          s++;
        }
      }
      return ret.ToString();
    }

    /// <summary>
    /// Read a string in UTF8 format at the specified location.
    /// The operator returns an UTFRawString object that has an implicit
    /// operator that casts to string. This is needed to distinguish from
    /// the string() operator.
    /// </summary>
    /// <param name="p">Location to be read</param>
    /// <returns>An UTFRawString object</returns>
    public static explicit operator UTFRawString(MapPtr p) {
      UTFRawString ret = new UTFRawString();
      byte[] arr;
      unsafe {
        byte* s = (byte*)p.p;
        while (*s != 0 && s < p.e) s++;
        // Note that +1 is omitted to avoid a 0... I'm not really sure of it!
        // My understanding is that this discards the trailing 0...
        arr = new byte[s - (byte*)p.p ]; // + 1];
        s = (byte*)p.p;
        for (int i = 0; i < arr.Length; i++)
          arr[i] = *s++;
      }
      ret.src = arr;
      return ret;
    }

    /// <summary>
    /// Test for equality. It reverts to operator== after typecheck.
    /// </summary>
    /// <param name="obj">Object to compare</param>
    /// <returns>True if obj refers to the same memory location</returns>
    public override bool Equals(object obj) {
      if (!(obj is MapPtr)) return false;
      return this == (MapPtr)obj;
    }

    /// <summary>
    /// Implemented only to avoid the compiler warning.
    /// </summary>
    /// <returns>The value computed by the overridden method</returns>
    public override int GetHashCode() {
      return base.GetHashCode();
    }

    /// <summary>
    /// Compare two pointers for equality. The standard C equality is applied.
    /// </summary>
    /// <param name="p">First pointer</param>
    /// <param name="q">Second pointer</param>
    /// <returns>True if the two values are equal</returns>
    public static bool operator==(MapPtr p, MapPtr q) {
      unsafe { return p.p == q.p; }
    }

    /// <summary>
    /// Compare two pointers for inequality.
    /// </summary>
    /// <param name="p">First pointer</param>
    /// <param name="q">Second pointer</param>
    /// <returns>True if the two pointers point to different memory locations
    /// </returns>
    public static bool operator!=(MapPtr p, MapPtr q) {
      unsafe { return p.p != q.p; }
    }
  }

  /// <summary>
  /// Value type used to wrap an UTF8 string read from memory. It uses
  /// the Encoding type to read an array of bytes.
  /// </summary>
  public struct UTFRawString {
    /// <summary>
    /// Bytes that represents the UTF8 encoding of the string.
    /// </summary>
    internal byte[] src;

    /// <summary>
    /// Operator that converts an UTFRawString into a string. This operator
    /// relies on the Encoding.UTF8 class.
    /// </summary>
    /// <param name="utf"></param>
    /// <returns></returns>
    public static implicit operator string(UTFRawString utf) {
      return System.Text.Encoding.UTF8.GetString(utf.src);
    }
  }

  /// <summary>
  /// Represents a mapping view of a file mapping.
  /// </summary>
  public class MapView : IDisposable {
    /// <summary>
    /// Base pointer to the mapped area
    /// </summary>
    private unsafe byte* pb;
    /// <summary>
    /// Origin of the mapped section (must be aligned). It is needed to free
    /// the view.
    /// </summary>
    private unsafe byte* addr;
    /// <summary>
    /// Size of the mapping.
    /// </summary>
    private int size;

    /// <summary>
    /// Builds a mapped view of a mapped file.
    /// </summary>
    /// <param name="m">File mapping to use</param>
    /// <param name="off">Offset to be mapped</param>
    /// <param name="sz">Size of the view</param>
    public MapView(FileMap m, long off, int sz) {
      long basep = off & Win32Mapping.MaskOffset;
      int gap = (int)(off & Win32Mapping.MaskBase);
      unsafe {
        addr = (byte*)Win32Mapping.MapViewOfFile(m.Handle, 
          FileMapAccess.FILE_MAP_READ, (int)(basep >> 32), 
          (int)(basep & 0xFFFFFFFFL), sz);
        pb = addr + gap;
        size = sz;
      }
    }

    /// <summary>
    /// Get a pointer wrapper to the mapped view.
    /// </summary>
    /// <returns>The pointer to the base of the mapped view.</returns>
    public MapPtr GetPointer() {
      unsafe { return new MapPtr(pb, pb + size); }
    }

    /// <summary>
    /// Release the mapping
    /// </summary>
    private void Release() {
      unsafe { Win32Mapping.UnmapViewOfFile((void*)addr); }
    }

    /// <summary>
    /// Finalize the object by calling Release
    /// </summary>
    ~MapView() {
      Release();
    }

    /// <summary>
    /// Dispose the resources by invoking Release. A call to
    /// GC.SuppressFinalize() avoids a call to Finalize().
    /// </summary>
    public void Dispose() {
      GC.SuppressFinalize(this);
      Release();
    }
  }

  /// <summary>
  /// Represent a file mapping.
  /// </summary>
  public class FileMap : IDisposable {
    /// <summary>
    /// Handle to the file mapping.
    /// </summary>
    private IntPtr handle;

    /// <summary>
    /// Build a FileMap object given a FileStream.
    /// </summary>
    /// <param name="f">File to be mapped. The stream can be closed because the handle
    /// is duplicated by the constructor. The file is released only when *both* handles
    /// are closed.</param>
    public FileMap(FileStream f) {      
      unsafe {
        handle = Win32Mapping.CreateFileMapping(f.SafeFileHandle.DangerousGetHandle(), (void*)0, 
          FileProtection.PAGE_READONLY, 0, 0, null);
      }
    }

    /// <summary>
    /// Handle to the mapped file. This property is read-only.
    /// </summary>
    public IntPtr Handle {
      get { return handle; }
    }

    /// <summary>
    /// Release the file mapping.
    /// </summary>
    private void Release() {
      Win32Mapping.CloseHandle(handle);
    }

    /// <summary>
    /// Finalize the object disposing the mapping
    /// </summary>
    ~FileMap() {
      Release();
    }

    /// <summary>
    /// Dispose the mapping and avoid a call to Finalize on the object.
    /// </summary>
    public void Dispose() {
      GC.SuppressFinalize(this);
      Release();
    }
  }

  /// <summary>
  /// Map a whole file into memory. The mapping is readonly.
  /// </summary>
  public class MappedFile : IDisposable {
    /// <summary>
    /// Mapping of the file
    /// </summary>
    private FileMap fm;
    /// <summary>
    /// Mapping view of the whole file
    /// </summary>
    private MapView mv;
    /// <summary>
    /// Pointer to the base of the file.
    /// </summary>
    private MapPtr start;
    /// <summary>
    /// Length of the file.
    /// </summary>
    private int len;

    /// <summary>
    /// Readonly. Return the length of the mapped file.
    /// </summary>
    public int Length {
      get { return len; }
    }

    /// <summary>
    /// Build a MappedFile object. The file is open in sharing.
    /// </summary>
    /// <param name="path">Path to the file</param>
    public MappedFile(string path) {
      FileStream f = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
      len = (int)f.Length;
      fm = new FileMap(f);
      f.Close(); // Not needed anymore: the handle has been duplicated
      mv = new MapView(fm, 0, len);
      start = mv.GetPointer();
    }

    /// <summary>
    /// Readonly. Pointer to the base of the mapped file.
    /// </summary>
    public MapPtr Start { 
      get { return start; } 
    }

    /// <summary>
    /// Free the used resources.
    /// </summary>
    public void Dispose() {
      GC.SuppressFinalize(this);
      mv.Dispose();
      fm.Dispose();
    }
  }
}
