﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ExtractMethod
{
    public partial class ExtractMethodTests
    {
        public class LanguageInteraction : ExtractMethodBase
        {
            #region Generics

            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void SelectTypeParameterWithConstraints()
            {
                var code = @"using System;

class Program
{
    object MyMethod1<TT>() where TT : ICloneable, new()
    {
        [|TT abcd; abcd = new TT();|]
        return abcd;
    }
}";
                var expected = @"using System;

class Program
{
    object MyMethod1<TT>() where TT : ICloneable, new()
    {
        TT abcd = NewMethod<TT>();
        return abcd;
    }

    private static TT NewMethod<TT>() where TT : ICloneable, new()
    {
        return new TT();
    }
}";

                TestExtractMethod(code, expected);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void SelectTypeParameter()
            {
                var code = @"using System;

class Program
{
    public string Method<T, R>()
    {
        T t;
        R r;
        [|t = default(T);
        r = default(R);
        string s = ""hello"";|]
        return s;
    }
}";
                var expected = @"using System;

class Program
{
    public string Method<T, R>()
    {
        T t;
        R r;
        string s;
        NewMethod(out t, out r, out s);
        return s;
    }

    private static void NewMethod<T, R>(out T t, out R r, out string s)
    {
        t = default(T);
        r = default(R);
        s = ""hello"";
    }
}";

                TestExtractMethod(code, expected, allowMovingDeclaration: false);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void SelectTypeOfTypeParameter()
            {
                var code = @"using System;

class Program
{
    public static Type meth<U>(U u)
    {
        return [|typeof(U)|];
    }
}";
                var expected = @"using System;

class Program
{
    public static Type meth<U>(U u)
    {
        return NewMethod<U>();
    }

    private static Type NewMethod<U>()
    {
        return typeof(U);
    }
}";

                TestExtractMethod(code, expected);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void SelectTypeParameterDataFlowOut()
            {
                var code = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{

    public class Test
    {
        public int i = 5;
    }

    public string Method<T>()
    {
        T t;
        [|t = (T)new Test();
        t.i = 10;|]
        return t.i.ToString();
    }
}";
                var expected = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{

    public class Test
    {
        public int i = 5;
    }

    public string Method<T>()
    {
        T t;
        t = NewMethod<T>();
        return t.i.ToString();
    }

    private static T NewMethod<T>()
    {
        T t = (T)new Test();
        t.i = 10;
        return t;
    }
}";

                TestExtractMethod(code, expected, allowMovingDeclaration: false);
            }

            [WorkItem(528198)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void BugFix6794()
            {
                var code = @"using System;
class Program
{
    static void Main(string[] args)
    {
        int i = 2;
        C<int> c = new C<int>(ref [|i|]) ;
    }
 
    private class C<T>
    {
        private int v;
        public C(ref int v)
        {
            this.v = v;
        }
    }
}";
                var expected = @"using System;
class Program
{
    static void Main(string[] args)
    {
        int i = 2;
        C<int> c = GetC(i);
    }

    private static C<int> GetC(int i)
    {
        return new C<int>(ref i);
    }

    private class C<T>
    {
        private int v;
        public C(ref int v)
        {
            this.v = v;
        }
    }
}";

                TestExtractMethod(code, expected);
            }

            [WorkItem(528198)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void BugFix6794_1()
            {
                var code = @"using System;
class Program
{
    static void Main(string[] args)
    {
        int i;
        C<int> c = new C<int>(out [|i|]) ;
    }
 
    private class C<T>
    {
        public C(out int v)
        {
            v = 1;
        }
    }
}";
                var expected = @"using System;
class Program
{
    static void Main(string[] args)
    {
        C<int> c = GetC();
    }

    private static C<int> GetC()
    {
        int i;
        return new C<int>(out i);
    }

    private class C<T>
    {
        public C(out int v)
        {
            v = 1;
        }
    }
}";

                TestExtractMethod(code, expected);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void SelectDefaultOfT()
            {
                var code = @"using System;
using System.Collections.Generic;
using System.Linq;

class Test11<T>
{
    T method()
    {
        T t = [|default(T)|];
        return t;
    }
}";
                var expected = @"using System;
using System.Collections.Generic;
using System.Linq;

class Test11<T>
{
    T method()
    {
        T t = GetT();
        return t;
    }

    private static T GetT()
    {
        return default(T);
    }
}";

                TestExtractMethod(code, expected);
            }

            #endregion

            #region Operators

            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void SelectPostIncrementOperatorExtractWithRef()
            {
                var code = @"class A
{
    int method(int i)
    {
        return [|i++|];
    }
}";
                var expected = @"class A
{
    int method(int i)
    {
        return NewMethod(ref i);
    }

    private static int NewMethod(ref int i)
    {
        return i++;
    }
}";
                TestExtractMethod(code, expected, allowMovingDeclaration: false);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void SelectPostIncrementOperator()
            {
                var code = @"class A
{
    int method(int i)
    {
        return [|i++|];
    }
}";
                var expected = @"class A
{
    int method(int i)
    {
        return NewMethod(i);
    }

    private static int NewMethod(int i)
    {
        return i++;
    }
}";
                TestExtractMethod(code, expected);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void SelectPreIncrementOperator()
            {
                var code = @"class A
{
    int method(int i)
    {
        return [|++i|];
    }
}";
                var expected = @"class A
{
    int method(int i)
    {
        return NewMethod(i);
    }

    private static int NewMethod(int i)
    {
        return ++i;
    }
}";
                TestExtractMethod(code, expected);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void SelectPostDecrementOperator()
            {
                var code = @"class A
{
    int method(int i)
    {
        return [|i--|];
    }
}";
                var expected = @"class A
{
    int method(int i)
    {
        return NewMethod(i);
    }

    private static int NewMethod(int i)
    {
        return i--;
    }
}";
                TestExtractMethod(code, expected);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void SelectPreDecrementOperator()
            {
                var code = @"class A
{
    int method(int i)
    {
        return [|--i|];
    }
}";
                var expected = @"class A
{
    int method(int i)
    {
        return NewMethod(i);
    }

    private static int NewMethod(int i)
    {
        return --i;
    }
}";
                TestExtractMethod(code, expected);
            }

            #endregion

            #region ExpressionBodiedMembers

            [WorkItem(528, "https://github.com/dotnet/roslyn/issues/528")]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void ExpressionBodiedMethod()
            {
                var code = @"using System;
class T
{
    int m;
    int M1() => [|1|] + 2 + 3 + m;
}";
                var expected = @"using System;
class T
{
    int m;
    int M1() => NewMethod() + 2 + 3 + m;

    private static int NewMethod()
    {
        return 1;
    }
}";
                TestExtractMethod(code, expected);
            }

            [WorkItem(528, "https://github.com/dotnet/roslyn/issues/528")]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void ExpressionBodiedOperator()
            {
                var code = @"using System;
class Complex
{
    int real; int imaginary;
    public static Complex operator +(Complex a, Complex b) => a.Add([|b.real + 1|]);

    private Complex Add(int b)
    {
        throw new NotImplementedException();
    }
}";
                var expected = @"using System;
class Complex
{
    int real; int imaginary;
    public static Complex operator +(Complex a, Complex b) => a.Add(NewMethod(b));

    private static int NewMethod(Complex b)
    {
        return b.real + 1;
    }

    private Complex Add(int b)
    {
        throw new NotImplementedException();
    }
}";
                TestExtractMethod(code, expected);
            }

            [WorkItem(528, "https://github.com/dotnet/roslyn/issues/528")]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void ExpressionBodiedConversionOperator()
            {
                var code = @"using System;
public struct DBBool
{
    public static readonly DBBool dbFalse = new DBBool(-1);
    int value;

    DBBool(int value)
    {
        this.value = value;
    }

    public static implicit operator DBBool(bool x) => x ? new DBBool([|1|]) : dbFalse;
}";
                var expected = @"using System;
public struct DBBool
{
    public static readonly DBBool dbFalse = new DBBool(-1);
    int value;

    DBBool(int value)
    {
        this.value = value;
    }

    public static implicit operator DBBool(bool x) => x ? new DBBool(NewMethod()) : dbFalse;

    private static int NewMethod()
    {
        return 1;
    }
}";
                TestExtractMethod(code, expected);
            }

            [WorkItem(528, "https://github.com/dotnet/roslyn/issues/528")]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void ExpressionBodiedProperty()
            {
                var code = @"using System;
class T
{
    int M1 => [|1|] + 2;
}";
                var expected = @"using System;
class T
{
    int M1 => NewMethod() + 2;

    private static int NewMethod()
    {
        return 1;
    }
}";
                TestExtractMethod(code, expected);
            }

            [WorkItem(528, "https://github.com/dotnet/roslyn/issues/528")]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void ExpressionBodiedIndexer()
            {
                var code = @"using System;
class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i] => i > 0 ? arr[[|i + 1|]] : arr[i + 2];
}";
                var expected = @"using System;
class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i] => i > 0 ? arr[NewMethod(i)] : arr[i + 2];

    private static int NewMethod(int i)
    {
        return i + 1;
    }
}";
                TestExtractMethod(code, expected);
            }

            [WorkItem(528, "https://github.com/dotnet/roslyn/issues/528")]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void ExpressionBodiedIndexer2()
            {
                var code = @"using System;
class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i] => [|i > 0 ? arr[i + 1]|] : arr[i + 2];
}";
                var expected = @"using System;
class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i] => NewMethod(i);

    private T NewMethod(int i)
    {
        return i > 0 ? arr[i + 1] : arr[i + 2];
    }
}";
                TestExtractMethod(code, expected);
            }

            [WorkItem(528, "https://github.com/dotnet/roslyn/issues/528")]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void ExpressionBodiedMethodWithBlockBodiedAnonymousMethodExpression()
            {
                var code = @"using System;
class TestClass
{
    Func<int, int> Y() => delegate (int x)
    {
        return [|9|];
    };
}";
                var expected = @"using System;
class TestClass
{
    Func<int, int> Y() => delegate (int x)
    {
        return NewMethod();
    };

    private static int NewMethod()
    {
        return 9;
    }
}";
                TestExtractMethod(code, expected);
            }

            [WorkItem(528, "https://github.com/dotnet/roslyn/issues/528")]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void ExpressionBodiedMethodWithSingleLineBlockBodiedAnonymousMethodExpression()
            {
                var code = @"using System;
class TestClass
{
    Func<int, int> Y() => delegate (int x) { return [|9|]; };
}";
                var expected = @"using System;
class TestClass
{
    Func<int, int> Y() => delegate (int x) { return NewMethod(); };

    private static int NewMethod()
    {
        return 9;
    }
}";
                TestExtractMethod(code, expected);
            }

            [WorkItem(528, "https://github.com/dotnet/roslyn/issues/528")]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void ExpressionBodiedMethodWithBlockBodiedSimpleLambdaExpression()
            {
                var code = @"using System;
class TestClass
{
    Func<int, int> Y() => f =>
    {
        return f * [|9|];
    };
}";
                var expected = @"using System;
class TestClass
{
    Func<int, int> Y() => f =>
    {
        return f * NewMethod();
    };

    private static int NewMethod()
    {
        return 9;
    }
}";
                TestExtractMethod(code, expected);
            }

            [WorkItem(528, "https://github.com/dotnet/roslyn/issues/528")]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void ExpressionBodiedMethodWithExpressionBodiedSimpleLambdaExpression()
            {
                var code = @"using System;
class TestClass
{
    Func<int, int> Y() => f => f * [|9|];
}";
                var expected = @"using System;
class TestClass
{
    Func<int, int> Y() => f => f * NewMethod();

    private static int NewMethod()
    {
        return 9;
    }
}";
                TestExtractMethod(code, expected);
            }

            [WorkItem(528, "https://github.com/dotnet/roslyn/issues/528")]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void ExpressionBodiedMethodWithBlockBodiedParenthesizedLambdaExpression()
            {
                var code = @"using System;
class TestClass
{
    Func<int, int> Y() => (f) =>
    {
        return f * [|9|];
    };
}";
                var expected = @"using System;
class TestClass
{
    Func<int, int> Y() => (f) =>
    {
        return f * NewMethod();
    };

    private static int NewMethod()
    {
        return 9;
    }
}";
                TestExtractMethod(code, expected);
            }

            [WorkItem(528, "https://github.com/dotnet/roslyn/issues/528")]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void ExpressionBodiedMethodWithExpressionBodiedParenthesizedLambdaExpression()
            {
                var code = @"using System;
class TestClass
{
    Func<int, int> Y() => (f) => f * [|9|];
}";
                var expected = @"using System;
class TestClass
{
    Func<int, int> Y() => (f) => f * NewMethod();

    private static int NewMethod()
    {
        return 9;
    }
}";
                TestExtractMethod(code, expected);
            }

            [WorkItem(528, "https://github.com/dotnet/roslyn/issues/528")]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void ExpressionBodiedMethodWithBlockBodiedAnonymousMethodExpressionInMethodArgs()
            {
                var code = @"using System;
class TestClass
{
    public int Prop => Method1(delegate()
    {
        return [|8|];
    });

    private int Method1(Func<int> p)
    {
        throw new NotImplementedException();
    }
}";
                var expected = @"using System;
class TestClass
{
    public int Prop => Method1(delegate()
    {
        return NewMethod();
    });

    private static int NewMethod()
    {
        return 8;
    }

    private int Method1(Func<int> p)
    {
        throw new NotImplementedException();
    }
}";
                TestExtractMethod(code, expected);
            }

            [WorkItem(528, "https://github.com/dotnet/roslyn/issues/528")]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void LeadingAndTrailingTriviaOnExpressionBodiedMethod()
            {
                var code = @"using System;
class TestClass
{
    int M1() => 1 + 2 + /*not moved*/ [|3|] /*not moved*/;

    void Cat() { }
}";
                var expected = @"using System;
class TestClass
{
    int M1() => 1 + 2 + /*not moved*/ NewMethod() /*not moved*/;

    private static int NewMethod()
    {
        return 3;
    }

    void Cat() { }
}";
                TestExtractMethod(code, expected);
            }

            #endregion

            [WorkItem(11155, "DevDiv_Projects/Roslyn")]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void AnonymousTypeMember1()
            {
                var code = @"using System;
 
class Program
{
    static void Main(string[] args)
    {
        var an = new { id = 123 };
        Console.Write(an.[|id|]); // here
    }
}
";
                ExpectExtractMethodToFail(code);
            }

            [WorkItem(544259)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void ExtractMethod_ConstructorInitializer()
            {
                var code = @"class Program
{
    public Program(string a, int b)
        : this(a, [|new Program()|])
    {
    }
}";
                var expected = @"class Program
{
    public Program(string a, int b)
        : this(a, NewMethod())
    {
    }

    private static Program NewMethod()
    {
        return new Program();
    }
}";

                TestExtractMethod(code, expected);
            }

            [WorkItem(543984)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void ExtractMethod_UnsafeAddressTaken()
            {
                var code = @"class C
{
    unsafe void M()
    {
        int i = 5;
        int* j = [|&i|];
    }
}";
                var expected = @"class C
{
    unsafe void M()
    {
        int i = 5;
        int* j = GetJ(out i);
    }

    private static unsafe int* GetJ(out int i)
    {
        return &i;
    }
}";

                ExpectExtractMethodToFail(code, expected);
            }

            [WorkItem(544387)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void ExtractMethod_PointerType()
            {
                var code = @"class Test
{
    static int x = 0;
    unsafe static void Main()
    {
        fixed (int* p1 = &x)
        {
            int a1 = [|*p1|];
        }
    }
}";
                var expected = @"class Test
{
    static int x = 0;
    unsafe static void Main()
    {
        fixed (int* p1 = &x)
        {
            int a1 = GetA1(p1);
        }
    }

    private static unsafe int GetA1(int* p1)
    {
        return *p1;
    }
}";

                TestExtractMethod(code, expected);
            }

            [WorkItem(544514)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void ExtractMethod_AnonymousType()
            {
                var code = @"public class Test
{
    public static void Main()
    {
        var p1 = new { Price = 45 };
        var p2 = new { Price = 50 };
 
        [|p1 = p2;|]
    }
}";
                var expected = @"public class Test
{
    public static void Main()
    {
        var p1 = new { Price = 45 };
        var p2 = new { Price = 50 };

        NewMethod(p2);
    }

    private static void NewMethod(object p2)
    {
        object p1 = p2;
    }
}";

                ExpectExtractMethodToFail(code, expected);
            }

            [WorkItem(544920)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void ExtractMethod_StackAllocExpression()
            {
                var code = @"
unsafe class C
{
    static void Main()
    {
        void* p = [|stackalloc int[10]|];
    }
}
";
                var expected = @"
unsafe class C
{
    static void Main()
    {
        NewMethod();
    }

    private static void NewMethod()
    {
        void* p = stackalloc int[10];
    }
}
";

                TestExtractMethod(code, expected);
            }

            [WorkItem(539310)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void Readonly_Field_WrittenTo()
            {
                var code = @"class C
{
    private readonly int i;

    C()
    {
        [|i = 1;|]
    }
}";
                ExpectExtractMethodToFail(code);
            }

            [WorkItem(539310)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void Readonly_Field()
            {
                var code = @"class C
{
    private readonly int i;

    C()
    {
        i = 1;
        [|var x = i;|]
    }
}";
                var expected = @"class C
{
    private readonly int i;

    C()
    {
        i = 1;
        NewMethod();
    }

    private void NewMethod()
    {
        var x = i;
    }
}";
                TestExtractMethod(code, expected);
            }

            [WorkItem(545180)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void NodeHasSyntacticErrors()
            {
                var code = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
 
class Program
{
    static void Main(string[] args)
    {
        Expression<Func<int>> f3 = ()=>[|switch {|]
 
        };
    }
}
";
                ExpectExtractMethodToFail(code);
            }

            [WorkItem(545292)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void LocalConst()
            {
                var code = @"class Test
{
    public static void Main()
    {
        const int v = [|3|];
    }
}";
                ExpectExtractMethodToFail(code);
            }

            [WorkItem(545315)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void Nullable()
            {
                var code = @"using System;
class Program
{
    static void Main()
    {
        int? q = 10;
        [|Console.WriteLine(q);|]
    }
}";
                var expected = @"using System;
class Program
{
    static void Main()
    {
        int? q = 10;
        NewMethod(q);
    }

    private static void NewMethod(int? q)
    {
        Console.WriteLine(q);
    }
}";

                TestExtractMethod(code, expected);
            }

            [WorkItem(545263)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void SyntacticErrorInSelection()
            {
                var code = @"class Program
{
    static void Main(string[] args)
    {
        if ((true)NewMethod()[|)|]
        {
        }
    }
 
    private static string NewMethod()
    {
        return ""true"";
    }
}
";
                ExpectExtractMethodToFail(code);
            }

            [WorkItem(544497)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void StackAllocExpression()
            {
                var code = @"using System;
class Test
{
    unsafe static void Main()
    {
        void* buffer = [|stackalloc char[16]|];
    }
}";
                var expected = @"using System;
class Test
{
    unsafe static void Main()
    {
        NewMethod();
    }

    private static unsafe void NewMethod()
    {
        void* buffer = stackalloc char[16];
    }
}";
                TestExtractMethod(code, expected);
            }

            [WorkItem(545503)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void MethodBodyInScript()
            {
                var code = @"#r ""System.Management""
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management; // WMI APIs

var output = new StringBuilder();
void CollectInfo(string title, string query, string[,] labelKeys)
{
    output.AppendLine(title);
    output.AppendLine(""-----------------------------------"");
    [|var info = new ManagementObjectSearcher(query);
    foreach (var mgtobj in info.Get())
    {
        for (int row = 0; row < labelKeys.GetLength(0); row++)
        {
            output.AppendLine(labelKeys[row, 0] + mgtobj[labelKeys[row, 1]].ToString());
        }
    }
    output.AppendLine();|]
}";
                var expected = @"#r ""System.Management""
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management; // WMI APIs

var output = new StringBuilder();
void CollectInfo(string title, string query, string[,] labelKeys)
{
    output.AppendLine(title);
    output.AppendLine(""-----------------------------------"");
    NewMethod(query, labelKeys);
}

void NewMethod(string query, string[,] labelKeys)
{
    var info = new ManagementObjectSearcher(query);
    foreach (var mgtobj in info.Get())
    {
        for (int row = 0; row < labelKeys.GetLength(0); row++)
        {
            output.AppendLine(labelKeys[row, 0] + mgtobj[labelKeys[row, 1]].ToString());
        }
    }
    output.AppendLine();
}";
                TestExtractMethod(code, expected, parseOptions: new CSharpParseOptions(kind: SourceCodeKind.Script));
            }

            [WorkItem(544920)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void NoSimplificationForStackAlloc()
            {
                var code = @"using System;
 
unsafe class C
{
    static void Main()
    {
        void* p = [|stackalloc int[10]|];
        Console.WriteLine((int)p);
    }
}";
                var expected = @"using System;
 
unsafe class C
{
    static void Main()
    {
        void* p = NewMethod();
        Console.WriteLine((int)p);
    }

    private static void* NewMethod()
    {
        void* p = stackalloc int[10];
        return p;
    }
}";
                TestExtractMethod(code, expected);
            }

            [WorkItem(545553)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void CheckStatementContext1()
            {
                var code = @"using System;

class X
{
    static void Foo(Func<X, byte> x, string y) { Console.WriteLine(1); }
    static void Foo(Func<int?, byte> x, object y) { Console.WriteLine(2); }

    const int Value = 1000;

    static void Main()
    {
        unchecked
        {
            [|Foo(X => (byte)X.Value, null);|] // Extract method
        }
    }
}";
                var expected = @"using System;

class X
{
    static void Foo(Func<X, byte> x, string y) { Console.WriteLine(1); }
    static void Foo(Func<int?, byte> x, object y) { Console.WriteLine(2); }

    const int Value = 1000;

    static void Main()
    {
        unchecked
        {
            NewMethod(); // Extract method
        }
    }

    private static void NewMethod()
    {
        unchecked
        {
            Foo(X => (byte)X.Value, null);
        }
    }
}";
                TestExtractMethod(code, expected);
            }

            [WorkItem(545553)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void CheckStatementContext2()
            {
                var code = @"using System;

class X
{
    static void Foo(Func<X, byte> x, string y) { Console.WriteLine(1); }
    static void Foo(Func<int?, byte> x, object y) { Console.WriteLine(2); }

    const int Value = 1000;

    static void Main()
    {
        unchecked
        [|{
            Foo(X => (byte)X.Value, null); // Extract method
        }|]
    }
}";
                var expected = @"using System;

class X
{
    static void Foo(Func<X, byte> x, string y) { Console.WriteLine(1); }
    static void Foo(Func<int?, byte> x, object y) { Console.WriteLine(2); }

    const int Value = 1000;

    static void Main()
    {
        NewMethod();
    }

    private static void NewMethod()
    {
        unchecked
        {
            Foo(X => (byte)X.Value, null); // Extract method
        }
    }
}";
                TestExtractMethod(code, expected);
            }

            [WorkItem(545553)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void CheckStatementContext3()
            {
                var code = @"using System;

class X
{
    static void Foo(Func<X, byte> x, string y) { Console.WriteLine(1); }
    static void Foo(Func<int?, byte> x, object y) { Console.WriteLine(2); }

    const int Value = 1000;

    static void Main()
    {
        unchecked
        {
            [|{
                Foo(X => (byte)X.Value, null); // Extract method
            }|]
        }
    }
}";
                var expected = @"using System;

class X
{
    static void Foo(Func<X, byte> x, string y) { Console.WriteLine(1); }
    static void Foo(Func<int?, byte> x, object y) { Console.WriteLine(2); }

    const int Value = 1000;

    static void Main()
    {
        unchecked
        {
            NewMethod();
        }
    }

    private static void NewMethod()
    {
        unchecked
        {
            Foo(X => (byte)X.Value, null); // Extract method
        }
    }
}";
                TestExtractMethod(code, expected);
            }

            [WorkItem(545553)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void CheckExpressionContext1()
            {
                var code = @"using System;

class X
{
    static int Foo(Func<X, byte> x, string y) { return 1; } // This Foo is invoked before refactoring
    static int Foo(Func<int?, byte> x, object y) { return 2; }

    const int Value = 1000;

    static void Main()
    {
        var s = unchecked(1 + [|Foo(X => (byte)X.Value, null)|]);
    }
}";
                var expected = @"using System;

class X
{
    static int Foo(Func<X, byte> x, string y) { return 1; } // This Foo is invoked before refactoring
    static int Foo(Func<int?, byte> x, object y) { return 2; }

    const int Value = 1000;

    static void Main()
    {
        var s = unchecked(1 + NewMethod());
    }

    private static int NewMethod()
    {
        return unchecked(Foo(X => (byte)X.Value, null));
    }
}";
                TestExtractMethod(code, expected);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void AwaitExpression_Normal_SingleStatement()
            {
                var code = @"using System;
using System.Threading.Tasks;

class X
{
    public async void Test()
    {
        [|await Task.Run(() => { });|]
    }
}";
                var expected = @"using System;
using System.Threading.Tasks;

class X
{
    public async void Test()
    {
        await NewMethod();
    }

    private static async Task NewMethod()
    {
        await Task.Run(() => { });
    }
}";
                TestExtractMethod(code, expected);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void AwaitExpression_Normal_Expression()
            {
                var code = @"using System;
using System.Threading.Tasks;

class X
{
    public async void Test()
    {
        [|await Task.Run(() => { })|];
    }
}";
                var expected = @"using System;
using System.Threading.Tasks;

class X
{
    public async void Test()
    {
        await NewMethod();
    }

    private static async Task NewMethod()
    {
        await Task.Run(() => { });
    }
}";
                TestExtractMethod(code, expected);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void AwaitExpression_Normal_MultipleStatements()
            {
                var code = @"using System;
using System.Threading.Tasks;

class X
{
    public async void Test()
    {
        [|await Task.Run(() => { });

        await Task.Run(() => 1);

        return;|]
    }
}";
                var expected = @"using System;
using System.Threading.Tasks;

class X
{
    public async void Test()
    {
        await NewMethod();
    }

    private static async Task NewMethod()
    {
        await Task.Run(() => { });

        await Task.Run(() => 1);

        return;
    }
}";
                TestExtractMethod(code, expected);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void AwaitExpression_Normal_ExpressionWithReturn()
            {
                var code = @"using System;
using System.Threading.Tasks;

class X
{
    public async void Test()
    {
        await Task.Run(() => { });

        [|await Task.Run(() => 1)|];
    }
}";
                var expected = @"using System;
using System.Threading.Tasks;

class X
{
    public async void Test()
    {
        await Task.Run(() => { });

        await NewMethod();
    }

    private static async Task<int> NewMethod()
    {
        return await Task.Run(() => 1);
    }
}";
                TestExtractMethod(code, expected);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void AwaitExpression_Normal_ExpressionInAwaitExpression()
            {
                var code = @"using System;
using System.Threading.Tasks;

class X
{
    public async void Test()
    {
        await [|Task.Run(() => 1)|];
    }
}";
                var expected = @"using System;
using System.Threading.Tasks;

class X
{
    public async void Test()
    {
        await NewMethod();
    }

    private static Task<int> NewMethod()
    {
        return Task.Run(() => 1);
    }
}";
                TestExtractMethod(code, expected);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void AwaitExpression_Normal_StatementWithAwaitExpressionWithReturn()
            {
                var code = @"using System;
using System.Threading.Tasks;

class X
{
    public async void Test()
    {
        [|await Task.Run(() => 1);|]
    }
}";
                var expected = @"using System;
using System.Threading.Tasks;

class X
{
    public async void Test()
    {
        await NewMethod();
    }

    private static async Task NewMethod()
    {
        await Task.Run(() => 1);
    }
}";
                TestExtractMethod(code, expected);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void AwaitExpression_Normal_AwaitWithReturnParameter()
            {
                var code = @"using System;
using System.Threading.Tasks;

class X
{
    public async void Test(int i)
    {
        [|await Task.Run(() => i++);|]

        Console.WriteLine(i);
    }
}";
                var expected = @"using System;
using System.Threading.Tasks;

class X
{
    public async void Test(int i)
    {
        i = await NewMethod(i);

        Console.WriteLine(i);
    }

    private static async Task<int> NewMethod(int i)
    {
        await Task.Run(() => i++);
        return i;
    }
}";
                TestExtractMethod(code, expected);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void AwaitExpression_Normal_AwaitWithReturnParameter_Error()
            {
                var code = @"using System;
using System.Threading.Tasks;

class X
{
    public async void Test(int i)
    {
        var i2 = [|await Task.Run(() => i++)|];

        Console.WriteLine(i);
    }
}";
                ExpectExtractMethodToFail(code);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void AwaitExpression_AsyncLambda()
            {
                // this is an error case. but currently, I didn't blocked this. but we could if we want to.
                var code = @"using System;
using System.Threading.Tasks;

class X
{
    public void Test(Func<Task<int>> a)
    {
        Test([|async () => await Task.Run(() => 1)|]);
    }
}";
                var expected = @"using System;
using System.Threading.Tasks;

class X
{
    public void Test(Func<Task<int>> a)
    {
        Test(NewMethod());
    }

    private static Func<Task<int>> NewMethod()
    {
        return async () => await Task.Run(() => 1);
    }
}";
                TestExtractMethod(code, expected);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void AwaitExpression_AsyncLambda_Body()
            {
                // this is an error case. but currently, I didn't blocked this. but we could if we want to.
                var code = @"using System;
using System.Threading.Tasks;

class X
{
    public void Test(Func<Task<int>> a)
    {
        Test(async () => [|await Task.Run(() => 1)|]);
    }
}";
                var expected = @"using System;
using System.Threading.Tasks;

class X
{
    public void Test(Func<Task<int>> a)
    {
        Test(async () => await NewMethod());
    }

    private static async Task<int> NewMethod()
    {
        return await Task.Run(() => 1);
    }
}";
                TestExtractMethod(code, expected);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void AwaitExpression_AsyncLambda_WholeExpression()
            {
                // this is an error case. but currently, I didn't blocked this. but we could if we want to.
                var code = @"using System;
using System.Threading.Tasks;

class X
{
    public void Test(Func<Task<int>> a)
    {
        [|Test(async () => await Task.Run(() => 1));|]
    }
}";
                var expected = @"using System;
using System.Threading.Tasks;

class X
{
    public void Test(Func<Task<int>> a)
    {
        NewMethod();
    }

    private void NewMethod()
    {
        Test(async () => await Task.Run(() => 1));
    }
}";
                TestExtractMethod(code, expected);
            }

            [WorkItem(1064798)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void ExpressionInStringInterpolation()
            {
                var code = @"using System;

class X
{
    public void Test()
    {
        var s = $""Alpha Beta {[|int.Parse(""12345"")|]} Gamma"";
    }
}";
                var expected = @"using System;

class X
{
    public void Test()
    {
        var s = $""Alpha Beta {NewMethod()} Gamma"";
    }

    private static int NewMethod()
    {
        return int.Parse(""12345"");
    }
}";
                TestExtractMethod(code, expected);
            }

            [WorkItem(859493)]
            [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
            public void ExpressionInYieldReturnStatement()
            {
                var code = @"using System;
using System.Collections.Generic;

public class Test<T> 
{
    protected class Node
    {
        internal Node(T item) { this._item = item; }
        internal T _item;
    }
    protected Node _current = null;

    public IEnumerator<T> GetEnumerator()
    {
        Node _localCurrent = _current;

        while (true)
        {
            yield return [|_localCurrent._item|];
        }
    }
}";
                var expected = @"using System;
using System.Collections.Generic;

public class Test<T> 
{
    protected class Node
    {
        internal Node(T item) { this._item = item; }
        internal T _item;
    }
    protected Node _current = null;

    public IEnumerator<T> GetEnumerator()
    {
        Node _localCurrent = _current;

        while (true)
        {
            yield return GetItem(_localCurrent);
        }
    }

    private static T GetItem(Node _localCurrent)
    {
        return _localCurrent._item;
    }
}";
                TestExtractMethod(code, expected);
            }
        }

        [WorkItem(3147, "https://github.com/dotnet/roslyn/issues/3147")]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void HandleFormattableStringTargetTyping1()
        {
            const string code = CodeSnippets.FormattableStringType + @"
namespace N
{
    using System;

    class C
    {
        public void M()
        {
            var f = FormattableString.Invariant([|$""""|]);
        }
    }
}";

            const string expected = CodeSnippets.FormattableStringType + @"
namespace N
{
    using System;

    class C
    {
        public void M()
        {
            var f = FormattableString.Invariant(NewMethod());
        }

        private static FormattableString NewMethod()
        {
            return $"""";
        }
    }
}";

            TestExtractMethod(code, expected);
        }
    }
}
