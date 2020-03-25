/// ------------------------------------------------------------
/// Copyright (c) 2002-2008 Antonio Cisternino (cisterni@di.unipi.it)
/// 
/// File: CLIType.cs
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
using System.Diagnostics;
using System.Reflection;
using MappedView;

namespace CLIFileRW
{
    public class CLIType
    {
        internal CLIFile file;
        internal CLITypeNode type;

        internal CLIType(CLIFile f, CLITypeNode t)
        {
            this.file = f;
            this.type = t;
        }

        /// <summary>
        /// File reader used to define the type.
        /// In case of native types it can be null.
        /// </summary>
        public CLIFile File
        {
            get { return file; }
        }

        /// <summary>
        /// Constructed form of a type.
        /// </summary>
        public CLITypeNode Type
        {
            get { return type; }
        }

        public static CLIType ReadType(ref MapPtr p, CLIFile f)
        {
            return CLITypeNode.ReadType(ref p, f);
        }

        /// <summary>
        /// Used to cross the bridge with Reflection.
        /// </summary>
        /// <param name="methodinst">
        /// The list of type parameters of the enclosing type.
        /// </param>
        /// <param name="typeinst">
        /// The list of type parameters of the enclosing method.
        /// </param>
        /// <returns>The Reflection type representing the assembly type.</returns>
        /// <remarks>
        /// Type arguments can be omitted by specifying null as the corresponding argument.
        /// If the context is empty but required an exception will be thrown. Context can usually
        /// be provided in two ways: when accessing directly through Reflection and handles the
        /// Reflection object can be asked for type arguments; when reading the metadata in the
        /// natural order by simply keeping track of read instantiations.
        /// </remarks>
        public Type GetReflectionType(Type[] typeinst, Type[] methodinst)
        {
            return type.GetReflectionType(this.file, typeinst, methodinst);
        }

        /// <summary>
        /// Used to cross the bridge with Reflection.
        /// </summary>
        /// <param name="typeinst">
        /// The list of type parameters of the enclosing method.
        /// </param>
        /// <returns>The Reflection type representing the assembly type.</returns>
        public Type GetReflectionType(Type[] typeinst)
        {
            return type.GetReflectionType(this.file, typeinst, null);
        }

        /// <summary>
        /// Used to cross the bridge with Reflection.
        /// </summary>
        /// <returns>The Reflection type representing the assembly type.</returns>
        public Type GetReflectionType()
        {
            return type.GetReflectionType(this.file, null, null);
        }

        /// <summary>
        /// True if the type is a reference to a type.
        /// </summary>
        public bool IsByRef { get { return type.IsByRef; } }

        /// <summary>
        /// True if the type is a pointer type.
        /// </summary>
        public bool IsPointer { get { return type.IsPointer; } }
    }

    /// <summary>
    /// Abstract class representing a Type in the Assembly. These types exist
    /// to navigate metadata without loading any assembly within the runtime.
    /// The representation follows the constructive approach to the type system:
    /// leaves represent base types and internal nodes of the tree type constructors.
    /// </summary>
    public abstract class CLITypeNode
    {
        /// <summary>
        /// Used to cross the bridge with Reflection.
        /// </summary>
        /// <param name="methodinst">
        /// The list of type parameters of the enclosing type.
        /// </param>
        /// <param name="typeinst">
        /// The list of type parameters of the enclosing method.
        /// </param>
        /// <returns>The Reflection type representing the assembly type.</returns>
        /// <remarks>
        /// Type arguments can be omitted by specifying null as the corresponding argument.
        /// If the context is empty but required an exception will be thrown. Context can usually
        /// be provided in two ways: when accessing directly through Reflection and handles the
        /// Reflection object can be asked for type arguments; when reading the metadata in the
        /// natural order by simply keeping track of read instantiations.
        /// </remarks>
        public abstract Type GetReflectionType(CLIFile f, Type[] typeinst, Type[] methodinst);

        /// <summary>
        /// True if the type is a reference to a type.
        /// </summary>
        public abstract bool IsByRef { get; }

        /// <summary>
        /// True if the type is a pointer type.
        /// </summary>
        public abstract bool IsPointer { get; }

        internal static CLIType ReadType(ref MapPtr p, CLIFile f)
        {
            return new CLIType(f, ReadTypeNode(ref p, f));
        }

