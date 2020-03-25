/// ------------------------------------------------------------
/// Copyright (c) 2002-2008 Antonio Cisternino (cisterni@di.unipi.it)
/// 
/// File: ILCursor.cs
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
    public class OperandStack<T>
    {
        private int sp;
        private System.Collections.Generic.List<T> data;

        public OperandStack()
        {
            this.sp = 0;
            this.data = new System.Collections.Generic.List<T>();
        }

        public OperandStack(int sz)
        {
            this.sp = 0;
            this.data = new System.Collections.Generic.List<T>(sz);
        }

        public int Count
        {
            get
            {
                return this.sp;
            }
        }

        public T Pop()
        {
            if (this.sp == 0)
                throw new InvalidOperationException("Stack is empty");
            T ret = this.data[this.sp - 1];
            this.data[this.sp - 1] = default(T);
            this.sp--;
            return ret;
        }

        public void Push(T v)
        {
            if (this.sp == this.data.Count)
                this.data.Add(v);
            else
                this.data[this.sp] = v;
            this.sp++;
        }

        public T Peek()
        {
            if (this.sp == 0)
                throw new InvalidOperationException("Stack is empty");
            return this.data[this.sp - 1];
        }

        public T this[int idx]
        {
            get
            {
                if (idx >= this.sp)
                    throw new InvalidOperationException("Not enough values on the operand stack");
                return this.data[this.sp - idx - 1];
            }
        }

        public void Clear()
        {
            this.sp = 0;
            this.data.Clear();
        }
    }

    /// <summary>
    /// This class abstracts the interface of the cursor to go through
    /// an IL stream. Inherited classes implement the access to a particular location.
    /// Notice that a substantial part of code is replicated across inherited classes.
    /// This can be improved.
    /// </summary>
    public class ILCursor
    {
        public interface ILStoreCursor
        {
            /// <summary>
            /// True if the cursor is at the beginning.
            /// </summary>
            bool BOF { get; }

            /// <summary>
            /// True if the cursor is at the end of the stream
            /// </summary>
            bool EOF { get; }

            /// <summary>
            /// Reset the cursor.
            /// </summary>
            void Reset();

            long Length { get; }

            long Offset { get; }

            void Advance(int i);

            short ToShort();

            int ToInt32();

            long ToInt64();

            sbyte ToSByte();

            byte ToByte();

            byte ToByte(int off);

            double ToDouble();

            float ToFloat();

            ILStoreCursor Clone();
        }

        /// <summary>
        /// Used to implement PushState and PopState.
        /// </summary>
        public struct State
        {
            /// <summary>
            /// Save the pos member.
            /// </summary>
            internal long pos;
            
            /// <summary>
            /// Internal cursor state.
            /// </summary>
            internal long off;
            
            /// <summary>
            /// Save current instruction
            /// </summary>
            internal ILInstruction Instr;
            
            /// <summary>
            /// Save current nextTarget
            /// </summary>
            internal int nextTarget;

            /// <summary>
            /// Save current lastStatement offset.
            /// </summary>
            internal long lastStatement;
            
            /// <summary>
            /// Save current label
            /// </summary>
            internal Target Label;
            
            /// <summary>
            /// This is used only for state given outside.
            /// It is necessary to recreate the cursor
            /// </summary>
            internal int MethodIdx;
            
            /// <summary>
            /// Used only to rebuild the cursor.
            /// </summary>
            internal CLIFile source;

            /// <summary>
            /// This index is used when the state is saved
            /// to be restored in a different cursor.
            /// -2 means that no track targets should be
            /// performed to restore the cursor.
            /// -1 means that the current instruction has
            /// no Label though TrackTargets() should be invoked.
            /// A valid index is the index on the array of labels
            /// (assuming that the array will be rebuilt as before!)
            /// </summary>
            internal int LabelIndex;

            /// <summary>
            /// Compare two ILCursor states.
            /// </summary>
            /// <param name="s1"></param>
            /// <param name="s2"></param>
            /// <returns>A value {&lt;,=,&gt;} 0 if s1 {precedes|is at same position|
            /// follows} s2.</returns>
            public static int Compare(State s1, State s2)
            {
                return (int)(s1.pos - s2.pos);
            }

            /// <summary>
            /// Test if two ILCursor states are comparable (are from the same method)
            /// </summary>
            /// <param name="s1"></param>
            /// <param name="s2"></param>
            /// <returns>true if the States relate to the same method</returns>
            public static bool Comparable(State s1, State s2)
            {
                return s1.MethodIdx == s2.MethodIdx && s1.source == s2.source;
            }

            /// <summary>
            /// Compare two ILCursor states (assuming they are comparable).
            /// </summary>
            /// <param name="s1"></param>
            /// <param name="s2"></param>
            /// <returns>true if s1 follows s2</returns>
            public static bool operator >(State s1, State s2)
            {
                return s1.pos > s2.pos;
            }
            /// <summary>
            /// Compare two ILCursor states (assuming they are comparable).
            /// </summary>
            /// <param name="s1"></param>
            /// <param name="s2"></param>
            /// <returns>true if s1 precedes s2</returns>
            public static bool operator <(State s1, State s2)
            {
                return s1.pos < s2.pos;
            }
            /// <summary>
            /// Compare two ILCursor states (assuming they are comparable).
            /// </summary>
            /// <param name="s1"></param>
            /// <param name="s2"></param>
            /// <returns>true if s1 is at the same position s2</returns>
            public static bool operator ==(State s1, State s2)
            {
                return s1.pos == s2.pos;
            }
            /// <summary>
            /// Compare two ILCursor states (assuming they are comparable).
            /// </summary>
            /// <param name="s1"></param>
            /// <param name="s2"></param>
            /// <returns>true if s1 is not at the same position s2</returns>
            public static bool operator !=(State s1, State s2)
            {
                return s1.pos != s2.pos;
            }

            /// <summary>
            /// Test for equality (reverting to operator==).
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                if (!(obj is State)) return false;
                return this == (State)obj;
            }


            /// <summary>
            /// Implemented only to avoid the warning of the compiler.
            /// </summary>
            /// <returns>The value computed from the inheriteed version</returns>
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

        }

        /// <summary>
        /// Represents a slot for stack interpretation.
        /// </summary>
        public struct StackSlot
        {
            public OperandCategory Category;
            public CLIType Type;
            public Target Source;
        }

        /// <summary>
        /// Position of current instruction.
        /// </summary>
        public long Position
        {
            get { return pos; }
        }

        /// <summary>
        /// Cursor to the store.
        /// </summary>
        private ILStoreCursor cur;

        /// <summary>
        /// Reference to the file being read
        /// </summary>
        private CLIFile File;

        /// <summary>
        /// Stack of saved states
        /// </summary>
        private Stack saved;

        /// <summary>
        /// Index of the method in the method table.
        /// We assume -1 when the method isn't available through the
        /// CLIFile.
        /// </summary>
        private int MethodIndex;

        /// <summary>
        /// Stack used to simulate the stack behaviour. It must be
        /// </summary>
        private OperandStack<StackSlot> AbstractStack;

        /// <summary>
        /// Used only when TrackStack is called
        /// </summary>
        private CLIType[] Locals;

        /// <summary>
        /// Used only when TrackStack is called
        /// </summary>
        private CLIType[] Arguments;

        /// <summary>
        /// Used only when TrackStack is called, it stores
        /// the beginning of the last statement for quick
        /// backtrack.
        /// </summary>
        private long lastStatement;

        internal ILCursor(int idx, ILStoreCursor c, CLIFile f)
        {
            this.MethodIndex = idx;
            this.cur = c;
            this.File = f;
            this.Instr = new ILInstruction();
            this.Labels = null;
            this.nextTarget = 0;
            this.Label = null;
            this.saved = null;
            this.AbstractStack = null;
            this.Locals = null;
            this.Arguments = null;
            this.lastStatement = 0;
        }

        /// <summary>
        /// Return a CLIFile.MethodBody given a reflection object.
        /// </summary>
        /// <param name="m">MethodInfo representing the method to be searched.</param>
        /// <returns>The method body associated with the method.</returns>
        public static MethodBody FromMethodInfo(MethodInfo m)
        {
            CLIFile f = CLIFile.Open(m.DeclaringType.Assembly.Location);
            MethodTableCursor cur = f[TableNames.Method].GetCursor() as MethodTableCursor;
            cur.Goto(m.MetadataToken & 0x00FFFFFF);
            return cur.MethodBody;
        }

        /// <summary>
        /// Position of the current instruction.
        /// </summary>
        private long pos;

        /// <summary>
        /// Positions of where instructions target of a branch are.
        /// </summary>
        internal Target[] Labels;

        /// <summary>
        /// Approximation of scopes of local variables.
        /// </summary>
        internal Target[] LocalsScope;

        /// <summary>
        /// Index in the array of Labels of the next target to be checked.
        /// </summary>
        private int nextTarget;

        /// <summary>
        /// Instruction read.
        /// </summary>
        public ILInstruction Instr;

        /// <summary>
        /// Not null if the current instruction is a target of one or more branches.
        /// </summary>
        public Target Label;

        /// <summary>
        /// Return the param size given the operand size.
        /// </summary>
        /// <param name="o">Operand type</param>
        /// <returns>The size of the operand, -1 if an error occurs.
        /// For the switch you have to multiply the size for the number
        /// of parameter.
        /// </returns>
        public static int ParamSize(OperandType o)
        {
            switch (o)
            {
                case OperandType.InlineNone:
                    return 0;

                case OperandType.InlinePhi:
                    throw new Exception("Obsolete: InlinePhi is a deprecated parameter type!");

                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineVar:
                case OperandType.ShortInlineI:
                    return 1;

                case OperandType.InlineVar:
                    return 2;

                case OperandType.InlineBrTarget:
                case OperandType.InlineField:
                case OperandType.InlineI:
                case OperandType.InlineMethod:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.ShortInlineR:
                    return 4;

                case OperandType.InlineI8:
                case OperandType.InlineR:
                    return 8;

                case OperandType.InlineSwitch:
                    return 4;
            }

            return -1;
        }

        /// <summary>
        /// Return the file reader used to obtain this cursor.
        /// It is useful for the ResolveParameter method.
        /// </summary>
        public CLIFile FileReader
        {
            get
            {
                return File;
            }
        }

        /// <summary>
        /// Enable stack tracking. It resets the cursor since the
        /// stack can be tracked only from the beginning.
        /// </summary>
        public void TrackStack(CLIType[] args, CLIType[] locals)
        {
            this.TrackStack(args, locals, -1);
        }
        
        /// <summary>
        /// Enable stack tracking. It resets the cursor since the
        /// stack can be tracked only from the beginning.
        /// </summary>
        public void TrackStack(CLIType[] args, CLIType[] locals, int maxStack)
        {
            this.AbstractStack = maxStack == -1 ? new OperandStack<StackSlot>() : new OperandStack<StackSlot>(maxStack);
            this.Locals = locals;
            this.Arguments = args;
            this.Reset();
        }

        /// <summary>
        /// True if the cursor is at the beginning.
        /// </summary>
        public bool BOF
        {
            get { return this.cur.BOF; }
        }

        /// <summary>
        /// True if the cursor is at the end of the stream
        /// </summary>
        public bool EOF
        {
            get { return this.cur.EOF; }
        }

        /// <summary>
        /// Return the targets found by TrackTargets.
        /// </summary>
        public Target[] Targets
        {
            get { return this.Labels; }
        }

        /// <summary>
        /// Reset the cursor.
        /// </summary>
        public void Reset()
        {
            cur.Reset();
            this.nextTarget = 0;
            if (this.AbstractStack != null)
            {
                this.AbstractStack.Clear();
                this.lastStatement = 0;
            }
        }

        private void UpdateAbstractStack(ILInstruction instr, long off, int n)
        {
            StackSlot s1 = new StackSlot(), s2 = new StackSlot(), s3 = new StackSlot();
            OPCODES op = (OPCODES)((instr.op.Value < 0 ? 256 : 0) + (instr.op.Value & 0xFF));

            switch (op)
            {
                case OPCODES.Add:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    s2 = this.AbstractStack.Pop();
                    s1 = this.AbstractStack.Pop();
                    switch (s1.Category)
                    {
                        case OperandCategory.int32:
                        case OperandCategory.nativeInt:
                            switch (s2.Category)
                            {
                                case OperandCategory.int32:
                                    s3.Category = s1.Category == OperandCategory.nativeInt ? OperandCategory.nativeInt : OperandCategory.int32;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                case OperandCategory.nativeInt:
                                    s3.Category = OperandCategory.nativeInt;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                case OperandCategory.Ref: // Not verifiable
                                    s3.Category = OperandCategory.Ref;
                                    s3.Type = s2.Type;
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.int64:
                            switch (s2.Category)
                            {
                                case OperandCategory.int64:
                                    s3.Category = OperandCategory.int64;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.F:
                            switch (s2.Category)
                            {
                                case OperandCategory.F:
                                    s3.Category = OperandCategory.F;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.Ref:
                            switch (s2.Category)
                            {
                                case OperandCategory.int32:
                                case OperandCategory.nativeInt:
                                    s3.Category = OperandCategory.Ref;
                                    s3.Type = s1.Type;
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                    }
                    this.AbstractStack.Push(s3);
                    break;

                case OPCODES.Add_Ovf_Un:
                case OPCODES.Add_Ovf:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    s2 = this.AbstractStack.Pop();
                    s1 = this.AbstractStack.Pop();
                    switch (s1.Category)
                    {
                        case OperandCategory.int32:
                        case OperandCategory.nativeInt:
                            switch (s2.Category)
                            {
                                case OperandCategory.int32:
                                    s3.Category = s1.Category == OperandCategory.nativeInt ? OperandCategory.nativeInt : OperandCategory.int32;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                case OperandCategory.nativeInt:
                                    s3.Category = OperandCategory.nativeInt;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                case OperandCategory.Ref: // Not verifiable
                                    s3.Category = OperandCategory.Ref;
                                    s3.Type = s2.Type;
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.int64:
                            switch (s2.Category)
                            {
                                case OperandCategory.int64:
                                    s3.Category = OperandCategory.int64;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.Ref:
                            switch (s2.Category)
                            {
                                case OperandCategory.int32:
                                case OperandCategory.nativeInt:
                                    s3.Category = OperandCategory.Ref;
                                    s3.Type = s1.Type;
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                    }
                    this.AbstractStack.Push(s3);
                    break;

                case OPCODES.And:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    s2 = this.AbstractStack.Pop();
                    s1 = this.AbstractStack.Pop();
                    switch (s1.Category)
                    {
                        case OperandCategory.int32:
                        case OperandCategory.nativeInt:
                            switch (s2.Category)
                            {
                                case OperandCategory.int32:
                                    s3.Category = s1.Category == OperandCategory.nativeInt ? OperandCategory.nativeInt : OperandCategory.int32;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                case OperandCategory.nativeInt:
                                    s3.Category = OperandCategory.nativeInt;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.int64:
                            switch (s2.Category)
                            {
                                case OperandCategory.int64:
                                    s3.Category = OperandCategory.int64;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                    }
                    this.AbstractStack.Push(s3);
                    break;

                case OPCODES.Arglist:
                    s1.Category = OperandCategory.None;
                    s1.Source = new Target(off);
                    // This should be a System.RuntimeArgumentHandle
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I);
                    this.AbstractStack.Push(s1);
                    break;

                case OPCODES.Beq:
                case OPCODES.Beq_S:
                case OPCODES.Bge:
                case OPCODES.Bge_S:
                case OPCODES.Bge_Un:
                case OPCODES.Bge_Un_S:
                case OPCODES.Bgt:
                case OPCODES.Bgt_S:
                case OPCODES.Bgt_Un:
                case OPCODES.Bgt_Un_S:
                case OPCODES.Ble:
                case OPCODES.Ble_S:
                case OPCODES.Ble_Un:
                case OPCODES.Ble_Un_S:
                case OPCODES.Blt:
                case OPCODES.Blt_S:
                case OPCODES.Blt_Un:
                case OPCODES.Blt_Un_S:
                case OPCODES.Bne_Un:
                case OPCODES.Bne_Un_S:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    // We don't do verification here!
                    break;

                case OPCODES.Box:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Category = OperandCategory.O;
                    s1.Source = new Target(off);
                    s1.Type = new CLIType(this.File, CompoundType.FromFToken(this.File, instr.par.iv, null));
                    this.AbstractStack.Push(s1);
                    break;

                case OPCODES.Br:
                case OPCODES.Br_S:
                case OPCODES.Break:
                    break;

                case OPCODES.Brfalse:
                case OPCODES.Brfalse_S:
                case OPCODES.Brtrue:
                case OPCODES.Brtrue_S:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    s1 = this.AbstractStack.Pop();
                    // We don't do verification here!
                    break;

                case OPCODES.Call:
                    {
                        MethodDesc md = new MethodDesc(this.File, instr.par.iv);
                        MethodSig sig = md.Signature;
                        if (sig.HasThis && !sig.ExplicitThis)
                            this.AbstractStack.Pop();

                        for (int i = 0; i < sig.Count; i++)
                            this.AbstractStack.Pop();

                        if (!(sig.ReturnType.type is BaseType) || ((BaseType)sig.ReturnType.type).Type != ELEMENT_TYPE.ELEMENT_TYPE_VOID)
                        {
                            s1.Source = new Target(off);
                            s1.Type = sig.ReturnType;
                            s1.Category = OperandCategory.O;
                            if (s1.Type.type is BaseType)
                                s1.Category = ((BaseType)s1.Type.type).Category;
                            this.AbstractStack.Push(s1);
                        }
                        break;
                    }

                case OPCODES.Calli:
                    {
                        Debug.Assert(((TableNames)(instr.par.iv >> 24)) == TableNames.StandAloneSig, "Must be a standalone signature!");
                        StandAloneSigTableCursor cur = this.File[TableNames.StandAloneSig].GetCursor() as StandAloneSigTableCursor;
                        cur.Goto(instr.par.iv & 0x00FFFFFF);
                        MethodSig sig = new MethodSig(this.File, this.File.Blob[cur.Signature], MethodSig.SigType.StandaloneSig);
                        if (sig.HasThis && sig.ExplicitThis)
                            throw new Exception("Calli shouldn't have this pointer!");

                        for (int i = 0; i < sig.Count; i++)
                            this.AbstractStack.Pop();

                        // For the function pointer.
                        this.AbstractStack.Pop();

                        if (!(sig.ReturnType.type is BaseType) || ((BaseType)sig.ReturnType.type).Type != ELEMENT_TYPE.ELEMENT_TYPE_VOID)
                        {
                            s1.Source = new Target(off);
                            s1.Type = sig.ReturnType;
                            s1.Category = OperandCategory.O;
                            if (s1.Type.type is BaseType)
                                s1.Category = ((BaseType)s1.Type.type).Category;
                            this.AbstractStack.Push(s1);
                        }
                        break;
                    }

                case OPCODES.Callvirt:
                    {
                        MethodDesc md = new MethodDesc(this.File, instr.par.iv);
                        MethodSig sig = md.Signature;

                        this.AbstractStack.Pop();

                        for (int i = 0; i < sig.Count; i++)
                            this.AbstractStack.Pop();

                        if (!(sig.ReturnType.type is BaseType) || ((BaseType)sig.ReturnType.type).Type != ELEMENT_TYPE.ELEMENT_TYPE_VOID)
                        {
                            s1.Source = new Target(off);
                            s1.Type = sig.ReturnType;
                            s1.Category = OperandCategory.O;
                            if (s1.Type.type is BaseType)
                                s1.Category = ((BaseType)s1.Type.type).Category;
                            this.AbstractStack.Push(s1);
                        }
                        break;
                    }
                case OPCODES.Castclass:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Category = OperandCategory.O;
                    s1.Source = new Target(off);
                    s1.Type = new CLIType(this.File, CompoundType.FromFToken(this.File, instr.par.iv, null));
                    this.AbstractStack.Push(s1);
                    break;

                case OPCODES.Ceq:
                case OPCODES.Cgt:
                case OPCODES.Cgt_Un:
                case OPCODES.Clt:
                case OPCODES.Clt_Un:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    s2 = this.AbstractStack.Pop();
                    s1 = this.AbstractStack.Pop();
                    // We don't do verification here!
                    s3.Category = OperandCategory.int32;
                    s3.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I4);
                    s3.Source = new Target(off);
                    this.AbstractStack.Push(s3);
                    break;

                case OPCODES.Ckfinite:
                    break;

                case OPCODES.Constrained:
                    break;

                case OPCODES.Conv_I:
                case OPCODES.Conv_Ovf_I:
                case OPCODES.Conv_Ovf_I_Un:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    s1 = this.AbstractStack.Pop();
                    // We don't do verification here!
                    s2.Category = OperandCategory.int32;
                    s2.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I);
                    s2.Source = new Target(off);
                    this.AbstractStack.Push(s2);
                    break;
                case OPCODES.Conv_I1:
                case OPCODES.Conv_Ovf_I1:
                case OPCODES.Conv_Ovf_I1_Un:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    s1 = this.AbstractStack.Pop();
                    // We don't do verification here!
                    s2.Category = OperandCategory.int32;
                    s2.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I1);
                    s2.Source = new Target(off);
                    this.AbstractStack.Push(s2);
                    break;
                case OPCODES.Conv_I2:
                case OPCODES.Conv_Ovf_I2:
                case OPCODES.Conv_Ovf_I2_Un:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    s1 = this.AbstractStack.Pop();
                    // We don't do verification here!
                    s2.Category = OperandCategory.int32;
                    s2.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I2);
                    s2.Source = new Target(off);
                    this.AbstractStack.Push(s2);
                    break;
                case OPCODES.Conv_I4:
                case OPCODES.Conv_Ovf_I4:
                case OPCODES.Conv_Ovf_I4_Un:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    s1 = this.AbstractStack.Pop();
                    // We don't do verification here!
                    s2.Category = OperandCategory.int32;
                    s2.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I4);
                    s2.Source = new Target(off);
                    this.AbstractStack.Push(s2);
                    break;
                case OPCODES.Conv_I8:
                case OPCODES.Conv_Ovf_I8:
                case OPCODES.Conv_Ovf_I8_Un:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    s1 = this.AbstractStack.Pop();
                    // We don't do verification here!
                    s2.Category = OperandCategory.int64;
                    s2.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I8);
                    s2.Source = new Target(off);
                    this.AbstractStack.Push(s2);
                    break;
                case OPCODES.Conv_R_Un: // FIXME: Check this, could be also R8...
                case OPCODES.Conv_R4:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    s1 = this.AbstractStack.Pop();
                    // We don't do verification here!
                    s2.Category = OperandCategory.F;
                    s2.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_R4);
                    s2.Source = new Target(off);
                    this.AbstractStack.Push(s2);
                    break;
                case OPCODES.Conv_R8:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    s1 = this.AbstractStack.Pop();
                    // We don't do verification here!
                    s2.Category = OperandCategory.F;
                    s2.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_R8);
                    s2.Source = new Target(off);
                    this.AbstractStack.Push(s2);
                    break;
                case OPCODES.Conv_U:
                case OPCODES.Conv_Ovf_U:
                case OPCODES.Conv_Ovf_U_Un:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    s1 = this.AbstractStack.Pop();
                    // We don't do verification here!
                    s2.Category = OperandCategory.int32;
                    s2.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_U);
                    s2.Source = new Target(off);
                    this.AbstractStack.Push(s2);
                    break;
                case OPCODES.Conv_U1:
                case OPCODES.Conv_Ovf_U1:
                case OPCODES.Conv_Ovf_U1_Un:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    s1 = this.AbstractStack.Pop();
                    // We don't do verification here!
                    s2.Category = OperandCategory.int32;
                    s2.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_U1);
                    s2.Source = new Target(off);
                    this.AbstractStack.Push(s2);
                    break;
                case OPCODES.Conv_U2:
                case OPCODES.Conv_Ovf_U2:
                case OPCODES.Conv_Ovf_U2_Un:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    s1 = this.AbstractStack.Pop();
                    // We don't do verification here!
                    s2.Category = OperandCategory.int32;
                    s2.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_U2);
                    s2.Source = new Target(off);
                    this.AbstractStack.Push(s2);
                    break;
                case OPCODES.Conv_U4:
                case OPCODES.Conv_Ovf_U4:
                case OPCODES.Conv_Ovf_U4_Un:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    s1 = this.AbstractStack.Pop();
                    // We don't do verification here!
                    s2.Category = OperandCategory.int32;
                    s2.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_U4);
                    s2.Source = new Target(off);
                    this.AbstractStack.Push(s2);
                    break;
                case OPCODES.Conv_U8:
                case OPCODES.Conv_Ovf_U8:
                case OPCODES.Conv_Ovf_U8_Un:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    s1 = this.AbstractStack.Pop();
                    // We don't do verification here!
                    s2.Category = OperandCategory.int64;
                    s2.Source = new Target(off);
                    s2.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_U8);
                    this.AbstractStack.Push(s2);
                    break;
                case OPCODES.Cpblk:
                    if (this.AbstractStack.Count < 3)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    // We don't do verification here!
                    break;
                case OPCODES.Cpobj:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    // We don't do verification here!
                    break;
                case OPCODES.Div:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    s2 = this.AbstractStack.Pop();
                    s1 = this.AbstractStack.Pop();
                    switch (s1.Category)
                    {
                        case OperandCategory.int32:
                        case OperandCategory.nativeInt:
                            switch (s2.Category)
                            {
                                case OperandCategory.int32:
                                    s3.Category = s1.Category == OperandCategory.nativeInt ? OperandCategory.nativeInt : OperandCategory.int32;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                case OperandCategory.nativeInt:
                                    s3.Category = OperandCategory.nativeInt;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.int64:
                            switch (s2.Category)
                            {
                                case OperandCategory.int64:
                                    s3.Category = OperandCategory.int64;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.F:
                            switch (s2.Category)
                            {
                                case OperandCategory.F:
                                    s3.Category = OperandCategory.F;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                    }
                    this.AbstractStack.Push(s3);
                    break;

                case OPCODES.Div_Un:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    s2 = this.AbstractStack.Pop();
                    s1 = this.AbstractStack.Pop();
                    switch (s1.Category)
                    {
                        case OperandCategory.int32:
                        case OperandCategory.nativeInt:
                            switch (s2.Category)
                            {
                                case OperandCategory.int32:
                                    s3.Category = s1.Category == OperandCategory.nativeInt ? OperandCategory.nativeInt : OperandCategory.int32;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                case OperandCategory.nativeInt:
                                    s3.Category = OperandCategory.nativeInt;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.int64:
                            switch (s2.Category)
                            {
                                case OperandCategory.int64:
                                    s3.Category = OperandCategory.int64;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                    }
                    this.AbstractStack.Push(s3);
                    break;

                case OPCODES.Dup:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    s1 = this.AbstractStack.Peek();
                    s1.Source = new Target(off);
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Endfilter:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    s1 = this.AbstractStack.Pop();
                    // We don't do verification here!
                    break;
                case OPCODES.Endfinally:
                    break;
                case OPCODES.Initblk:
                    if (this.AbstractStack.Count < 3)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    // We don't do verification here!
                    break;
                case OPCODES.Initobj:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    break;

                case OPCODES.Isinst:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Category = OperandCategory.O;
                    s1.Source = new Target(off);
                    // FIXME: Check for nullness
                    s1.Type = new CLIType(this.File, CompoundType.FromFToken(this.File, instr.par.iv, null));
                    this.AbstractStack.Push(s1);
                    break;

                case OPCODES.Jmp:
                    break;
                case OPCODES.Ldarg_0:
                    instr.par.iv = 0;
                    goto LDARG;
                case OPCODES.Ldarg_1:
                    instr.par.iv = 1;
                    goto LDARG;
                case OPCODES.Ldarg_2:
                    instr.par.iv = 2;
                    goto LDARG;
                case OPCODES.Ldarg_3:
                    instr.par.iv = 3;
                    goto LDARG;
                case OPCODES.Ldarg_S:
                    instr.par.iv = instr.par.bv;
                    goto LDARG;
                case OPCODES.Ldarg:
                    instr.par.iv = instr.par.sv;
                LDARG:
                    s1.Source = new Target(off);
                    s1.Type = this.Arguments[instr.par.iv];
                    s1.Category = OperandCategory.O;
                    if (s1.Type.type is BaseType)
                        s1.Category = ((BaseType)s1.Type.type).Category;
                    this.AbstractStack.Push(s1);
                    break;

                case OPCODES.Ldarga_S:
                    instr.par.iv = instr.par.bv;
                    goto LDARGA;
                case OPCODES.Ldarga:
                    instr.par.iv = instr.par.sv;
                LDARGA:
                    s1.Source = new Target(off);
                    s1.Type = this.Arguments[instr.par.iv];
                    s1.Category = OperandCategory.Ref;
                    if (s1.Type.type is BaseType)
                        s1.Category = ((BaseType)s1.Type.type).Category;
                    s1.Type = new CLIType(this.File, new ByRefType(s1.Type.type));
                    this.AbstractStack.Push(s1);
                    break;

                case OPCODES.Ldc_I4:
                case OPCODES.Ldc_I4_0:
                case OPCODES.Ldc_I4_1:
                case OPCODES.Ldc_I4_2:
                case OPCODES.Ldc_I4_3:
                case OPCODES.Ldc_I4_4:
                case OPCODES.Ldc_I4_5:
                case OPCODES.Ldc_I4_6:
                case OPCODES.Ldc_I4_7:
                case OPCODES.Ldc_I4_8:
                case OPCODES.Ldc_I4_M1:
                case OPCODES.Ldc_I4_S:
                    s1.Category = OperandCategory.int32;
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I4);
                    s1.Source = new Target(off);
                    this.AbstractStack.Push(s1);
                    break;

                case OPCODES.Ldc_I8:
                    s1.Category = OperandCategory.int64;
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I8);
                    s1.Source = new Target(off);
                    this.AbstractStack.Push(s1);
                    break;

                case OPCODES.Ldc_R4:
                    s1.Category = OperandCategory.F;
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_R4);
                    s1.Source = new Target(off);
                    this.AbstractStack.Push(s1);
                    break;

                case OPCODES.Ldc_R8:
                    s1.Category = OperandCategory.F;
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_R8);
                    s1.Source = new Target(off);
                    this.AbstractStack.Push(s1);
                    break;

                case OPCODES.Ldelem:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Category = OperandCategory.O;
                    s1.Type = new CLIType(this.File, CompoundType.FromFToken(this.File, instr.par.iv, null));
                    this.AbstractStack.Push(s1);
                    break;

                case OPCODES.Ldelem_I:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I);
                    s1.Category = OperandCategory.nativeInt;
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldelem_I1:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I1);
                    s1.Category = OperandCategory.int32;
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldelem_I2:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I2);
                    s1.Category = OperandCategory.int32;
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldelem_I4:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I4);
                    s1.Category = OperandCategory.int32;
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldelem_I8:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I8);
                    s1.Category = OperandCategory.int64;
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldelem_R4:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_R4);
                    s1.Category = OperandCategory.F;
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldelem_R8:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_R8);
                    s1.Category = OperandCategory.F;
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldelem_Ref:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1 = this.AbstractStack.Pop();
                    if (!(s1.Type.type is ArrayType))
                        throw new Exception("Invalid stack state!");
                    s2.Source = new Target(off);
                    s2.Type = new CLIType(this.File, ((ArrayType)s1.Type.type).Type);
                    s2.Category = OperandCategory.O;
                    this.AbstractStack.Push(s2);
                    break;
                case OPCODES.Ldelem_U1:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_U1);
                    s1.Category = OperandCategory.int32;
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldelem_U2:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_U2);
                    s1.Category = OperandCategory.int32;
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldelem_U4:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_U4);
                    s1.Category = OperandCategory.int32;
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldelema:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Category = OperandCategory.Ref;
                    s1.Type = new CLIType(this.File, new ByRefType(CompoundType.FromFToken(this.File, instr.par.iv, null)));
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldfld:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Category = OperandCategory.O;
                    s1.Source = new Target(off);
                    s1.Type = (new FieldDesc(this.File, instr.par.iv)).Type;
                    if (s1.Type.type is BaseType)
                        s1.Category = ((BaseType)s1.Type.type).Category;
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldflda:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Category = OperandCategory.Ref;
                    s1.Source = new Target(off);
                    s1.Type = (new FieldDesc(this.File, instr.par.iv)).Type;
                    if (s1.Type.type is BaseType)
                        s1.Category = ((BaseType)s1.Type.type).Category;
                    s1.Type = new CLIType(this.File, new ByRefType(s1.Type.type));
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldftn:
                    {
                        MethodDesc md = new MethodDesc(this.File, instr.par.iv);

                        s1.Source = new Target(off);
                        // FIXME: Do we want to retain the origin of the pointer function?
                        s1.Type = new CLIType(this.File, new FunPointerType(this.File, md.Signature));
                        s1.Category = OperandCategory.nativeInt;
                        this.AbstractStack.Push(s1);
                        break;
                    }
                case OPCODES.Ldind_I:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I);
                    s1.Category = OperandCategory.nativeInt;
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldind_I1:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I1);
                    s1.Category = OperandCategory.int32;
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldind_I2:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I2);
                    s1.Category = OperandCategory.int32;
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldind_I4:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I4);
                    s1.Category = OperandCategory.int32;
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldind_I8:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I8);
                    s1.Category = OperandCategory.int64;
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldind_R4:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_R4);
                    s1.Category = OperandCategory.F;
                    this.AbstractStack.Push(s1);
                    break;

                case OPCODES.Ldind_R8:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_R8);
                    s1.Category = OperandCategory.F;
                    this.AbstractStack.Push(s1);
                    break;

                case OPCODES.Ldind_Ref:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    // FIXME: I think this can be improved
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_OBJECT);
                    s1.Category = OperandCategory.O;
                    this.AbstractStack.Push(s1);
                    break;

                case OPCODES.Ldind_U1:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_U1);
                    s1.Category = OperandCategory.int32;
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldind_U2:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_U2);
                    s1.Category = OperandCategory.int32;
                    this.AbstractStack.Push(s1);
                    break;

                case OPCODES.Ldind_U4:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_U4);
                    s1.Category = OperandCategory.int32;
                    this.AbstractStack.Push(s1);
                    break;

                case OPCODES.Ldlen:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_U);
                    s1.Category = OperandCategory.nativeInt;
                    this.AbstractStack.Push(s1);
                    break;

                case OPCODES.Ldloc_0:
                    instr.par.iv = 0;
                    goto LDLOC;
                case OPCODES.Ldloc_1:
                    instr.par.iv = 1;
                    goto LDLOC;
                case OPCODES.Ldloc_2:
                    instr.par.iv = 2;
                    goto LDLOC;
                case OPCODES.Ldloc_3:
                    instr.par.iv = 3;
                    goto LDLOC;
                case OPCODES.Ldloc_S:
                    instr.par.iv = instr.par.bv;
                    goto LDLOC;
                case OPCODES.Ldloc:
                    instr.par.iv = instr.par.sv;
                LDLOC:
                    s1.Source = new Target(off);
                    s1.Type = this.Locals[instr.par.iv];
                    s1.Category = OperandCategory.O;
                    if (s1.Type.type is BaseType)
                        s1.Category = ((BaseType)s1.Type.type).Category;
                    this.AbstractStack.Push(s1);
                    break;

                case OPCODES.Ldloca_S:
                    instr.par.iv = instr.par.bv;
                    goto LDLOCA;
                case OPCODES.Ldloca:
                    instr.par.iv = instr.par.sv;
                LDLOCA:
                    s1.Source = new Target(off);
                    s1.Type = this.Locals[instr.par.iv];
                    s1.Category = OperandCategory.Ref;
                    if (s1.Type.type is BaseType)
                        s1.Category = ((BaseType)s1.Type.type).Category;
                    s1.Type = new CLIType(this.File, new ByRefType(s1.Type.type));
                    this.AbstractStack.Push(s1);
                    break;

                case OPCODES.Ldnull:
                    s1.Category = OperandCategory.O;
                    s1.Source = new Target(off);
                    // FIXME: Should we introduce a null value?
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_OBJECT);
                    this.AbstractStack.Push(s1);
                    break;

                case OPCODES.Ldobj:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Category = OperandCategory.O;
                    s1.Source = new Target(off);
                    s1.Type = new CLIType(this.File, CompoundType.FromFToken(this.File, instr.par.iv, null));
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldsfld:
                    s1.Category = OperandCategory.O;
                    s1.Source = new Target(off);
                    s1.Type = (new FieldDesc(this.File, instr.par.iv)).Type;
                    if (s1.Type.type is BaseType)
                        s1.Category = ((BaseType)s1.Type.type).Category;
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldsflda:
                    s1.Category = OperandCategory.Ref;
                    s1.Source = new Target(off);
                    s1.Type = (new FieldDesc(this.File, instr.par.iv)).Type;
                    if (s1.Type.type is BaseType)
                        s1.Category = ((BaseType)s1.Type.type).Category;
                    s1.Type = new CLIType(this.File, new ByRefType(s1.Type.type));
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldstr:
                    s1.Category = OperandCategory.O;
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_STRING);
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldtoken:
                    s1.Category = OperandCategory.nativeInt;
                    s1.Source = new Target(off);
                    // This is a runtime handle!!!
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I);
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Ldvirtftn:
                    {
                        MethodDesc md = new MethodDesc(this.File, instr.par.iv);

                        s1.Source = new Target(off);
                        // FIXME: Do we want to retain the origin of the pointer function?
                        s1.Type = new CLIType(this.File, new FunPointerType(this.File, md.Signature));
                        s1.Category = OperandCategory.nativeInt;
                        this.AbstractStack.Push(s1);
                        break;
                    }
                case OPCODES.Leave:
                case OPCODES.Leave_S:
                    this.AbstractStack.Clear();
                    break;
                case OPCODES.Localloc:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I);
                    s1.Category = OperandCategory.nativeInt;
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Mkrefany:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Source = new Target(off);
                    // Here we simulate the behavior of mkrefany!
                    s1.Type = new CLIType(this.File, new ByRefType(CompoundType.FromFToken(this.File, instr.par.iv, null)));
                    s1.Category = OperandCategory.Ref;
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Mul:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    s2 = this.AbstractStack.Pop();
                    s1 = this.AbstractStack.Pop();
                    switch (s1.Category)
                    {
                        case OperandCategory.int32:
                        case OperandCategory.nativeInt:
                            switch (s2.Category)
                            {
                                case OperandCategory.int32:
                                    s3.Category = s1.Category == OperandCategory.nativeInt ? OperandCategory.nativeInt : OperandCategory.int32;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                case OperandCategory.nativeInt:
                                    s3.Category = OperandCategory.nativeInt;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.int64:
                            switch (s2.Category)
                            {
                                case OperandCategory.int64:
                                    s3.Category = OperandCategory.int64;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.F:
                            switch (s2.Category)
                            {
                                case OperandCategory.F:
                                    s3.Category = OperandCategory.F;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                    }
                    this.AbstractStack.Push(s3);
                    break;

                case OPCODES.Mul_Ovf:
                case OPCODES.Mul_Ovf_Un:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    s2 = this.AbstractStack.Pop();
                    s1 = this.AbstractStack.Pop();
                    switch (s1.Category)
                    {
                        case OperandCategory.int32:
                        case OperandCategory.nativeInt:
                            switch (s2.Category)
                            {
                                case OperandCategory.int32:
                                    s3.Category = s1.Category == OperandCategory.nativeInt ? OperandCategory.nativeInt : OperandCategory.int32;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                case OperandCategory.nativeInt:
                                    s3.Category = OperandCategory.nativeInt;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.int64:
                            switch (s2.Category)
                            {
                                case OperandCategory.int64:
                                    s3.Category = OperandCategory.int64;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                    }
                    this.AbstractStack.Push(s3);
                    break;

                case OPCODES.Neg:
                    // Leave stack unchanged (as types)
                    break;
                case OPCODES.Newarr:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Category = OperandCategory.O;
                    s1.Source = new Target(off);
                    s1.Type = new CLIType(this.File, new ArrayType(CompoundType.FromFToken(this.File, instr.par.iv, null)));
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Newobj:
                    {
                        MethodDesc md = new MethodDesc(this.File, instr.par.iv);
                        MethodSig sig = md.Signature;

                        for (int i = 0; i < sig.Count; i++)
                            this.AbstractStack.Pop();

                        s1.Category = OperandCategory.O;
                        s1.Source = new Target(off);
                        s1.Type = md.GetParent();
                        this.AbstractStack.Push(s1);
                        break;
                    }
                case OPCODES.Nop:
                    break;

                case OPCODES.Not:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    break;

                case OPCODES.Or:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    s2 = this.AbstractStack.Pop();
                    s1 = this.AbstractStack.Pop();
                    switch (s1.Category)
                    {
                        case OperandCategory.int32:
                        case OperandCategory.nativeInt:
                            switch (s2.Category)
                            {
                                case OperandCategory.int32:
                                    s3.Category = s1.Category == OperandCategory.nativeInt ? OperandCategory.nativeInt : OperandCategory.int32;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                case OperandCategory.nativeInt:
                                    s3.Category = OperandCategory.nativeInt;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.int64:
                            switch (s2.Category)
                            {
                                case OperandCategory.int64:
                                    s3.Category = OperandCategory.int64;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                    }
                    this.AbstractStack.Push(s3);
                    break;

                case OPCODES.Pop:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    break;
                case OPCODES.Prefix1:
                case OPCODES.Prefix2:
                case OPCODES.Prefix3:
                case OPCODES.Prefix4:
                case OPCODES.Prefix5:
                case OPCODES.Prefix6:
                case OPCODES.Prefix7:
                case OPCODES.Prefixref:
                    throw new Exception("Reserved instruction!");

                case OPCODES.Readonly:
                    break;

                case OPCODES.Refanytype:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    s1 = this.AbstractStack.Pop();
                    if (!(s1.Type.type is ByRefType))
                        throw new Exception("Invalid value on top of the stack");
                    s2.Source = new Target(off);
                    s2.Type = new CLIType(this.File, ((ByRefType)s1.Type.type).Type);
                    s2.Category = s2.Type.type is BaseType ? ((BaseType)s2.Type.type).Category : OperandCategory.O;
                    this.AbstractStack.Push(s2);
                    break;

                case OPCODES.Refanyval:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    s1 = this.AbstractStack.Pop();
                    if (!(s1.Type.type is ByRefType))
                        throw new Exception("Invalid value on top of the stack");
                    s2.Source = new Target(off);
                    s2.Type = s1.Type;
                    s2.Category = OperandCategory.Ref;
                    this.AbstractStack.Push(s2);
                    break;

                case OPCODES.Rem:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    s2 = this.AbstractStack.Pop();
                    s1 = this.AbstractStack.Pop();
                    switch (s1.Category)
                    {
                        case OperandCategory.int32:
                        case OperandCategory.nativeInt:
                            switch (s2.Category)
                            {
                                case OperandCategory.int32:
                                    s3.Category = s1.Category == OperandCategory.nativeInt ? OperandCategory.nativeInt : OperandCategory.int32;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                case OperandCategory.nativeInt:
                                    s3.Category = OperandCategory.nativeInt;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.int64:
                            switch (s2.Category)
                            {
                                case OperandCategory.int64:
                                    s3.Category = OperandCategory.int64;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.F:
                            switch (s2.Category)
                            {
                                case OperandCategory.F:
                                    s3.Category = OperandCategory.F;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                    }
                    this.AbstractStack.Push(s3);
                    break;

                case OPCODES.Rem_Un:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    s2 = this.AbstractStack.Pop();
                    s1 = this.AbstractStack.Pop();
                    switch (s1.Category)
                    {
                        case OperandCategory.int32:
                        case OperandCategory.nativeInt:
                            switch (s2.Category)
                            {
                                case OperandCategory.int32:
                                    s3.Category = s1.Category == OperandCategory.nativeInt ? OperandCategory.nativeInt : OperandCategory.int32;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                case OperandCategory.nativeInt:
                                    s3.Category = OperandCategory.nativeInt;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.int64:
                            switch (s2.Category)
                            {
                                case OperandCategory.int64:
                                    s3.Category = OperandCategory.int64;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        default:
                            throw new Exception("Wrong arguments for Add operation!");
                    }
                    this.AbstractStack.Push(s3);
                    break;

                case OPCODES.Ret:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    break;
                case OPCODES.Rethrow:
                    break;

                case OPCODES.Shr:
                case OPCODES.Shr_Un:
                case OPCODES.Shl:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    s2 = this.AbstractStack.Pop();
                    s1 = this.AbstractStack.Pop();
                    switch (s1.Category)
                    {
                        case OperandCategory.int32:
                            switch (s2.Category)
                            {
                                case OperandCategory.int32:
                                case OperandCategory.nativeInt:
                                    s3.Category = s1.Category;
                                    s3.Source = new Target(off);
                                    s3.Type = s1.Type; // FIXME: check this
                                    break;
                                default:
                                    throw new Exception("Invalid argument for shift operation");
                            }
                            break;
                        case OperandCategory.int64:
                            switch (s2.Category)
                            {
                                case OperandCategory.int32:
                                case OperandCategory.nativeInt:
                                    s3.Category = s1.Category;
                                    s3.Source = new Target(off);
                                    s3.Type = s1.Type;
                                    break;
                                default:
                                    throw new Exception("Invalid argument for shift operation");
                            }
                            break;
                        case OperandCategory.nativeInt:
                            switch (s2.Category)
                            {
                                case OperandCategory.int32:
                                case OperandCategory.nativeInt:
                                    s3.Category = s1.Category;
                                    s3.Source = new Target(off);
                                    s3.Type = s1.Type;
                                    break;
                                default:
                                    throw new Exception("Invalid argument for shift operation");
                            }
                            break;
                        default:
                            throw new Exception("Invalid argument for shift operation");
                    }
                    break;

                case OPCODES.Sizeof:
                    s1.Category = OperandCategory.int32;
                    s1.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_U4);
                    s1.Source = new Target(off);
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Starg:
                case OPCODES.Starg_S:
                case OPCODES.Stloc:
                case OPCODES.Stloc_0:
                case OPCODES.Stloc_1:
                case OPCODES.Stloc_2:
                case OPCODES.Stloc_3:
                case OPCODES.Stloc_S:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    break;
                case OPCODES.Stelem:
                case OPCODES.Stelem_I:
                case OPCODES.Stelem_I1:
                case OPCODES.Stelem_I2:
                case OPCODES.Stelem_I4:
                case OPCODES.Stelem_I8:
                case OPCODES.Stelem_R4:
                case OPCODES.Stelem_R8:
                case OPCODES.Stelem_Ref:
                    if (this.AbstractStack.Count < 3)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    break;
                case OPCODES.Stfld:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    break;
                case OPCODES.Stind_I:
                case OPCODES.Stind_I1:
                case OPCODES.Stind_I2:
                case OPCODES.Stind_I4:
                case OPCODES.Stind_I8:
                case OPCODES.Stind_R4:
                case OPCODES.Stind_R8:
                case OPCODES.Stind_Ref:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    this.AbstractStack.Pop();
                    break;
                case OPCODES.Stobj:
                case OPCODES.Stsfld:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    break;
                case OPCODES.Sub:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    s2 = this.AbstractStack.Pop();
                    s1 = this.AbstractStack.Pop();
                    switch (s1.Category)
                    {
                        case OperandCategory.int32:
                        case OperandCategory.nativeInt:
                            switch (s2.Category)
                            {
                                case OperandCategory.int32:
                                    s3.Category = s1.Category == OperandCategory.nativeInt ? OperandCategory.nativeInt : OperandCategory.int32;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                case OperandCategory.nativeInt:
                                    s3.Category = OperandCategory.nativeInt;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.int64:
                            switch (s2.Category)
                            {
                                case OperandCategory.int64:
                                    s3.Category = OperandCategory.int64;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.F:
                            switch (s2.Category)
                            {
                                case OperandCategory.F:
                                    s3.Category = OperandCategory.F;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.Ref:
                            switch (s2.Category)
                            {
                                case OperandCategory.int32:
                                case OperandCategory.nativeInt:
                                    s3.Category = OperandCategory.Ref;
                                    s3.Type = s1.Type;
                                    s3.Source = new Target(off);
                                    break;
                                case OperandCategory.Ref:
                                    s3.Category = OperandCategory.nativeInt;
                                    s3.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                    }
                    this.AbstractStack.Push(s3);
                    break;

                case OPCODES.Sub_Ovf:
                case OPCODES.Sub_Ovf_Un:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    s2 = this.AbstractStack.Pop();
                    s1 = this.AbstractStack.Pop();
                    switch (s1.Category)
                    {
                        case OperandCategory.int32:
                        case OperandCategory.nativeInt:
                            switch (s2.Category)
                            {
                                case OperandCategory.int32:
                                    s3.Category = s1.Category == OperandCategory.nativeInt ? OperandCategory.nativeInt : OperandCategory.int32;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                case OperandCategory.nativeInt:
                                    s3.Category = OperandCategory.nativeInt;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.int64:
                            switch (s2.Category)
                            {
                                case OperandCategory.int64:
                                    s3.Category = OperandCategory.int64;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.Ref:
                            switch (s2.Category)
                            {
                                case OperandCategory.int32:
                                case OperandCategory.nativeInt:
                                    s3.Category = OperandCategory.Ref;
                                    s3.Type = s1.Type;
                                    s3.Source = new Target(off);
                                    break;
                                case OperandCategory.Ref:
                                    s3.Category = OperandCategory.nativeInt;
                                    s3.Type = BaseType.TypeOf(ELEMENT_TYPE.ELEMENT_TYPE_I);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                    }
                    this.AbstractStack.Push(s3);
                    break;

                case OPCODES.Switch:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    break;
                case OPCODES.Tailcall:
                    break;
                case OPCODES.Throw:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    break;
                case OPCODES.Unaligned:
                    break;
                case OPCODES.Unbox:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Type = new CLIType(this.File, new ByRefType(CompoundType.FromFToken(this.File, instr.par.iv, null)));
                    s1.Category = OperandCategory.Ref;
                    s1.Source = new Target(off);
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Unbox_Any:
                    if (this.AbstractStack.Count < 1)
                        throw new Exception("Stack underflow!");
                    this.AbstractStack.Pop();
                    s1.Type = new CLIType(this.File, CompoundType.FromFToken(this.File, instr.par.iv, null));
                    s1.Category = OperandCategory.Ref;
                    s1.Source = new Target(off);
                    this.AbstractStack.Push(s1);
                    break;
                case OPCODES.Volatile:
                    break;
                case OPCODES.Xor:
                    if (this.AbstractStack.Count < 2)
                        throw new Exception("Stack underflow!");
                    s2 = this.AbstractStack.Pop();
                    s1 = this.AbstractStack.Pop();
                    switch (s1.Category)
                    {
                        case OperandCategory.int32:
                        case OperandCategory.nativeInt:
                            switch (s2.Category)
                            {
                                case OperandCategory.int32:
                                    s3.Category = s1.Category == OperandCategory.nativeInt ? OperandCategory.nativeInt : OperandCategory.int32;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                case OperandCategory.nativeInt:
                                    s3.Category = OperandCategory.nativeInt;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                        case OperandCategory.int64:
                            switch (s2.Category)
                            {
                                case OperandCategory.int64:
                                    s3.Category = OperandCategory.int64;
                                    s3.Type = BaseType.Promote(s1.Type, s2.Type);
                                    s3.Source = new Target(off);
                                    break;
                                default:
                                    throw new Exception("Wrong arguments for Add operation!");
                            }
                            break;
                    }
                    this.AbstractStack.Push(s3);
                    break;

                default:
                    throw new Exception("Internal error");
            }
        }

        /// <summary>
        /// Advance the cursor to the next instruction. The instruction read is
        /// stored in the Instr field.
        /// </summary>
        /// <returns>True if the instruction has been read.</returns>
        public bool Next()
        {
            if (this.cur.EOF)
                return false;

            // Abstract interpretation of operands stack
            // This is DELAYED by one instruction
            if (this.AbstractStack != null && !this.cur.BOF)
            {
                if (this.AbstractStack.Count == 0)
                    this.lastStatement = this.pos;
                UpdateAbstractStack(this.Instr, this.pos, 0);
            }

            this.pos = this.cur.Offset;

            if (this.Labels != null)
                if (nextTarget < this.Labels.Length && this.Labels[nextTarget].Position == this.Position)
                    this.Label = this.Labels[nextTarget++];
                else
                    this.Label = null;

            if (this.cur.ToByte() == 0xFE)
            {
                this.Instr.op = OpCodesMap.HighCodes[this.cur.ToByte(1)];
                this.cur.Advance(2);
            }
            else
            {
                this.Instr.op = OpCodesMap.LowCodes[this.cur.ToByte()];
                this.cur.Advance(1);
            }

            this.Instr.sw = null;

            this.Instr.par.Read(this.Instr.op, this.cur);

            if (this.Instr.op.OperandType == OperandType.InlineSwitch)
            {
                int[] arr = new int[this.Instr.par.iv];
                for (int i = 0; i < this.Instr.par.iv; i++)
                {
                    arr[i] = this.cur.ToInt32();
                    this.cur.Advance(4);
                }
                this.Instr.sw = arr;
            }

            // FIXME: This could be above
            if (this.Instr.op.Value > 0x01 && this.Instr.op.Value < 0x06)
                this.Instr.par.iv = this.Instr.op.Value - 0x02;
            else if (this.Instr.op.Value > 0x05 && this.Instr.op.Value < 0x0a)
                this.Instr.par.iv = this.Instr.op.Value - 0x06;
            else if (this.Instr.op.Value > 0x09 && this.Instr.op.Value < 0x0e)
                this.Instr.par.iv = this.Instr.op.Value - 0x0a;

            return true;
        }

        /// <summary>
        /// Finds an approximation of locals scope.
        /// </summary>
        /// <param name="localscount">Number of local variables for the current method.</param>
        /// <returns>
        /// An array of targets that is encoded as following:
        /// Begin of scope for variable i has index 2*i, and the end has index 2*i+1.
        /// </returns>
        public Target[] LocalScope(int localscount)
        {
            if (this.LocalsScope != null)
                return this.LocalsScope;

            Target[] ret = new Target[localscount * 2];
            this.LocalsScope = ret;
            cur.Reset();
            int instr = 0;
            ILInstruction.Parameter par = new ILInstruction.Parameter();

            while (this.cur.Length > 0)
            {
                OpCode op;
                long pos = this.cur.Offset;
                if (this.cur.ToByte() == 0xFE)
                {
                    op = OpCodesMap.HighCodes[this.cur.ToByte(1)];
                    this.cur.Advance(2);
                }
                else
                {
                    op = OpCodesMap.LowCodes[this.cur.ToByte()];
                    this.cur.Advance(1);
                }
                ++instr;

                par.Read(op, cur);

                if (op.OperandType == OperandType.InlineSwitch)
                    // Note that the last element is advanced by the next advanced
                    // because ParamSize returns 4 in case InlineSwitch.
                    this.cur.Advance(par.iv * 4);

                OpCode norm = op;
                ILInstruction.Normalize(ref norm, ref par);

                if (norm == OpCodes.Ldloc || norm == OpCodes.Stloc)
                {
                    if (ret[par.iv * 2] == null) ret[par.iv * 2] = new Target(pos);
                    if (ret[par.iv * 2 + 1] == null) ret[par.iv * 2 + 1] = new Target(pos);
                    else
                        ret[par.iv * 2 + 1].Position = pos;
                }
            }

            this.cur.Reset();
            return ret;
        }

        /// <summary>
        /// This should be called before reading the entries. An internal table
        /// is used to keep track of targets. The method resets the cursor.
        /// </summary>
        public void TrackTargets()
        {
            if (this.Labels != null)
                return;

            ArrayList al = new ArrayList();
            cur.Reset();
            int instr = 0;

            while (this.cur.Length > 0)
            {
                OpCode op;
                long pos = this.cur.Offset;
                if (this.cur.ToByte() == 0xFE)
                {
                    op = OpCodesMap.HighCodes[this.cur.ToByte(1)];
                    this.cur.Advance(2);
                }
                else
                {
                    op = OpCodesMap.LowCodes[this.cur.ToByte()];
                    this.cur.Advance(1);
                }
                ++instr;
                switch (op.OperandType)
                {
                    case OperandType.InlineBrTarget:
                    case OperandType.ShortInlineBrTarget:
                        {
                            int t;
                            int sz;
                            if (op.OperandType == OperandType.InlineBrTarget)
                            {
                                t = this.cur.ToInt32();
                                sz = op.Size + 4;
                            }
                            else
                            {
                                t = (int)this.cur.ToSByte();
                                sz = op.Size + 1;
                            }
                            // sz because 0 is from the next instruction.
                            long p = pos + t + sz;
                            bool found = false;
                            foreach (Target tg in al)
                                if (tg.Position == p)
                                {
                                    found = true;
                                    break;
                                }

                            if (!found)
                                al.Add(new Target(p));
                            break;
                        }
                    case OperandType.InlineSwitch:
                        {
                            int t;
                            int sz;
                            int s = this.cur.ToInt32();
                            sz = op.Size + 4 + s * 4;

                            for (int i = 0; i < s; i++)
                            {
                                this.cur.Advance(4);
                                t = this.cur.ToInt32();

                                // sz because 0 is from the next instruction.
                                long p = pos + t + sz;
                                bool found = false;
                                foreach (Target tg in al)
                                    if (tg.Position == p)
                                    {
                                        found = true;
                                        break;
                                    }

                                if (!found)
                                    al.Add(new Target(p));
                            }
                            // Note that the last element is advanced by the next advanced
                            // because ParamSize returns 4 in case InlineSwitch.
                            break;
                        }
                }
                this.cur.Advance(ILCursor.ParamSize(op.OperandType));

            }

            al.Sort();
            this.Labels = new Target[al.Count];
            for (int i = 0; i < al.Count; i++)
            {
                this.Labels[i] = al[i] as Target;
            }

            this.cur.Reset();
            this.Instr.cur = this;
        }

        /// <summary>
        /// Push the state of the cursor on an internal stack.
        /// </summary>
        public void PushState()
        {
            // Note that we don't bother saving method index and CLIFile!
            if (this.saved == null)
                this.saved = new Stack();
            State s = new State();
            s.pos = this.pos;
            s.off = this.cur.Offset;
            s.Instr = this.Instr;
            s.nextTarget = this.nextTarget;
            s.lastStatement = this.AbstractStack == null ? -1 : this.lastStatement;
            s.Label = this.Label;
            s.LabelIndex = -2;
            this.saved.Push(s);
        }

        /// <summary>
        /// Restore the state of the cursor from the internal stack.
        /// </summary>
        public void PopState()
        {
            // Note that we don't restore method index and CLIFile (because aren't
            // set by Push)
            if (this.saved == null || this.saved.Count == 0)
                throw new Exception("States stack is empty!");
            State s = (State)this.saved.Pop();
            this.pos = s.pos;
            this.cur.Reset();
            if (s.lastStatement != -1)
            {
                this.AbstractStack.Clear();
                this.cur.Advance((int)s.lastStatement);
                this.pos = this.cur.Offset;
                // To avoid stack computation in the first place
                this.Instr.op = OpCodes.Nop;
                while (this.cur.Offset != s.off) this.Next();
            } 
            else 
            {
                this.cur.Advance((int)s.off); // FIXME: I'm assuming 4Gb max code! ;-)
                this.Instr = s.Instr;
            }
            this.nextTarget = s.nextTarget;
            this.Label = s.Label;
        }

        /// <summary>
        /// Return the state of the cursor. The only use of cursor state is
        /// to be passed to RestoreCursor!
        /// Beware: if the ILCursor has been obtained from a byte array rather
        /// than the CLIFile the State cannot be obtained.
        /// </summary>
        public State CursorState
        {
            get
            {
                if (this.MethodIndex == -1)
                    throw new Exception("Internal error: attempt to obtain a cursor state on a byte array!");
                State s = new State();
                s.pos = this.pos;
                s.off = this.cur.Offset;
                s.Instr = this.Instr;
                s.nextTarget = this.nextTarget;
                s.Label = null;
                if (this.Label != null)
                {
                    s.LabelIndex = Array.IndexOf(this.Labels, this.Label);
                }
                else if (this.Labels != null)
                    s.LabelIndex = -1;
                else
                    s.LabelIndex = -2;
                s.MethodIdx = this.MethodIndex;
                s.source = this.File;
                s.lastStatement = this.AbstractStack == null ? -1 : this.lastStatement;
                return s;
            }
        }

        public static ILCursor RestoreCursor(State s)
        {
            MethodTableCursor c = s.source[TableNames.Method].GetCursor() as MethodTableCursor;
            c.Goto(s.MethodIdx);
            ILCursor ret = c.MethodBody.ILInstructions;
            if (s.LabelIndex > -2)
                ret.TrackTargets();

            // Reset of internal cursor isn't required!
            ret.cur.Advance((int)s.off); // FIXME: I'm assuming 4Gb max code! ;-)
            ret.Instr = s.Instr;
            ret.nextTarget = s.nextTarget;
            ret.Label = s.LabelIndex > -1 ? ret.Labels[s.LabelIndex] : null;

            return ret;
        }

        public bool BeginStatement
        {
            get
            {
                if (this.AbstractStack == null)
                    throw new Exception("TrackStack must be invoked to enable stack analysis");
                return this.AbstractStack.Count == 0;
            }
        }

        public System.Collections.Generic.IEnumerable<long> Statements(CLIType[] args, CLIType[] locals)
        {
            this.Reset();
            this.TrackStack(args, locals);
            while (this.Next())
            {
                if (this.BeginStatement)
                    yield return this.pos;
            }
        }

        public OperandStack<StackSlot> OperandStack
        {
            get
            {
                return this.AbstractStack;
            }
        }

        public void BackToStatement()
        {
            if (this.AbstractStack == null)
                throw new Exception("TrackStack must be invoked to enable stack analysis");
            this.cur.Advance((int)(this.lastStatement - this.cur.Offset));
            this.pos = this.cur.Offset;
            // To avoid stack computation!
            this.Instr.op = OpCodes.Nop;
            this.Next();
            this.AbstractStack.Clear();
        }

        public bool IsCall
        {
            get
            {
                return this.Instr.op == OpCodes.Call || this.Instr.op == OpCodes.Calli || this.Instr.op == OpCodes.Callvirt;
            }
        }

        private void GotoOffsetAndTrackStack(long off)
        {
            BackToStatement();
            while (this.pos != off) this.Next();
        }

        private static int OpStackArgCount(CLIFile f, OpCode op, int par)
        {
            switch (op.StackBehaviourPop)
            {
                case StackBehaviour.Pop0:
                    return 0;

                case StackBehaviour.Pop1:
                case StackBehaviour.Popi:
                case StackBehaviour.Popref:
                    return 1;

                case StackBehaviour.Pop1_pop1:
                case StackBehaviour.Popi_pop1:
                case StackBehaviour.Popi_popi:
                case StackBehaviour.Popi_popi8:
                case StackBehaviour.Popi_popr4:
                case StackBehaviour.Popi_popr8:
                case StackBehaviour.Popref_pop1:
                case StackBehaviour.Popref_popi:
                    return 2;

                case StackBehaviour.Popi_popi_popi:
                case StackBehaviour.Popref_popi_pop1:
                case StackBehaviour.Popref_popi_popi:
                case StackBehaviour.Popref_popi_popi8:
                case StackBehaviour.Popref_popi_popr4:
                case StackBehaviour.Popref_popi_popr8:
                case StackBehaviour.Popref_popi_popref:
                    return 3;

                case StackBehaviour.Varpop:
                    MethodSig s = (new MethodDesc(f, par)).Signature;
                    return s.Count + (s.HasThis ? 1 : 0);
            }
            throw new Exception("Internal error");
        }

        public Target[] CallLoadArguments()
        {
            if (this.AbstractStack == null)
                throw new Exception("TrackStack must be invoked to enable stack analysis");
            if (!this.IsCall)
                throw new Exception("BackToBeginCall must be invoked on a callXXX instruction");

            MethodSig m = (new MethodDesc(this.File, this.Instr.par.iv)).Signature;
            int sp = this.AbstractStack.Count - 1;
            int count = m.Count + (m.HasThis ? 1 : 0);

            if (count == 0) return new Target[0];

            Target[] ret = new Target[count];
            for (int i = 0; i < count; i++)
                ret[i] = this.AbstractStack[count - 1 - i].Source;

            ILStoreCursor c = this.cur.Clone();

            // Back to the first instruction

            // Here I must pay since I need stack emulation
            this.PushState();
            for (int i = 0; i < ret.Length; i++)
            {
                c.Advance((int)(ret[i].Position - c.Offset));

                OpCode op = (c.ToByte() == 0xFE) ? OpCodesMap.HighCodes[c.ToByte(1)] : OpCodesMap.LowCodes[c.ToByte()];

                int last = 1; // To enter the first time 
                while ((op.StackBehaviourPop != StackBehaviour.Pop0 && op.StackBehaviourPop != StackBehaviour.Varpop) ||
                       (op.StackBehaviourPop == StackBehaviour.Varpop && last != 0))
                {
                    GotoOffsetAndTrackStack(ret[i].Position);

                    last = OpStackArgCount(this.File, this.Instr.op, this.Instr.par.iv);

                    // AbstractStack indexer is stack oriented (i.e. top == [0])
                    if (last > 0) // No arguments but varpop
                        ret[i] = this.AbstractStack[last - 1].Source;

                    c.Reset();
                    c.Advance((int)ret[0].Position);
                    op = (c.ToByte() == 0xFE) ? OpCodesMap.HighCodes[c.ToByte(1)] : OpCodesMap.LowCodes[c.ToByte()];
                }
            }

            this.PopState();

            return ret;
        }

        public void GoTo(Target t)
        {
            if (this.AbstractStack != null)
            {
                if (t.Position >= lastStatement && t.Position < this.cur.Offset)
                {
                    BackToStatement();
                    return;
                }
                else if (t.Position < lastStatement)
                    this.Reset();

                while (t.Position != this.pos && this.Next()) ;

                if (t.Position != this.pos) throw new ArgumentException("Invalid target!");
                return;
            }
            else
            {
                this.cur.Reset();
                this.cur.Advance((int)t.Position);
            }
        }
    }

    /// <summary>
    /// Cursor to a stream of IL instructions.
    /// </summary>
    public class MapPtrILCursor : ILCursor.ILStoreCursor
    {
        /// <summary>
        /// Pointer to the base of the stream.
        /// </summary>
        private MapPtr Base;

        /// <summary>
        /// Pointer to the current instruction.
        /// </summary>
        private MapPtr cur;

        /// <summary>
        /// Build a cursor for IL instructions.
        /// </summary>
        /// <param name="p">Pointer to the IL stream</param>
        private MapPtrILCursor(MapPtr p)
        {
            this.Base = p;
            this.cur = p;
        }

        /// <summary>
        /// Factory pattern. This method builds an ILCursor.
        /// </summary>
        /// <param name="idx">Index in the method table of the method</param>
        /// <param name="p">Pointer to the IL stream</param>
        /// <param name="f">Assembly being read</param>
        /// <returns>The ILCursor.</returns>
        internal static ILCursor CreateILCursor(int idx, MapPtr p, CLIFile f)
        {
            return new ILCursor(idx, new MapPtrILCursor(p), f);
        }

        /// <summary>
        /// True if the cursor is at the beginning.
        /// </summary>
        public bool BOF
        {
            get { return this.Base == this.cur; }
        }

        /// <summary>
        /// True if the cursor is at the end of the stream
        /// </summary>
        public bool EOF
        {
            get { return cur.Length == 0; }
        }

        /// <summary>
        /// Reset the cursor.
        /// </summary>
        public void Reset()
        {
            this.cur = this.Base;
        }

        /// <summary>
        /// Length of the ILStream in bytes from the current position.
        /// </summary>
        public long Length { get { return this.cur.Length; } }

        /// <summary>
        /// Number of bytes from the beginning of the stream.
        /// </summary>
        public long Offset { get { return this.cur - this.Base; } }

        /// <summary>
        /// Advance the cursor of a number of bytes.
        /// </summary>
        /// <param name="i">The number of bytes to advance</param>
        public void Advance(int i) { this.cur += i; }

        /// <summary>
        /// Convert the next four bytes in the IL stream into an int
        /// </summary>
        /// <returns>The integer read from the stream</returns>
        public int ToInt32() { return (int)this.cur; }

        /// <summary>
        /// Convert the next two bytes in the IL stream into a short
        /// </summary>
        /// <returns>The short read from the stream</returns>
        public short ToShort() { return (short)this.cur; }

        /// <summary>
        /// Convert the next eight bytes in the IL stream into a long integer.
        /// </summary>
        /// <returns>The long read from the stream</returns>
        public long ToInt64() { return (long)this.cur; }

        /// <summary>
        /// Convert the next byte in the IL stream into an sbyte
        /// </summary>
        /// <returns>The sbyte read from the stream</returns>
        public sbyte ToSByte() { return (sbyte)this.cur; }

        /// <summary>
        /// Return the next byte in the stream.
        /// </summary>
        /// <returns>The byte read from the stream</returns>
        public byte ToByte() { return this.cur[0]; }

        /// <summary>
        /// Return the byte after off bytes in the stream from the current 
        /// position.
        /// </summary>
        /// <returns>The byte read from the stream</returns>
        public byte ToByte(int off) { return this.cur[off]; }

        /// <summary>
        /// Convert the next eight bytes in the IL stream into a double.
        /// </summary>
        /// <returns>The double read from the stream</returns>
        public double ToDouble() { return (double)this.cur; }

        /// <summary>
        /// Convert the next four bytes in the IL stream into a float.
        /// </summary>
        /// <returns>The float read from the stream</returns>
        public float ToFloat() { return (float)this.cur; }

        /// <summary>
        /// Creates a copy of the cursor.
        /// </summary>
        /// <returns></returns>
        public ILCursor.ILStoreCursor Clone()
        {
            MapPtrILCursor ret = new MapPtrILCursor(this.Base);
            ret.cur = this.cur;
            return ret;
        }
    }

    /// <summary>
    /// Cursor to a stream of IL instructions stored into a byte array.
    /// FIXME: Find a better integration with ILCursor!
    /// </summary>
    public class ByteArrayILCursor : ILCursor.ILStoreCursor
    {
        /// <summary>
        /// Pointer to the base of the stream.
        /// </summary>
        private int Base;

        /// <summary>
        /// Pointer to the current instruction.
        /// </summary>
        private int cur;

        /// <summary>
        /// array containing the IL.
        /// </summary>
        private byte[] array;

        /// <summary>
        /// Length of the IL bytes contained in the array.
        /// </summary>
        private int len;

        /// <summary>
        /// Build a cursor for IL instructions.
        /// </summary>
        /// <param name="p">Pointer to the IL stream</param>
        private ByteArrayILCursor(byte[] a, int from, int l)
        {
            this.Base = from;
            this.cur = from;
            this.len = l;
            this.array = a;
        }

        public static ILCursor CreateILCursor(byte[] a, int from, int l, CLIFile f)
        {
            return new ILCursor(-1, new ByteArrayILCursor(a, from, l), f);
        }

        /// <summary>
        /// True if the cursor is at the beginning.
        /// </summary>
        public bool BOF
        {
            get { return this.Base == this.cur; }
        }

        /// <summary>
        /// True if the cursor is at the end of the stream
        /// </summary>
        public bool EOF
        {
            get { return this.len == this.cur - this.Base; }
        }

        /// <summary>
        /// Reset the cursor.
        /// </summary>
        public void Reset()
        {
            this.cur = this.Base;
        }

        /// <summary>
        /// Length of the ILStream in bytes from the current position.
        /// </summary>
        public long Length { get { return this.len - this.cur + this.Base; } }

        /// <summary>
        /// Number of bytes from the beginning of the stream.
        /// </summary>
        public long Offset { get { return this.cur - this.Base; } }

        /// <summary>
        /// Advance the cursor of a number of bytes.
        /// </summary>
        /// <param name="i">The number of bytes to advance</param>
        public void Advance(int i) { this.cur += i; }

        /// <summary>
        /// Convert the next four bytes in the IL stream into an int
        /// </summary>
        /// <returns>The integer read from the stream</returns>
        public int ToInt32() { return MapPtr.ToInt32(this.array, this.cur); }

        /// <summary>
        /// Convert the next two bytes in the IL stream into a short
        /// </summary>
        /// <returns>The short read from the stream</returns>
        public short ToShort() { return MapPtr.ToShort(this.array, this.cur); }

        /// <summary>
        /// Convert the next eight bytes in the IL stream into a long integer.
        /// </summary>
        /// <returns>The long read from the stream</returns>
        public long ToInt64() { return MapPtr.ToInt64(this.array, this.cur); }

        /// <summary>
        /// Convert the next byte in the IL stream into an sbyte
        /// </summary>
        /// <returns>The sbyte read from the stream</returns>
        public sbyte ToSByte() { return MapPtr.ToSByte(this.array, this.cur); }

        /// <summary>
        /// Return the next byte in the stream.
        /// </summary>
        /// <returns>The byte read from the stream</returns>
        public byte ToByte() { return this.array[cur]; }

        /// <summary>
        /// Return the byte after off bytes in the stream from the current 
        /// position.
        /// </summary>
        /// <returns>The byte read from the stream</returns>
        public byte ToByte(int off) { return this.array[cur + off]; }

        /// <summary>
        /// Convert the next eight bytes in the IL stream into a double.
        /// </summary>
        /// <returns>The double read from the stream</returns>
        public double ToDouble() { return MapPtr.ToDouble(this.array, this.cur); }

        /// <summary>
        /// Convert the next four bytes in the IL stream into a float.
        /// </summary>
        /// <returns>The float read from the stream</returns>
        public float ToFloat() { return MapPtr.ToFloat(this.array, this.cur); }

        /// <summary>
        /// Creates a copy of the cursor.
        /// </summary>
        /// <returns></returns>
        public ILCursor.ILStoreCursor Clone()
        {
            ByteArrayILCursor ret = new ByteArrayILCursor(this.array, this.Base, this.len);
            ret.cur = this.cur;
            return ret;
        }
    }
}
