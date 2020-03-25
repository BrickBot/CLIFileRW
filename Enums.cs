/// ------------------------------------------------------------
/// Copyright (c) 2002-2008 Antonio Cisternino (cisterni@di.unipi.it)
/// 
/// File: Enums.cs
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
using System.Reflection.Emit;
using MappedView;

namespace CLIFileRW {
  /// <summary>
  /// Name and ID of tables.
  /// </summary>
  public enum TableNames {
    Module = 0x00,
    TypeRef = 0x01,
    TypeDef = 0x02,
    Field = 0x04,
    Method = 0x06,
    Param = 0x08,
    InterfaceImpl = 0x09,
    MemberRef = 0x0A,
    Constant = 0x0B,
    CustomAttribute = 0x0C,
    FieldMarshal = 0x0D,
    DeclSecurity = 0x0E,
    ClassLayout = 0x0F,
    FieldLayout = 0x10,
    StandAloneSig = 0x11,
    EventMap = 0x12,
    Event = 0x14,
    PropertyMap = 0x15,
    Property = 0x17,
    MethodSemantics = 0x18,
    MethodImpl = 0x19,
    ModuleRef = 0x1A,
    TypeSpec = 0x1B,
    ImplMap = 0x1C,
    FieldRVA = 0x1D,
    Assembly = 0x20,
    AssemblyProcessor = 0x21,
    AssemblyOS = 0x22,
    AssemblyRef = 0x23,
    AssemblyRefProcessor = 0x24,
    AssemblyRefOS = 0x25,
    File = 0x26,
    ExportedType = 0x27,
    ManifestResource = 0x28,
    NestedClass = 0x29,
		GenericParam = 0x2A,
		MethodSpec = 0x2B,
		GenericParamConstraint = 0x2C,
    MaxNum = 0x2D
  }

  /// <summary>
  /// TypeDefOrRef Tag
  /// </summary>
  internal enum TypeDefOrRefTag {
    TypeDef = 0x00,
    TypeRef = 0x01,
    TypeSpec = 0x02,
		BaseType = 0x03
  }

  /// <summary>
  /// HasContent Tag
  /// </summary>
  internal enum HasConstantTag {
    FieldDef = 0x00,
    ParamDef = 0x01,
    Property = 0x02
  }

  /// <summary>
  /// HasCustomAttribute Tag
  /// </summary>
  internal enum HasCustomattributeTag {
    MethodDef = 0x00,
    FieldDef = 0x01,
    TypeRef	= 0x02,
    TypeDef	= 0x03,
    ParamDef = 0x04,
    InterfaceImpl = 0x05,
    MemberRef = 0x06,
    Module = 0x07,
    Permission = 0x08,
    Property = 0x09,
    Event = 0x0A,
    Signature = 0x0B,
    ModuleRef = 0x0C,
    TypeSpec = 0x0D,
    Assembly = 0x0E,
    AssemblyRef = 0x0F,
    File = 0x10,
    ExportedType = 0x11,
    ManifestResource = 0x12
  }

  /// <summary>
  /// HasFieldMarshal Tag
  /// </summary>
  internal enum HasFieldMarshalTag {
    FieldDef = 0x00,
    ParamDef = 0x01
  }

  /// <summary>
  /// HasDeclSecurity Tag
  /// </summary>
  internal enum HasDeclSecurityTag {
    TypeDef = 0x00,
    MethodDef = 0x01,
    Assembly = 0x02
  }

  /// <summary>
  /// MemberRefParent Tag
  /// </summary>
  internal enum MemberRefParentTag {
    NotUsed = 0x00,
    TypeRef = 0x01,
    ModuleRef = 0x02,
    MethodDef = 0x03,
    TypeSpec = 0x04
  }

  /// <summary>
  /// HasSemantics Tag
  /// </summary>
  internal enum HasSemanticsTag {
    Event = 0x00,
    Property = 0x01
  }