        /// <summary>
        /// Read a type from a signature.
        /// </summary>
        /// <param name="p">Pointer to the signature to be read</param>
        /// <param name="f">CLIFileReader in use</param>
        /// <returns>
        /// A CLIType object representing a type. It is similar to a Reflection.Type
        /// object though it is less expensive and contains enough information to
        /// access type definitions in CLIFile table.
        /// </returns>
        private static CLITypeNode ReadTypeNode(ref MapPtr p, CLIFile f)
        {
            ELEMENT_TYPE t = (ELEMENT_TYPE)SignatureUtil.ReadCompressedInt(ref p);
            switch (t)
            {
                case ELEMENT_TYPE.ELEMENT_TYPE_BOOLEAN:
                case ELEMENT_TYPE.ELEMENT_TYPE_CHAR:
                case ELEMENT_TYPE.ELEMENT_TYPE_I1:
                case ELEMENT_TYPE.ELEMENT_TYPE_U1:
                case ELEMENT_TYPE.ELEMENT_TYPE_I2:
                case ELEMENT_TYPE.ELEMENT_TYPE_U2:
                case ELEMENT_TYPE.ELEMENT_TYPE_I4:
                case ELEMENT_TYPE.ELEMENT_TYPE_U4:
                case ELEMENT_TYPE.ELEMENT_TYPE_I8:
                case ELEMENT_TYPE.ELEMENT_TYPE_U8:
                case ELEMENT_TYPE.ELEMENT_TYPE_R4:
                case ELEMENT_TYPE.ELEMENT_TYPE_R8:
                case ELEMENT_TYPE.ELEMENT_TYPE_I:
                case ELEMENT_TYPE.ELEMENT_TYPE_U:
                case ELEMENT_TYPE.ELEMENT_TYPE_STRING:
                case ELEMENT_TYPE.ELEMENT_TYPE_OBJECT:
                case ELEMENT_TYPE.ELEMENT_TYPE_TYPEDBYREF:
                case ELEMENT_TYPE.ELEMENT_TYPE_VOID:
                    return BaseType.TypeOf(t).type;

                case ELEMENT_TYPE.ELEMENT_TYPE_VAR:
                    return new VariableType(f, SignatureUtil.ReadCompressedInt(ref p));

                case ELEMENT_TYPE.ELEMENT_TYPE_MVAR:
                    return new MethodVariableType(f, SignatureUtil.ReadCompressedInt(ref p));

                case ELEMENT_TYPE.ELEMENT_TYPE_VALUETYPE:
                case ELEMENT_TYPE.ELEMENT_TYPE_CLASS:
                    return new CompoundType(f, SignatureUtil.ReadCompressedInt(ref p));

                case ELEMENT_TYPE.ELEMENT_TYPE_GENERICINST:
                    {
                        ELEMENT_TYPE isClass = (ELEMENT_TYPE)SignatureUtil.ReadCompressedInt(ref p);
                        int ttk = SignatureUtil.ReadCompressedInt(ref p);
                        int count = SignatureUtil.ReadCompressedInt(ref p);
                        CLIType[] args = new CLIType[count];
                        for (int i = 0; i < count; i++)
                            args[i] = new CLIType(f, ReadTypeNode(ref p, f));

                        return new CompoundType(f, ttk, args);
                    }

                case ELEMENT_TYPE.ELEMENT_TYPE_PTR:
                    {
                        int sz;
                        ELEMENT_TYPE opt = (ELEMENT_TYPE)SignatureUtil.ReadCompressedInt(p, out sz);
                        ELEMENT_TYPE? cmod = null;
                        if (opt == ELEMENT_TYPE.ELEMENT_TYPE_CMOD_OPT || opt == ELEMENT_TYPE.ELEMENT_TYPE_CMOD_REQD)
                        {
                            cmod = opt;
                            p += 1;
                        }
                        CLITypeNode ptrt = new PointerType(ReadTypeNode(ref p, f));
                        if (cmod.HasValue)
                            ptrt = new CustomModType(cmod.Value, ptrt);
                        return ptrt;
                    }

                case ELEMENT_TYPE.ELEMENT_TYPE_FNPTR:
                    return new FunPointerType(f, p);

                case ELEMENT_TYPE.ELEMENT_TYPE_ARRAY:
                    {
                        CLITypeNode at = ReadTypeNode(ref p, f);
                        int rank = SignatureUtil.ReadCompressedInt(ref p);
                        int sz = SignatureUtil.ReadCompressedInt(ref p);
                        int[] szs = new int[sz];
                        for (int i = 0; i < sz; i++)
                            szs[i] = SignatureUtil.ReadCompressedInt(ref p);
                        sz = SignatureUtil.ReadCompressedInt(ref p);
                        int[] lb = new int[sz];
                        for (int i = 0; i < sz; i++)
                            lb[i] = SignatureUtil.ReadCompressedInt(ref p);
                        return new ArrayType(at, rank, szs, lb);
                    }
                case ELEMENT_TYPE.ELEMENT_TYPE_SZARRAY:
                    {
                        int sz;
                        ELEMENT_TYPE opt = (ELEMENT_TYPE)SignatureUtil.ReadCompressedInt(p, out sz);
                        ELEMENT_TYPE? cmod = null;
                        if (opt == ELEMENT_TYPE.ELEMENT_TYPE_CMOD_OPT || opt == ELEMENT_TYPE.ELEMENT_TYPE_CMOD_REQD)
                        {
                            cmod = opt;
                            p += 1;
                        }
                        CLITypeNode sat = new ArrayType(ReadTypeNode(ref p, f));
                        if (cmod.HasValue)
                            sat = new CustomModType(cmod.Value, sat);
                        return sat;
                    }
                default:
                    throw new Exception("Internal error in CLI File Reader library!");
            }
        }
    }

    /// <summary>
    /// A type representing an array type  (see 23.2.13 for definition)
    /// </summary>
    public class ArrayType : CLITypeNode
    {
        private int rank;
        private int[] sizes;
        private int[] lobounds;
        private CLITypeNode type;
        private bool szarray;

        /// <summary>
        /// Array rank
        /// </summary>
        public int Rank { get { return rank; } }

        /// <summary>
        /// CLI allows for specifying the dimension of first n elements
        /// of an array definition (n &lt;= rank).
        /// </summary>
        public int[] Sizes { get { return sizes; } }

        /// <summary>
        /// For each of the given dimensions it is possible to specify a
        /// lower bound. The number of bounds can be smaller than those of sizes.
        /// </summary>
        public int[] LowBounds { get { return lobounds; } }

