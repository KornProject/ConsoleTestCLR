using Korn.Hooking;

public unsafe class Program
{
    public static void Main()
    {        
        /*
        var a = new MethodStatement(((Delegate)A).Method);
        var b = new MethodStatement(((Delegate)B).Method);

        a.EnsureMethodIsCompiled();
        b.EnsureMethodIsCompiled();

        Console.WriteLine($"{a.MethodPointer:X} {b.MethodPointer:X}");
        Console.ReadLine();
        A();
        Console.ReadLine();        
        */

        var hook = MethodHook.Create(A);
        hook.AddHook(B);
        hook.AddHook(C);
        hook.Enable();

        Console.WriteLine("pre");
        Console.ReadLine();
        var result = A(1000, 100, 10, 1);
        Console.WriteLine(result);

        Console.WriteLine("end");
        Console.ReadLine();
    }

    static int A(int a1, int a2, int a3, int a4)
    {
        return a1 + a2 + a3 + a4;
    }

    static bool B(ref int a1, ref int a2, ref int a3, ref int a4, ref int result)
    {
        a1 *= 2;
        return true;
    }

    static bool C(ref int a1, ref int a2, ref int a3, ref int a4, ref int result)
    {
        a2 *= 2;
        return true;
    }
}