  /// <summary>
  /// MethodDefOrRef Tag
  /// </summary>
  internal enum MethodDefOrRefTag {
    MethodDef = 0x00,
    MemberRef = 0x01
  }

  /// <summary>
  /// MemberForwarded Tag
  /// </summary>
  internal enum MemberForwardedTag {
    FieldDef = 0x00,
    MethodDef = 0x01
  }

  /// <summary>
  /// Implementation Tag
  /// </summary>
  internal enum ImplementationTag {
    File = 0x00,
    AssemblyRef = 0x01,
    ExportedType = 0x02
  }

  /// <summary>
  /// CustomAttributeType Tag
  /// </summary>
  internal enum CustomAttributeTypeTag {
    NotUsed = 0x00,
    NotUsed2 = 0x01,
    MethodDef = 0x02,
    MemberRef = 0x03,
    NotUsed3 = 0x04
  }

  /// <summary>
  /// ResolutionScope Tag
  /// </summary>
  internal enum ResolutionScopeTag {
    Module = 0x00,
    ModuleRef = 0x01,
    AssemblyRef = 0x02,
    TypeRef	= 0x03
  }

  /// <summary>
  /// Used by signature blobs.
  /// </summary>
  public enum ELEMENT_TYPE {
    ELEMENT_TYPE_END = 0x00, // Marks end of a list
    ELEMENT_TYPE_VOID  = 0x01,
    ELEMENT_TYPE_BOOLEAN  = 0x02,
    ELEMENT_TYPE_CHAR  = 0x03,
    ELEMENT_TYPE_I1  = 0x04,
    ELEMENT_TYPE_U1  = 0x05,
    ELEMENT_TYPE_I2  = 0x06,
    ELEMENT_TYPE_U2  = 0x07,
    ELEMENT_TYPE_I4  = 0x08,
    ELEMENT_TYPE_U4  = 0x09,
    ELEMENT_TYPE_I8  = 0x0a,
    ELEMENT_TYPE_U8  = 0x0b,
    ELEMENT_TYPE_R4  = 0x0c,
    ELEMENT_TYPE_R8  = 0x0d,
    ELEMENT_TYPE_STRING = 0x0e,
    ELEMENT_TYPE_PTR = 0x0f, // Followed by <type> token
    ELEMENT_TYPE_BYREF  = 0x10, // Followed by <type> token
    ELEMENT_TYPE_VALUETYPE  = 0x11, // Followed by <type> token
    ELEMENT_TYPE_CLASS  = 0x12, // Followed by <type> token
    ELEMENT_TYPE_VAR = 0x13,     // a class type variable VAR <U1>
    ELEMENT_TYPE_ARRAY = 0x14, // <type> <rank> <boundsCount> <bound1> … <loCount> <lo1> …
    ELEMENT_TYPE_GENERICINST = 0x15,     // Generic type instantiation. Followed by type typearg-count type-1 ... type-n
    ELEMENT_TYPE_TYPEDBYREF = 0x16,
    ELEMENT_TYPE_I = 0x18, // System.IntPtr
    ELEMENT_TYPE_U  = 0x19, // System.UIntPtr
    ELEMENT_TYPE_FNPTR = 0x1b, // Followed by full method signature
    ELEMENT_TYPE_OBJECT = 0x1c, // System.Object
    ELEMENT_TYPE_SZARRAY = 0x1d, // Single-dim array with 0 lower bound
    ELEMENT_TYPE_MVAR = 0x1e,     // a method type variable MVAR <U1>
    ELEMENT_TYPE_CMOD_REQD = 0x1f, // Required modifier : followed by a TypeDef or TypeRef token
    ELEMENT_TYPE_CMOD_OPT = 0x20, // Optional modifier : followed by a TypeDef or TypeRef token
    ELEMENT_TYPE_INTERNAL = 0x21, // Implemented within the CLI

    // Note that this is the max of base type excluding modifiers
    ELEMENT_TYPE_MAX = 0x22,     // first invalid element type
 