        /// <summary>
        /// It retains the information if in the signature the array was
        /// specified as a SZARRAY or as an ARRAY. Custom Modifiers are
        /// allowed only in the former case.
        /// </summary>
        public bool IsSZArray { get { return szarray; } }

        public CLITypeNode Type
        {
            get { return type; }
        }

        /// <summary>
        /// Builds an array type from an ARRAY signature
        /// </summary>
        /// <param name="t">Type of the array</param>
        /// <param name="r">Rank of the array</param>
        /// <param name="s">Sizes of the lower dimensions (possibly all)</param>
        /// <param name="l">Lower bounds of lower dimensions</param>
        internal ArrayType(CLITypeNode t, int r, int[] s, int[] l)
        {
            // Used to not loose the declaration SZARRAY
            szarray = false;
            type = t;
            rank = r;
            sizes = s;
            lobounds = l;
        }

        /// <summary>
        /// Builds an array type from SZARRAY signature
        /// </summary>
        /// <param name="t">Type of the array</param>
        internal ArrayType(CLITypeNode t)
            : this(t, 1, null, null)
        {
            szarray = true;
        }

        /// <summary>
        /// Returns the reflection type for the array type.
        /// </summary>
        /// <param name="f">The CLIFileReader defining the assembly.</param>
        /// <returns>The reflection type.</returns>
        public override Type GetReflectionType(CLIFile f, Type[] typepars, Type[] methodpars)
        {
            Type t = type.GetReflectionType(f, typepars, methodpars);
            return szarray ? t.MakeArrayType() : t.MakeArrayType(rank);
        }

        /// <summary>
        /// Return false since an array is not a reference to a type.
        /// </summary>
        public override bool IsByRef
        {
            get { return false; }
        }

        /// <summary>
        /// Return false since an array is not a pointer type.
        /// </summary>
        public override bool IsPointer
        {
            get { return false; }
        }
    }

    /// <summary>
    /// Function type, identified by FNPTR type in the Metadata spec.
    /// </summary>
    public class FunPointerType : CLITypeNode
    {
        private MethodSig s;

        /// <summary>
        /// The method signature referred by the pointer.
        /// </summary>
        public MethodSig Signature { get { return s; } }

        /// <summary>
        /// Build a FunType constructor.
        /// </summary>
        /// <param name="p"></param>
        internal FunPointerType(CLIFile f, MethodSig sig)
        {
            s = sig;
        }

        /// <summary>
        /// Build a FunType constructor.
        /// </summary>
        /// <param name="p"></param>
        internal FunPointerType(CLIFile f, MapPtr p)
        {
            // Use of MethodRef which is more general than Def
            s = new MethodSig(f, p, MethodSig.SigType.MethodRefSig);
        }

        /// <summary>
        /// Not yet implemented. How does a function pointer get represented by
        /// reflection?
        /// </summary>
        /// <param name="f">Reader to be used for resolving tokens.</param>
        /// <param name="typepars">
        /// Generic instantiation of enclosing generic type arguments if any
        /// (null otherwise)
        /// </param>
        /// <param name="methodpars">
        /// Generic instantiation of enclosing generic method arguments if any 
        /// (null otherwise)
        /// </param>
        /// <returns>The Reflection type associated with the type subtree.</returns>
        public override Type GetReflectionType(CLIFile f, Type[] typepars, Type[] methodpars)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return false since it is not a pointer type.
        /// </summary>
        public override bool IsByRef
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Return true since it is a pointer type.
        /// </summary>
        public override bool IsPointer
        {
            get { return true; }
        }
    }

    /// <summary>
    /// Represents custom modifiers, it could have been defined as a flag,
    /// though not all types are allowed to be marked, therefore we introduced
    /// a node in the type tree. This is a design decision that could change
    /// in future versions.
    /// </summary>
    public class CustomModType : CLITypeNode
    {
        ELEMENT_TYPE cmod;
        CLITypeNode type;

        /// <summary>
        /// Build a CustomModType node.
        /// </summary>
        /// <param name="mod">The type modifier (actually one of CMOD_OPT or CMOD_REQD)</param>
        /// <param name="t">The type to which the modifier applies</param>
        internal CustomModType(ELEMENT_TYPE mod, CLITypeNode t)
        {
            Debug.Assert(mod == ELEMENT_TYPE.ELEMENT_TYPE_CMOD_OPT || mod == ELEMENT_TYPE.ELEMENT_TYPE_CMOD_REQD, "Invalid Custom Modifier!");
            cmod = mod;
            type = t;
        }

        /// <summary>
        /// Return the custom modifier associated with the enclosed type.
        /// </summary>
        public ELEMENT_TYPE CustomMod
        {
            get { return cmod; }
        }

        /// <summary>
        /// Type annotated with the modifier.
        /// </summary>
        public CLITypeNode Type
        {
            get { return type; }
        }

        /// <summary>
        /// Return the Reflection type of the enclosed type.
        /// </summary>
        /// <param name="f">Reader to be used for resolving tokens.</param>
        /// <param name="typepars">
        /// Generic instantiation of enclosing generic type arguments if any
        /// (null otherwise)
        /// </param>
        /// <param name="methodpars">
        /// Generic instantiation of enclosing generic method arguments if any 
        /// (null otherwise)
        /// </param>
        /// <returns>The Reflection type associated with the type subtree.</returns>
        public override Type GetReflectionType(CLIFile f, Type[] typepars, Type[] methodpars)
        {
            return type.GetReflectionType(f, typepars, methodpars);
        }

