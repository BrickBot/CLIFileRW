/// ------------------------------------------------------------
/// Copyright (c) 2002-2008 Antonio Cisternino (cisterni@di.unipi.it)
/// 
/// File: CLIFile.cs
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
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using MappedView;

namespace CLIFileRW
{
    /// <summary>
    /// Objects of this class represent CLI tables as specified in ...
    /// The TabelType property contains the code of the table.
    /// </summary>
    public class Table
    {
        /// <summary>
        /// Number of rows of the table.
        /// </summary>
        private int sz = 0;

        /// <summary>
        /// Empty table. It is used as a placeholder instead of null.
        /// </summary>
        internal static Table Null = new Table(null, TableNames.MaxNum, false);
        /// <summary>
        /// Used to decide if 2 or 4 bytes are required to index this table.
        /// </summary>
        internal bool LargeIndexes = false;
        /// <summary>
        /// Number of bytes required to represent an index to the table.
        /// </summary>
        internal int IndexSize = 2;
        /// <summary>
        /// Type of the table <seealso cref="CLIFile.TableNames"/>
        /// </summary>
        internal TableNames Type;
        /// <summary>
        /// File that contains the table.
        /// </summary>
        internal CLIFile File;
        /// <summary>
        /// The pointer to the origin of the table.
        /// </summary>
        internal MapPtr Start;

        /// <summary>
        /// Type of the table.
        /// </summary>
        public TableNames TableType
        {
            get { return Type; }
        }

        /// <summary>
        /// File to which the table belongs.
        /// </summary>
        public CLIFile CLIFile
        {
            get { return File; }
        }

        /// <summary>
        /// Indicate if the table is sorted or not as specified in ...
        /// </summary>
        public bool Sorted
        {
            get { return sorted; }
        }

        /// <summary>
        /// Return the pointer to the beginning of the table.
        /// </summary>
        public MapPtr Begin
        {
            get { return Start; }
        }

        /// <summary>
        /// Return the number of rows of the table.
        /// </summary>
        public int Rows
        {
            get { return sz; }
        }
        /// <summary>
        /// Number of rows of the table.
        /// When set it updates LargeIndexes and IndexSize.
        /// </summary>
        internal int Size
        {
            get { return sz; }
            set
            {
                sz = value;
                LargeIndexes = sz > 65536;
                IndexSize = LargeIndexes ? 4 : 2;
            }
        }
        /// <summary>
        /// Record if the table is sorted or not.
        /// </summary>
        internal bool sorted;

        /// <summary>
        /// Build a table that represents a CLI file table.
        /// </summary>
        /// <param name="f">File to which the table belongs.</param>
        /// <param name="n">Name of the table</param>
        /// <param name="s">Is it sorted?</param>
        internal Table(CLIFile f, TableNames n, bool s)
        {
            File = f;
            Type = n;
            sorted = s;
        }

        /// <summary>
        /// Build the appropriate cursor for the table. A downcast to the
        /// appropriated type is often needed.
        /// </summary>
        /// <returns>A cursor to the table or null if a wrong table name is
        /// provided.</returns>
        public TableCursor GetCursor()
        {
            switch ((TableNames)Type)
            {
                case TableNames.Module: return new ModuleTableCursor(this);
                case TableNames.TypeRef: return new TypeRefTableCursor(this);
                case TableNames.TypeDef: return new TypeDefTableCursor(this);
                case TableNames.Field: return new FieldTableCursor(this);
                case TableNames.Method: return new MethodTableCursor(this);
                case TableNames.Param: return new ParamTableCursor(this);
                case TableNames.InterfaceImpl: return new InterfaceImplTableCursor(this);
                case TableNames.MemberRef: return new MemberRefTableCursor(this);
                case TableNames.Constant: return new ConstantTableCursor(this);
                case TableNames.CustomAttribute: return new CustomAttributeTableCursor(this);
                case TableNames.FieldMarshal: return new FieldMarshalTableCursor(this);
                case TableNames.DeclSecurity: return new DeclSecurityTableCursor(this);
                case TableNames.ClassLayout: return new ClassLayoutTableCursor(this);
                case TableNames.FieldLayout: return new FieldLayoutTableCursor(this);
                case TableNames.StandAloneSig: return new StandAloneSigTableCursor(this);
                case TableNames.EventMap: return new EventMapTableCursor(this);
                case TableNames.Event: return new EventTableCursor(this);
                case TableNames.PropertyMap: return new PropertyMapTableCursor(this);
                case TableNames.Property: return new PropertyTableCursor(this);
                case TableNames.MethodSemantics: return new MethodSemanticsTableCursor(this);
                case TableNames.MethodImpl: return new MethodImplTableCursor(this);
                case TableNames.ModuleRef: return new ModuleRefTableCursor(this);
                case TableNames.TypeSpec: return new TypeSpecTableCursor(this);
                case TableNames.ImplMap: return new ImplMapTableCursor(this);
                case TableNames.FieldRVA: return new FieldRVATableCursor(this);
                case TableNames.Assembly: return new AssemblyTableCursor(this);
                case TableNames.AssemblyProcessor: return new AssemblyProcessorTableCursor(this);
                case TableNames.AssemblyOS: return new AssemblyOSTableCursor(this);
                case TableNames.AssemblyRef: return new AssemblyRefTableCursor(this);
                case TableNames.AssemblyRefProcessor: return new AssemblyRefProcessorTableCursor(this);
                case TableNames.AssemblyRefOS: return new AssemblyRefOSTableCursor(this);
                case TableNames.File: return new FileTableCursor(this);
                case TableNames.ExportedType: return new ExportedTypeTableCursor(this);
                case TableNames.ManifestResource: return new ManifestResourceTableCursor(this);
                case TableNames.NestedClass: return new NestedClassTableCursor(this);
                case TableNames.GenericParam: return new GenericParamTableCursor(this);
                case TableNames.MethodSpec: return new MethodSpecTableCursor(this);
                case TableNames.GenericParamConstraint: return new GenericParamConstraintTableCursor(this);
            }
            return null;
        }

        /// <summary>
        /// Calculate the size of the table.
        /// </summary>
        /// <returns>The size, in bytes, of the table</returns>
        internal int TableSize()
        {
            switch ((TableNames)Type)
            {
                case TableNames.Module: return sz * ModuleTableCursor.RowSz(this);
                case TableNames.TypeRef: return sz * TypeRefTableCursor.RowSz(this);
                case TableNames.TypeDef: return sz * TypeDefTableCursor.RowSz(this);
                case TableNames.Field: return sz * FieldTableCursor.RowSz(this);
                case TableNames.Method: return sz * MethodTableCursor.RowSz(this);
                case TableNames.Param: return sz * ParamTableCursor.RowSz(this);
                case TableNames.InterfaceImpl: return sz * InterfaceImplTableCursor.RowSz(this);
                case TableNames.MemberRef: return sz * MemberRefTableCursor.RowSz(this);
                case TableNames.Constant: return sz * ConstantTableCursor.RowSz(this);
                case TableNames.CustomAttribute: return sz * CustomAttributeTableCursor.RowSz(this);
                case TableNames.FieldMarshal: return sz * FieldMarshalTableCursor.RowSz(this);
                case TableNames.DeclSecurity: return sz * DeclSecurityTableCursor.RowSz(this);
                case TableNames.ClassLayout: return sz * ClassLayoutTableCursor.RowSz(this);
                case TableNames.FieldLayout: return sz * FieldLayoutTableCursor.RowSz(this);
                case TableNames.StandAloneSig: return sz * StandAloneSigTableCursor.RowSz(this);
                case TableNames.EventMap: return sz * EventMapTableCursor.RowSz(this);
                case TableNames.Event: return sz * EventTableCursor.RowSz(this);
                case TableNames.PropertyMap: return sz * PropertyMapTableCursor.RowSz(this);
                case TableNames.Property: return sz * PropertyTableCursor.RowSz(this);
                case TableNames.MethodSemantics: return sz * MethodSemanticsTableCursor.RowSz(this);
                case TableNames.MethodImpl: return sz * MethodImplTableCursor.RowSz(this);
                case TableNames.ModuleRef: return sz * ModuleRefTableCursor.RowSz(this);
                case TableNames.TypeSpec: return sz * TypeSpecTableCursor.RowSz(this);
                case TableNames.ImplMap: return sz * ImplMapTableCursor.RowSz(this);
                case TableNames.FieldRVA: return sz * FieldRVATableCursor.RowSz(this);
                case TableNames.Assembly: return sz * AssemblyTableCursor.RowSz(this);
                case TableNames.AssemblyProcessor: return sz * AssemblyProcessorTableCursor.RowSz(this);
                case TableNames.AssemblyOS: return sz * AssemblyOSTableCursor.RowSz(this);
                case TableNames.AssemblyRef: return sz * AssemblyRefTableCursor.RowSz(this);
                case TableNames.AssemblyRefProcessor: return sz * AssemblyRefProcessorTableCursor.RowSz(this);
                case TableNames.AssemblyRefOS: return sz * AssemblyRefOSTableCursor.RowSz(this);
                case TableNames.File: return sz * FileTableCursor.RowSz(this);
                case TableNames.ExportedType: return sz * ExportedTypeTableCursor.RowSz(this);
                case TableNames.ManifestResource: return sz * ManifestResourceTableCursor.RowSz(this);
                case TableNames.NestedClass: return sz * NestedClassTableCursor.RowSz(this);
                case TableNames.GenericParam: return sz * GenericParamTableCursor.RowSz(this);
                case TableNames.MethodSpec: return sz * MethodSpecTableCursor.RowSz(this);
                case TableNames.GenericParamConstraint: return sz * GenericParamConstraintTableCursor.RowSz(this);
            }
            return -1;
        }
    }

    /// <summary>
    /// This class wraps the blob heap of a PE file.
    /// </summary>
    public class BlobHeap
    {
        /// <summary>
        /// Origin of the blob heap.
        /// </summary>
        internal MapPtr Base;
        /// <summary>
        /// Large is true if the size is greater than 65536.
        /// </summary>
        internal bool Large;
        /// <summary>
        /// Size, in bytes, of indexes to the blob heap.
        /// </summary>
        internal int IndexSize;

