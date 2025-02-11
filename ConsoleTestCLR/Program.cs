using System.Runtime.CompilerServices;

public unsafe class Program
{
    public static void Main()
    {
        while (true)
        {
            Console.WriteLine($"{((Action)A).Method.MethodHandle.GetFunctionPointer():X}");
            Console.WriteLine(GetMethodStatement(((Action)A).Method.MethodHandle.GetFunctionPointer()));
            Console.ReadLine();
            RuntimeHelpers.PrepareMethod(((Action)A).Method.MethodHandle);
        }

        MethodStatement GetMethodStatement(nint method)
        {
            if ((*(uint*)method & 0xFFFFFFFF) == 0x66666666)
                method += sizeof(int);

            if (*(ushort*)method == 0x25FF &&
                (*(uint*)(method + 0x06) & 0xFFFFFF) == 0x158B4C &&
                *(ushort*)(method + 0x0D) == 0x25FF)
            {
                var innerMethod = *(nint*)(method + 6 + *(int*)(method + 2));
                if (innerMethod - method == 0x06)
                    return new (method, MethodType.NotCompiledStub);
                method = innerMethod;

                if ((*(uint*)method & 0xFFFFFF) == 0x058B48 &&
                    *(byte*)(method + 0x07) == 0x66 &&
                    *(ushort*)(method + 0x0A) == 0x0674)
                    return new(method, MethodType.ThresholdCounterStub);

                return new(method, MethodType.DirectNativeStub);
            }

            // push rbp
            if (*(byte*)method == 0x55)
                return new(method, MethodType.Native);

            return new(method, MethodType.UnknownStub);
        }
    }

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