        /// <summary>
        /// True if the enclosed type is annotated as byref.
        /// </summary>
        public override bool IsByRef
        {
            get { return type.IsByRef; }
        }

        /// <summary>
        /// True if the enclosed type is a pointer type.
        /// </summary>
        public override bool IsPointer
        {
            get { return type.IsPointer; }
        }
    }

    /// <summary>
    /// As for CustomModType this type is used to flag a local variable
    /// type as pinned.
    /// </summary>
    public class PinnedType : CLITypeNode
    {
        private CLITypeNode type;

        /// <summary>
        /// Build a PinnedType object for a given type.
        /// </summary>
        /// <param name="t">Type to mark as pinned.</param>
        internal PinnedType(CLITypeNode t)
        {
            type = t;
        }

        /// <summary>
        /// Type embedded within the node pinned.
        /// </summary>
        public CLITypeNode Type
        {
            get { return type; }
        }

        /// <summary>
        /// Return the Reflection type of the enclosed type.
        /// </summary>
        /// <param name="f">Reader to be used for resolving tokens.</param>
        /// <param name="typepars">
        /// Generic instantiation of enclosing generic type arguments if any
        /// (null otherwise)
        /// </param>
        /// <param name="methodpars">
        /// Generic instantiation of enclosing generic method arguments if any 
        /// (null otherwise)
        /// </param>
        /// <returns>The Reflection type associated with the type subtree.</returns>
        public override Type GetReflectionType(CLIFile f, Type[] typepars, Type[] methodpars)
        {
            return type.GetReflectionType(f, typepars, methodpars);
        }

        /// <summary>
        /// True if the enclosed type is byref.
        /// </summary>
        public override bool IsByRef
        {
            get { return type.IsByRef; }
        }

        /// <summary>
        /// True if the enclosed type is a pointer type.
        /// </summary>
        public override bool IsPointer
        {
            get { return type.IsPointer; }
        }
    }

    /// <summary>
    /// Node that allows building a ref type from a type.
    /// </summary>
    public class ByRefType : CLITypeNode
    {
        private CLITypeNode type;

        /// <summary>
        /// Build the ByRefType node.
        /// </summary>
        /// <param name="t">Type to be used to build the ref type.</param>
        internal ByRefType(CLITypeNode t)
        {
            type = t;
        }

        /// <summary>
        /// Embedded type.
        /// </summary>
        public CLITypeNode Type
        {
            get { return type; }
        }

        /// <summary>
        /// Return the Reflection type associated with the type subtree.
        /// </summary>
        /// <param name="f">Reader to be used for resolving tokens.</param>
        /// <param name="typepars">
        /// Generic instantiation of enclosing generic type arguments if any
        /// (null otherwise)
        /// </param>
        /// <param name="methodpars">
        /// Generic instantiation of enclosing generic method arguments if any 
        /// (null otherwise)
        /// </param>
        /// <returns>The Reflection type associated with the type subtree.</returns>
        public override Type GetReflectionType(CLIFile f, Type[] typepars, Type[] methodpars)
        {
            return type.GetReflectionType(f, typepars, methodpars).MakeByRefType();
        }

        /// <summary>
        /// Return true.
        /// </summary>
        public override bool IsByRef
        {
            get { return true; }
        }

        /// <summary>
        /// True if the enclosed type is a pointer type.
        /// </summary>
        public override bool IsPointer
        {
            get { return type.IsPointer; }
        }
    }

    /// <summary>
    /// Represents a Pointer type constructor.
    /// </summary>
    public class PointerType : CLITypeNode
    {
        private CLITypeNode type;

        /// <summary>
        /// Builds a Pointer type for a given type.
        /// </summary>
        /// <param name="t">Type to be used as a pointer type.</param>
        internal PointerType(CLITypeNode t)
        {
            type = t;
        }

        /// <summary>
        /// Return the enclosed type.
        /// </summary>
        public CLITypeNode Type
        {
            get { return type; }
        }

        /// <summary>
        /// Return the Reflection type associated with the type subtree.
        /// </summary>
        /// <param name="f">Reader to be used for resolving tokens.</param>
        /// <param name="typepars">
        /// Generic instantiation of enclosing generic type arguments if any
        /// (null otherwise)
        /// </param>
        /// <param name="methodpars">
        /// Generic instantiation of enclosing generic method arguments if any 
        /// (null otherwise)
        /// </param>
        /// <returns>The Reflection type associated with the type subtree.</returns>
        public override Type GetReflectionType(CLIFile f, Type[] typepars, Type[] methodpars)
        {
            return type.GetReflectionType(f, typepars, methodpars).MakePointerType();
        }

        /// <summary>
        /// Return false.
        /// </summary>
        public override bool IsByRef
        {
            get { return false; }
        }

        /// <summary>
        /// Return true.
        /// </summary>
        public override bool IsPointer
        {
            get { return true; }
        }
    }

    /// <summary>
    /// Type constructor for compound types (class or value type).
    /// </summary>
    public class CompoundType : CLITypeNode
    {
        private int typedeforrefidx;

        private CLIType[] args;

        internal static CompoundType FromFToken(CLIFile f, int tk, CLIType[] genpars)
        {
            int t = 0;
            switch ((TableNames)(tk >> 24))
            {
                case TableNames.TypeDef:
                    t = (int)TypeDefOrRefTag.TypeDef;
                    break;
                case TableNames.TypeRef:
                    t = (int)TypeDefOrRefTag.TypeRef;
                    break;
                case TableNames.TypeSpec:
                    t = (int)TypeDefOrRefTag.TypeSpec;
                    break;
                default:
                    throw new Exception("Invalid token!");
            }
            t = t | ((tk & 0x00FFFFFF) << 2);
            return new CompoundType(f, t);
        }

