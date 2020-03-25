/// ------------------------------------------------------------
/// Copyright (c) 2002-2008 Antonio Cisternino (cisterni@di.unipi.it)
/// 
/// File: Cursors.cs
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
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using MappedView;

namespace CLIFileRW
{
    /// <summary>
    /// Abstract class that implements the base features of a table cursor.
    /// </summary>
    public abstract class TableCursor
    {
        /// <summary>
        /// Table indexed by the cursor.
        /// </summary>
        internal Table table;
        /// <summary>
        /// Pointer to the current position of the cursor.
        /// </summary>
        protected MapPtr cur;

        /// <summary>
        /// Build the cursor on the given table. The generic cursor is able to
        /// operate because it relies on the abstract property Size that returns
        /// the size of a row in bytes. A better approach would be using generics
        /// when they come out.
        /// </summary>
        /// <param name="t">Table of the cursor</param>
        internal TableCursor(Table t) { table = t; cur = t.Start; }

        /// <summary>
        /// Return true if the cursor is at the end of the table.
        /// </summary>
        public bool EOF
        {
            get { return (table.Start + (this.RowSize * table.Size)) == cur; }
        }

        /// <summary>
        /// Return true if the cursor is at the beginning of the table.
        /// </summary>
        public bool BOF
        {
            get { return table.Start == cur; }
        }

        /// <summary>
        /// Position of the cursor (index 1-based).
        /// </summary>
        public int Position
        {
            get { return (int)(((this.cur - this.table.Start) / this.RowSize) + 1); }
        }

        /// <summary>
        /// Metadata token of the row as represented by the MS implementation of
        /// reflection (high byte table id, three bytes == row). To be checked on Mono!
        /// </summary>
        public int MetadataToken
        {
            get { return (((int)table.TableType) << 24) | this.Position; }
        }

        /// <summary>
        /// Return the size in bytes of a row. Must be defined in a derivate class
        /// because it depends on the table.
        /// </summary>
        public abstract int RowSize { get; }

        /// <summary>
        /// Advance the cursor to the next position. Return true if the next
        /// record is valid.
        /// </summary>
        /// <returns></returns>
        public bool Next()
        {
            cur += this.RowSize;
            return !this.EOF;
        }

        /// <summary>
        /// Go to the specified record. Remember that indexes are 1-based. 0 means
        /// no value.
        /// </summary>
        /// <param name="i">Index of the row.</param>
        public void Goto(int i)
        {
            Debug.Assert(i <= this.table.Rows, "Index out of range");
            cur = table.Start + (i - 1) * this.RowSize;
        }

        /// <summary>
        /// Reset the cursor to the beginning of the table (it is equivalent to
        /// Goto(0)).
        /// </summary>
        public void Reset()
        {
            cur = table.Start;
        }

        /// <summary>
        /// Read a blob index. It asserts on the index value.
        /// </summary>
        /// <param name="cur">Pointer to be used</param>
        /// <returns>The index to the heap</returns>
        protected int ReadBlob(MapPtr cur)
        {
            if (this.table.File.Blob.Large)
            {
                Debug.Assert(
                  (int)cur < this.table.File.Blob.Base.Length,
                  "Error reading CLI file.");
                return (int)cur;
            }
            else
            {
                Debug.Assert(
                  (int)(short)cur < this.table.File.Blob.Base.Length,
                  "Error reading CLI file.");
                return (int)(short)cur;
            }
        }

        /// <summary>
        /// Read a GUID index. It asserts on the index value.
        /// </summary>
        /// <param name="cur">Pointer to be used</param>
        /// <returns>The index to the heap</returns>
        protected int ReadGuid(MapPtr cur)
        {
            if (this.table.File.Guid.Large)
            {
                Debug.Assert(
                  (int)cur <= this.table.File.Guid.Size,
                  "Error reading CLI file.");
                return (int)cur;
            }
            else
            {
                Debug.Assert(
                  (int)(short)cur <= this.table.File.Guid.Size,
                  "Error reading CLI file.");
                return (int)(short)cur;
            }
        }

        /// <summary>
        /// Read a string index. It asserts on the index value.
        /// </summary>
        /// <param name="cur">Pointer to be used</param>
        /// <returns>The index read</returns>
        protected int ReadString(MapPtr cur)
        {
            if (this.table.File.Strings.Large)
            {
                Debug.Assert(
                  (int)cur < this.table.File.Strings.Base.Length,
                  "Error reading CLI file.");
                return (int)cur;
            }
            else
            {
                Debug.Assert(
                  (int)(short)cur < this.table.File.Strings.Base.Length,
                  "Error reading CLI file.");
                return (int)(short)cur;
            }
        }

        /// <summary>
        /// Read an index of the specified table.
        /// </summary>
        /// <param name="t">Table referred by the index</param>
        /// <param name="cur">Pointer to be used</param>
        /// <returns>The index read</returns>
        protected int ReadTableIndex(TableNames t, MapPtr cur)
        {
            if (this.table.File[t].LargeIndexes)
            {
                Debug.Assert((int)cur <= this.table.File[t].Rows,
                  "Error reading CLI file");
                return (int)cur;
            }
            else
            {
                Debug.Assert((int)(short)cur <= this.table.File[t].Rows,
                  "Error reading CLI file");
                return (int)(short)cur;
            }
        }
    }

    /// <summary>
    /// Cursor for the Module Table
    /// </summary>
    public class ModuleTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// 2 byte value, reserved, shall be zero.
        /// </summary>
        public int Generation
        {
            get { return (short)this.cur; }
        }

        /// <summary>
        /// Index into String heap
        /// </summary>
        public int Name
        {
            get { return this.ReadString(this.cur + 2); }
        }

        /// <summary>
        /// Index into Guid heap; simply a Guid used to distinguish between two 
        /// versions of the same module
        /// </summary>
        public int Mvid
        {
            get
            {
                int sz = 2 + this.table.File.Strings.IndexSize;
                return this.ReadGuid(this.cur + sz);
            }
        }

        /// <summary>
        /// Index into Guid heap, reserved, shall be zero.
        /// </summary>
        public int EncId
        {
            get
            {
                int sz = 2 + this.table.File.Strings.IndexSize +
                  table.File.Guid.IndexSize;
                return this.ReadGuid(this.cur + sz);
            }
        }