    ELEMENT_TYPE_MODIFIER = 0x40, // Or'd with following element types
    ELEMENT_TYPE_SENTINEL = 0x41, // Sentinel for varargs method signature
    ELEMENT_TYPE_PINNED = 0x45 // Denotes a local variable that points at a pinned object
  }

  public enum CorTokenType {
    mdtModule = 0x00000000,
    mdtTypeRef = 0x01000000,
    mdtTypeDef = 0x02000000,
    mdtFieldDef = 0x04000000,
    mdtMethodDef = 0x06000000,
    mdtParamDef = 0x08000000,
    mdtInterfaceImpl = 0x09000000,
    mdtMemberRef = 0x0a000000,
    mdtCustomAttribute = 0x0c000000,
    mdtPermission = 0x0e000000,
    mdtSignature = 0x11000000,
    mdtEvent = 0x14000000,
    mdtProperty = 0x17000000,
    mdtModuleRef = 0x1a000000,
    mdtTypeSpec = 0x1b000000,
    mdtAssembly = 0x20000000,
    mdtAssemblyRef = 0x23000000,
    mdtFile = 0x26000000,
    mdtExportedType = 0x27000000,
    mdtManifestResource = 0x28000000,
    mdtGenericParam = 0x2a000000,
    mdtMethodSpec = 0x2b000000,
    mdtGenericParamConstraint = 0x2c000000,

    mdtString = 0x70000000,
    mdtName = 0x71000000,
    mdtBaseType = 0x72000000,       // Leave this on the high end value. This does not correspond to metadata table
  }


  /// <summary>
  /// This class is used to map opcodes into OpCode objects.
  /// </summary>
  internal class OpCodesMap {
    public static OpCode[] LowCodes;
    public static OpCode[] HighCodes;