        /// <summary>
        /// Return the pointer to the beginning of the blob heap.
        /// The pointer is restricted to the Blob area.
        /// </summary>
        public MapPtr Begin
        {
            get { return Base; }
        }

        /// <summary>
        /// Size of the heap in bytes
        /// </summary>
        public int Size
        {
            get { return (int)Base.Length; }
        }

        /// <summary>
        /// Constructor of the wrapper.
        /// </summary>
        /// <param name="p">Pointer to the blob heap</param>
        /// <param name="large">Size of the heap > 655236</param>
        internal BlobHeap(MapPtr p, bool large)
        {
            Base = p;
            Large = large;
            IndexSize = large ? 4 : 2;
        }

        /// <summary>
        /// Calculate the size of a blob and return an appropriate pointer.
        /// </summary>
        /// <param name="p">Pointer to the beginning of the blob.</param>
        /// <returns>The pointer to the data limited to the appropriate
        /// size.</returns>
        static private MapPtr GetPtr(MapPtr p)
        {
            if ((p[0] & 0x80) == 0)
                return new MapPtr(p + 1, p[0]);

            if ((p[0] & 0x40) == 0)
                return new MapPtr(p + 2, (p[0] & ~0x80) << 8 | p[1]);

            return new MapPtr(p + 4,
              (p[0] & ~0xC0) << 24 | p[1] << 16 | p[2] << 8 | p[3]);
        }

        /// <summary>
        /// Access the blob heap reading a coded index and returing a pointer
        /// to the specified location within the heap.
        /// </summary>
        /// <param name="idx">Pointer to the coded index</param>
        /// <returns>The pointer to the location into the heap.</returns>
        public MapPtr Access(MapPtr idx)
        {
            int id = Large ? (int)idx : (short)idx;
            return GetPtr(Base + id);
        }

        /// <summary>
        /// Return the pointer to the Blob heap given the byte offset.
        /// </summary>
        public MapPtr this[int idx]
        {
            get { return GetPtr(Base + idx); }
        }
    }

    /// <summary>
    /// This class wraps the Strings heap.
    /// </summary>
    public class StringsHeap
    {
        /// <summary>
        /// Origin of the Strings table.
        /// </summary>
        internal MapPtr Base;
        /// <summary>
        /// True if the size of the table is greater than 65536.
        /// </summary>
        internal bool Large;
        /// <summary>
        /// Size of indexes to the table (2 or 4 bytes).
        /// </summary>
        internal int IndexSize;

        /// <summary>
        /// Pointer to the begin of the strings table. The pointer is constrained
        /// to the area of the table.
        /// </summary>
        public MapPtr Begin
        {
            get { return Base; }
        }

        /// <summary>
        /// Constructor of the wrapper class.
        /// </summary>
        /// <param name="p">Pointer to the table.</param>
        /// <param name="large">Must be true if the size of the table is greater
        /// than 65536.</param>
        internal StringsHeap(MapPtr p, bool large)
        {
            Base = p;
            Large = large;
            IndexSize = large ? 4 : 2;
        }

        /// <summary>
        /// Access the strings table given the byte offset within the table.
        /// </summary>
        /// <param name="idx">Pointer to the coded offset of the string.</param>
        /// <returns>The string within the table.</returns>
        public string Access(MapPtr idx)
        {
            int id = Large ? (int)idx : (short)idx;
            return (UTFRawString)(Base + id);
        }

        /// <summary>
        /// Access the string heap at the specified offset.
        /// </summary>
        public string this[int idx]
        {
            get { return (UTFRawString)(Base + idx); }
        }
    }

    /// <summary>
    /// This class wraps the blob heap of a PE file.
    /// </summary>
    public class UserStringsHeap
    {
        /// <summary>
        /// Origin of the #US heap.
        /// </summary>
        internal MapPtr Base;

        /// <summary>
        /// Return the pointer to the beginning of the blob heap.
        /// The pointer is restricted to the Blob area.
        /// </summary>
        public MapPtr Begin
        {
            get { return Base; }
        }

        /// <summary>
        /// Constructor of the wrapper.
        /// </summary>
        /// <param name="p">Pointer to the blob heap</param>
        internal UserStringsHeap(MapPtr p)
        {
            Base = p;
        }

        /// <summary>
        /// Calculate the size of a blob and return an appropriate pointer.
        /// </summary>
        /// <param name="p">Pointer to the beginning of the blob.</param>
        /// <returns>The pointer to the data limited to the appropriate
        /// size.</returns>
        static private MapPtr GetPtr(MapPtr p)
        {
            if ((p[0] & 0x80) == 0)
                return new MapPtr(p + 1, p[0]);

            if ((p[0] & 0x40) == 0)
                return new MapPtr(p + 2, (p[0] & ~0x80) << 8 | p[1]);

            return new MapPtr(p + 4, (
              p[0] & ~0xC0) << 24 | p[1] << 16 | p[2] << 8 | p[3]);
        }

        /// <summary>
        /// Return the pointer to the Blob heap given the byte offset.
        /// </summary>
        public string this[int idx]
        {
            get
            {
                MapPtr p = GetPtr(Base + idx);
                System.Text.StringBuilder sb = new System.Text.StringBuilder((int)p.Length / 2);
                while (p.Length > 1)
                {
                    sb.Append((char)p);
                    p += 2;
                }
                return sb.ToString();
            }
        }
    }

    /// <summary>
    /// Wrapper class to the GUID heap.
    /// </summary>
    public class GuidHeap
    {
        /// <summary>
        /// Origin of the table.
        /// </summary>
        internal MapPtr Base;
        /// <summary>
        /// True if the size of the table is greater than 65536.
        /// </summary>
        internal bool Large;
        /// <summary>
        /// Size, in bytes, of the index to the table.
        /// </summary>
        internal int IndexSize;

        /// <summary>
        /// Number of rows within the Guid heap.
        /// </summary>
        internal int Size
        {
            get { return (int)(Base.Length / 16); }
        }

        /// <summary>
        /// Beginning of the table. The pointer is constrained to the memory
        /// occupied by the table.
        /// </summary>
        public MapPtr Begin
        {
            get { return Base; }
        }

        /// <summary>
        /// Constructor of the wrapper.
        /// TODO: Check if large is measured in bytes or in GUIDs...
        /// </summary>
        /// <param name="p">Pointer to the beginning of the table</param>
        /// <param name="large">True if the size is greater than 65536</param>
        internal GuidHeap(MapPtr p, bool large)
        {
            Base = p;
            Large = large;
            IndexSize = large ? 4 : 2;
        }

        /// <summary>
        /// Access the Guid heap.
        /// TODO: Check the byte order in the GUID...
        /// </summary>
        /// <param name="idx">Pointer to the coded index to the heap</param>
        /// <returns>The Guid read.</returns>
        public Guid Access(MapPtr idx)
        {
            int id = Large ? (int)idx : (short)idx;
            MapPtr b = Base + (id * 16);
            return new Guid((int)b, (short)(b + 4), (short)(b + 6),
              b[8], b[9], b[10], b[11], b[12], b[13], b[14], b[15]);
        }
    }

    /// <summary>
    /// This class describes a method body
    /// </summary>
    public class MethodBody
    {
        /// <summary>
        /// Header of the method body.
        /// </summary>
        private MapPtr Base;

        /// <summary>
        /// Pointer to the IL byte stream.
        /// </summary>
        private MapPtr Body;

        /// <summary>
        /// File to which the method belongs.
        /// </summary>
        private CLIFile File;

        /// <summary>
        /// Index of the method in the method table.
        /// </summary>
        private int MethodIndex;

        /// <summary>
        /// Return the max stack property of the method.
        /// </summary>
        public int MaxStack
        {
            get { return (Base[0] & 0x01) == 0 ? 8 : (int)(short)(Base + 2); }
        }

        /// <summary>
        /// True if the body has more sections (Exception handling's).
        /// </summary>
        public bool MoreSections
        {
            get { return (Base[0] & 0x01) == 0 ? false : ((Base[0] & 0x08) != 0); }
        }

        /// <summary>
        /// True if locals must be initialized with the default constructor.
        /// </summary>
        public bool InitLocals
        {
            get { return (Base[0] & 0x01) == 0 ? false : ((Base[0] & 0x10) != 0); }
        }

        /// <summary>
        /// Token to the StandAloneSig table with the Locals signature.
        /// </summary>
        public int LocalsSig
        {
            get
            {
                return (Base[0] & 0x01) == 0 ? 0 :
                  (File[TableNames.StandAloneSig].LargeIndexes ? (int)(Base + 8) :
                  (int)(short)(Base + 8));
            }
        }

        /// <summary>
        /// Return an object able to access the locals signature in the heap.
        /// </summary>
        public LocalsVarSig LocalsSignature
        {
            get
            {
                int loc = this.LocalsSig;
                if (loc == 0) return null;
                StandAloneSigTableCursor cur =
                  File[TableNames.StandAloneSig].GetCursor() as StandAloneSigTableCursor;
                cur.Goto(loc);
                return new LocalsVarSig(File, File.Blob[cur.Signature]);
            }
        }

        /// <summary>
        /// Return a cursor to the stream of IL instructions.
        /// </summary>
        public ILCursor ILInstructions
        {
            get { return MapPtrILCursor.CreateILCursor(this.MethodIndex, this.Body, this.File); }
        }

        /// <summary>
        /// Pointer to SEHSection which follows the Body.
        /// </summary>
        /// DL 2002-08-10
        private MapPtr SEHSection;

        ///<summary>
        ///Provides cursor into the EH (Exception Handling) clauses that
        ///follow the IL instructions of the body.
        ///<summary>
        ///DL 2002-08-10
        ///
        public SEHTableCursor SEHTableCursor
        {
            get
            {
                return ((Base[0] & 0x01) == 0 ? null :
                  new SEHTableCursor(this.SEHSection));
            }
        }