        /// <summary>
        /// Build a Compound type given the type and the TypeDefOrRef token.
        /// </summary>
        /// <param name="tk">A TypeDefOrRef token.</param>
        internal CompoundType(CLIFile f, int tk) : this(f, tk, null) { }

        /// <summary>
        /// Build a Compound type given the type and the TypeDefOrRef token.
        /// </summary>
        /// <param name="tk">A TypeDefOrRef token.</param>
        /// <param name="genpars">An array of types for instantiating a generic type.</param>
        internal CompoundType(CLIFile f, int tk, CLIType[] genpars)
        {
            typedeforrefidx = tk;
            args = genpars;
        }

        /// <summary>
        /// The generic argument for a generic type (null if none).
        /// </summary>
        public CLIType[] GenericArgs
        {
            get
            {
                return args;
            }
        }

        /// <summary>
        /// The table containing the type definition.
        /// </summary>
        public TableNames Type
        {
            get
            {
                switch ((TypeDefOrRefTag)(typedeforrefidx & 0x03))
                {
                    case TypeDefOrRefTag.TypeDef:
                        return TableNames.TypeDef;
                    case TypeDefOrRefTag.TypeRef:
                        return TableNames.TypeRef;
                    case TypeDefOrRefTag.TypeSpec:
                        return TableNames.TypeSpec;
                }
                throw new Exception("Internal error: invalid TypeDefOrRef encoded index type!");
            }
        }

        /// <summary>
        /// The Row of the table containing the definition for the type.
        /// </summary>
        public int Row
        {
            get
            {
                return typedeforrefidx >> 2;
            }
        }

        /// <summary>
        /// Return the Reflection type associated with the type subtree.
        /// </summary>
        /// <param name="f">Reader to be used for resolving tokens.</param>
        /// <param name="typepars">
        /// Generic instantiation of enclosing generic type arguments if any
        /// (null otherwise)
        /// </param>
        /// <param name="methodpars">
        /// Generic instantiation of enclosing generic method arguments if any 
        /// (null otherwise)
        /// </param>
        /// <returns>The Reflection type associated with the type subtree.</returns>
        public override Type GetReflectionType(CLIFile f, Type[] typepars, Type[] methodpars)
        {
            Type ret = f.Assembly.ManifestModule.ResolveType((((int)Type) << 24) | Row);
            if (args != null)
            {
                Type[] ts = Array.ConvertAll<CLIType, Type>(args, delegate(CLIType tt) { return tt.GetReflectionType(typepars, methodpars); });
                ret = ret.MakeGenericType(ts);
            }
            return ret;
        }

        /// <summary>
        /// Return false.
        /// </summary>
        public override bool IsByRef
        {
            get { return false; }
        }

        /// <summary>
        /// Return false.
        /// </summary>
        public override bool IsPointer
        {
            get { return false; }
        }
    }

    /// <summary>
    /// Represents a type variable in a method (a !! reference).
    /// </summary>
    public class MethodVariableType : VariableType
    {
        /// <summary>
        /// Build a type variable holding the number of the generic argument.
        /// </summary>
        /// <param name="n">Index of the generic argument.</param>
        internal MethodVariableType(CLIFile f, int n) : base(f, n) {}

        /// <summary>
        /// Return the Reflection type associated with the type subtree.
        /// </summary>
        /// <param name="f">Reader to be used for resolving tokens.</param>
        /// <param name="typepars">
        /// Generic instantiation of enclosing generic type arguments if any
        /// (null otherwise)
        /// </param>
        /// <param name="methodpars">
        /// Generic instantiation of enclosing generic method arguments if any 
        /// (null otherwise)
        /// </param>
        /// <returns>The Reflection type associated with the type subtree.</returns>
        public override Type GetReflectionType(CLIFile f, Type[] typepars, Type[] methodpars)
        {
            if (methodpars == null)
                throw new Exception("Type parameters of method instantiation are required to resolve type!");
            return methodpars[Index];
        }

        /// <summary>
        /// Return false.
        /// </summary>
        public override bool IsByRef
        {
            get { return false; }
        }

        /// <summary>
        /// Return false.
        /// </summary>
        public override bool IsPointer
        {
            get { return false; }
        }
    }

    /// <summary>
    /// Represents a type variable (a ! argument).
    /// </summary>
    public class VariableType : CLITypeNode
    {
        private int num;

        /// <summary>
        /// Index of the argument.
        /// </summary>
        public virtual int Index
        {
            get
            {
                return num;
            }
        }

        /// <summary>
        /// Build a type variable node holding the index of the type in the generic signature.
        /// </summary>
        /// <param name="n">Index of the type parameter</param>
        internal VariableType(CLIFile f, int n)
        {
            num = n;
        }

        /// <summary>
        /// Return the Reflection type associated with the type subtree.
        /// </summary>
        /// <param name="f">Reader to be used for resolving tokens.</param>
        /// <param name="typepars">
        /// Generic instantiation of enclosing generic type arguments if any
        /// (null otherwise)
        /// </param>
        /// <param name="methodpars">
        /// Generic instantiation of enclosing generic method arguments if any 
        /// (null otherwise)
        /// </param>
        /// <returns>The Reflection type associated with the type subtree.</returns>
        public override Type GetReflectionType(CLIFile f, Type[] typepars, Type[] methodpars)
        {
            if (typepars == null)
                throw new Exception("Type parameters of type instantiation are required to resolve type!");
            return typepars[num];
        }

