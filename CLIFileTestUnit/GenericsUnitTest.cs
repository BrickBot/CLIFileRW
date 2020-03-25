/// ------------------------------------------------------------
/// 
/// File: Enums.cs
/// Author: Antonio Cisternino (cisterni@di.unipi.it)
/// Author: Sebastien Vacouleur
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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CLIFileRW;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CLIFileTestUnit
{
    public delegate T GenericDelegate<T, V>(V v);

    class Stack<T>
    {
        T[] array;
        int sp;
        public Stack()
        {
            array = new T[15];
            sp = 0;
        }
        public T Pop()
        {
            if (sp == 0)
                return default(T);
            return array[--sp];
        }
        public void Push(T v)
        {
            if (sp == array.Length)
            {
                T[] tmp = new T[array.Length * 2];
                Array.Copy(array, tmp, array.Length);
                array = tmp;
            }
            array[sp++] = v;
        }
    }

    public class LocalScope
    {
        // To ensure side effect and ensure calls to Hint.
        public static int count = 0;
        public static void BeginHint(string msg)
        {
            count++;
        }
        public static void EndHint(string msg)
        {
            count++;
        }
        public static void Local()
        {
            int v1 = 0;
            BeginHint("v1:int");
            if (v1 > 0) Console.WriteLine("Positive");
            BeginHint("i:int for");
            for (int i = 0; i < 10; i++)
            {
                BeginHint("j:int for");
                for (int j = 0; j < 10; i++)
                {
                    BeginHint("z:int");
                    int z = j * j;
                    Console.WriteLine(j + z);
                    EndHint("z");
                }
                EndHint("j");

                BeginHint("k:int for");
                for (int k = 0; k < 10; k++)
                {
                    Console.WriteLine(k);
                }
                EndHint("k");
                Console.WriteLine(i);
            }
            EndHint("i");
            EndHint("v1");
        }
    }

    /// <summary>
    /// Summary description for GenericsUnitTest
    /// </summary>
    [TestClass]
    public class GenericsUnitTest
    {
        private CLIFile f;

        public GenericsUnitTest()
        {
            f = CLIFile.Open(GetType().Assembly.Location);
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void GeneralTest()
        {
            Assembly me = GetType().Assembly;
            // -1 is because in the TypeDef table the first row is reserved for the module.
            Assert.AreEqual(me.GetTypes().Length, f[TableNames.TypeDef].Rows - 1);
        }
        [TestMethod]
        public void TypeTest()
        {
            Assembly me = GetType().Assembly;
            TypeDefTableCursor cur = f[TableNames.TypeDef].GetCursor() as TypeDefTableCursor;
            foreach (Type t in me.GetTypes())
            {
                cur.Goto(t.MetadataToken & 0xFFFFFF); // It is a TypeDefOrRef token
                Assert.AreEqual(t.Name, f.Strings[cur.Name]);
            }
        }
        [TestMethod]
        public void MethodTest()
        {
            Type me = typeof(Stack<GenericsUnitTest>);
            MethodTableCursor cur = f[TableNames.Method].GetCursor() as MethodTableCursor;
            foreach (MethodInfo m in me.GetMethods(
              BindingFlags.DeclaredOnly | BindingFlags.Static |
              BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                cur.Goto(m.MetadataToken & 0xFFFFFF);
                Assert.AreEqual(m.Name, f.Strings[cur.Name]);
                MethodSig s = cur.GetMethodSignature();
                // Type instantiation is not known by looking only at metadata, we must help the resolver here!
                Assert.AreSame(s.ReturnType.GetReflectionType(me.GetGenericArguments()), m.ReturnType);
                IEnumerator<CLIType> cp = s.GetParameters().GetEnumerator();
                foreach (ParameterInfo p in m.GetParameters())
                {
                    Assert.IsTrue(cp.MoveNext());
                    Assert.AreSame(cp.Current.GetReflectionType(me.GetGenericArguments()), p.ParameterType);
                }
            }
        }
        [TestMethod]
        public void MethodBodyTest()
        {
            MethodInfo m = typeof(GenericsUnitTest).GetMethod("MethodTest");
            MethodTableCursor cur = f[TableNames.Method].GetCursor() as MethodTableCursor;
            cur.Goto(m.MetadataToken & 0xFFFFFF);

            System.Reflection.MethodBody rb = m.GetMethodBody();
            CLIFileRW.MethodBody cb = cur.MethodBody;
            Assert.AreEqual(rb.LocalSignatureMetadataToken & 0xFFFFFF, cb.LocalsSig);

            LocalsVarSig locals = cb.LocalsSignature;
            IEnumerator<CLIType> lv = locals.GetVariables().GetEnumerator();
            foreach (LocalVariableInfo v in rb.LocalVariables)
            {
                Assert.IsTrue(lv.MoveNext());
                // Fetch instantiation from local type since we are not driven by CLIFile reflection
                Assert.AreSame(v.LocalType, lv.Current.GetReflectionType(v.LocalType.GetGenericArguments()));
            }

            ILCursor ilc = cb.ILInstructions;
            while (ilc.Next())
            {
                ILInstruction instr = ilc.Instr;
                object o = instr.ResolveParameter(f);
            }
        }

        [TestMethod]
        public void StatementsBodyTest()
        {

            MethodTableCursor cur = CLIFile.FindMethod(typeof(GenericsUnitTest).GetMethod("MethodTest"));

            CLIFileRW.MethodBody cb = cur.MethodBody;

            LocalsVarSig locals = cb.LocalsSignature;
            CLIType[] vars = locals.GetVariables().ToArray<CLIType>();
            CLIType[] args = cur.GetMethodSignature().GetParameters(cur.FindCLIType()).ToArray<CLIType>();

            ILCursor ilc = cb.ILInstructions;
            ilc.TrackStack(args, vars, cb.MaxStack);
            while (ilc.Next())
            {
                ILInstruction instr = ilc.Instr;
                object o = instr.ResolveParameter(f);
                if (ilc.BeginStatement)
                    System.Diagnostics.Debug.WriteLine(string.Format("MethodTest statement at offset: {0:X4}", ilc.Position));
            }
        }

        [TestMethod]
        public void LocalsTest()
        {
            MethodInfo m = typeof(LocalScope).GetMethod("Local");
            MethodTableCursor cur = f[TableNames.Method].GetCursor() as MethodTableCursor;
            cur.Goto(m.MetadataToken & 0x00FFFFFF);
            ILCursor ilc = cur.MethodBody.ILInstructions;
            Target[] lvs = ilc.LocalScope(m.GetMethodBody().LocalVariables.Count);
            for (int i = 0; i < lvs.Length / 2; i++)
            {
                Target b = lvs[i * 2], e = lvs[i * 2 + 1];

                System.Diagnostics.Debug.WriteLine(string.Format("{0}: ({1}, {2})", i, b, e));
                Assert.IsTrue((b == null && e == null) || (b.Position <= e.Position));
            }
        }

        [TestMethod]
        public void ReflectionCursorTest()
        {
            MethodInfo m = typeof(LocalScope).GetMethod("Local");
            ILCursor ilc = ILCursor.FromMethodInfo(m).ILInstructions;
            Target[] lvs = ilc.LocalScope(m.GetMethodBody().LocalVariables.Count);
            for (int i = 0; i < lvs.Length / 2; i++)
            {
                Target b = lvs[i * 2], e = lvs[i * 2 + 1];

                System.Diagnostics.Debug.WriteLine(string.Format("{0}: ({1}, {2})", i, b, e));
                Assert.IsTrue((b == null && e == null) || (b.Position <= e.Position));
            }
        }

        [TestMethod]
        public void MethodBodyAllTest()
        {
            Assembly mscorlib = typeof(object).Assembly;
            CLIFile f = CLIFile.Open(mscorlib.Location);
            MethodTableCursor cur = f[TableNames.Method].GetCursor() as MethodTableCursor;
            int instrcnt = 0;
            int methods = 0;
            int implmeth = 0;
            int brcnt = 0;

            while (cur.Next())
            {
                // The token 0x0600449a is not resolved by ResolveMethod, it is likely a bug in reflection.
                // With .NET 3.5 SP1 the method has moved into token 0x44df.
                if (cur.Position == 0x44df) continue;
                MethodBase mb = mscorlib.ManifestModule.ResolveMethod(cur.MetadataToken);

                System.Reflection.MethodBody rb = mb.GetMethodBody();
                methods++;
                if (rb == null)
                {
                    Assert.IsTrue(rb == null && cur.RVA == 0);
                    continue;
                }
                implmeth++;

                CLIFileRW.MethodBody cb = cur.MethodBody;
                Assert.AreEqual(rb.LocalSignatureMetadataToken & 0xFFFFFF, cb.LocalsSig);
                if (rb.LocalSignatureMetadataToken != 0)
                {
                    LocalsVarSig locals = cb.LocalsSignature;

                    IEnumerator<CLIType> lv = locals.GetVariables().GetEnumerator();
                    foreach (LocalVariableInfo v in rb.LocalVariables)
                    {
                        Assert.IsTrue(lv.MoveNext());
                        // Fetch instantiation from local type since we are not driven by CLIFile reflection
                        Assert.AreSame(v.LocalType, lv.Current.GetReflectionType(mb.DeclaringType.ContainsGenericParameters ? mb.DeclaringType.GetGenericArguments() : null, mb.ContainsGenericParameters ? mb.GetGenericArguments() : null));
                    }
                }

                ILCursor ilc = cb.ILInstructions;
                while (ilc.Next())
                {
                    ILInstruction instr = ilc.Instr;
                    object o = instr.ResolveParameter(f);
                    instrcnt++;
                    if (instr.op == System.Reflection.Emit.OpCodes.Br || instr.op == System.Reflection.Emit.OpCodes.Br_S)
                        brcnt++;
                }

            }

            System.Diagnostics.Debug.WriteLine(string.Format("Total methods: {0}", methods));
            System.Diagnostics.Debug.WriteLine(string.Format("Impl methods: {0}", implmeth));
            System.Diagnostics.Debug.WriteLine(string.Format("Total instructions: {0}", instrcnt));
            System.Diagnostics.Debug.WriteLine(string.Format("Total unconditional branch: {0}", brcnt));
            System.Diagnostics.Debug.WriteLine(string.Format("Average method len: {0}", (instrcnt / (double)methods)));
        }

        public int op(int a, int b)
        {
            return a + b;
        }

        public void ExpressionInput()
        {
            int a = 0;
            int b = 0;
            a = op(op(op(a + b, b), a), op(a, b));
        }

        [TestMethod]
        public void TrackCallsTest()
        {

            MethodTableCursor cur = CLIFile.FindMethod(typeof(GenericsUnitTest).GetMethod("ExpressionInput"));

            CLIFileRW.MethodBody cb = cur.MethodBody;

            LocalsVarSig locals = cb.LocalsSignature;
            CLIType[] vars = locals.GetVariables().ToArray<CLIType>();
            CLIType[] args = cur.GetMethodSignature().GetParameters(cur.FindCLIType()).ToArray<CLIType>();

            ILCursor ilc = cb.ILInstructions;
            ilc.TrackStack(args, vars, cb.MaxStack);
            while (ilc.Next())
            {
                ILInstruction instr = ilc.Instr;
                object o = instr.ResolveParameter(f);
                if (ilc.IsCall)
                {
                    Target[] cargs = ilc.CallLoadArguments();
                    MethodDesc m = (MethodDesc)o;
                    System.Diagnostics.Debug.WriteLine(string.Format("{0} to {1} at offset {2:X4}", ilc.Instr.op, m.Name, ilc.Position));
                    for (int i = 0; i < cargs.Length; i++)
                        System.Diagnostics.Debug.WriteLine(string.Format("\tPar {0} at offset {1:X4}", i, cargs[i].Position));
                }
            }
        }

    }

    [TestClass]
    public class MetadataTests
    {
        [TestMethod]
        public void StandAloneSigTest()
        {
            CLIFile f = CLIFile.Open(typeof(object).Assembly.Location);
            Table t = f[TableNames.StandAloneSig];
            StandAloneSigTableCursor cur = t.GetCursor() as StandAloneSigTableCursor;

            while (cur.Next())
            {

                Assert.IsTrue(cur.Signature < f.Blob.Size, "Wrong index into the blob heap!");
            }
        }
    }

    /// <summary>
    /// Regressions Tests on Mscorlib, Currently avoiding Generic Members
    /// </summary>
    [TestClass]
    public class MscorlibRegressionTest
    {

        private CLIFile reader;
        private Assembly asm;

        public MscorlibRegressionTest()
        {
            Type objectType = typeof(Object);
            asm = objectType.Assembly;
            reader = CLIFile.Open(asm.Location);
        }

        public void MscorlibRegression(Action<MethodTableCursor, MethodInfo> action)
        {
            MethodTableCursor cur = reader[TableNames.Method].GetCursor() as MethodTableCursor;

            foreach (Module m in asm.GetModules())
                foreach (Type t in asm.GetTypes())
                    if (!t.IsAbstract && t.IsClass)
                    {
                        foreach (MethodInfo method in t.GetMethods())
                            if (method.IsStatic && !method.IsAbstract &&
                                ((method.MetadataToken >> 24) == (int)TableNames.Method))
                            {
                                var txt = t.ToString() + "\t" + method.ToString();
                                System.Diagnostics.Debug.WriteLine(txt);
                                cur.Goto(method.MetadataToken & 0x00FFFFFF);
                                action(cur, method);
                            }
                    }
        }

        [TestMethod]
        public void RegressiveTestCheckMethodName()
        {
            MscorlibRegression(delegate(MethodTableCursor cur, MethodInfo method)
            {
                cur.Goto(method.MetadataToken & 0x00FFFFFF);
                Assert.AreEqual(method.Name, reader.Strings[cur.Name]);
            });
        }

        [TestMethod]
        public void RegressiveTestCheckMethodBody()
        {
            MscorlibRegression(delegate(MethodTableCursor cur, MethodInfo method)
            {
                var rb = method.GetMethodBody();
                if (rb == null)
                {
                    Assert.AreEqual(cur.RVA, 0, "Invalid RVA!");
                }
            });
        }

        [TestMethod]
        public void RegressiveTestCheckLocalSignature()
        {
            MscorlibRegression(delegate(MethodTableCursor cur, MethodInfo method)
            {
                var rb = method.GetMethodBody();
                if (rb != null)
                {
                    CLIFileRW.MethodBody cb = cur.MethodBody;
                    Assert.AreEqual(rb.LocalSignatureMetadataToken & 0x00FFFFFF, cb.LocalsSig);
                    if (cb.LocalsSig != 0)
                    {
                        LocalsVarSig locals = cb.LocalsSignature;
                        IEnumerator<CLIType> lv = locals.GetVariables().GetEnumerator();
                        foreach (LocalVariableInfo v in rb.LocalVariables)
                        {
                            Assert.IsTrue(lv.MoveNext());
                            // Note: Here we are not traversing metadata in order so I get the signature from
                            // the reflection object.
                            Assert.AreSame(v.LocalType, lv.Current.GetReflectionType(v.LocalType.GetGenericArguments()));
                        }
                    }
                }
            });
        }


        [TestMethod]
        public void RegressiveTestILSplit()
        {
            MscorlibRegression(delegate(MethodTableCursor cur, MethodInfo method)
            {
                if (cur.RVA == 0) return;

                //FIXME: This test cannot pass because in exception handlers the catch blocks load the exception
                // onto the evaluation stack. I need to decide how to deal with it.

                CLIFileRW.MethodBody cb = cur.MethodBody;
                LocalsVarSig locals = cb.LocalsSignature;

                if (locals != null)
                {
                    CLIType[] vars = locals.GetVariables().ToArray<CLIType>();
                    CLIType[] args = cur.GetMethodSignature().GetParameters(method.IsStatic ? null : cur.FindCLIType()).ToArray<CLIType>();

                    ILCursor ilc = cb.ILInstructions;
                    ilc.TrackStack(args, vars);

                    while (ilc.Next())
                    {
                        ILInstruction instr = ilc.Instr;
                        object o = instr.ResolveParameter(reader);
                        if (ilc.BeginStatement)
                            System.Diagnostics.Debug.WriteLine(string.Format("MethodTest statement at offset: {0:X4}", ilc.Position));
                    }
                }
            });
        }
    }
}