        /// <summary>
        /// Build a MethodBody object.
        /// </summary>
        /// <param name="idx">Method index in the method table</param>
        /// <param name="b">Location of the method body.</param>
        /// <param name="f">CLIFileReader associated with this MethodBody</param>
        internal MethodBody(int idx, MapPtr b, CLIFile f)
        {
            this.MethodIndex = idx;
            this.File = f;
            if ((b[0] & 0x01) != 0)
            { // Fat format
                Base = new MapPtr(b, ((((short)b) >> 12) & 0x0F) * 4);
                Body = new MapPtr(b + ((((short)b) >> 12) & 0x0F) * 4, (int)(b + 4));
            }
            else
            { // Tiny format
                Base = new MapPtr(b, 1);
                Body = new MapPtr(b + 1, b[0] >> 2);
            }
            /// DL 2002-08-10
            /// If there are additional sections, we should make a MapPtr
            /// that references them.
            if ((Base[0] & 0x01) == 0 ? false : ((Base[0] & 0x08) != 0))
            {
                /// Fat Method Body headers are 4 bytes aligned, so we use it as

                /// our starting point for calculating the SEHSection's location.
                MapPtr sectionStart = b;
                long endOfIL = (this.Base.Length + this.Body.Length);

                /// SEH Table Sections are start on DWORD boundaries.
                /// We have to calculate the point after the end of
                /// the IL stream that the SEHSection begins.
                ///
                int align = 0;
                // If its zero, we are aligned, if not, we need to add 4-modulus
                if (endOfIL % 4 != 0)
                {
                    align = 4 - (int)(endOfIL % 4);
                }
                sectionStart += (align + (int)this.Body.Length + (int)this.Base.Length);
                SEHSection = sectionStart;
            }
        }
    }

    /// <summary>
    /// Instances of this class indicate a place into a method stream.
    /// </summary>
    public class Target : IComparable
    {
        /// <summary>
        /// Offset into the CIL byte stream
        /// </summary>
        public long Position;
        /// <summary>
        /// Build an instance of Target.
        /// </summary>
        /// <param name="p">Offset into CIL byte stream</param>
        public Target(long p)
        {
            Position = p;
        }
        /// <summary>
        /// Compare two instance of Target class. This is a method needed to
        /// implement IComparable.
        /// </summary>
        /// <param name="o">Object to compare to this instance</param>
        /// <returns>An integer less than 0 if this &lt; o; 0 if they are equal; a
        /// positive value otherwise</returns>
        public int CompareTo(object o)
        {
            Target t = o as Target;
            if (t == null) throw new ArgumentException("Wrong type in comparison");
            return (int)(this.Position - t.Position);
        }
        /// <summary>
        /// Generate a name for the current target.
        /// </summary>
        /// <returns>A string representing the target.</returns>
        public override string ToString()
        {
            return string.Format("IL_{0:X4}", Position);
        }
    }

    /// <summary>
    /// This type value represents an IL Instruction. To reduce any overhead
    /// the instruction is represented with unchanged parameters. Other layers
    /// may process parameters to obtain the desired information.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{op}")]
    public struct ILInstruction
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct Parameter
        {
            [FieldOffset(0)]
            public byte bv;
            [FieldOffset(0)]
            public sbyte sbv;
            [FieldOffset(0)]
            public short sv;
            [FieldOffset(0)]
            public int iv;
            [FieldOffset(0)]
            public float fv;
            [FieldOffset(0)]
            public long lv;
            [FieldOffset(0)]
            public double dv;

            internal void Read(OpCode op, ILCursor.ILStoreCursor cur)
            {
                switch (op.OperandType)
                {
                    case OperandType.InlineBrTarget:
                    case OperandType.InlineField:
                    case OperandType.InlineI:
                    case OperandType.InlineMethod:
                    case OperandType.InlineSig:
                    case OperandType.InlineString:
                    case OperandType.InlineSwitch:
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                        iv = cur.ToInt32();
                        cur.Advance(4);
                        break;
                    case OperandType.InlineI8:
                        lv = cur.ToInt64();
                        cur.Advance(8);
                        break;
                    case OperandType.InlineR:
                        dv = cur.ToDouble();
                        cur.Advance(8);
                        break;
                    case OperandType.InlineVar:
                        sv = cur.ToShort();
                        cur.Advance(2);
                        break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar:
                        bv = cur.ToByte();
                        cur.Advance(1);
                        break;
                    case OperandType.ShortInlineR:
                        fv = cur.ToFloat();
                        cur.Advance(4);
                        break;
                }
            }
        }
        public OpCode op;
        public Parameter par;
        public int[] sw;
        internal ILCursor cur;

        /// <summary>
        /// Resolve the parameter of the instruction.
        /// </summary>
        /// <returns>The resolved parameter</returns>
        public object ResolveParameter(CLIFile f)
        {
            switch (op.OperandType)
            {
                // This should be checked by outside code. The returned value is
                // meaningless.
                case OperandType.InlineBrTarget:
                    {
                        if (cur != null)
                        {
                            // FIXME: this could be improved using a binary search.
                            foreach (Target t in this.cur.Labels)
                            {
                                // (op.Size + 4) because 0 the offset is from the next instruction
                                int off = par.iv;
                                if (t.Position == this.cur.Position + off + op.Size + 4)
                                    return t;
                            }
                        }
                        return (object)par.iv;
                    }
                case OperandType.InlineSwitch:
                    {
                        // Look for all targets in a switch and label everything
                        if (cur != null)
                        {
                            object[] ret = new object[par.iv];
                            for (int i = 0; i < par.iv; i++)
                            {
                                ret[i] = null;
                                foreach (Target t in this.cur.Labels)
                                {
                                    // (op.Size + 4) because 0 the offset is from the next instruction
                                    int off = this.sw[i];
                                    if (t.Position == this.cur.Position + off + op.Size + 4 + par.iv * 4)
                                        ret[i] = t;
                                }
                                if (ret[i] == null)
                                    ret[i] = this.sw[i];
                            }
                            return ret;
                        }
                        return this.sw;
                    }
                case OperandType.InlineField:
                    {

                        object ret = (new FieldDesc(f, par.iv));
                        Debug.Assert(ret != null, "Error finding the field!");
                        return ret;
                    }
                case OperandType.InlineI:
                    return (object)par.iv;
                case OperandType.InlineI8:
                    return (object)par;
                case OperandType.InlineMethod:
                    {
                        object ret = (new MethodDesc(f, par.iv));
                        Debug.Assert(ret != null, "Error finding the method!");
                        return ret;
                    }
                case OperandType.InlineNone:
                    return null;
                case OperandType.InlinePhi:
                    throw new Exception("Obsolete argument: parameter InlinePhi has been deprecated");
                case OperandType.InlineR:
                    unsafe { fixed (long* l = &par.lv) { return *((double*)l); } }
                case OperandType.InlineSig:
                    {
                        Debug.Assert(((TableNames)(par.iv >> 24)) == TableNames.StandAloneSig, "Must be a standalone signature!");
                        StandAloneSigTableCursor cur = f[TableNames.StandAloneSig].GetCursor() as StandAloneSigTableCursor;
                        cur.Goto(par.iv & 0x00FFFFFF);
                        return new MethodSig(f, f.Blob[cur.Signature], MethodSig.SigType.StandaloneSig);
                    }
                case OperandType.InlineString:
                    {
                        System.Diagnostics.Debug.Assert(par.iv >> 24 == 0x70, "Wrong token in ldstr");
                        return f.UserStrings[par.iv & 0x00ffffff];
                    }
                case OperandType.InlineTok:
                    {
                        TableNames table = (TableNames)(par.iv >> 24);
                        switch (table)
                        {
                            case TableNames.TypeDef:
                            case TableNames.TypeRef:
                            case TableNames.TypeSpec:
                                return new CLIType(f, CompoundType.FromFToken(f, par.iv, null));
                            case TableNames.MemberRef:
                                if (FieldDesc.IsFieldFToken(f, par.iv))
                                    goto FIELD;
                                else
                                    goto METHOD;
                            case TableNames.Method:
                            METHOD:
                                return new MethodDesc(f, par.iv);
                            case TableNames.Field:
                            FIELD:
                                return new FieldDesc(f, par.iv);
                        }
                        throw new Exception("Internal error");
                    }
                case OperandType.InlineType:
                    {
                        return new CLIType(f, CompoundType.FromFToken(f, par.iv, null));
                    }
                case OperandType.InlineVar:
                    return (object)par.sv;
                // This should be checked by outside code. The returned value is
                // meaningless.
                case OperandType.ShortInlineBrTarget:
                    {
                        if (cur != null)
                        {
                            // FIXME: this could be improved using a binary search.
                            foreach (Target t in this.cur.Labels)
                            {
                                // (op.Size + 1) because 0 the offset is from the next instruction
                                int off = par.sbv;
                                if (t.Position == this.cur.Position + off + op.Size + 1)
                                    return t;
                            }
                        }
                        return (object)par.sbv;
                    }
                case OperandType.ShortInlineI:
                    return (object)par.sbv;
                case OperandType.ShortInlineR:
                    unsafe { fixed (long* l = &par.lv) { return *((float*)l); } }
                case OperandType.ShortInlineVar:
                    return (object)par.bv;
            }
            return null;
        }