        /// <summary>
        /// Return false.
        /// </summary>
        public override bool IsByRef
        {
            get { return false; }
        }

        /// <summary>
        /// Return false.
        /// </summary>
        public override bool IsPointer
        {
            get { return false; }
        }
    }

    public enum OperandCategory
    {
        None,
        int32,
        int64,
        nativeInt,
        F,
        Ref,
        O
    }

    /// <summary>
    /// Represents all the base types of CLI.
    /// </summary>
    public class BaseType : CLITypeNode
    {
        private ELEMENT_TYPE type;

        private static System.Collections.Generic.Dictionary<ELEMENT_TYPE, CLIType> BaseTypes = new System.Collections.Generic.Dictionary<ELEMENT_TYPE, CLIType>();

        static BaseType() {
            BaseTypes.Add(ELEMENT_TYPE.ELEMENT_TYPE_BOOLEAN, new CLIType(null, new BaseType(ELEMENT_TYPE.ELEMENT_TYPE_BOOLEAN)));
            BaseTypes.Add(ELEMENT_TYPE.ELEMENT_TYPE_CHAR, new CLIType(null, new BaseType(ELEMENT_TYPE.ELEMENT_TYPE_CHAR)));
            BaseTypes.Add(ELEMENT_TYPE.ELEMENT_TYPE_I1, new CLIType(null, new BaseType(ELEMENT_TYPE.ELEMENT_TYPE_I1)));
            BaseTypes.Add(ELEMENT_TYPE.ELEMENT_TYPE_U1, new CLIType(null, new BaseType(ELEMENT_TYPE.ELEMENT_TYPE_U1)));
            BaseTypes.Add(ELEMENT_TYPE.ELEMENT_TYPE_I2, new CLIType(null, new BaseType(ELEMENT_TYPE.ELEMENT_TYPE_I2)));
            BaseTypes.Add(ELEMENT_TYPE.ELEMENT_TYPE_U2, new CLIType(null, new BaseType(ELEMENT_TYPE.ELEMENT_TYPE_U2)));
            BaseTypes.Add(ELEMENT_TYPE.ELEMENT_TYPE_I4, new CLIType(null, new BaseType(ELEMENT_TYPE.ELEMENT_TYPE_I4)));
            BaseTypes.Add(ELEMENT_TYPE.ELEMENT_TYPE_U4, new CLIType(null, new BaseType(ELEMENT_TYPE.ELEMENT_TYPE_U4)));
            BaseTypes.Add(ELEMENT_TYPE.ELEMENT_TYPE_I8, new CLIType(null, new BaseType(ELEMENT_TYPE.ELEMENT_TYPE_I8)));
            BaseTypes.Add(ELEMENT_TYPE.ELEMENT_TYPE_U8, new CLIType(null, new BaseType(ELEMENT_TYPE.ELEMENT_TYPE_U8)));
            BaseTypes.Add(ELEMENT_TYPE.ELEMENT_TYPE_R4, new CLIType(null, new BaseType(ELEMENT_TYPE.ELEMENT_TYPE_R4)));
            BaseTypes.Add(ELEMENT_TYPE.ELEMENT_TYPE_R8, new CLIType(null, new BaseType(ELEMENT_TYPE.ELEMENT_TYPE_R8)));
            BaseTypes.Add(ELEMENT_TYPE.ELEMENT_TYPE_I, new CLIType(null, new BaseType(ELEMENT_TYPE.ELEMENT_TYPE_I)));
            BaseTypes.Add(ELEMENT_TYPE.ELEMENT_TYPE_U, new CLIType(null, new BaseType(ELEMENT_TYPE.ELEMENT_TYPE_U)));
            BaseTypes.Add(ELEMENT_TYPE.ELEMENT_TYPE_STRING, new CLIType(null, new BaseType(ELEMENT_TYPE.ELEMENT_TYPE_STRING)));
            BaseTypes.Add(ELEMENT_TYPE.ELEMENT_TYPE_OBJECT, new CLIType(null, new BaseType(ELEMENT_TYPE.ELEMENT_TYPE_OBJECT)));
            BaseTypes.Add(ELEMENT_TYPE.ELEMENT_TYPE_VOID, new CLIType(null, new BaseType(ELEMENT_TYPE.ELEMENT_TYPE_VOID)));
            BaseTypes.Add(ELEMENT_TYPE.ELEMENT_TYPE_TYPEDBYREF, new CLIType(null, new BaseType(ELEMENT_TYPE.ELEMENT_TYPE_TYPEDBYREF)));
        }