        /// <summary>
        /// Index into Guid heap, reserved, shall be zero.
        /// </summary>
        public int EncBaseId
        {
            get
            {
                int sz = 2 + this.table.File.Strings.IndexSize +
                  2 * table.File.Guid.IndexSize;
                return this.ReadGuid(this.cur + sz);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal ModuleTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Access the name column
        /// </summary>
        /// <returns>The name of the table</returns>
        public string GetName()
        {
            return table.File.Strings.Access(cur + 2);
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 2 + t.File.Strings.IndexSize + 3 * t.File.Guid.IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the TypeRef Table
    /// </summary>
    public class TypeRefTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// Index into Module, ModuleRef, AssemblyRef or TypeRef tables, or null; 
        /// more precisely, a ResolutionScope coded index
        /// </summary>
        public int ResolutionScope
        {
            get
            {
                if (this.table.File.ResolutionScopeSize == 4)
                    return (int)this.cur;
                else
                    return (int)(short)this.cur;
            }
        }

        /// <summary>
        /// Index into String heap. 2.	Name shall index a non-null string in the 
        /// String heap.
        /// </summary>
        public int Name
        {
            get
            {
                int sz = this.table.File.ResolutionScopeSize;
                Debug.Assert(this.ReadString(this.cur + sz) != 0,
                   "Error reading CLI File");
                return this.ReadString(this.cur + sz);
            }
        }

        /// <summary>
        /// Index into String heap
        /// </summary>
        public int Namespace
        {
            get
            {
                int sz = this.table.File.ResolutionScopeSize +
                  this.table.File.Strings.IndexSize;
                return this.ReadString(this.cur + sz);
            }
        }

        /// <summary>
        /// Find the Reflection type associated with the typeref.
        /// </summary>
        /// <returns>The type found or null</returns>
        public Type GetReflectionType()
        {
            return this.table.File.Assembly.ManifestModule.ResolveType(this.MetadataToken);
        }

        /// <summary>
        /// Donal Lafferty (laffertd@cs.tcd.ie) 
        /// 2002-07-31
        ///
        /// Method to supply an Assembly's QualifiedName.  Having the
        /// full name is important if the Assembly is in the GAC (Global
        /// Assembly Cache) and a call to System.Type.GetType is made.
        ///
        /// General format see: 
        /// http://msdn.microsoft.com/library/default.asp?url=/nhp/default.asp?contentid=28000519
        ///
        /// "The format of the display name of an AssemblyName is a
        /// comma-delimited Unicode string that begins with the name,
        /// as follows:
        /// Name [,Culture = CultureInfo] [,Version = Major.Minor.Build.Revision] [, StrongName] [,PublicKeyToken] '\0'
        /// </summary>
        /// <param name="ar">Cursor to the AssemblyRef table referencing the 
        /// required assembly</param>
        /// <returns>The assembly qualified name for the assembly</returns>
        [Obsolete("This method was designed to support interop with Reflection 1.0")]
        internal string GetAssemblyQualifiedName(AssemblyRefTableCursor ar)
        {
            string typeName = this.table.File.Strings[this.Name];

            string typeNamespace = null;
            if (this.Namespace != 0)
                typeNamespace =
                  this.table.File.Strings[this.Namespace] + ".";

            string assemblyName = this.table.File.Strings[ar.Name];

            string assemblyCulture = null;
            if (ar.Culture != 0)
                assemblyCulture = ", " + this.table.File.Strings[ar.Culture];
            else
                assemblyCulture = ", Culture=neutral";

            // PublicKeyToken is an 8 byte *optional* entity located in the Blob heap.
            string publicKeyToken = null;
            if (ar.PublicKeyOrToken != 0)
            {
                MapPtr key = this.table.File.Blob[ar.PublicKeyOrToken];
                publicKeyToken = string.Format(
                  ", PublicKeyToken={0:x2}{1:x2}{2:x2}{3:x2}{4:x2}{5:x2}{6:x2}{7:x2}",
                  key[0], key[1], key[2], key[3],
                  key[4], key[5], key[6], key[7]);
            }
            // Version is <Major>.<Minor>.<Build>.<Revision>
            string version = ", " + string.Format("Version={0}.{1}.{2}.{3}",
              ar.MajorVersion,
              ar.MinorVersion,
              ar.BuildNumber,
              ar.RevisionNumber);

            string tn = string.Format("{1}{2}, {0}{3}{4}{5}",
              assemblyName,
              typeNamespace,
              typeName,
              version,
              assemblyCulture,
              publicKeyToken);
            Debug.Assert(tn != null, "Error looking up Qualified name");

            return tn;
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal TypeRefTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return t.File.ResolutionScopeSize + 2 * t.File.Strings.IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the TypeDef Table
    /// </summary>
    public class TypeDefTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// A 4 byte bitmask of type TypeAttributes
        /// </summary>
        public int Flags
        {
            get { return (int)this.cur; }
        }

        /// <summary>
        /// Index into String heap
        /// </summary>
        public int Name
        {
            get
            {
                Debug.Assert(this.ReadString(this.cur + 4) != 0,
                  "Error reading CLI file");
                return this.ReadString(this.cur + 4);
            }
        }

        /// <summary>
        /// Index into String heap
        /// </summary>
        public int Namespace
        {
            get
            {
                int sz = 4 + this.table.CLIFile.Strings.IndexSize;
                return this.ReadString(this.cur + sz);
            }
        }

        /// <summary>
        /// Index into TypeDef, TypeRef or TypeSpec table; more precisely, a 
        /// TypeDefOrRef coded index
        /// </summary>
        public int Extends
        {
            get
            {
                int sz = 4 + 2 * this.table.File.Strings.IndexSize;
                if (this.table.File.TypeDefOrRefSize == 4)
                    return (int)(this.cur + sz);
                else
                    return (int)(short)(this.cur + sz);
            }
        }

        /// <summary>
        /// index into Field table; it marks the first of a continguous run of 
        /// Fields owned by this Type.
        /// The run continues to the smaller of:
        ///    o	the last row of the Field table
        ///    o	the next run of Fields, found by inspecting the FieldList of 
        ///       the next row in this TypeDef table
        /// </summary>
        public int FieldList
        {
            get
            {
                int sz = 4 + 2 * this.table.File.Strings.IndexSize +
                  this.table.File.TypeDefOrRefSize;
                return this.ReadTableIndex(TableNames.Field, this.cur + sz);
            }
        }

        /// <summary>
        /// Index into Method table; it marks the first of a continguous run of 
        /// Methods owned by this Type.  
        /// The run continues to the smaller of:
        ///     o	the last row of the Method table
        ///     o	the next run of Methods, found by inspecting the MethodList of 
        ///       the next row in this TypeDef table
        /// Note that any type shall be one, and only one, of
        ///     •	Class (Flags.Interface = 0, and derives ultimately from 
        ///       System.Object)
        ///     •	Interface (Flags.Interface = 1)
        ///     •	Value type, derived ultimately from System.ValueType
        /// For any given type, there are two separate, and quite distinct 
        /// ‘inheritance’ chains of pointers to other types (the pointers are 
        /// actually implemented as indexes into metadata tables).  
        /// The two chains are:
        ///     •	Extension chain – defined via the Extends column of the TypeDef 
        ///       table.  Typically, a derived Class extends a base Class (always 
        ///       one, and only one, base Class)
        ///     •	Interface chains – defined via the InterfaceImpl table.  
        ///       Typically, a Class implements zero, one or more Interfaces
        /// These two chains (extension and interface) are always kept separate in 
        /// metadata.  The Extends chain represents one-to-one relations – that is, 
        /// one Class extends  (or ‘derives from’) exactly one other Class (called 
        /// its immediate base Class).  The Interface chains may represent 
        /// one-to-many relations – that is,  one Class might well implement two 
        /// or more Interfaces.
        /// </summary>
        public int MethodList
        {
            get
            {
                int sz = 4 + 2 * this.table.File.Strings.IndexSize +
                  this.table.File.TypeDefOrRefSize +
                  this.table.File[TableNames.Field].IndexSize;

                return this.ReadTableIndex(TableNames.Method, this.cur + sz);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal TypeDefTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 4 + 2 * t.File.Strings.IndexSize + t.File.TypeDefOrRefSize +
              t.File[TableNames.Field].IndexSize +
              t.File[TableNames.Method].IndexSize;
        }

        /// <summary>
        /// Find the type associated with the current TypeDef entry.
        /// </summary>
        /// <returns>The reflection type.</returns>
        public Type GetReflectionType()
        {
            return this.table.File.Assembly.ManifestModule.ResolveType(this.MetadataToken);
        }

        /// <summary>
        /// Donal Lafferty (laffertd@cs.tcd.ie) 
        /// 2002-07-31
        ///
        /// Method to supply an Assembly's QualifiedName.  Having the
        /// full name is important if the Assembly is in the GAC (Global
        /// Assembly Cache) and a call to System.Type.GetType is made.
        ///
        /// General format see: 
        /// http://msdn.microsoft.com/library/default.asp?url=/nhp/default.asp?contentid=28000519
        ///
        /// "The format of the display name of an AssemblyName is a
        /// comma-delimited Unicode string that begins with the name,
        /// as follows:
        /// Name [,Culture = CultureInfo] [,Version = Major.Minor.Build.Revision] [, StrongName] [,PublicKeyToken] '\0'
        /// </summary>
        /// <param name="ar">Cursor to the AssemblyRef table referencing the 
        /// required assembly</param>
        /// <returns>The assembly qualified name for the assembly</returns>
        internal string GetAssemblyQualifiedName(AssemblyTableCursor ar)
        {
            string typeName = this.table.File.Strings[this.Name];

            string typeNamespace = null;
            if (this.Namespace != 0)
                typeNamespace =
                  this.table.File.Strings[this.Namespace] + ".";

            string assemblyName = this.table.File.Strings[ar.Name];

            string assemblyCulture = null;
            if (ar.Culture != 0)
                assemblyCulture = ", " + this.table.File.Strings[ar.Culture];
            else
                assemblyCulture = ", Culture=neutral";

            // PublicKeyToken is an 8 byte *optional* entity located in the Blob heap.
            string publicKeyToken = null;
            if (ar.PublicKey != 0)
            {
                MapPtr key = this.table.File.Blob[ar.PublicKey];
                publicKeyToken = string.Format(
                  ", PublicKeyToken={0:x2}{1:x2}{2:x2}{3:x2}{4:x2}{5:x2}{6:x2}{7:x2}",
                  key[0], key[1], key[2], key[3],
                  key[4], key[5], key[6], key[7]);
            }
            // Version is <Major>.<Minor>.<Build>.<Revision>
            string version = ", " + string.Format("Version={0}.{1}.{2}.{3}",
              ar.MajorVersion,
              ar.MinorVersion,
              ar.BuildNumber,
              ar.RevisionNumber);

            string tn = string.Format("{1}{2}, {0}{3}{4}{5}",
              assemblyName,
              typeNamespace,
              typeName,
              version,
              assemblyCulture,
              publicKeyToken);
            Debug.Assert(tn != null, "Error looking up Qualified name");

            return tn;
        }
    }

    /// <summary>
    /// Cursor for the Field Table
    /// </summary>
    public class FieldTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// A 2 byte bitmask of type FieldAttributes
        /// </summary>
        public short Flags
        {
            get { return (short)this.cur; }
        }

        /// <summary>
        /// Index into String heap
        /// </summary>
        public int Name
        {
            get
            {
                Debug.Assert(this.ReadString(this.cur + 2) != 0,
                  "Error reading CLI file");
                return this.ReadString(this.cur + 2);
            }
        }

        /// <summary>
        /// Index into Blob heap
        /// </summary>
        public int Signature
        {
            get
            {
                int sz = 2 + table.File.Strings.IndexSize;
                return this.ReadBlob(this.cur + sz);
            }
        }

        /// <summary>
        /// Return an object that allows to inspect the field signature
        /// </summary>
        public CLIType FieldSignature
        {
            get
            {
                return FieldSig.Read(this.table.File, this.table.File.Blob[this.Signature]);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal FieldTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 2 + t.File.Strings.IndexSize + t.File.Blob.IndexSize;
        }

        public System.Reflection.FieldInfo GetReflectionField()
        {
            return this.table.File.Assembly.ManifestModule.ResolveField(this.MetadataToken);
        }
    }

    /// <summary>
    /// Cursor for the Method Table
    /// </summary>
    public class MethodTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// Get the relative virtual address of the method body.
        /// </summary>
        public int RVA
        {
            get { return (int)this.cur; }
        }

        /// <summary>
        /// Two bytes bitmask from MethImplAttributes
        /// </summary>
        public short ImplFlags
        {
            get { return (short)(this.cur + 4); }
        }

        /// <summary>
        /// Two bytes bitmask from MethodAttributes
        /// </summary>
        public short Flags
        {
            get { return (short)(this.cur + 6); }
        }

        /// <summary>
        /// Index of the name string into the String heap
        /// </summary>
        public int Name
        {
            get
            {
                Debug.Assert(this.ReadString(this.cur + 8) != 0,
                  "Error reading CLI file");
                return this.ReadString(this.cur + 8);
            }
        }

        /// <summary>
        /// Index of the signature into the Blob heap
        /// </summary>
        public int Signature
        {
            get
            {
                return this.ReadBlob(this.cur + 8 + table.CLIFile.Strings.IndexSize);
            }
        }

        /// <summary>
        /// Index into Parameter Table to a run of parameters
        /// </summary>
        public int ParamList
        {
            get
            {
                int sz = 8 + this.table.File.Strings.IndexSize +
                  this.table.File.Blob.IndexSize;
                return this.ReadTableIndex(TableNames.Param, this.cur + sz);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal MethodTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Return an object that allows to inspect the method body.
        /// </summary>
        public MethodBody MethodBody
        {
            get
            {
                return new MethodBody((int)(((cur - table.Start) / this.RowSize) + 1),
                                      this.table.File.ResolveRVA(this.RVA),
                                      this.table.File);
            }
        }

        /// <summary>
        /// Return an object that allows to inspect the method signature
        /// </summary>
        public MethodSig GetMethodSignature()
        {
            return new MethodSig(this.table.File, this.table.File.Blob[this.Signature], MethodSig.SigType.MethodDefSig);
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 8 + t.File.Strings.IndexSize + t.File.Blob.IndexSize +
              t.File[TableNames.Param].IndexSize;
        }

        /// <summary>
        /// Get the reflection object associated with the current row.
        /// </summary>
        /// <returns>The MethodInfo or the ContructorInfo referred by the current row</returns>
        public MethodBase GetReflectionMethod()
        {
            // FIXME: Yet to be validated!
            return this.table.File.Assembly.ManifestModule.ResolveMethod(this.MetadataToken);
        }

        /// <summary>
        /// Return the Metadata token of the Type declaring the method. It requires
        /// a search in the TypeDef table implemented with a linear search: O(n).
        /// </summary>
        /// <returns>The Metadata token corresponding with the type</returns>
        public int FindType()
        {
            if (this.BOF) throw new Exception("MethodTableCursor at BOF!");
            TypeDefTableCursor cur = this.table.CLIFile[TableNames.TypeDef].GetCursor() as TypeDefTableCursor;
            int idx = 0;
            while (cur.Next() && (cur.MethodList < this.Position)) idx = cur.Position;
            return (((int)TableNames.TypeDef) << 24) | idx;
        }

        /// <summary>
        /// Return the CLIType declaring the method. It requires
        /// a search in the TypeDef table implemented with a linear search: O(n).
        /// </summary>
        /// <returns>The CLIType of the type or null if it is a global function</returns>
        public CLIType FindCLIType()
        {
            if (this.BOF) throw new Exception("MethodTableCursor at BOF!");
            TypeDefTableCursor cur = this.table.CLIFile[TableNames.TypeDef].GetCursor() as TypeDefTableCursor;
            int idx = 0;
            while (cur.Next() && (cur.MethodList < this.Position)) idx = cur.Position;
            if (idx == 0) return null;

            return new CLIType(this.table.CLIFile, CompoundType.FromFToken(this.table.CLIFile, (((int)TableNames.TypeDef) << 24) | idx, null));
        }

        /// <summary>
        /// Find the method index in the method table given a MethodInfo.
        /// </summary>
        /// <param name="m">MethodInfo to look for</param>
        /// <returns>The index of the method in the method table</returns>
        public int FindToken(MethodBase m)
        {
            return m.MetadataToken & 0x00FFFFFF;
        }
    }

    /// <summary>
    /// Cursor for the Param Table
    /// </summary>
    public class ParamTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// A 2  byte bitmask of type ParamAttributes, clause 22.1.12.
        /// </summary>
        public int Flags
        {
            get { return (short)(this.cur); }
        }

        /// <summary>
        /// A 2 byte constant
        /// </summary>
        public int Sequence
        {
            get { return (short)(this.cur + 2); }
        }

        /// <summary>
        /// Index into String heap
        /// </summary>
        public int Name
        {
            get { return this.ReadString(this.cur + 4); }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal ParamTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 4 + t.File.Strings.IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the InterfaceImpl Table
    /// </summary>
    public class InterfaceImplTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// Index into the TypeDef table
        /// </summary>
        public int Class
        {
            get { return this.ReadTableIndex(TableNames.TypeDef, this.cur); }
        }

        /// <summary>
        /// Index into the TypeDef, TypeRef or TypeSpec table; more precisely, a 
        /// TypeDefOrRef coded index
        /// </summary>
        public int Interface
        {
            get
            {
                int sz = this.table.File[TableNames.TypeDef].IndexSize;
                if (this.table.File.TypeDefOrRefSize == 4)
                    return (int)(this.cur + sz);
                else
                    return (int)(short)(this.cur + sz);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal InterfaceImplTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return t.File[TableNames.TypeDef].IndexSize +
              t.File.TypeDefOrRefSize;
        }
    }

    /// <summary>
    /// Cursor for the MemberRef Table
    /// </summary>
    public class MemberRefTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// index into the TypeRef, ModuleRef, Method, TypeSpec or TypeDef tables;
        /// more precisely, a MemberRefParent coded index.
        /// </summary>
        public int Class
        {
            get
            {
                if (this.table.File.MemberRefParentSize == 4)
                    return (int)this.cur;
                else
                    return (int)(short)this.cur;
            }
        }

        /// <summary>
        /// Index into String heap
        /// </summary>
        public int Name
        {
            get
            {
                int sz = this.table.File.MemberRefParentSize;
                Debug.Assert(this.ReadString(this.cur + sz) != 0,
                  "Error reading CLI file");
                return this.ReadString(this.cur + sz);
            }
        }

        /// <summary>
        /// Index into Blob heap
        /// </summary>
        public int Signature
        {
            get
            {
                int sz = this.table.File.MemberRefParentSize +
                  this.table.File.Strings.IndexSize;
                return this.ReadBlob(this.cur + sz);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal MemberRefTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return t.File.MemberRefParentSize +
              t.File.Strings.IndexSize + t.File.Blob.IndexSize;
        }

        /// <summary>
        /// Return the Method referred by the current row (if it is a method).
        /// </summary>
        /// <returns>The MethodInfo or ConstructorInfo associated with the current row</returns>
        public MethodBase GetReflectionMethod()
        {
            return this.table.File.Assembly.ManifestModule.ResolveMethod(this.MetadataToken);
        }

        public FieldInfo GetReflectionField()
        {
            return this.table.File.Assembly.ManifestModule.ResolveField(this.MetadataToken);
        }
    }

    /// <summary>
    /// Cursor for the Constant Table
    /// </summary>
    public class ConstantTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// A 1 byte constant, followed by a 1-byte padding zero: see Clause 
        /// 22.1.15. The encoding of Type for the nullref value for 
        /// &lt;fieldInit&gt; in ilasm (see Section 15.2) is ELEMENT_TYPE_CLASS
        /// with a Value of zero.  Unlike uses of ELEMENT_TYPE_CLASS in signatures,
        /// this one is not followed by a type token.
        /// </summary>
        public int Type
        {
            get { return (short)this.cur; }
        }

        /// <summary>
        /// Index into the Param or Field or Property table; more precisely, a 
        /// HasConst coded index.
        /// </summary>
        public int Parent
        {
            get
            {
                if (this.table.File.HasConstantSize == 4)
                    return (int)(this.cur + 2);
                else
                    return (int)(short)(this.cur + 2);
            }
        }

        /// <summary>
        /// Inde into Blob Heap
        /// </summary>
        public int Value
        {
            get { return this.ReadBlob(this.cur + this.table.File.HasConstantSize); }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal ConstantTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 2 + t.File.HasConstantSize + t.File.Blob.IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the CustomAttribute Table
    /// </summary>
    public class CustomAttributeTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// Index into any metadata table, except the CustomAttribute table itself;
        /// more precisely, a HasCustomAttribute coded index.
        /// </summary>
        public int Parent
        {
            get
            {
                if (this.table.File.HasCustomAttributeSize == 4)
                    return (int)this.cur;
                else
                    return (int)(short)this.cur;
            }
        }

        /// <summary>
        /// Index into the Method or MethodRef table; more precisely, a
        /// CustomAttributeType coded index.
        /// </summary>
        public int Type
        {
            get
            {
                int sz = this.table.File.HasCustomAttributeSize;
                if (this.table.File.CustomAttributeTypeSize == 4)
                    return (int)(this.cur + sz);
                else
                    return (int)(short)(this.cur + sz);
            }
        }

        /// <summary>
        /// Index into Blob heap.
        /// </summary>
        public int Value
        {
            get
            {
                int sz = this.table.File.HasCustomAttributeSize +
                  this.table.File.CustomAttributeTypeSize;
                return this.ReadBlob(this.cur + sz);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal CustomAttributeTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return t.File.HasCustomAttributeSize +
              t.File.CustomAttributeTypeSize + t.File.Blob.IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the FieldMarshal Table
    /// </summary>
    public class FieldMarshalTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// Index into Field or Param table; more precisely, a HasFieldMarshal 
        /// coded index
        /// </summary>
        public int Parent
        {
            get
            {
                if (this.table.File.HasFieldMarshalSize == 4)
                    return (int)this.cur;
                else
                    return (int)(short)cur;
            }
        }

        /// <summary>
        /// Index into Blob heap.
        /// </summary>
        public int NativeType
        {
            get
            {
                return this.ReadBlob(this.cur + this.table.File.HasFieldMarshalSize);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal FieldMarshalTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return t.File.HasFieldMarshalSize + t.File.Blob.IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the DeclSecurity Table
    /// </summary>
    public class DeclSecurityTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// 2 byte value.
        /// </summary>
        public int Action
        {
            get { return (short)this.cur; }
        }

        /// <summary>
        /// Index into the TypeDef, Method or Assembly table; more precisely, a
        /// HasDeclSecurity coded index.
        /// </summary>
        public int Parent
        {
            get
            {
                if (this.table.File.HasDeclSecuritySize == 4)
                    return (int)(this.cur + 2);
                else
                    return (int)(short)(this.cur + 2);
            }
        }

        /// <summary>
        /// Index into Blob heap.
        /// </summary>
        public int PermissionSet
        {
            get
            {
                int sz = 2 + this.table.File.HasDeclSecuritySize;
                return this.ReadBlob(this.cur + sz);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal DeclSecurityTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 2 + t.File.HasDeclSecuritySize + t.File.Blob.IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the ClassLayout Table
    /// </summary>
    public class ClassLayoutTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// A 2 byte constant.
        /// </summary>
        public int PackingSize
        {
            get { return (short)this.cur; }
        }

        /// <summary>
        /// A 4 byte constant.
        /// </summary>
        public int ClassSize
        {
            get { return (int)(this.cur + 2); }
        }

        /// <summary>
        /// Index into TypeDef table.
        /// </summary>
        public int Parent
        {
            get { return this.ReadTableIndex(TableNames.TypeDef, this.cur + 6); }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal ClassLayoutTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 6 + t.File[TableNames.TypeDef].IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the FieldLayout Table
    /// </summary>
    public class FieldLayoutTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// A 4 byte constant.
        /// </summary>
        public int Offset
        {
            get { return (int)this.cur; }
        }

        /// <summary>
        /// Index into Field table.
        /// </summary>
        public int Field
        {
            get { return this.ReadTableIndex(TableNames.Field, this.cur + 4); }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal FieldLayoutTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 4 + t.File[TableNames.Field].IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the StandAloneSig Table
    /// </summary>
    public class StandAloneSigTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// Index into the Blob heap
        /// </summary>
        public int Signature
        {
            get { return this.ReadBlob(this.cur); }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal StandAloneSigTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return t.File.Blob.IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the EventMap Table
    /// </summary>
    public class EventMapTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// Index into TypeDef table.
        /// </summary>
        public int Parent
        {
            get { return this.ReadTableIndex(TableNames.TypeDef, this.cur); }
        }

        /// <summary>
        /// Index into Event table.  It marks the first of a contiguous run of
        /// Events owned by this Type.  The run continues to the smaller of:
        /// o	the last row of the Event table
        /// o	the next run of Events, found by inspecting the EventList of the
        ///   next row in the EventMap  table.
        /// </summary>
        public int EventList
        {
            get
            {
                int sz = this.table.File[TableNames.TypeDef].IndexSize;
                return this.ReadTableIndex(TableNames.Event, this.cur + sz);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal EventMapTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return t.File[TableNames.Event].IndexSize +
              t.File[TableNames.TypeDef].IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the Event Table
    /// </summary>
    public class EventTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// A 2 byte bitmask of type EventAttribute, clause 22.1.4.
        /// </summary>
        public int EventFlags
        {
            get { return (short)this.cur; }
        }

        /// <summary>
        /// Index into String heap.
        /// </summary>
        public int Name
        {
            get
            {
                Debug.Assert(this.ReadString(this.cur + 2) != 0,
                  "Error reading CLI file");
                return this.ReadString(this.cur + 2);
            }
        }

        /// <summary>
        /// Index into TypeDef, TypeRef or TypeSpec tables; more precisely, a
        /// TypeDefOrRef coded index. This corresponds to the Type of the Event;
        /// it is not the Type that owns this event.
        /// </summary>
        public int EventType
        {
            get
            {
                int sz = this.table.File.Blob.IndexSize;
                if (this.table.File.TypeDefOrRefSize == 4)
                    return (int)(this.cur + sz);
                else
                    return (int)(short)(this.cur + sz);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal EventTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 2 + t.File.Strings.IndexSize + t.File.TypeDefOrRefSize;
        }
    }

    /// <summary>
    /// Cursor for the PropertyMap Table
    /// </summary>
    public class PropertyMapTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// Index into the TypeDef table.
        /// </summary>
        public int Parent
        {
            get { return this.ReadTableIndex(TableNames.TypeDef, this.cur); }
        }

        /// <summary>
        /// Index into Property table. It marks the first of a contiguous run of
        /// Properties owned by Parent.  The run continues to the smaller of:
        /// o	the last row of the Property table
        /// o	the next run of Properties, found by inspecting the PropertyList of
        ///   the next row in this PropertyMap table
        /// </summary>
        public int PropertyList
        {
            get
            {
                int sz = this.table.File[TableNames.TypeDef].IndexSize;
                return this.ReadTableIndex(TableNames.Property, this.cur + sz);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal PropertyMapTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return t.File[TableNames.TypeDef].IndexSize +
              t.File[TableNames.Property].IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the Property Table
    /// </summary>
    public class PropertyTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// A 2 byte bitmask of type PropertyAttributes, clause 22.1.13.
        /// </summary>
        public int Flags
        {
            get { return (short)this.cur; }
        }

        /// <summary>
        /// Index into String heap.
        /// </summary>
        public int Name
        {
            get
            {
                Debug.Assert(this.ReadString(this.cur + 2) != 0,
                  "Error reading CLI file");
                return this.ReadString(this.cur + 2);
            }
        }

        /// <summary>
        /// Index into Blob heap. The name of this column is misleading.  It does
        /// not index a TypeDef or TypeRef table – instead it indexes the signature
        /// in the Blob heap of the Property.
        /// </summary>
        public int Type
        {
            get
            {
                int sz = 2 + this.table.File.Strings.IndexSize;
                return this.ReadBlob(this.cur + sz);
            }
        }

        /// <summary>
        /// Return an object that allows to inspect the property signature
        /// </summary>
        /// DL 2002-08-06
        /// Based on MethodTableCursor.MethodSignature
        public PropertySig GetPropertySignature()
        {
            return new PropertySig(this.table.File, this.table.File.Blob[this.Type]);
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal PropertyTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 2 + t.File.Strings.IndexSize + t.File.Blob.IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the MethodSemantics Table
    /// </summary>
    public class MethodSemanticsTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// A 2 byte bitmask of type MethodSemanticsAttributes, clause 22.1.10.
        /// </summary>
        public int Semantics
        {
            get { return (short)this.cur; }
        }

        /// <summary>
        /// Index into Method table.
        /// </summary>
        public int Method
        {
            get { return this.ReadTableIndex(TableNames.Method, this.cur + 2); }
        }

        /// <summary>
        /// Index into the Event or Property table; more precisely, a HasSemantics
        /// coded index.
        /// </summary>
        public int Association
        {
            get
            {
                int sz = 2 + this.table.File[TableNames.Method].IndexSize;
                if (this.table.File.HasSemanticsSize == 4)
                    return (int)(this.cur + sz);
                else
                    return (int)(short)(this.cur + sz);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal MethodSemanticsTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 2 + t.File[TableNames.Method].IndexSize +
              t.File.HasSemanticsSize;
        }
    }

    /// <summary>
    /// Cursor for the MethodImpl Table
    /// </summary>
    public class MethodImplTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// Index into TypeDef table.
        /// </summary>
        public int Class
        {
            get { return this.ReadTableIndex(TableNames.TypeDef, this.cur); }
        }

        /// <summary>
        /// Index into Method or MemberRef table; more precisely, a MethodDefOrRef
        /// coded index.
        /// </summary>
        public int MethodBody
        {
            get
            {
                int sz = this.table.File[TableNames.TypeDef].IndexSize;
                if (this.table.File.MethodDefOrRefSize == 4)
                    return (int)(this.cur + sz);
                else
                    return (int)(short)(this.cur + sz);
            }
        }

        /// <summary>
        /// Index into Method or MemberRef table; more precisely, a MethodDefOrRef
        /// coded index.
        /// </summary>
        public int MethodDeclaration
        {
            get
            {
                int sz = this.table.File[TableNames.TypeDef].IndexSize +
                  this.table.File.MethodDefOrRefSize;
                if (this.table.File.MethodDefOrRefSize == 4)
                    return (int)(this.cur + sz);
                else
                    return (int)(short)(this.cur + sz);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal MethodImplTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return t.File[TableNames.TypeDef].IndexSize +
              2 * t.File.MethodDefOrRefSize;
        }
    }

    /// <summary>
    /// Cursor for the ModuleRef Table
    /// </summary>
    public class ModuleRefTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// Index into String heap.
        /// </summary>
        public int Name
        {
            get
            {
                Debug.Assert(this.ReadString(this.cur) != 0, "Error reading CLI file");
                return this.ReadString(this.cur);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal ModuleRefTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return t.File.Strings.IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the TypeSpec Table
    /// </summary>
    public class TypeSpecTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// The TypeSpec table has just one column, which indexes the 
        /// specification of a Type, stored in the Blob heap.  This provides 
        /// a metadata token for that Type (rather than simply an index into 
        /// the Blob heap) – this is required, typically, for array operations – 
        /// creating, or calling methods on the array class.
        /// The TypeSpec table has the following column:
        /// •	Signature (index into the Blob heap, where the blob is formatted as 
        ///   specified in clause 22.2.14)
        /// Note that TypeSpec tokens can be used with any of the CIL instructions 
        /// that take a TypeDef or TypeRef token – specifically:
        /// castclass, cpobj, initobj, isinst, ldelema, ldobj, mkrefany, newarr, 
        /// refanyval, sizeof, stobj, box, unbox
        /// </summary>
        public int Signature
        {
            get
            {
                return this.ReadBlob(this.cur);
            }
        }

        public Type GetReflectionType()
        {
            return this.table.File.Assembly.ManifestModule.ResolveType(this.MetadataToken);
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal TypeSpecTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return t.File.Blob.IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the ImplMapTable Table
    /// </summary>
    public class ImplMapTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// A 2 byte bitmask of type PInvokeAttributes, clause 22.1.7.
        /// </summary>
        public int MappingFlags
        {
            get { return (short)this.cur; }
        }

        /// <summary>
        /// Index into the Field or Method table; more precisely, a MemberForwarded
        /// coded index. However, it only ever indexes the Method table, since 
        /// Field export is not supported.
        /// </summary>
        public int MemberForwarded
        {
            get
            {
                if (this.table.File.MemberForwardedSize == 4)
                    return (int)(this.cur + 2);
                else
                    return (int)(short)(this.cur + 2);
            }
        }

        /// <summary>
        /// Index into the String heap.
        /// </summary>
        public int ImportName
        {
            get
            {
                int sz = 2 + this.table.File.MemberForwardedSize;
                Debug.Assert(this.ReadString(this.cur + sz) != 0,
                  "Error reading CLI file");
                return this.ReadString(this.cur + sz);
            }
        }

        /// <summary>
        /// Index into the ModuleRef table.
        /// </summary>
        public int ImportScope
        {
            get
            {
                int sz = 2 + this.table.File.MemberForwardedSize +
                  this.table.File.Strings.IndexSize;
                return this.ReadTableIndex(TableNames.ModuleRef, this.cur + sz);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal ImplMapTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 2 + t.File.MemberForwardedSize + t.File.Strings.IndexSize +
              t.File[TableNames.ModuleRef].IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the FieldRVA Table
    /// </summary>
    public class FieldRVATableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// A 4 byte constant.
        /// </summary>
        public int RVA
        {
            get { return (int)this.cur; }
        }

        /// <summary>
        /// Index into Field table.
        /// </summary>
        public int Field
        {
            get { return this.ReadTableIndex(TableNames.Field, this.cur + 4); }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal FieldRVATableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 4 + t.File[TableNames.Field].IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the Assembly Table
    /// </summary>
    public class AssemblyTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// A 4 byte constant of type AssemblyHashAlgorithm, clause 22.1.1.
        /// </summary>
        public int HashAlgId
        {
            get { return (int)this.cur; }
        }

        /// <summary>
        /// Major version of the Assembly
        /// </summary>
        public int MajorVersion
        {
            get { return (int)(short)(this.cur + 4); }
        }

        /// <summary>
        ///  Minor Version of the Assembly
        /// </summary>
        public int MinorVersion
        {
            get { return (int)(short)(this.cur + 6); }
        }

        /// <summary>
        /// Build Number of the Assembly
        /// </summary>
        public int BuildNumber
        {
            get { return (int)(short)(this.cur + 8); }
        }

        /// <summary>
        /// Revision Number of the assembly
        /// </summary>
        public int RevisionNumber
        {
            get { return (int)(short)(this.cur + 10); }
        }

        /// <summary>
        /// A 4 byte bitmask of type AssemblyFlags, clause 22.1.2.
        /// </summary>
        public int Flags
        {
            get { return (int)(this.cur + 12); }
        }

        /// <summary>
        /// Index into Blob heap.
        /// </summary>
        public int PublicKey
        {
            get { return this.ReadBlob(this.cur + 16); }
        }

        /// <summary>
        /// Index into String heap.
        /// </summary>
        public int Name
        {
            get
            {
                int sz = 16 + table.File.Blob.IndexSize;
                Debug.Assert(this.ReadString(this.cur + sz) != 0,
                  "Error reading CLI file");
                return this.ReadString(this.cur + sz);
            }
        }

        /// <summary>
        /// Index into String heap.
        /// </summary>
        public int Culture
        {
            get
            {
                int sz = 16 + table.File.Blob.IndexSize + table.File.Strings.IndexSize;
                return this.ReadString(this.cur + sz);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal AssemblyTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 16 + t.File.Blob.IndexSize + 2 * t.File.Strings.IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the AssemblyProcessor Table
    /// </summary>
    public class AssemblyProcessorTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// A 4 byte constant.
        /// </summary>
        public int Processor
        {
            get { return (int)this.cur; }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal AssemblyProcessorTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 4;
        }
    }

    /// <summary>
    /// Cursor for the AssemblyOS Table
    /// </summary>
    public class AssemblyOSTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// A 4 byte constant.
        /// </summary>
        public int OSPlatformID
        {
            get { return (int)this.cur; }
        }

        /// <summary>
        /// A 4 byte constant.
        /// </summary>
        public int OSMajorVersion
        {
            get { return (int)(this.cur + 4); }
        }

        /// <summary>
        /// A 4 byte constant.
        /// </summary>
        public int OSMinorVersion
        {
            get { return (int)(this.cur + 8); }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal AssemblyOSTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 12;
        }
    }

    /// <summary>
    /// Cursor for the AssemblyRef Table
    /// </summary>
    public class AssemblyRefTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// Major Version of assembly
        /// </summary>
        public ushort MajorVersion
        {
            get { return (ushort)this.cur; }
        }

        /// <summary>
        /// Minor Version of assembly
        /// </summary>
        public ushort MinorVersion
        {
            get { return (ushort)(this.cur + 2); }
        }

        /// <summary>
        /// Build Number of assembly
        /// </summary>
        public ushort BuildNumber
        {
            get { return (ushort)(this.cur + 4); }
        }

        /// <summary>
        /// Revision Number of assembly
        /// </summary>
        public ushort RevisionNumber
        {
            get { return (ushort)(this.cur + 6); }
        }

        /// <summary>
        /// A 4 byte bitmask of type AssemblyFlags, clause 22.1.2
        /// </summary>
        public int Flags
        {
            get { return (int)(this.cur + 8); }
        }

        /// <summary>
        /// Index into Blob heap – the public key or token that identifies the 
        /// author of this Assembly
        /// </summary>
        public int PublicKeyOrToken
        {
            get { return this.ReadBlob(this.cur + 12); }
        }

        /// <summary>
        /// Index into String heap
        /// </summary>
        public int Name
        {
            get
            {
                int sz = 12 + this.table.File.Blob.IndexSize;
                Debug.Assert(this.ReadString(this.cur + sz) != 0,
                  "Error reading CLI file");
                return this.ReadString(this.cur + sz);
            }
        }

        /// <summary>
        /// Index into String heap
        /// </summary>
        public int Culture
        {
            get
            {
                int sz = 12 + this.table.File.Blob.IndexSize +
                  this.table.File.Strings.IndexSize;
                return this.ReadString(this.cur + sz);
            }
        }

        /// <summary>
        /// Index into Blob heap
        /// </summary>
        public int HashValue
        {
            get
            {
                int sz = 12 + this.table.File.Blob.IndexSize +
                  2 * this.table.File.Strings.IndexSize;
                return this.ReadBlob(this.cur + sz);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal AssemblyRefTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 12 + 2 * t.File.Blob.IndexSize + 2 * t.File.Strings.IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the AssemblyRefProcessor Table
    /// </summary>
    public class AssemblyRefProcessorTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// A 4 byte constant.
        /// </summary>
        public int Processor
        {
            get { return (int)this.cur; }
        }

        /// <summary>
        /// Index into the AssemblyRef table.
        /// </summary>
        public int AssemblyRef
        {
            get { return this.ReadTableIndex(TableNames.AssemblyRef, this.cur + 4); }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal AssemblyRefProcessorTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 4 + t.File[TableNames.AssemblyRef].IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the AssemblyRefOS Table
    /// </summary>
    public class AssemblyRefOSTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// A 4 byte constant.
        /// </summary>
        public int OSPlatformId
        {
            get { return (int)this.cur; }
        }

        /// <summary>
        /// A 4 byte constant.
        /// </summary>
        public int OSMajorVersion
        {
            get { return (int)(this.cur + 4); }
        }

        /// <summary>
        /// A 4 byte constant.
        /// </summary>
        public int OSMinorVersion
        {
            get { return (int)(this.cur + 8); }
        }

        /// <summary>
        /// Index into AssemblyRef table.
        /// </summary>
        public int AssemblyRef
        {
            get { return this.ReadTableIndex(TableNames.AssemblyRef, this.cur + 12); }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal AssemblyRefOSTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 12 + t.File[TableNames.AssemblyRef].IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the File Table
    /// </summary>
    public class FileTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// A 4 byte bitmask of type FileAttributes, clause 22.1.6.
        /// </summary>
        public int Flags
        {
            get { return (int)this.cur; }
        }

        /// <summary>
        /// Index into String heap.
        /// </summary>
        public int Name
        {
            get
            {
                Debug.Assert(this.ReadString(this.cur + 4) != 0,
                  "Error reading CLI file");
                return this.ReadString(this.cur + 4);
            }
        }

        /// <summary>
        /// Index into Blob heap.
        /// </summary>
        public int HashValue
        {
            get
            {
                return this.ReadBlob(this.cur + 4 + this.table.File.Strings.IndexSize);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal FileTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 4 + t.File.Strings.IndexSize + t.File.Blob.IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the ExportedType Table
    /// </summary>
    public class ExportedTypeTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// A 4 byte bitmask of type TypeAttributes, clause 22.1.14.
        /// </summary>
        public int Flags
        {
            get { return (int)this.cur; }
        }

        /// <summary>
        /// 4 byte index into a TypeDef table of another module in this Assembly.
        /// This field is used as a hint only.  If the entry in the target TypeDef
        /// table matches the TypeName and TypeNamespace entries in this table,
        /// resolution has succeeded.  But if there is a mismatch, the CLI shall
        /// fall back to a search of the target TypeDef table
        /// </summary>
        public int TypeDefId
        {
            get { return (int)(this.cur + 4); }
        }

        /// <summary>
        /// Index into the String heap.
        /// </summary>
        public int TypeName
        {
            get
            {
                Debug.Assert(this.ReadString(this.cur + 8) != 0,
                  "Error reading CLI file");
                return this.ReadString(this.cur + 8);
            }
        }

        /// <summary>
        /// Index into String heap.
        /// </summary>
        public int TypeNamespace
        {
            get
            {
                int sz = 8 + this.table.File.Strings.IndexSize;
                return this.ReadString(this.cur + sz);
            }
        }

        /// <summary>
        /// This can be an index (more precisely, an Implementation coded index.
        /// into one of 2 tables, as follows:
        /// o	File table, where that entry says which module in the current
        ///   assembly holds the TypeDef
        /// o	ExportedType table, where that entry is the enclosing Type of the
        ///   current nested Type
        /// </summary>
        public int Implementation
        {
            get
            {
                int sz = 8 + 2 * this.table.File.Strings.IndexSize;
                if (this.table.File.ImplementationSize == 4)
                    return (int)(this.cur + sz);
                else
                    return (int)(short)(this.cur + sz);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal ExportedTypeTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 8 + 2 * t.File.Strings.IndexSize + t.File.ImplementationSize;
        }
    }

    /// <summary>
    /// Cursor for the ManifestResource Table
    /// </summary>
    public class ManifestResourceTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// A 4 byte constant.
        /// </summary>
        public int Offset
        {
            get { return (int)this.cur; }
        }

        /// <summary>
        /// A 4 byte bitmask of type ManifestResourceAttributes, clause 22.1.8.
        /// </summary>
        public int Flags
        {
            get { return (int)(this.cur + 4); }
        }

        /// <summary>
        /// Index into String heap.
        /// </summary>
        public int Name
        {
            get
            {
                Debug.Assert(this.ReadString(this.cur + 8) != 0,
                  "Error reading CLI file");
                return this.ReadString(this.cur + 8);
            }
        }

        /// <summary>
        /// Index into File table, or AssemblyRef table, or  null; more precisely,
        /// an Implementation coded index.
        /// </summary>
        public int Implementation
        {
            get
            {
                int sz = 8 + this.table.File.Strings.IndexSize;
                if (this.table.File.ImplementationSize == 4)
                    return (int)(this.cur + sz);
                else
                    return (int)(short)(this.cur + sz);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal ManifestResourceTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 8 + t.File.Strings.IndexSize + t.File.ImplementationSize;
        }
    }

    /// <summary>
    /// Cursor for the NestedClass Table
    /// </summary>
    public class NestedClassTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// Index into TypeDef table.
        /// </summary>
        public int NestedClass
        {
            get { return this.ReadTableIndex(TableNames.TypeDef, this.cur); }
        }

        /// <summary>
        /// Index into TypeDef table.
        /// </summary>
        public int EnclosingClass
        {
            get
            {
                int sz = this.table.File[TableNames.TypeDef].IndexSize;
                return this.ReadTableIndex(TableNames.TypeDef, this.cur + sz);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal NestedClassTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 2 * t.File[TableNames.TypeDef].IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the GenericParam Table
    /// </summary>
    public class GenericParamTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// the 2-byte index of the generic parameter, numbered left-to-right, from zero
        /// </summary>
        public int Number
        {
            get { return (int)(ushort)this.cur; }
        }

        /// <summary>
        /// a 2-byte bitmask of type GenericParamAttributes, §23.1.7
        /// </summary>
        public int Flags
        {
            get { return (int)(ushort)(this.cur + 2); }
        }

        /// <summary>
        /// an index into the TypeDef or MethodDef table, specifying the Type or Method to
        /// which this generic parameter applies; more precisely, a TypeOrMethodDef (§24.2.6)
        /// coded index
        /// </summary>
        public int Owner
        {
            get { return (int)(ushort)(this.cur + 4); }
        }

        /// <summary>
        /// a non-null index into the String heap, giving the name for the generic parameter.
        /// This is purely descriptive and is used only by source language compilers and by
        /// Reflection
        /// </summary>
        public int Name
        {
            get
            {
                Debug.Assert(this.ReadString(this.cur + 4 + this.table.File.TypeOrMethodDefSize) != 0,
                    "Error reading CLI file");
                return this.ReadString(this.cur + 6);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal GenericParamTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return 4 + t.File.TypeOrMethodDefSize + t.File.Strings.IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the NestedClass Table
    /// </summary>
    public class MethodSpecTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// an index into the MethodDef or MemberRef table, specifying to which
        /// generic method this row refers; that is, which generic method this
        /// row is an instantiation of; more precisely, a MethodDefOrRef
        /// (§24.2.6) coded index
        /// </summary>
        public int Method
        {
            get
            {
                if (this.table.File.MethodDefOrRefSize == 4)
                    return (int)(this.cur);
                else
                    return (int)(short)(this.cur);
            }
        }

        /// <summary>
        /// an index into the Blob heap (§23.2.15), holding the signature of
        /// this instantiation
        /// </summary>
        public int Instantiation
        {
            get { return this.ReadBlob(this.cur + this.table.File.MethodDefOrRefSize); }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal MethodSpecTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return t.File.MethodDefOrRefSize + t.File.Blob.IndexSize;
        }
    }

    /// <summary>
    /// Cursor for the NestedClass Table
    /// </summary>
    public class GenericParamConstraintTableCursor : TableCursor
    {
        /// <summary>
        /// Size of the row in bytes
        /// </summary>
        private int sz;

        /// <summary>
        /// an index into the GenericParam table, specifying to which generic parameter
        /// this row refers
        /// </summary>
        public int Owner
        {
            get
            {
                if (this.table.File[TableNames.GenericParam].IndexSize == 4)
                    return (int)(this.cur);
                else
                    return (int)(short)(this.cur);
            }
        }

        /// <summary>
        /// an index into the TypeDef, TypeRef, or TypeSpec tables, specifying
        /// from which class this generic parameter is constrained to derive;
        /// or which interface this generic parameter is constrained to implement;
        /// more precisely, a TypeDefOrRef (§24.2.6) coded index
        /// </summary>
        public int Constraint
        {
            get
            {
                int sz = this.table.File[TableNames.GenericParam].IndexSize;
                if (this.table.File.TypeDefOrRefSize == 4)
                    return (int)(this.cur + sz);
                else
                    return (int)(short)(this.cur + sz);
            }
        }

        /// <summary>
        /// Build a cursor to the table.
        /// </summary>
        /// <param name="t">Table accessed by the cursor</param>
        internal GenericParamConstraintTableCursor(Table t)
            : base(t)
        {
            sz = RowSz(t);
        }

        /// <summary>
        /// Size of a row of the table
        /// </summary>
        public override int RowSize
        {
            get { return sz; }
        }

        /// <summary>
        /// Perform the real computation of the row
        /// </summary>
        /// <param name="t">Table to which the cursor belongs</param>
        /// <returns>The size of the row in bytes</returns>
        internal static int RowSz(Table t)
        {
            return t.File[TableNames.GenericParam].IndexSize + t.File.TypeDefOrRefSize;
        }
    }

    /// <summary>
    /// SEH (structured exception handling) clause
    /// </summary>
    /// DL 2002-08-10
    /// Used by SEHTableCursor.
    public struct SEHClause
    {
        public int Flags;
        public int TryOffset;
        public int TryLength;
        public int HandlerOffset;
        public int HandlerLength;
        public int ClassTokenOrFilterOffset;

        /// <summary>
        /// Builds an SEH Clause knowing its start and
        /// whether or not Fat records are being used.
        /// </summary>
        /// <param name="cur">Pointer to SEH Clause.</param>
        /// <param name="fat">Bool to indicate if fat of thin SEH clause
        /// format used.</param>
        internal SEHClause(MapPtr cur, bool fat)
        {
            if (fat)
            {
                this.Flags = (int)cur;
                this.TryOffset = (int)(cur + 4);
                this.TryLength = (int)(cur + 8);
                this.HandlerOffset = (int)(cur + 12);
                this.HandlerLength = (int)(cur + 16);
                this.ClassTokenOrFilterOffset = (int)(cur + 20);
            }
            else
            {
                this.Flags = (short)cur;
                this.TryOffset = (short)(cur + 2);
                this.TryLength = cur[4];
                this.HandlerOffset = (short)(cur + 5);
                this.HandlerLength = cur[7];
                this.ClassTokenOrFilterOffset = (int)(cur + 8);
            }
        }
    }

    /// <summary>
    /// Class provides CLIFile.Cursor capabilities for the SEH Table section
    /// that follow the IL of a method.
    /// </summary>
    /// <remarks>
    /// The SEH, or structured exception handling, clauses of a method do not
    /// constitute a metadata table.  Thus, SEHTableCursor does not inherit from
    /// CLIFile.TableCursor.  However, like ILCursor, it does implement
    /// the similar methods to CLIFile.Table Cursor.
    /// </remarks>
    /// DL 2002-08-09
    ///
    public class SEHTableCursor
    {
        /// <summary>
        /// Pointer to the header of the SEH Table section.
        /// </summary>
        private MapPtr header;

        /// <summary>
        /// Pointer to array of SEH Clauses.
        /// </summary>
        private MapPtr Base;

        /// <summary>
        /// Pointer to the current clause.
        /// </summary>
        private MapPtr cur;

        /// <summary>
        /// Avoids need to continually calculate whether header format is fat or thin.
        /// </summary>
        private bool FatHeader;

        /// <summary>
        /// Number of rows in the table
        /// </summary>
        private int rows;

        /// <summary>
        /// Position of current clause.
        /// </summary>
        public long Position
        {
            get { return (this.cur - this.Base) / RowSize; }
        }

        public SEHClause Clause;

        /// <summary>
        /// Build a cursor for SEH Table clauses.
        /// </summary>
        /// <param name="p">Pointer to start of SEH Section</param>
        internal SEHTableCursor(MapPtr p)
        {

            /// Determine type of headers used in SEH Table.
            this.FatHeader = ((p[0] & 0x40) == 1);

            /// Construct references to table header and clauses.
            /// SEH clauses always started 4 bytes after the header start.
            /// DataSize includes portion of section used by header.
            if (this.FatHeader)
            {
                this.header = new MapPtr(p, 4);
                /// Fat Header stores DataSize is in 3 bytes.  Casting inserted
                /// to avoid sign extension on left shift of int.
                uint SEHTableSize = ((uint)((int)header)) >> 8;
                this.Base = new MappedView.MapPtr(p + 4, ((int)SEHTableSize) - 4);
                this.cur = this.Base;
                this.rows = (int)(SEHTableSize / 24);
            }
            else
            {
                this.header = new MapPtr(p, 2);
                int SEHTableSize = header[1];
                this.Base = new MappedView.MapPtr(p + 4, SEHTableSize - 4);
                this.cur = this.Base;
                this.rows = SEHTableSize / 12;
            }

            /// Verify that this is an SEH Table!
            Debug.Assert((this.header[0] & 0x01) == 1, "SEH Cursor not initialized with SEH Table!");

            this.Clause = new SEHClause(this.cur, this.FatHeader);
            this.cur += this.RowSize;
        }

        /// <summary>
        /// Number of rows of the table. 
        /// </summary>
        public int Rows
        {
            get
            {
                return rows;
            }
        }

        /// <summary>
        /// Provide header data.
        /// </summary>
        public int Header
        {
            get { return (int)(this.FatHeader ? (int)this.header : (short)this.header); }
        }

        /// <summary>
        /// True if the cursor points for first record.
        /// BOF and EOF can be true at same time.
        /// </summary>
        public bool BOF
        {
            get { return ((this.Base + this.RowSize) == this.cur); }
        }

        /// <summary>
        /// True if the cursor is at the end of the stream.
        /// BOF and EOF can be true at same time.
        /// </summary>
        public bool EOF
        {
            get { return cur.Length == 0; }
        }

        /// <summary>
        /// Reset cursor to first valid SEH Clause.
        /// </summary>
        public void Reset()
        {
            this.cur = this.Base;
            Clause = new SEHClause(this.cur, this.FatHeader);
            this.cur += this.RowSize;
        }

        /// <summary>
        /// Advance the cursor to the next SEH Clause.  The SEHClause read is
        /// stored in the SEHClause field.
        /// </summary>
        /// <returns>True if the instruction has been read.</returns>
        public bool Next()
        {
            if (this.EOF)
                return false;

            Clause = new SEHClause(this.cur, this.FatHeader);
            this.cur += this.RowSize;
            return true;
        }

        /// <summary>
        /// Return the size in bytes of a row.
        /// </summary>
        public int RowSize
        {
            get { return (this.FatHeader ? 24 : 12); }
        }
    } // class
}