        internal static void Normalize(ref OpCode op, ref Parameter v)
        {
            switch ((OPCODES)(op.Value < 0 ? 256 + (op.Value & 0xFF) : op.Value))
            {
                case OPCODES.Ldc_I4_M1:
                    v.iv = -1; goto LDC_I4;
                case OPCODES.Ldc_I4_0:
                    v.iv = 0; goto LDC_I4;
                case OPCODES.Ldc_I4_1:
                    v.iv = 1; goto LDC_I4;
                case OPCODES.Ldc_I4_2:
                    v.iv = 2; goto LDC_I4;
                case OPCODES.Ldc_I4_3:
                    v.iv = 3; goto LDC_I4;
                case OPCODES.Ldc_I4_4:
                    v.iv = 4; goto LDC_I4;
                case OPCODES.Ldc_I4_5:
                    v.iv = 5; goto LDC_I4;
                case OPCODES.Ldc_I4_6:
                    v.iv = 6; goto LDC_I4;
                case OPCODES.Ldc_I4_7:
                    v.iv = 7; goto LDC_I4;
                case OPCODES.Ldc_I4_8:
                    v.iv = 8; goto LDC_I4;
                case OPCODES.Ldc_I4_S:
                    v.iv = v.sv; goto LDC_I4;
                case OPCODES.Ldc_I4:
                LDC_I4:
                    op = System.Reflection.Emit.OpCodes.Ldc_I4;
                return;
                case OPCODES.Stloc_0:
                v.iv = 0; goto STLOC;
                case OPCODES.Stloc_1:
                    v.iv = 1; goto STLOC;
                case OPCODES.Stloc_2:
                    v.iv = 2; goto STLOC;
                case OPCODES.Stloc_3:
                    v.iv = 3; goto STLOC;
                case OPCODES.Stloc_S:
                    v.iv = v.sv; goto STLOC;
                case OPCODES.Stloc:
                STLOC:
                    op = System.Reflection.Emit.OpCodes.Stloc;
                return;
                case OPCODES.Ldarg_0:
                v.iv = 0; goto LDARG;
                case OPCODES.Ldarg_1:
                    v.iv = 1; goto LDARG;
                case OPCODES.Ldarg_2:
                    v.iv = 2; goto LDARG;
                case OPCODES.Ldarg_3:
                    v.iv = 3; goto LDARG;
                case OPCODES.Ldarg_S:
                    v.iv = v.sv; goto LDARG;
                case OPCODES.Ldarg:
                LDARG:
                    op = System.Reflection.Emit.OpCodes.Ldarg;
                return;
                case OPCODES.Ldarga_S:
                v.iv = v.sv; goto LDARGA;
                case OPCODES.Ldarga:
                LDARGA:
                    op = System.Reflection.Emit.OpCodes.Ldarga;
                return;
                case OPCODES.Ldloc_0:
                v.iv = 0; goto LDLOC;
                case OPCODES.Ldloc_1:
                    v.iv = 1; goto LDLOC;
                case OPCODES.Ldloc_2:
                    v.iv = 2; goto LDLOC;
                case OPCODES.Ldloc_3:
                    v.iv = 3; goto LDLOC;
                case OPCODES.Ldloc_S:
                    v.iv = v.bv; goto LDLOC;
                case OPCODES.Ldloc:
                    v.iv = v.sv;
                LDLOC:
                    op = System.Reflection.Emit.OpCodes.Ldloc;
                return;
                case OPCODES.Ldloca_S:
                v.iv = v.bv; goto LDLOCA;
                case OPCODES.Ldloca:
                    v.iv = v.sv;
                LDLOCA:
                    op = System.Reflection.Emit.OpCodes.Ldloca;
                return;

                case OPCODES.Bge_S:
                case OPCODES.Bge_Un_S:
                v.iv = v.sbv - 3; goto BGE;
                case OPCODES.Bge_Un:
                case OPCODES.Bge:
                BGE:
                    op = System.Reflection.Emit.OpCodes.Bge;
                return;

                case OPCODES.Bgt_S:
                case OPCODES.Bgt_Un_S:
                v.iv = v.sbv - 3; goto BGT;
                case OPCODES.Bgt_Un:
                case OPCODES.Bgt:
                BGT:
                    op = System.Reflection.Emit.OpCodes.Bgt;
                return;

                case OPCODES.Ble_S:
                case OPCODES.Ble_Un_S:
                v.iv = v.sbv - 3; goto BLE;
                case OPCODES.Ble_Un:
                case OPCODES.Ble:
                BLE:
                    op = System.Reflection.Emit.OpCodes.Ble;
                return;

                case OPCODES.Blt_S:
                case OPCODES.Blt_Un_S:
                v.iv = v.sbv - 3; goto BLT;
                case OPCODES.Blt_Un:
                case OPCODES.Blt:
                BLT:
                    op = System.Reflection.Emit.OpCodes.Blt;
                return;

                case OPCODES.Bne_Un_S:
                v.iv = v.sbv - 3; goto BNE;
                case OPCODES.Bne_Un:
                BNE:
                    op = System.Reflection.Emit.OpCodes.Bne_Un;
                return;

                case OPCODES.Beq_S:
                v.iv = v.sbv - 3; goto BEQ;
                case OPCODES.Beq:
                BEQ:
                    op = System.Reflection.Emit.OpCodes.Beq;
                return;

                case OPCODES.Brtrue_S:
                v.iv = v.sbv - 3; goto BRTRUE;
                case OPCODES.Brtrue:
                BRTRUE:
                    op = System.Reflection.Emit.OpCodes.Brtrue;
                return;

                case OPCODES.Brfalse_S:
                v.iv = v.sbv - 3; goto BRFALSE;
                case OPCODES.Brfalse:
                BRFALSE:
                    op = System.Reflection.Emit.OpCodes.Brfalse;
                return;

                case OPCODES.Br_S:
                v.iv = v.sbv - 3; goto BR;
                case OPCODES.Br:
                BR:
                    op = System.Reflection.Emit.OpCodes.Br;
                return;
            }
        }

        /// <summary>
        /// Normalize the instruction (to simplify analisys).
        /// </summary>
        /// <param name="il">Intruction to normalize</param>
        /// <returns>Normalized (uncompressed) instruction</returns>
        public static ILInstruction Normalize(ILInstruction il)
        {
            Normalize(ref il.op, ref il.par);

            return il;
        }