        public static CLIType Promote(CLIType nt1, CLIType nt2)
        {
            if (!(nt1.type is BaseType) || !(nt2.type is BaseType))
                throw new Exception("Internal error");

            BaseType t1 = (BaseType)nt1.type;
            BaseType t2 = (BaseType)nt2.type;

            switch (t1.type)
            {
                case ELEMENT_TYPE.ELEMENT_TYPE_CHAR:
                    switch (t2.type)
                    {
                        case ELEMENT_TYPE.ELEMENT_TYPE_CHAR:
                        case ELEMENT_TYPE.ELEMENT_TYPE_U1:
                        case ELEMENT_TYPE.ELEMENT_TYPE_U2:
                        case ELEMENT_TYPE.ELEMENT_TYPE_U4:
                        case ELEMENT_TYPE.ELEMENT_TYPE_U8:
                            return nt2;
                    }
                    return null;
                case ELEMENT_TYPE.ELEMENT_TYPE_I1:
                    switch (t2.type)
                    {
                        case ELEMENT_TYPE.ELEMENT_TYPE_I1:
                        case ELEMENT_TYPE.ELEMENT_TYPE_I2:
                        case ELEMENT_TYPE.ELEMENT_TYPE_I4:
                        case ELEMENT_TYPE.ELEMENT_TYPE_I8:
                            return nt2;
                    }
                    return null;
                case ELEMENT_TYPE.ELEMENT_TYPE_U1:
                    switch (t2.type)
                    {
                        case ELEMENT_TYPE.ELEMENT_TYPE_CHAR:
                            return nt1;
                        case ELEMENT_TYPE.ELEMENT_TYPE_U1:
                        case ELEMENT_TYPE.ELEMENT_TYPE_U2:
                        case ELEMENT_TYPE.ELEMENT_TYPE_U4:
                        case ELEMENT_TYPE.ELEMENT_TYPE_U8:
                            return nt2;
                    }
                    return null;
                case ELEMENT_TYPE.ELEMENT_TYPE_I2:
                    switch (t2.type)
                    {
                        case ELEMENT_TYPE.ELEMENT_TYPE_I1:
                            return nt1;
                        case ELEMENT_TYPE.ELEMENT_TYPE_I2:
                        case ELEMENT_TYPE.ELEMENT_TYPE_I4:
                        case ELEMENT_TYPE.ELEMENT_TYPE_I8:
                            return nt2;
                    }
                    return null;
                case ELEMENT_TYPE.ELEMENT_TYPE_U2:
                    switch (t2.type)
                    {
                        case ELEMENT_TYPE.ELEMENT_TYPE_CHAR:
                        case ELEMENT_TYPE.ELEMENT_TYPE_U1:
                            return nt1;
                        case ELEMENT_TYPE.ELEMENT_TYPE_U2:
                        case ELEMENT_TYPE.ELEMENT_TYPE_U4:
                        case ELEMENT_TYPE.ELEMENT_TYPE_U8:
                            return nt2;
                    }
                    return null;
                case ELEMENT_TYPE.ELEMENT_TYPE_I4:
                    switch (t2.type)
                    {
                        case ELEMENT_TYPE.ELEMENT_TYPE_I1:
                        case ELEMENT_TYPE.ELEMENT_TYPE_I2:
                        case ELEMENT_TYPE.ELEMENT_TYPE_I4:
                            return nt1;
                        case ELEMENT_TYPE.ELEMENT_TYPE_I8:
                            return nt2;
                    }
                    return null;
                case ELEMENT_TYPE.ELEMENT_TYPE_U4:
                    switch (t2.type)
                    {
                        case ELEMENT_TYPE.ELEMENT_TYPE_CHAR:
                        case ELEMENT_TYPE.ELEMENT_TYPE_U1:
                        case ELEMENT_TYPE.ELEMENT_TYPE_U2:
                        case ELEMENT_TYPE.ELEMENT_TYPE_U4:
                            return nt1;
                        case ELEMENT_TYPE.ELEMENT_TYPE_U8:
                            return nt2;
                    }
                    return null;
                case ELEMENT_TYPE.ELEMENT_TYPE_I8:
                    switch (t2.type)
                    {
                        case ELEMENT_TYPE.ELEMENT_TYPE_I1:
                        case ELEMENT_TYPE.ELEMENT_TYPE_I2:
                        case ELEMENT_TYPE.ELEMENT_TYPE_I4:
                        case ELEMENT_TYPE.ELEMENT_TYPE_I8:
                            return nt1;
                    }
                    return null;
                case ELEMENT_TYPE.ELEMENT_TYPE_U8:
                    switch (t2.type)
                    {
                        case ELEMENT_TYPE.ELEMENT_TYPE_CHAR:
                        case ELEMENT_TYPE.ELEMENT_TYPE_U1:
                        case ELEMENT_TYPE.ELEMENT_TYPE_U2:
                        case ELEMENT_TYPE.ELEMENT_TYPE_U4:
                        case ELEMENT_TYPE.ELEMENT_TYPE_U8:
                            return nt1;
                    }
                    return null;
                case ELEMENT_TYPE.ELEMENT_TYPE_R4:
                    switch (t2.type)
                    {
                        case ELEMENT_TYPE.ELEMENT_TYPE_R4:
                        case ELEMENT_TYPE.ELEMENT_TYPE_R8:
                            return nt2;
                    }
                    return null;
                case ELEMENT_TYPE.ELEMENT_TYPE_R8:
                    switch (t2.type)
                    {
                        case ELEMENT_TYPE.ELEMENT_TYPE_R4:
                        case ELEMENT_TYPE.ELEMENT_TYPE_R8:
                            return nt1;
                    }
                    return null;
                case ELEMENT_TYPE.ELEMENT_TYPE_I:
                    // FIXME: Check this
                    switch (t2.type)
                    {
                        case ELEMENT_TYPE.ELEMENT_TYPE_I1:
                        case ELEMENT_TYPE.ELEMENT_TYPE_I2:
                        case ELEMENT_TYPE.ELEMENT_TYPE_I4:
                            return nt1;
                    }
                    return null;
                case ELEMENT_TYPE.ELEMENT_TYPE_U:
                    // FIXME: Check this
                    switch (t2.type)
                    {
                        case ELEMENT_TYPE.ELEMENT_TYPE_CHAR:
                        case ELEMENT_TYPE.ELEMENT_TYPE_U1:
                        case ELEMENT_TYPE.ELEMENT_TYPE_U2:
                        case ELEMENT_TYPE.ELEMENT_TYPE_U4:
                            return nt1;
                    }
                    return null;
                default:
                    return null;
            }
        }

