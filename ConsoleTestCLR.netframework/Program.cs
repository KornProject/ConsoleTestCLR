using System;
using System.Runtime.CompilerServices;

public unsafe class Program
{
    public static void Main()
    {
        while (true)
        {
            Console.WriteLine(Hex(((Action)A).Method.MethodHandle.GetFunctionPointer()));
            Console.WriteLine(GetMethodStatement(((Action)A).Method.MethodHandle.GetFunctionPointer()));
            Console.ReadLine();
            RuntimeHelpers.PrepareMethod(((Action)A).Method.MethodHandle);
            A();
        }

        MethodStatement GetMethodStatement(IntPtr method)
        {
            // call rel32
            if (*(byte*)method == 0xE8)
                return new MethodStatement(method, MethodType.NotCompiledStub);

            // jmp rel32
            if (*(byte*)method == 0xE9)
                method = method + 5 + *(int*)(method + 1);

            // push rbp
            if (*(byte*)method == 0x55)
                return new MethodStatement(method, MethodType.Native); 

            return new MethodStatement(method, MethodType.UnknownStub);
        }
    }

    static string Hex(IntPtr pointer) => Convert.ToString((long)pointer, 16);

    static void A()
    {
        Console.WriteLine("A");
    }

    static void B()
    {
        Console.WriteLine("B");
    }
}

enum MethodType
{
    NotCompiledStub,
    ThresholdCounterStub,
    DirectNativeStub,
    UnknownStub,
    Native
}

struct MethodStatement
{
    public MethodStatement(IntPtr pointer, MethodType type)
        => (Pointer, MethodType) = (pointer, type);

    public readonly IntPtr Pointer;
    public readonly MethodType MethodType;

    public override string ToString() => $"MethodStatement{{ Pointer: {Convert.ToString((long)Pointer, 16)}, MethodType: {MethodType} }}";
}