        /// <summary>
        /// Useful instruction to write an instruction into an ILGenerator.
        /// </summary>
        /// <param name="ilg">Generator to write into</param>
        /// <param name="compress">True if the instruction should be compressed,
        /// if possible</param>
        //public void WriteInstruction(ILGenerator ilg, bool compress)
        public void WriteInstruction(ILGenerator ilg)
        {	//c.Instr -> this
            //switch (this.op.OperandType)
            //{
            //    case OperandType.InlineBrTarget:
            //    case OperandType.InlineI:
            //        ilg.Emit(this.op, this.par.iv);
            //        break;
            //    case OperandType.InlineField:
            //        //ilg.Emit(this.op, (FieldInfo)par);
            //        ilg.Emit(this.op, this.par.bv);
            //        break;
            //    case OperandType.InlineI8:
            //        ilg.Emit(this.op, this.par.lv);
            //        break;
            //    case OperandType.InlineMethod:
            //        //ilg.Emit(this.op, (MethodInfo)par);
            //        ilg.Emit(this.op, this.par.bv);
            //        break;
            //    case OperandType.InlineNone:
            //        ilg.Emit(this.op);
            //        break;
            //    case OperandType.InlinePhi:
            //        throw new Exception("Unsupported");
            //    case OperandType.InlineR:
            //        ilg.Emit(this.op, this.par.dv);
            //        break;
            //    case OperandType.InlineSig:
            //        throw new Exception("Unsupported");
            //    case OperandType.InlineString:
            //        //ilg.Emit(this.op, (string)par);
            //        ilg.Emit(this.op, this.par.bv.ToString());
            //        break;
            //    case OperandType.InlineSwitch:
            //        {
            //            Label[] lab = new Label[this.par.iv];
            //            for (int i = 0; i < lab.Length; i++)
            //                lab[i] = (Label)lab[this.sw[i]];
            //            ilg.Emit(this.op, lab);
            //            break;
            //        }
            //    //case OperandType.InlineTok:
            //    //    if (this.par.bv is Type) ilg.Emit(this.op, (Type)this.par.);
            //    //    else if (this.par.bv is MethodInfo) ilg.Emit(this.op, (MethodInfo)this.par.bv);
            //    //    else if (this.par.bv is FieldInfo) ilg.Emit(this.op, (FieldInfo)this.par.bv);
            //    //    else throw new Exception("Unsupported");
            //    //    break;
            //    //case OperandType.InlineType:
            //    //    ilg.Emit(this.op, (Type)par);
            //    //    break;
            //    case OperandType.InlineVar:
            //        ilg.Emit(this.op, this.par.sv);
            //        break;
            //    case OperandType.ShortInlineBrTarget:
            //        ilg.Emit(this.op, this.par.sbv);
            //        break;
            //    case OperandType.ShortInlineI:
            //        ilg.Emit(this.op, this.par.sbv);
            //        break;
            //    case OperandType.ShortInlineR:
            //        ilg.Emit(this.op, this.par.fv);
            //        break;
            //    case OperandType.ShortInlineVar:
            //        ilg.Emit(this.op, this.par.bv);
            //        break;
            //}
        }
    }

    /// <summary>
    /// This class contains help functions for signatures.
    /// </summary>
    public class SignatureUtil
    {
        /// <summary>
        /// Read a compressed integer from a signature.
        /// </summary>
        /// <param name="p">Memory to read from</param>
        /// <param name="sz">Number of byte read</param>
        /// <returns>The uncompressed int. -1 represents the special value 0xFF 
        /// to be used for null in strings</returns>
        internal static int ReadCompressedInt(MapPtr p, out int sz)
        {
            if (p[0] == 0xFF)
            {
                sz = 1;
                return -1;
            }

            if ((p[0] & 0x80) == 0)
            {
                sz = 1;
                return p[0];
            }

            if ((p[0] & 0x40) == 0)
            {
                sz = 2;
                return (p[0] & ~0x80) << 8 | p[1];
            }

            sz = 4;
            return (p[0] & ~0xC0) << 24 | p[1] << 16 | p[2] << 8 | p[3];
        }

        /// <summary>
        /// Read a compressed integer from a signature. The input pointer is 
        /// modified.
        /// </summary>
        /// <param name="p">Memory to read from</param>
        /// <returns>The uncompressed int. -1 represents the special value 0xFF 
        /// to be used for null in strings</returns>
        internal static int ReadCompressedInt(ref MapPtr p)
        {
            int sz;
            int ret = ReadCompressedInt(p, out sz);
            p += sz;
            return ret;
        }

        /// <summary>
        /// Find a type given the type def or ref.
        /// </summary>
        /// <param name="typedeforref">Type def or ref read in the signature.</param>
        /// <param name="f">File containing the data</param>
        /// <param name="byRef">If the type is passed by ref</param>
        /// <returns>The requested type or null</returns>
        internal static Type FindType(int typedeforref, CLIFile f)
        {
            TypeDefOrRefTag table = (TypeDefOrRefTag)(typedeforref & 0x03);
            int token = typedeforref >> 2;
            Type ret = null;
            switch (table)
            {
                case TypeDefOrRefTag.TypeDef:
                    {
                        ret = f.Assembly.ManifestModule.ResolveType((((int)TableNames.TypeDef) << 24) | token);
                        break;
                    }
                case TypeDefOrRefTag.TypeRef:
                    {
                        ret = f.Assembly.ManifestModule.ResolveType((((int)TableNames.TypeRef) << 24) | token);
                        break;
                    }
                case TypeDefOrRefTag.TypeSpec:
                    {
                        ret = f.Assembly.ManifestModule.ResolveType((((int)TableNames.TypeSpec) << 24) | token);
                        break;
                    }
                default:
                    throw new Exception("Internal error in type def or ref!");
            }
            return ret;
        }
    }

    /// <summary>
    /// This class is an helper for reading a field token.
    /// </summary>
    public class FieldDesc
    {
        private CLIFile file;
        private int token;

        public static bool IsFieldFToken(CLIFile f, int tk)
        {
            TableNames t = ((TableNames)(tk >> 24));
            if (t == TableNames.Field)
                return true;
            if (t == TableNames.MemberRef)
            {
                MemberRefTableCursor cur = f[TableNames.MemberRef].GetCursor() as MemberRefTableCursor;
                cur.Goto(tk & 0x00FFFFFF);
                return ((f.Blob[cur.Signature])[0] == 0x06);
            }
            return false;
        }

        internal FieldDesc(CLIFile f, int t)
        {
            this.file = f;
            this.token = t;
        }

        public TableNames Kind
        {
            get { return (TableNames)(token >> 24); }
        }

        public int Row
        {
            get { return token & 0x00FFFFFF; }
        }

        public CLIType Type {
            get
            {
                switch (this.Kind)
                {
                    case TableNames.Field:
                        {
                            FieldTableCursor cur = this.file[TableNames.Field].GetCursor() as FieldTableCursor;
                            cur.Goto(this.Row);
                            return FieldSig.Read(this.file, this.file.Blob[cur.Signature]);
                        }
                    case TableNames.MemberRef:
                        {
                            MemberRefTableCursor cur = this.file[TableNames.MemberRef].GetCursor() as MemberRefTableCursor;
                            cur.Goto(this.Row);
                            return FieldSig.Read(this.file, this.file.Blob[cur.Signature]);
                        }
                }
                return null;
            }
        }

        public FieldInfo GetReflectionField()
        {
            switch (this.Kind)
            {
                case TableNames.Field:
                    {
                        FieldTableCursor cur = this.file[TableNames.Field].GetCursor() as FieldTableCursor;
                        cur.Goto(this.Row);
                        return cur.GetReflectionField();
                    }
                case TableNames.MemberRef:
                    {
                        MemberRefTableCursor cur = this.file[TableNames.MemberRef].GetCursor() as MemberRefTableCursor;
                        cur.Goto(this.Row);
                        return cur.GetReflectionField();
                    }
            }
            return null;
        }
    }

    /// <summary>
    /// This class is an helper for reading a method token
    /// in call instructions.
    /// </summary>
    public class MethodDesc
    {
        private CLIFile file;
        private int token;

        public static bool IsMethodFToken(CLIFile f, int tk)
        {
            TableNames t = ((TableNames)(tk >> 24));
            if (t == TableNames.Method || t == TableNames.MethodSpec)
                return true;
            if (t == TableNames.MemberRef)
            {
                MemberRefTableCursor cur = f[TableNames.MemberRef].GetCursor() as MemberRefTableCursor;
                cur.Goto(tk & 0x00FFFFFF);
                return ((f.Blob[cur.Signature])[0] != 0x06);
            }
            return false;
        }

        public TableNames Kind
        {
            get
            {
                return (TableNames)(token >> 24);
            }
        }

        public int Row
        {
            get
            {
                return token & 0x00FFFFFF;
            }
        }

        public CLIType GetParent()
        {
            int row = this.Row;
            switch (this.Kind)
            {
                case TableNames.Method:
                    {
                        TypeDefTableCursor cur = this.file[TableNames.TypeDef].GetCursor() as TypeDefTableCursor;
                        int type = 0;
                        while (cur.Next() && cur.MethodList < row) type = cur.Position;
                        return new CLIType(this.file, new CompoundType(this.file, (type << 1) | ((int)TypeDefOrRefTag.TypeDef))); 
                    }
                case TableNames.MethodSpec:
                    {
                        MethodSpecTableCursor cur = this.file[TableNames.MethodSpec].GetCursor() as MethodSpecTableCursor;
                        cur.Goto(this.Row);
                        switch ((MethodDefOrRefTag)(cur.Method & 0x1))
                        {
                            case MethodDefOrRefTag.MemberRef:
                                {
                                    MemberRefTableCursor cur1 = this.file[TableNames.MemberRef].GetCursor() as MemberRefTableCursor;
                                    cur1.Goto(cur.Method >> 1);
                                    switch ((MemberRefParentTag)(cur1.Class & 0x3))
                                    {
                                        case MemberRefParentTag.MethodDef:
                                        case MemberRefParentTag.ModuleRef:
                                            return null;
                                        case MemberRefParentTag.TypeRef:
                                            return new CLIType(this.file, new CompoundType(this.file, (cur1.Class & ~3) | ((int)TypeDefOrRefTag.TypeRef)));
                                        case MemberRefParentTag.TypeSpec:
                                            {
                                                TypeSpecTableCursor cur2 = this.file[TableNames.TypeSpec].GetCursor() as TypeSpecTableCursor;
                                                cur1.Goto(cur1.Class >> 2);
                                                return TypeSpecSig.Read(this.file, this.file.Blob[cur2.Signature]);
                                            }
                                    }
                                    return null;
                                }
                            case MethodDefOrRefTag.MethodDef:
                                {
                                    row = cur.Method >> 1;
                                    TypeDefTableCursor cur1 = this.file[TableNames.TypeDef].GetCursor() as TypeDefTableCursor;
                                    int type = 0;
                                    while (cur1.Next() && cur1.MethodList < row) type = cur1.Position;
                                    return new CLIType(this.file, new CompoundType(this.file, (type << 1) | ((int)TypeDefOrRefTag.TypeDef)));
                                }
                        }
                        return null;
                    }
                case TableNames.MemberRef:
                    {
                        MemberRefTableCursor cur = this.file[TableNames.MemberRef].GetCursor() as MemberRefTableCursor;
                        cur.Goto(this.Row);
                        switch ((MemberRefParentTag)(cur.Class & 0x3))
                        {
                            case MemberRefParentTag.MethodDef:
                            case MemberRefParentTag.ModuleRef:
                                return null;
                            case MemberRefParentTag.TypeRef:
                                return new CLIType(this.file, new CompoundType(this.file, (cur.Class & ~3) | ((int)TypeDefOrRefTag.TypeRef)));
                            case MemberRefParentTag.TypeSpec:
                                {
                                    TypeSpecTableCursor cur1 = this.file[TableNames.TypeSpec].GetCursor() as TypeSpecTableCursor;
                                    cur1.Goto(cur.Class >> 2);
                                    return TypeSpecSig.Read(this.file, this.file.Blob[cur1.Signature]);
                                }
                        }
                        return null;
                    }
            }
            return null;
        }

        public MethodSig Signature
        {
            get
            {
                switch (this.Kind)
                {
                    case TableNames.Method:
                        {
                            MethodTableCursor cur = this.file[TableNames.Method].GetCursor() as MethodTableCursor;
                            cur.Goto(this.Row);
                            return cur.GetMethodSignature();
                        }
                    case TableNames.MethodSpec:
                        {
                            MethodSpecTableCursor cur = this.file[TableNames.MethodSpec].GetCursor() as MethodSpecTableCursor;
                            cur.Goto(this.Row);
                            switch ((MethodDefOrRefTag)(cur.Method & 0x1))
                            {
                                case MethodDefOrRefTag.MemberRef:
                                    {
                                        MemberRefTableCursor cur1 = this.file[TableNames.MemberRef].GetCursor() as MemberRefTableCursor;
                                        cur1.Goto(cur.Method >> 1);
                                        return new MethodSig(this.file, this.file.Blob[cur1.Signature], MethodSig.SigType.MethodRefSig);
                                    }
                                case MethodDefOrRefTag.MethodDef:
                                    {
                                        MethodTableCursor cur1 = this.file[TableNames.Method].GetCursor() as MethodTableCursor;
                                        cur1.Goto(cur.Method >> 1);
                                        return cur1.GetMethodSignature();
                                    }
                            }
                            return null;
                        }
                    case TableNames.MemberRef:
                        {
                            MemberRefTableCursor cur = this.file[TableNames.MemberRef].GetCursor() as MemberRefTableCursor;
                            cur.Goto(this.Row);
                            return new MethodSig(this.file, this.file.Blob[cur.Signature], MethodSig.SigType.MethodRefSig);
                        }
                }
                return null;
            }
        }

        public string Name
        {
            get
            {
                switch (this.Kind)
                {
                    case TableNames.Method:
                        {
                            MethodTableCursor cur = this.file[TableNames.Method].GetCursor() as MethodTableCursor;
                            cur.Goto(this.Row);
                            return this.file.Strings[cur.Name];
                        }
                    case TableNames.MethodSpec:
                        {
                            MethodSpecTableCursor cur = this.file[TableNames.MethodSpec].GetCursor() as MethodSpecTableCursor;
                            cur.Goto(this.Row);
                            switch ((MethodDefOrRefTag)(cur.Method & 0x1))
                            {
                                case MethodDefOrRefTag.MemberRef:
                                    {
                                        MemberRefTableCursor cur1 = this.file[TableNames.MemberRef].GetCursor() as MemberRefTableCursor;
                                        cur1.Goto(cur.Method >> 1);
                                        return this.file.Strings[cur1.Name];
                                    }
                                case MethodDefOrRefTag.MethodDef:
                                    {
                                        MethodTableCursor cur1 = this.file[TableNames.Method].GetCursor() as MethodTableCursor;
                                        cur1.Goto(cur.Method >> 1);
                                        return this.file.Strings[cur1.Name];
                                    }
                            }
                            return null;
                        }
                    case TableNames.MemberRef:
                        {
                            MemberRefTableCursor cur = this.file[TableNames.MemberRef].GetCursor() as MemberRefTableCursor;
                            cur.Goto(this.Row);
                            return this.file.Strings[cur.Name];
                        }
                }
                return null;
            }
        }

        public MethodBody Body
        {
            get
            {
                switch (this.Kind)
                {
                    case TableNames.Method:
                        {
                            MethodTableCursor cur = this.file[TableNames.Method].GetCursor() as MethodTableCursor;
                            cur.Goto(this.Row);
                            return cur.MethodBody;
                        }
                    case TableNames.MethodSpec:
                        {
                            MethodSpecTableCursor cur = this.file[TableNames.MethodSpec].GetCursor() as MethodSpecTableCursor;
                            cur.Goto(this.Row);
                            switch ((MethodDefOrRefTag)(cur.Method & 0x1))
                            {
                                case MethodDefOrRefTag.MethodDef:
                                    {
                                        MethodTableCursor cur1 = this.file[TableNames.Method].GetCursor() as MethodTableCursor;
                                        cur1.Goto(cur.Method >> 1);
                                        return cur1.MethodBody;
                                    }
                            }
                            return null;
                        }
                }
                return null;
            }
        }

        public MethodBase GetReflectionMethod(Type[] typeinst)
        {
            switch (this.Kind)
            {
                case TableNames.Method:
                    {
                        MethodTableCursor cur = this.file[TableNames.Method].GetCursor() as MethodTableCursor;
                        cur.Goto(this.Row);
                        return cur.GetReflectionMethod();
                    }
                case TableNames.MethodSpec:
                    {
                        MethodSpecTableCursor cur = this.file[TableNames.MethodSpec].GetCursor() as MethodSpecTableCursor;
                        cur.Goto(this.Row);
                        MethodBase m = null;
                        switch ((MethodDefOrRefTag)(cur.Method & 0x1))
                        {
                            case MethodDefOrRefTag.MemberRef:
                                {
                                    MemberRefTableCursor cur1 = this.file[TableNames.MemberRef].GetCursor() as MemberRefTableCursor;
                                    cur1.Goto(cur.Method >> 1);
                                    m = cur1.GetReflectionMethod();
                                    break;
                                }
                            case MethodDefOrRefTag.MethodDef:
                                {
                                    MethodTableCursor cur1 = this.file[TableNames.Method].GetCursor() as MethodTableCursor;
                                    cur1.Goto(cur.Method >> 1);
                                    m = cur1.GetReflectionMethod();
                                    break;
                                }
                        }
                        CLIType[] inst = MethodSpecSig.Read(this.file, file.Blob[cur.Instantiation]);
                        Type[] pars = Array.ConvertAll<CLIType, Type>(inst, delegate(CLIType a) { return a.GetReflectionType(typeinst); });
                        
                        return ((MethodInfo)m).MakeGenericMethod(pars);
                    }
                case TableNames.MemberRef:
                    {
                        MemberRefTableCursor cur = this.file[TableNames.MemberRef].GetCursor() as MemberRefTableCursor;
                        cur.Goto(this.Row);
                        return cur.GetReflectionMethod();
                    }
            }
            return null;
        }

        internal MethodDesc(CLIFile f, int calltk)
        {
            this.file = f;
            this.token = calltk;
        }
    }

    /// <summary>
    /// This class helps reading Method signatures.
    /// The class has been designed just to access the ByRef value so CustomMod
    /// are skipped. Moreover variable argument aren't yet supported.
    /// Note that for performance reasons member are accessible although
    /// they are intended read only!
    /// </summary>
    public class MethodSig
    {
        public enum SigType
        {
            MethodDefSig,
            MethodRefSig,
            StandaloneSig
        }

        /// <summary>
        /// If HASTHIS is present in the signature.
        /// </summary>
        public bool HasThis;

        /// <summary>
        /// True if flag EXPLICITTHIS is present within the signature.
        /// </summary>
        public bool ExplicitThis;

        /// <summary>
        /// Call convention.
        /// </summary>
        public CallingConvention CallConvention;

        /// <summary>
        /// Number of parameters in the signature.
        /// </summary>
        public int Count;

        /// <summary>
        /// Returned type in the signature.
        /// </summary>
        public CLIType ReturnType;

        /// <summary>
        /// Pointer to the base.
        /// </summary>
        private MapPtr Base;

        /// <summary>
        /// Pointer to the parameters.
        /// </summary>
        private MapPtr Pars;

        /// <summary>
        /// Number of generic parameters (excluding return type)
        /// -1 if the method is not generic
        /// </summary>
        public int GenericParameterCount;

        private CLIFile file;

        public enum CallingConvention
        {
            DEFAULT = 0x0,
            VARARG = 0x05,
            C = 0x01,
            STDCALL = 0x02,
            THISCALL = 0x03,
            FASTCALL = 0x04,
            GENERIC = 0x10
        }

        /// <summary>
        /// Build a reader of a MethodDef Signature.
        /// </summary>
        /// <param name="p">Pointer to the blob heap</param>
        internal MethodSig(CLIFile f, MapPtr p, SigType t)
        {
            this.file = f;
            Base = p;

            this.HasThis = ((p[0] & 0x20) != 0);
            this.ExplicitThis = ((p[0] & 0x40) != 0);

            // This should be expressed better
            this.CallConvention = (CallingConvention)p[0];
            p += 1;

            if (t != SigType.StandaloneSig && this.CallConvention == CallingConvention.GENERIC)
                this.GenericParameterCount = SignatureUtil.ReadCompressedInt(ref p);
            else
                this.GenericParameterCount = -1;

            this.Count = SignatureUtil.ReadCompressedInt(ref p);

            int sz, tok = SignatureUtil.ReadCompressedInt(p, out sz);

            ELEMENT_TYPE? cmod = null;
            
            if (tok == (int)ELEMENT_TYPE.ELEMENT_TYPE_CMOD_OPT ||
                   tok == (int)ELEMENT_TYPE.ELEMENT_TYPE_CMOD_REQD)
            {
                p += sz;
                cmod = (ELEMENT_TYPE)tok;
            }

            this.ReturnType = null;
            if (tok == (int)ELEMENT_TYPE.ELEMENT_TYPE_TYPEDBYREF)
                this.ReturnType = CLIType.ReadType(ref p, f);
            else
            {
                bool byref = (tok == (int)ELEMENT_TYPE.ELEMENT_TYPE_BYREF);
                if (byref) p += sz;

                CLIType rt = CLIType.ReadType(ref p, f);
                this.ReturnType = byref ? new CLIType(f, new ByRefType(rt.type)) : rt;
            }

            if (cmod.HasValue) this.ReturnType = new CLIType(f, new CustomModType(cmod.Value, this.ReturnType.type));

            Pars = p;
        }

        public System.Collections.Generic.IEnumerable<CLIType> GetParameters()
        {
            return GetParameters(null);
        }


        public System.Collections.Generic.IEnumerable<CLIType> GetParameters(CLIType thistype)
        {
            MapPtr p = Pars;
            int sz;

            if (thistype != null)
                yield return thistype;

            for (int i = 0; i < this.Count; i++)
            {
                int opt = SignatureUtil.ReadCompressedInt(p, out sz);
                if (opt == 0x41) // SENTINEL detected return null
                {
                    yield return null;
                    p += sz;
                    opt = SignatureUtil.ReadCompressedInt(p, out sz);
                }

                ELEMENT_TYPE? cmod = null;
                if (opt == (int)ELEMENT_TYPE.ELEMENT_TYPE_CMOD_OPT || opt == (int)ELEMENT_TYPE.ELEMENT_TYPE_CMOD_REQD)
                {
                    cmod = (ELEMENT_TYPE)opt;
                    p += sz;
                }

                opt = SignatureUtil.ReadCompressedInt(p, out sz);
                if (opt == (int)ELEMENT_TYPE.ELEMENT_TYPE_TYPEDBYREF)
                {
                    yield return CLIType.ReadType(ref p, this.file);
                    continue;
                }

                bool byref = opt == (int)ELEMENT_TYPE.ELEMENT_TYPE_BYREF;
                if (byref) p += sz;

                yield return CLIType.ReadType(ref p, this.file);
            }
        }
    }

    /// <summary>
    /// This class helps reading TypeSpec signatures.
    /// Since a TypeSpec signature is a subset of the TypeSignature we provide just the
    /// reader.
    /// </summary>
    public class TypeSpecSig
    {
        /// <summary>
        /// Read the signature at the given pointer.
        /// </summary>
        /// <param name="b">Base pointer for the TypeSpec signature</param>
        /// <returns>The CLIType representing the signature</returns>
        /// <remarks>
        /// We use the TypeSig helper, therefore it can read also types not
        /// allowed by TypeSpec signature specification.
        /// </remarks>
        public static CLIType Read(CLIFile f, MapPtr b)
        {
            return CLIType.ReadType(ref b, f);
        }
    }

    /// <summary>
    /// This class helps reading Property signatures.
    /// </summary>
    public class PropertySig
    {
        /// <summary>
        /// If HASTHIS is present in the signature.
        /// </summary>
        public bool HasThis;

        /// <summary>
        /// Number of parameters in the signature.
        /// </summary>
        public int Count;

        /// <summary>
        /// Returned type in the signature.
        /// </summary>
        public CLIType ReturnType;

        /// <summary>
        /// Pointer to the base.
        /// </summary>
        private MapPtr Base;

        /// <summary>
        /// Pointer to the parameters.
        /// </summary>
        private MapPtr Pars;

        private CLIFile file;

        /// <summary>
        /// Build a reader of a MethodDef Signature.
        /// </summary>
        /// <param name="p">Pointer to the blob heap</param>
        internal PropertySig(CLIFile f, MapPtr p)
        {
            this.file = f;
            Base = p;

            if (p[0] != 0x08 && p[0] != 0x28)
                throw new Exception("Invalid Property signature!");

            if (p[0] == 0x28)
            {
                p += 1;
                this.HasThis = ((p[0] & 0x20) != 0);
                if (this.HasThis) p += 1;
            }
            else
                p += 1;

            this.Count = SignatureUtil.ReadCompressedInt(ref p);

            int sz, tok = SignatureUtil.ReadCompressedInt(p, out sz);

            ELEMENT_TYPE? cmod = null;
            
            if (tok == (int)ELEMENT_TYPE.ELEMENT_TYPE_CMOD_OPT ||
                   tok == (int)ELEMENT_TYPE.ELEMENT_TYPE_CMOD_REQD)
            {
                p += sz;
                cmod = (ELEMENT_TYPE)tok;
            }

            this.ReturnType = CLIType.ReadType(ref p, f);

            if (cmod.HasValue) this.ReturnType = new CLIType(f, new CustomModType(cmod.Value, this.ReturnType.type));

            Pars = p;
        }

        public System.Collections.Generic.IEnumerator<CLIType> GetParameters()
        {
            MapPtr p = Pars;
            int sz;
            for (int i = 0; i < this.Count; i++)
            {
                int opt = SignatureUtil.ReadCompressedInt(p, out sz);
                ELEMENT_TYPE? cmod = null;
                if (opt == (int)ELEMENT_TYPE.ELEMENT_TYPE_CMOD_OPT || opt == (int)ELEMENT_TYPE.ELEMENT_TYPE_CMOD_REQD)
                {
                    cmod = (ELEMENT_TYPE)opt;
                    p += sz;
                }

                opt = SignatureUtil.ReadCompressedInt(p, out sz);
                if (opt == (int)ELEMENT_TYPE.ELEMENT_TYPE_TYPEDBYREF)
                {
                    yield return CLIType.ReadType(ref p, this.file);
                    continue;
                }

                bool byref = opt == (int)ELEMENT_TYPE.ELEMENT_TYPE_BYREF;
                if (byref) p += sz;

                yield return CLIType.ReadType(ref p, this.file);
            }
        }
    }

    /// <summary>
    /// This class helps reading LocalVar signatures.
    /// The class is conceived to read just the type. Constraint and byref
    /// information is discarded.
    /// </summary>
    public class LocalsVarSig
    {
        private int count;

        private MapPtr Pars;

        private CLIFile file;

        public int Count
        {
            get { return count; }
        }

        internal LocalsVarSig(CLIFile f, MapPtr p)
        {
            this.file = f;
            int sz;

            Debug.Assert(p[0] == 0x07, "Internal error: wrong Local var signature");

            count = SignatureUtil.ReadCompressedInt(p + 1, out sz);
            p += 1 + sz;
            Pars = p;
        }

        public System.Collections.Generic.IEnumerable<CLIType> GetVariables()
        {
            int sz;
            MapPtr p = Pars;
            for (int i = 0; i < count; i++)
            {
                ELEMENT_TYPE? cmod = null;
                ELEMENT_TYPE opt = (ELEMENT_TYPE)SignatureUtil.ReadCompressedInt(p, out sz);
                if (opt == ELEMENT_TYPE.ELEMENT_TYPE_TYPEDBYREF)
                {
                    p += sz;
                    yield return BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_TYPEDBYREF);
                    continue;
                }

                if (opt == ELEMENT_TYPE.ELEMENT_TYPE_CMOD_OPT || opt == ELEMENT_TYPE.ELEMENT_TYPE_CMOD_REQD)
                {
                    cmod = opt;
                    p += sz;
                }
                bool pinned = SignatureUtil.ReadCompressedInt(p, out sz) == (int)ELEMENT_TYPE.ELEMENT_TYPE_PINNED;
                if (pinned) p += sz;
                bool byref = SignatureUtil.ReadCompressedInt(p, out sz) == (int)ELEMENT_TYPE.ELEMENT_TYPE_BYREF;
                if (byref) p += sz;
                CLIType t = CLIType.ReadType(ref p, this.file);
                if (byref) t = new CLIType(this.file, new ByRefType(t.type));
                if (pinned) t = new CLIType(this.file, new PinnedType(t.type));
                if (cmod.HasValue) t = new CLIType(this.file, new CustomModType(cmod.Value, t.type));
                yield return t;
            }
        }
    }

    /// <summary>
    /// This class helps reading Field signatures.
    /// </summary>
    public class FieldSig
    {
        public static CLIType Read(CLIFile f, MapPtr p)
        {
            Debug.Assert(p[0] == 0x06, "Field Signature not that of a field!");
            p += 1;

            int sz, tok = SignatureUtil.ReadCompressedInt(p, out sz);
            ELEMENT_TYPE? cmod = null;

            if (tok == (int)ELEMENT_TYPE.ELEMENT_TYPE_CMOD_OPT || tok == (int)ELEMENT_TYPE.ELEMENT_TYPE_CMOD_REQD)
            {
                p += sz;
                cmod = (ELEMENT_TYPE)tok;
            }

            CLIType t = CLIType.ReadType(ref p, f);
            if (cmod.HasValue) t = new CLIType(f, new CustomModType(cmod.Value, t.type));
            return t;
        }
    }

    /// <summary>
    /// This class helps reading Field signatures.
    /// </summary>
    public class MethodSpecSig
    {
        public static CLIType[] Read(CLIFile f, MapPtr p)
        {
            if (p[0] != 0x0A)
                throw new Exception("Invalid MethodSpec signature!");
            p += 1;

            int count = SignatureUtil.ReadCompressedInt(ref p);
            CLIType[] ret = new CLIType[count];
            for (int i = 0; i < count; i++)
                ret[i] = CLIType.ReadType(ref p, f);
            return ret;
        }
    }

    /// <summary>
    /// This class allows accessing a CLI File, metadata and IL code in the raw
    /// format.
    /// </summary>
    public class CLIFile : IDisposable
    {
        /// <summary>
        /// Mapped file of the CLI file.
        /// </summary>
        private MappedFile file;
        /// <summary>
        /// Path of the assembly file.
        /// </summary>
        private string path;
        /// <summary>
        /// Reflection object that represents the assembly. It is null and
        /// initialized only if it is used GetType method.
        /// </summary>
        private Assembly assembly;
        /// <summary>
        /// Pointer to the metadata
        /// </summary>
        private MapPtr meta;
        /// <summary>
        /// Pointer to the section table of the PE file: it is required to resolve
        /// RVAses.
        /// </summary>
        private MapPtr sections;
        /// <summary>
        /// Number of sections within the PE file.
        /// </summary>
        private int sectno;
        /// <summary>
        /// Tables of the CLI File.
        /// </summary>
        internal Table[] tables = new Table[(int)TableNames.MaxNum];

        /// <summary>
        /// Reference to the wrapper to the blob heap.
        /// </summary>
        public BlobHeap Blob;
        /// <summary>
        /// Reference to the wrapper to the Strings heap.
        /// </summary>
        public StringsHeap Strings;
        /// <summary>
        /// Reference to the wrapper to the User Strings heap.
        /// </summary>
        public UserStringsHeap UserStrings;
        /// <summary>
        /// Reference to the wrapper to the Guid heap.
        /// </summary>
        public GuidHeap Guid;
        /// <summary>
        /// Pointer to the #~ heap.
        /// </summary>
        private MapPtr tilde_p;
        /// <summary>
        /// Pointer to CLI Header
        /// </summary>
        private MapPtr cliHd;

        /// <summary>
        /// Entry point token for the assembly
        /// </summary>
        public int EntryPointToken
        {
            get { return (int)(cliHd + 20); }
        }

        // Size of set of tables
        internal int ResolutionScopeSize = 2;
        internal int TypeDefOrRefSize = 2;
        internal int MemberRefParentSize = 2;
        internal int HasConstantSize = 2;
        internal int HasCustomAttributeSize = 2;
        internal int CustomAttributeTypeSize = 2;
        internal int HasFieldMarshalSize = 2;
        internal int HasDeclSecuritySize = 2;
        internal int HasSemanticsSize = 2;
        internal int MethodDefOrRefSize = 2;
        internal int MemberForwardedSize = 2;
        internal int ImplementationSize = 2;
        // FIXME: Not deduced by the standard
        internal int TypeOrMethodDefSize = 2;

        /// <summary>
        /// Open a PE file that contains CLI information (.dll and .exe).
        /// </summary>
        /// <param name="path">Path to the file.</param>
        private CLIFile(string path)
        {
            file = new MappedFile(path);
            this.path = path;
            assembly = null;
            // Check the PE header
            MapPtr bp = file.Start + (int)(file.Start + 60);
            if (!bp.Matches("PE\0\0"))
                throw new Exception("Format of file not valid!");
            // Number of sections
            sectno = (short)(bp + 6);

            //  232 = 4 ("PE\0\0") + 20 (HD) + 208 (OptHeader)
            bp += 232;

            // CLI Table entry (8 bytes: 4 RVA, 4 Size)
            int cliHdRva = (int)bp;
            int cliHdSz = (int)(bp + 4);

            // End of the OptHeader
            bp += 16;

            sections = bp;
            cliHd = ResolveRVA(cliHdRva);

            // METADATA
            meta = new MapPtr(ResolveRVA((int)(cliHd + 8)), (int)(cliHd + 12));
            if ((int)meta != 0x424A5342)
                throw new Exception("Illegal format");

            int len = (int)(meta + 12); // String length
            if (len % 4 != 0) len += 4 - (len % 4);

            // STREAM HEADERS
            bp = meta + (16 + len + 2);
            short streamno = (short)bp;

            bp += 2;
            for (int i = 0; i < streamno; i++)
            {
                int off = (int)bp;
                int size = (int)(bp + 4);
                string name = (string)(bp + 8);
                if (name == "#~")
                    tilde_p = new MapPtr(meta + off, size);
                else if (name == "#Strings")
                    Strings = new StringsHeap(new MapPtr(meta + off, size), (tilde_p[6] & 0x01) != 0);
                else if (name == "#GUID")
                    Guid = new GuidHeap(new MapPtr(meta + off, size), (tilde_p[6] & 0x02) != 0);
                else if (name == "#US")
                    UserStrings = new UserStringsHeap(new MapPtr(meta + off, size));
                else if (name == "#Blob")
                    Blob = new BlobHeap(new MapPtr(meta + off, size), (tilde_p[6] & 0x04) != 0);
                else
                    throw new Exception("Internal Error: Unknown Stream Name!");
                int sl = name.Length + 1;
                bp += sl + 8;
                if (sl % 4 != 0) bp += 4 - (sl % 4);
            }

            // Read valid tables
            long valid = (long)(tilde_p + 8);
            long sorted = (long)(tilde_p + 16);
            int n = 0;
            for (int i = 0; i < (int)TableNames.MaxNum; i++)
            {
                long mask = (long)1 << i;
                if ((valid & mask) != 0)
                {
                    tables[i] = new Table(this, (TableNames)i, (sorted & mask) != 0);
                    n++;
                }
                else tables[i] = Table.Null;
            }

            // Read the tables' size
            bp = tilde_p + 24;
            for (int i = 0, j = 0; i < n; i++)
            {
                while (tables[j] == Table.Null) j++;
                tables[j].Size = (int)(bp + (4 * i));
                UpdateSets(j++);
            }

            // Compute the begin of the tables
            bp += 4 * n;
            for (int i = 0, j = 0; i < n; i++)
            {
                while (tables[j] == Table.Null) j++;
                tables[j].Start = bp;
                bp += tables[j].TableSize();
                j++;
            }
        }

        /// <summary>
        /// This metho is used to compute incrementally the size of indexes
        /// to set of tables. It is called by the constructor when the size
        /// of tables is read.
        /// </summary>
        /// <param name="j">Index of the table</param>
        private void UpdateSets(int j)
        {
            // Compute in an incremental way the size of combined indexes
            switch ((TableNames)j)
            {
                case TableNames.TypeSpec:
                    if (tables[j].Size > (1 << (0x10 - 0x02)))
                    {
                        TypeDefOrRefSize = 4;
                        // FIXME: I presume it is like this
                        TypeOrMethodDefSize = 4;
                    }
                    if (tables[j].Size > (1 << (0x10 - 0x03))) MemberRefParentSize = 4;
                    if (tables[j].Size > (1 << (0x10 - 0x05))) HasCustomAttributeSize = 4;
                    break;
                case TableNames.TypeDef:
                    if (tables[j].Size > (1 << (0x10 - 0x02)))
                    {
                        TypeDefOrRefSize = 4;
                        HasDeclSecuritySize = 4;
                        // FIXME: I presume it is like this
                        TypeOrMethodDefSize = 4;
                    }
                    if (tables[j].Size > (1 << (0x10 - 0x03))) MemberRefParentSize = 4;
                    if (tables[j].Size > (1 << (0x10 - 0x05))) HasCustomAttributeSize = 4;
                    break;
                case TableNames.TypeRef:
                    if (tables[j].Size > (1 << (0x10 - 0x02)))
                    {
                        TypeDefOrRefSize = 4;
                        ResolutionScopeSize = 4;
                    }
                    if (tables[j].Size > (1 << (0x10 - 0x03))) MemberRefParentSize = 4;
                    if (tables[j].Size > (1 << (0x10 - 0x05))) HasCustomAttributeSize = 4;
                    break;
                case TableNames.ModuleRef:
                    if (tables[j].Size > (1 << (0x10 - 0x02))) ResolutionScopeSize = 4;
                    if (tables[j].Size > (1 << (0x10 - 0x03))) MemberRefParentSize = 4;
                    if (tables[j].Size > (1 << (0x10 - 0x05))) HasCustomAttributeSize = 4;
                    break;
                case TableNames.Module:
                    if (tables[j].Size > (1 << (0x10 - 0x02))) ResolutionScopeSize = 4;
                    if (tables[j].Size > (1 << (0x10 - 0x05))) HasCustomAttributeSize = 4;
                    break;
                case TableNames.AssemblyRef:
                    if (tables[j].Size > (1 << (0x10 - 0x02)))
                    {
                        ResolutionScopeSize = 4;
                        ImplementationSize = 4;
                    }
                    if (tables[j].Size > (1 << (0x10 - 0x05))) HasCustomAttributeSize = 4;
                    break;
                case TableNames.Method:
                    if (tables[j].Size > (1 << (0x10 - 0x02))) HasDeclSecuritySize = 4;
                    if (tables[j].Size > (1 << (0x10 - 0x03)))
                    {
                        CustomAttributeTypeSize = 4;
                        MemberRefParentSize = 4;
                    }
                    if (tables[j].Size > (1 << (0x10 - 0x05))) HasCustomAttributeSize = 4;
                    if (tables[j].Size > (1 << (0x10 - 0x01)))
                    {
                        // FIXME: I presume it is like this
                        TypeOrMethodDefSize = 4;
                        MethodDefOrRefSize = 4;
                        MemberForwardedSize = 4;
                    }
                    break;
                case TableNames.Field:
                    if (tables[j].Size > (1 << (0x10 - 0x02))) HasConstantSize = 4;
                    if (tables[j].Size > (1 << (0x10 - 0x05))) HasCustomAttributeSize = 4;
                    if (tables[j].Size > (1 << (0x10 - 0x01)))
                    {
                        HasFieldMarshalSize = 4;
                        MemberForwardedSize = 4;
                    }
                    break;
                case TableNames.Param:
                    if (tables[j].Size > (1 << (0x10 - 0x02))) HasConstantSize = 4;
                    if (tables[j].Size > (1 << (0x10 - 0x05))) HasCustomAttributeSize = 4;
                    if (tables[j].Size > (1 << (0x10 - 0x01))) HasFieldMarshalSize = 4;
                    break;
                case TableNames.Property:
                    if (tables[j].Size > (1 << (0x10 - 0x02))) HasConstantSize = 4;
                    if (tables[j].Size > (1 << (0x10 - 0x05))) HasCustomAttributeSize = 4;
                    if (tables[j].Size > (1 << (0x10 - 0x01))) HasSemanticsSize = 4;
                    break;
                case TableNames.MemberRef:
                    if (tables[j].Size > (1 << (0x10 - 0x05))) HasCustomAttributeSize = 4;
                    if (tables[j].Size > (1 << (0x10 - 0x03))) CustomAttributeTypeSize = 4;
                    if (tables[j].Size > (1 << (0x10 - 0x01))) MethodDefOrRefSize = 4;
                    break;
                case TableNames.Assembly:
                    if (tables[j].Size > (1 << (0x10 - 0x05))) HasCustomAttributeSize = 4;
                    if (tables[j].Size > (1 << (0x10 - 0x02))) HasDeclSecuritySize = 4;
                    break;
                case TableNames.Event:
                    if (tables[j].Size > (1 << (0x10 - 0x05))) HasCustomAttributeSize = 4;
                    if (tables[j].Size > (1 << (0x10 - 0x01))) HasSemanticsSize = 4;
                    break;
                case TableNames.File:
                    if (tables[j].Size > (1 << (0x10 - 0x05))) HasCustomAttributeSize = 4;
                    if (tables[j].Size > (1 << (0x10 - 0x02))) ImplementationSize = 4;
                    break;
                case TableNames.ExportedType:
                    if (tables[j].Size > (1 << (0x10 - 0x05))) HasCustomAttributeSize = 4;
                    if (tables[j].Size > (1 << (0x10 - 0x02))) ImplementationSize = 4;
                    break;
                case TableNames.InterfaceImpl:
                case TableNames.ManifestResource:
                case TableNames.StandAloneSig: // Check this (in place of Signature)
                case TableNames.DeclSecurity:  // Check this (in place of Permission)
                    if (tables[j].Size > (1 << (0x10 - 0x05))) HasCustomAttributeSize = 4;
                    break;
            }
        }

        /// <summary>
        /// Resolve a RVA within the PE file. It makes use of the section table
        /// referred by the sections field.
        /// </summary>
        /// <param name="rva">Address to resolve</param>
        /// <returns>The pointer to the location within the mapped file.</returns>
        internal MapPtr ResolveRVA(int rva)
        {
            MapPtr bp = sections;
            int sectva = 0;
            int sectvs = 0;
            int sectpt = 0;

            // Find the section
            for (int i = 0; i < sectno; i++)
            {
                //Console.WriteLine("Section {0}{1}{2}{3}{4}{5}{6}{7}", (char)bp[0], (char)bp[1], (char)bp[2], (char)bp[3], (char)bp[4], (char)bp[5], (char)bp[6], (char)bp[7]);
                sectvs = (int)(bp + 8);
                sectva = (int)(bp + 12);
                //Console.WriteLine("VS: {0} VA: {1}, RS: {2}, PT: {3}", sectvs, sectva, (int)(bp + 16), (int)(bp + 20));
                if (sectva <= rva && rva < (sectva + sectvs))
                {
                    sectpt = (int)(bp + 20);
                    return file.Start + (sectpt + rva - sectva);
                }
                bp += 40;
            }
            throw new Exception("Wrong RVA!");
        }

        internal Assembly Assembly
        {
            get
            {
                if (assembly == null)
                {
                    assembly = Assembly.LoadFrom(path);
                    Debug.Assert(assembly.GetModules().Length == 1, "CLIFileReader does not support multiple modules into an assembly yet!");
                }
                return assembly;
            }

        }

        /// <summary>
        /// Return the Type object given the name of a type.
        /// </summary>
        /// <param name="name">Name of the type in the assembly 
        /// (namespace.typename)</param>
        /// <returns>The reflection type</returns>
        internal Type GetType(string name)
        {
            return Assembly.GetType(name);
        }

        /// <summary>
        /// Dispose the mapped file.
        /// </summary>
        public void Dispose()
        {
            if (file != null)
                file.Dispose();
            file = null;
        }

        /// <summary>
        /// This allows exposing tables to outside.
        /// </summary>
        public Table this[TableNames t]
        {
            get { return tables[(int)t]; }
        }

        private static Hashtable CLIFileCache = new Hashtable();

        /// <summary>
        /// Factory for reading CLIFiles.
        /// </summary>
        /// <param name="path">Path to the CLI File</param>
        /// <returns>The required reader</returns>
        public static CLIFile Open(string path)
        {
            if (!System.IO.File.Exists(path))
                return null;

            path = (new System.IO.DirectoryInfo(path)).FullName;
            if (!CLIFileCache.ContainsKey(path))
                CLIFileCache[path] = new WeakReference(new CLIFile(path));

            WeakReference wr = CLIFileCache[path] as WeakReference;
            if (!wr.IsAlive)
                wr.Target = new CLIFile(path);

            return wr.Target as CLIFile;
        }

        /// <summary>
        /// Useful to find a method in the method table!
        /// </summary>
        /// <param name="m">MethodInfo object to look for</param>
        /// <returns>The method table cursor positioned to the method</returns>
        public static MethodTableCursor FindMethod(MethodInfo m)
        {
            CLIFile f = Open(m.DeclaringType.Assembly.Location);
            MethodTableCursor cur = (MethodTableCursor)f[TableNames.Method].GetCursor();
            cur.Goto(cur.FindToken(m));
            return cur;
        }
    }
}