        public OperandCategory Category
        {
            get
            {
                switch (type)
                {
                    case ELEMENT_TYPE.ELEMENT_TYPE_CHAR:
                    case ELEMENT_TYPE.ELEMENT_TYPE_I1:
                    case ELEMENT_TYPE.ELEMENT_TYPE_U1:
                    case ELEMENT_TYPE.ELEMENT_TYPE_I2:
                    case ELEMENT_TYPE.ELEMENT_TYPE_U2:
                    case ELEMENT_TYPE.ELEMENT_TYPE_I4:
                    case ELEMENT_TYPE.ELEMENT_TYPE_U4:
                        return OperandCategory.int32;
                    case ELEMENT_TYPE.ELEMENT_TYPE_I8:
                    case ELEMENT_TYPE.ELEMENT_TYPE_U8:
                        return OperandCategory.int64;
                    case ELEMENT_TYPE.ELEMENT_TYPE_R4:
                    case ELEMENT_TYPE.ELEMENT_TYPE_R8:
                        return OperandCategory.F;
                    case ELEMENT_TYPE.ELEMENT_TYPE_I:
                    case ELEMENT_TYPE.ELEMENT_TYPE_U:
                        return OperandCategory.nativeInt;
                    case ELEMENT_TYPE.ELEMENT_TYPE_STRING:
                    case ELEMENT_TYPE.ELEMENT_TYPE_OBJECT:
                        return OperandCategory.O;
                    default:
                        return OperandCategory.None;
                }
            }
        }

        /// <summary>
        /// Type represented by the node.
        /// </summary>
        public ELEMENT_TYPE Type
        {
            get
            {
                return type;
            }
        }

        internal static CLIType TypeOf(ELEMENT_TYPE t)
        {
            Debug.Assert(BaseTypes.ContainsKey(t), "Invalid base type!");
            return BaseTypes[t];
        }

        /// <summary>
        /// Build a base type given an ELEMENT_TYPE.
        /// </summary>
        /// <param name="t">Base type</param>
        private BaseType(ELEMENT_TYPE t)
        {
            type = t;
        }

        /// <summary>
        /// Return the Reflection type associated with the type subtree.
        /// </summary>
        /// <param name="f">Reader to be used for resolving tokens.</param>
        /// <param name="typepars">
        /// Generic instantiation of enclosing generic type arguments if any
        /// (null otherwise)
        /// </param>
        /// <param name="methodpars">
        /// Generic instantiation of enclosing generic method arguments if any 
        /// (null otherwise)
        /// </param>
        /// <returns>The Reflection type associated with the type subtree.</returns>
        public override Type GetReflectionType(CLIFile f, Type[] typepars, Type[] methodpars)
        {
            switch (type)
            {
                case ELEMENT_TYPE.ELEMENT_TYPE_BOOLEAN: return typeof(System.Boolean);
                case ELEMENT_TYPE.ELEMENT_TYPE_CHAR: return typeof(System.Char);
                case ELEMENT_TYPE.ELEMENT_TYPE_I1: return typeof(System.SByte);
                case ELEMENT_TYPE.ELEMENT_TYPE_U1: return typeof(System.Byte);
                case ELEMENT_TYPE.ELEMENT_TYPE_I2: return typeof(System.Int16);
                case ELEMENT_TYPE.ELEMENT_TYPE_U2: return typeof(System.UInt16);
                case ELEMENT_TYPE.ELEMENT_TYPE_I4: return typeof(System.Int32);
                case ELEMENT_TYPE.ELEMENT_TYPE_U4: return typeof(System.UInt32);
                case ELEMENT_TYPE.ELEMENT_TYPE_I8: return typeof(System.Int64);
                case ELEMENT_TYPE.ELEMENT_TYPE_U8: return typeof(System.UInt64);
                case ELEMENT_TYPE.ELEMENT_TYPE_R4: return typeof(System.Single);
                case ELEMENT_TYPE.ELEMENT_TYPE_R8: return typeof(System.Double);
                case ELEMENT_TYPE.ELEMENT_TYPE_I: return typeof(System.IntPtr);
                case ELEMENT_TYPE.ELEMENT_TYPE_U: return typeof(System.UIntPtr);
                case ELEMENT_TYPE.ELEMENT_TYPE_STRING: return typeof(System.String);
                case ELEMENT_TYPE.ELEMENT_TYPE_OBJECT: return typeof(System.Object);
                case ELEMENT_TYPE.ELEMENT_TYPE_VOID: return typeof(void);
                case ELEMENT_TYPE.ELEMENT_TYPE_TYPEDBYREF: return typeof(System.TypedReference);
                default:
                    throw new Exception("Internal error in CLI File Reader library!");
            }
        }

        /// <summary>
        /// Return false.
        /// </summary>
        public override bool IsByRef
        {
            get { return false; }
        }

        /// <summary>
        /// Return false.
        /// </summary>
        public override bool IsPointer
        {
            get { return false; }
        }
    }
}