    static OpCodesMap() {
      LowCodes = new OpCode[256];
      HighCodes = new OpCode[31];
      LowCodes[0] = OpCodes.Nop;
      LowCodes[1] = OpCodes.Break;
      LowCodes[2] = OpCodes.Ldarg_0;
      LowCodes[3] = OpCodes.Ldarg_1;
      LowCodes[4] = OpCodes.Ldarg_2;
      LowCodes[5] = OpCodes.Ldarg_3;
      LowCodes[6] = OpCodes.Ldloc_0;
      LowCodes[7] = OpCodes.Ldloc_1;
      LowCodes[8] = OpCodes.Ldloc_2;
      LowCodes[9] = OpCodes.Ldloc_3;
      LowCodes[10] = OpCodes.Stloc_0;
      LowCodes[11] = OpCodes.Stloc_1;
      LowCodes[12] = OpCodes.Stloc_2;
      LowCodes[13] = OpCodes.Stloc_3;
      LowCodes[14] = OpCodes.Ldarg_S;
      LowCodes[15] = OpCodes.Ldarga_S;
      LowCodes[16] = OpCodes.Starg_S;
      LowCodes[17] = OpCodes.Ldloc_S;
      LowCodes[18] = OpCodes.Ldloca_S;
      LowCodes[19] = OpCodes.Stloc_S;
      LowCodes[20] = OpCodes.Ldnull;
      LowCodes[21] = OpCodes.Ldc_I4_M1;
      LowCodes[22] = OpCodes.Ldc_I4_0;
      LowCodes[23] = OpCodes.Ldc_I4_1;
      LowCodes[24] = OpCodes.Ldc_I4_2;
      LowCodes[25] = OpCodes.Ldc_I4_3;
      LowCodes[26] = OpCodes.Ldc_I4_4;
      LowCodes[27] = OpCodes.Ldc_I4_5;
      LowCodes[28] = OpCodes.Ldc_I4_6;
      LowCodes[29] = OpCodes.Ldc_I4_7;
      LowCodes[30] = OpCodes.Ldc_I4_8;
      LowCodes[31] = OpCodes.Ldc_I4_S;
      LowCodes[32] = OpCodes.Ldc_I4;
      LowCodes[33] = OpCodes.Ldc_I8;
      LowCodes[34] = OpCodes.Ldc_R4;
      LowCodes[35] = OpCodes.Ldc_R8;
      LowCodes[37] = OpCodes.Dup;
      LowCodes[38] = OpCodes.Pop;
      LowCodes[39] = OpCodes.Jmp;
      LowCodes[40] = OpCodes.Call;
      LowCodes[41] = OpCodes.Calli;
      LowCodes[42] = OpCodes.Ret;
      LowCodes[43] = OpCodes.Br_S;
      LowCodes[44] = OpCodes.Brfalse_S;
      LowCodes[45] = OpCodes.Brtrue_S;
      LowCodes[46] = OpCodes.Beq_S;
      LowCodes[47] = OpCodes.Bge_S;
      LowCodes[48] = OpCodes.Bgt_S;
      LowCodes[49] = OpCodes.Ble_S;
      LowCodes[50] = OpCodes.Blt_S;
      LowCodes[51] = OpCodes.Bne_Un_S;
      LowCodes[52] = OpCodes.Bge_Un_S;
      LowCodes[53] = OpCodes.Bgt_Un_S;
      LowCodes[54] = OpCodes.Ble_Un_S;
      LowCodes[55] = OpCodes.Blt_Un_S;
      LowCodes[56] = OpCodes.Br;
      LowCodes[57] = OpCodes.Brfalse;
      LowCodes[58] = OpCodes.Brtrue;
      LowCodes[59] = OpCodes.Beq;
      LowCodes[60] = OpCodes.Bge;
      LowCodes[61] = OpCodes.Bgt;
      LowCodes[62] = OpCodes.Ble;
      LowCodes[63] = OpCodes.Blt;
      LowCodes[64] = OpCodes.Bne_Un;
      LowCodes[65] = OpCodes.Bge_Un;
      LowCodes[66] = OpCodes.Bgt_Un;
      LowCodes[67] = OpCodes.Ble_Un;
      LowCodes[68] = OpCodes.Blt_Un;
      LowCodes[69] = OpCodes.Switch;
      LowCodes[70] = OpCodes.Ldind_I1;
      LowCodes[71] = OpCodes.Ldind_U1;
      LowCodes[72] = OpCodes.Ldind_I2;
      LowCodes[73] = OpCodes.Ldind_U2;
      LowCodes[74] = OpCodes.Ldind_I4;
      LowCodes[75] = OpCodes.Ldind_U4;
      LowCodes[76] = OpCodes.Ldind_I8;
      LowCodes[77] = OpCodes.Ldind_I;
      LowCodes[78] = OpCodes.Ldind_R4;
      LowCodes[79] = OpCodes.Ldind_R8;
      LowCodes[80] = OpCodes.Ldind_Ref;
      LowCodes[81] = OpCodes.Stind_Ref;
      LowCodes[82] = OpCodes.Stind_I1;
      LowCodes[83] = OpCodes.Stind_I2;
      LowCodes[84] = OpCodes.Stind_I4;
      LowCodes[85] = OpCodes.Stind_I8;
      LowCodes[86] = OpCodes.Stind_R4;
      LowCodes[87] = OpCodes.Stind_R8;
      LowCodes[88] = OpCodes.Add;
      LowCodes[89] = OpCodes.Sub;
      LowCodes[90] = OpCodes.Mul;
      LowCodes[91] = OpCodes.Div;
      LowCodes[92] = OpCodes.Div_Un;
      LowCodes[93] = OpCodes.Rem;
      LowCodes[94] = OpCodes.Rem_Un;
      LowCodes[95] = OpCodes.And;
      LowCodes[96] = OpCodes.Or;
      LowCodes[97] = OpCodes.Xor;
      LowCodes[98] = OpCodes.Shl;
      LowCodes[99] = OpCodes.Shr;
      LowCodes[100] = OpCodes.Shr_Un;
      LowCodes[101] = OpCodes.Neg;
      LowCodes[102] = OpCodes.Not;
      LowCodes[103] = OpCodes.Conv_I1;
      LowCodes[104] = OpCodes.Conv_I2;
      LowCodes[105] = OpCodes.Conv_I4;
      LowCodes[106] = OpCodes.Conv_I8;
      LowCodes[107] = OpCodes.Conv_R4;
      LowCodes[108] = OpCodes.Conv_R8;
      LowCodes[109] = OpCodes.Conv_U4;
      LowCodes[110] = OpCodes.Conv_U8;
      LowCodes[111] = OpCodes.Callvirt;
      LowCodes[112] = OpCodes.Cpobj;
      LowCodes[113] = OpCodes.Ldobj;
      LowCodes[114] = OpCodes.Ldstr;
      LowCodes[115] = OpCodes.Newobj;
      LowCodes[116] = OpCodes.Castclass;
      LowCodes[117] = OpCodes.Isinst;
      LowCodes[118] = OpCodes.Conv_R_Un;
      LowCodes[121] = OpCodes.Unbox;
      LowCodes[122] = OpCodes.Throw;
      LowCodes[123] = OpCodes.Ldfld;
      LowCodes[124] = OpCodes.Ldflda;
      LowCodes[125] = OpCodes.Stfld;
      LowCodes[126] = OpCodes.Ldsfld;
      LowCodes[127] = OpCodes.Ldsflda;
      LowCodes[128] = OpCodes.Stsfld;
      LowCodes[129] = OpCodes.Stobj;
      LowCodes[130] = OpCodes.Conv_Ovf_I1_Un;
      LowCodes[131] = OpCodes.Conv_Ovf_I2_Un;
      LowCodes[132] = OpCodes.Conv_Ovf_I4_Un;
      LowCodes[133] = OpCodes.Conv_Ovf_I8_Un;
      LowCodes[134] = OpCodes.Conv_Ovf_U1_Un;
      LowCodes[135] = OpCodes.Conv_Ovf_U2_Un;
      LowCodes[136] = OpCodes.Conv_Ovf_U4_Un;
      LowCodes[137] = OpCodes.Conv_Ovf_U8_Un;
      LowCodes[138] = OpCodes.Conv_Ovf_I_Un;
      LowCodes[139] = OpCodes.Conv_Ovf_U_Un;
      LowCodes[140] = OpCodes.Box;
      LowCodes[141] = OpCodes.Newarr;
      LowCodes[142] = OpCodes.Ldlen;
      LowCodes[143] = OpCodes.Ldelema;
      LowCodes[144] = OpCodes.Ldelem_I1;
      LowCodes[145] = OpCodes.Ldelem_U1;
      LowCodes[146] = OpCodes.Ldelem_I2;
      LowCodes[147] = OpCodes.Ldelem_U2;
      LowCodes[148] = OpCodes.Ldelem_I4;
      LowCodes[149] = OpCodes.Ldelem_U4;
      LowCodes[150] = OpCodes.Ldelem_I8;
      LowCodes[151] = OpCodes.Ldelem_I;
      LowCodes[152] = OpCodes.Ldelem_R4;
      LowCodes[153] = OpCodes.Ldelem_R8;
      LowCodes[154] = OpCodes.Ldelem_Ref;
      LowCodes[155] = OpCodes.Stelem_I;
      LowCodes[156] = OpCodes.Stelem_I1;
      LowCodes[157] = OpCodes.Stelem_I2;
      LowCodes[158] = OpCodes.Stelem_I4;
      LowCodes[159] = OpCodes.Stelem_I8;
      LowCodes[160] = OpCodes.Stelem_R4;
      LowCodes[161] = OpCodes.Stelem_R8;
      LowCodes[162] = OpCodes.Stelem_Ref;
      LowCodes[163] = OpCodes.Ldelem;
      LowCodes[164] = OpCodes.Stelem;
      LowCodes[165] = OpCodes.Unbox_Any;
      LowCodes[179] = OpCodes.Conv_Ovf_I1;
      LowCodes[180] = OpCodes.Conv_Ovf_U1;
      LowCodes[181] = OpCodes.Conv_Ovf_I2;
      LowCodes[182] = OpCodes.Conv_Ovf_U2;
      LowCodes[183] = OpCodes.Conv_Ovf_I4;
      LowCodes[184] = OpCodes.Conv_Ovf_U4;
      LowCodes[185] = OpCodes.Conv_Ovf_I8;
      LowCodes[186] = OpCodes.Conv_Ovf_U8;
      LowCodes[194] = OpCodes.Refanyval;
      LowCodes[195] = OpCodes.Ckfinite;
      LowCodes[198] = OpCodes.Mkrefany;
      LowCodes[208] = OpCodes.Ldtoken;
      LowCodes[209] = OpCodes.Conv_U2;
      LowCodes[210] = OpCodes.Conv_U1;
      LowCodes[211] = OpCodes.Conv_I;
      LowCodes[212] = OpCodes.Conv_Ovf_I;
      LowCodes[213] = OpCodes.Conv_Ovf_U;
      LowCodes[214] = OpCodes.Add_Ovf;
      LowCodes[215] = OpCodes.Add_Ovf_Un;
      LowCodes[216] = OpCodes.Mul_Ovf;
      LowCodes[217] = OpCodes.Mul_Ovf_Un;
      LowCodes[218] = OpCodes.Sub_Ovf;
      LowCodes[219] = OpCodes.Sub_Ovf_Un;
      LowCodes[220] = OpCodes.Endfinally;
      LowCodes[221] = OpCodes.Leave;
      LowCodes[222] = OpCodes.Leave_S;
      LowCodes[223] = OpCodes.Stind_I;
      LowCodes[224] = OpCodes.Conv_U;
      LowCodes[248] = OpCodes.Prefix7;
      LowCodes[249] = OpCodes.Prefix6;
      LowCodes[250] = OpCodes.Prefix5;
      LowCodes[251] = OpCodes.Prefix4;
      LowCodes[252] = OpCodes.Prefix3;
      LowCodes[253] = OpCodes.Prefix2;
      LowCodes[254] = OpCodes.Prefix1;
      LowCodes[255] = OpCodes.Prefixref;
      HighCodes[0] = OpCodes.Arglist;
      HighCodes[1] = OpCodes.Ceq;
      HighCodes[2] = OpCodes.Cgt;
      HighCodes[3] = OpCodes.Cgt_Un;
      HighCodes[4] = OpCodes.Clt;
      HighCodes[5] = OpCodes.Clt_Un;
      HighCodes[6] = OpCodes.Ldftn;
      HighCodes[7] = OpCodes.Ldvirtftn;
      HighCodes[9] = OpCodes.Ldarg;
      HighCodes[10] = OpCodes.Ldarga;
      HighCodes[11] = OpCodes.Starg;
      HighCodes[12] = OpCodes.Ldloc;
      HighCodes[13] = OpCodes.Ldloca;
      HighCodes[14] = OpCodes.Stloc;
      HighCodes[15] = OpCodes.Localloc;
      HighCodes[17] = OpCodes.Endfilter;
      HighCodes[18] = OpCodes.Unaligned;
      HighCodes[19] = OpCodes.Volatile;
      HighCodes[20] = OpCodes.Tailcall;
      HighCodes[21] = OpCodes.Initobj;
      HighCodes[22] = OpCodes.Constrained;
      HighCodes[23] = OpCodes.Cpblk;
      HighCodes[24] = OpCodes.Initblk;
      HighCodes[26] = OpCodes.Rethrow;
      HighCodes[28] = OpCodes.Sizeof;
      HighCodes[29] = OpCodes.Refanytype;
      HighCodes[30] = OpCodes.Readonly;
    }
  }
}
