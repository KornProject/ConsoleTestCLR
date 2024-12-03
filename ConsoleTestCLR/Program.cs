using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

unsafe class Program 
{
    public static void Main()
    {
        const int MDSimpleEnum = 0;

        var domain = clr_AppDomain.AppDomain;
        var rootAssembly = domain->RootAssembly;
        var assemblies = domain->Assemblies.Array.ToList();

        foreach (var assemblyAddress in assemblies)
        {
            var assembly = (clr_Assembly*)assemblyAddress;
            var module = assembly->Module;
            var peAssembly = module->PEAssembly;
            var import = peAssembly->MDImport;

            clr_HENUMTypeDefInternalHolder hEnum;
            hEnum.Enum.EnumType = MDSimpleEnum;
            hEnum.InternalImport = import;

            if (import->VTable->EnumTypeDefInit(import, &hEnum.Enum) < 0)
                throw new Exception();

            hEnum.Acquired = 1;
            var count = hEnum.Enum.Count;
            var token = 0x2000000;

            while(import->EnumNext(&hEnum.Enum, &token) != 0)
            {
                var result = clr_ClassLoader.LoadTypeDefOrRef(module, token);

                _ = 3;
            }


            _ = 3;
        }

        _ = 3;
    }
}

static class ArrayListExtensions
{
    public static List<nint> ToList<T>(this clr_ArrayList<T> arrayList) where T : unmanaged => new ArrayListInterator<T>(arrayList).ToList();
}

unsafe struct ArrayListInterator<T> where T : unmanaged
{
    public ArrayListInterator(clr_ArrayList<T> array) => this.array = array;

    clr_ArrayList<T> array;

    public List<nint> ToList()
    {
        List<nint> result = [];

        var count = array.Count;
        var firstBlock = array.FirstBlock;
        for (var i = 0; i < firstBlock.BlockSize; i++)
        {
            var element = firstBlock.GetArrayElement(i);
            result.Add((nint)element);
            if (result.Count == count)
                goto RETURN;
        }

        var next = firstBlock.Next;
        while (next is not null)
        {
            for (var i = 0; i < next->BlockSize; i++)
            {
                var element = next->GetArrayElement(i);
                result.Add((nint)element);
                if (result.Count == count)
                    goto RETURN;
            }

            next = next->Next;
        }

        RETURN:
        return result;
    }
}

unsafe struct clr_ClassLoader
{
    static void* LoadTypeDefOrRefThrowing = (void*)(CoreClr.ModuleHandle + UnsafeAccessOffsets.Coreclr_ClassLoader_LoadTypeDefOrRefThrowing);

    static int a;

    public static clr_TypeHandle* LoadTypeDefOrRef(clr_Module* moduleBaseVTable, int token)
    {
        clr_TypeHandle result;

        var stub = (byte*)Interop.VirtualAlloc(0, 0x1000, MemoryState.Commit, MemoryProtect.ExecuteReadWrite);
        var stubShellcode = 
            "41 50 41 51 53 48 8B 44 24 60 48 8B 4C 24 58 48 8B 54 24 50 4C 8B 4C 24 48 4C 8B 44 24 40 48 8B 5C 24 38 53 48 8B 5C 24 38 53 48 8B 5C 24 38 53 FF D0 5B 48 89 5C 24 38 5B 48 89 5C 24 38 5B 48 89 5C 24 38 4C 89 44 24 40 4C 89 4C 24 48 48 89 54 24 50 48 89 4C 24 58 5B 41 59 41 58 C3"
            .Split(' ')
            .Select(p => byte.Parse(p, System.Globalization.NumberStyles.HexNumber))
            .ToArray();

        Interop.WriteMemory(stub, stubShellcode);

        var cdelcStub = (delegate* unmanaged<clr_TypeHandle*, clr_ModuleBase.vtable*, long, void*, void*, uint, ClassLoadLevel, clr_TypeHandle*>)stub;
                
        var resultP = &result;
        Console.WriteLine($"{(nint)stub:X}, args: call {(nint)LoadTypeDefOrRefThrowing:X}, typeHandle* {(nint)resultP:X}, vtable: {(nint)moduleBaseVTable->AsModuleBase->VTable:X}, token: {token:X}, null, null, 0, 5");
        Console.ReadLine();

        a = 0x7071C;

        ((delegate* unmanaged<clr_TypeHandle*, clr_ModuleBase.vtable*, long, void*, void*, uint, ClassLoadLevel, clr_TypeHandle*>)LoadTypeDefOrRefThrowing)(&result, moduleBaseVTable->AsModuleBase->VTable, token, null, null, 0, ClassLoadLevel.Loaded);


        return &result;
    }
}

enum ClassLoadLevel
{
    LoadBegin,
    LoadUnrestoredTypeKey,
    LoadUnrestored,
    LoadApproxParents,
    LoadExactParents,
    DependeciesLoaded,
    Loaded,
    LoadLevelFinal
}

[StructLayout(LayoutKind.Explicit, Size = 0x08)]
unsafe struct clr_TypeHandle
{
    [FieldOffset(0x00)]
    public nint Value;
}

[StructLayout(LayoutKind.Explicit)]
unsafe struct clr_AppDomain
{
    static clr_AppDomain** appDomainPointer = (clr_AppDomain**)(CoreClr.ModuleHandle + UnsafeAccessOffsets.Coreclr_AppDomain_m_pTheAppDomain);
    public static clr_AppDomain* AppDomain = *appDomainPointer;

    [FieldOffset(0x4B8)]
    public clr_DomainAssemblyList Assemblies;

    [FieldOffset(0x590)]
    public clr_Assembly* RootAssembly;
}

[StructLayout(LayoutKind.Explicit, Size = 0x58)]
unsafe struct clr_Assembly
{
    [FieldOffset(0x18)]
    public clr_Module* Module;

    [FieldOffset(0x30)]
    public bool IsDynamic;

    [FieldOffset(0x34)]
    public bool IsCollectible;

    [FieldOffset(0x54)]
    public bool IsInstrumentedStatus;
}

[StructLayout(LayoutKind.Explicit, Size = 0xA8)]
unsafe struct clr_ModuleBase
{
    [FieldOffset(0x00)]
    public vtable* VTable;

    [FieldOffset(0xC8)]
    public clr_Assembly* Assembly;

    [StructLayout(LayoutKind.Explicit)]
    public struct vtable
    {

    }
}

[StructLayout(LayoutKind.Explicit, Size = 0x3B0)]
unsafe struct clr_Module
{
    public clr_ModuleBase* AsModuleBase
    {
        get
        {
            fixed (clr_Module* pointer = &this)
                return (clr_ModuleBase*)pointer;
        }
    }

    [FieldOffset(0xA8)]
    public clr_Utf8String SimpleName;

    [FieldOffset(0xB0)]
    public clr_PEAssembly* PEAssembly;

    [FieldOffset(0x2E8)]
    public clr_DomainLocalModule* ModuleID;
}

[StructLayout(LayoutKind.Explicit)]
unsafe struct clr_DomainLocalModule
{
    [FieldOffset(0x00)]
    public clr_DomainAssembly* DomainAssembly;
}

[StructLayout(LayoutKind.Explicit)]
unsafe struct clr_PEAssembly
{
    [FieldOffset(0x18)]
    public clr_IMDInternalImport* MDImport;
}

[StructLayout(LayoutKind.Explicit)]
unsafe struct clr_IMDInternalImport
{
    [FieldOffset(0x00)]
    public vtable* VTable;

    [FieldOffset(0x10)]
    public clr_CLiteWeightStgdb<clr_CMiniMd> LiteWeightStgdb;

    public int EnumNext(clr_HENUMInternal* hEnum, int* token)
    {
        int current = hEnum->U.Current;
        if ((uint)current >= hEnum->U.End )
            return 0;

        if (hEnum->EnumType != 0)
        {
            hEnum->U.Current = current + 1;
            *token = *((int*)&hEnum->U4 + current);
        }
        else
        {
            *token = hEnum->Kind | current;
            ++hEnum->U.Current;
        }

        return 1;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct vtable
    {
        [FieldOffset(0x20)]
        public delegate* unmanaged<clr_IMDInternalImport*, clr_HENUMInternal*, clr_HResult> EnumTypeDefInit;
    }
}

unsafe struct clr_CLiteWeightStgdb<T> where T : unmanaged
{
    public clr_CMiniMd MiniMd;
}

[StructLayout(LayoutKind.Explicit)]
unsafe struct clr_CMiniMd
{
    [FieldOffset(0x08)]
    public clr_CMiniMdScheme Scheme;
}

[StructLayout(LayoutKind.Explicit, Size = 0xD0)]
unsafe struct clr_CMiniMdScheme
{
    [FieldOffset(0x18)]
    public fixed int Records[45];
}

enum clr_HResult : int { }

[StructLayout(LayoutKind.Explicit, Size = 0x48)]
unsafe struct clr_HENUMTypeDefInternalHolder
{
    [FieldOffset(0x00)]
    public clr_IMDInternalImport* InternalImport;

    [FieldOffset(0x08)]
    public clr_HENUMInternal Enum;

    [FieldOffset(0x40)]
    public int Acquired;
}

[StructLayout(LayoutKind.Explicit, Size = 0x38)]
unsafe struct clr_HENUMInternal
{
    [FieldOffset(0x00)]
    public int Kind;

    [FieldOffset(0x04)]
    public int Count;

    [FieldOffset(0x08)]
    public int EnumType;

    [FieldOffset(0x0C)]
    public Unnamed U;

    [FieldOffset(0x18)]
    public D6B39EC5995399DFFF9242F54E85023C U4;

    [StructLayout(LayoutKind.Explicit, Size = 0x0C)]
    public struct Unnamed
    {
        [FieldOffset(0x00)]
        public int Start;

        [FieldOffset(0x04)]
        public int End;

        [FieldOffset(0x08)]
        public int Current;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x20)]
    public struct D6B39EC5995399DFFF9242F54E85023C
    {
        [FieldOffset(0x00)]
        public long AlignPad;
    }
}

unsafe struct clr_Utf8String
{
    public nint Address;

    public override string ToString() => ReadFromMemory(Address);

    public string ReadFromMemory(nint address) => new string((sbyte*)address);
}

[StructLayout(LayoutKind.Explicit)]
unsafe struct clr_DomainAssemblyList
{
    [FieldOffset(0x00)]
    public clr_ArrayList<clr_Assembly> Array;
}

unsafe struct clr_DomainAssembly
{

}

unsafe struct clr_ArrayList<T> where T : unmanaged
{
    public int Count;

    public clr_FirstArrayListBlock<T> FirstBlock;
}

[StructLayout(LayoutKind.Sequential, Pack = 0)]
unsafe struct clr_ArrayListBlock<T> where T : unmanaged
{
    public clr_ArrayListBlock<T>* Next;
    public int BlockSize;
    int padding;
    public T* GetArrayElement(int index)
    {
        fixed (clr_ArrayListBlock<T>* pointer = &this)
            return (T*)*((nint*)(pointer + 1) + index);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 0)]
unsafe struct clr_FirstArrayListBlock<T> where T : unmanaged
{
    const int ARRAY_BLOCK_SIZE_START = 5;

    public clr_ArrayListBlock<T>* Next;
    public int BlockSize;
    int padding;
    fixed long array[ARRAY_BLOCK_SIZE_START];
    public T* GetArrayElement(int index) => (T*)(nint)array[index];
}

static class CoreClr
{
    static nint moduleHandle;

    public static nint ModuleHandle
    {
        get
        {
            if (moduleHandle == 0)
            {
                moduleHandle = Interop.GetModuleHandle("coreclr");
                if (moduleHandle == 0)
                    throw new Exception("no loaded coreclr.dll module in the process");
            }

            return moduleHandle;
        }
    }
}

static class UnsafeAccessOffsets
{
    public readonly static nint 
        Coreclr_AppDomain_m_pTheAppDomain = 0x488080,
        Coreclr_ClassLoader_LoadTypeDefOrRefThrowing = 0x27510;
}

unsafe static class Interop 
{
    const string kernel = "kernel32";

    [DllImport(kernel)] public static extern 
        nint GetModuleHandle(string name);

    [DllImport(kernel)] public static extern
        nint VirtualAlloc(nint address, long size, MemoryState allocationType, MemoryProtect protect);

    [DllImport(kernel)] public static extern
        bool VirtualFree(nint address, long size, MemoryFreeType freeType);

    public static void CopyMemory(void* to, void* from, int byteLength) => Buffer.MemoryCopy(from, to, byteLength, byteLength);
    
    public static void WriteMemory(void* to, void* from, int len) => CopyMemory(to, from, len);

    public static void WriteMemory(void* str, byte[] array)
    {
        fixed (byte* pointer = array)
            WriteMemory(str, pointer, array.Length);
    }
}

unsafe struct MBI
{
    public nint BaseAddress;
    public nint AllocationBase;
    public uint AllocationProtect;
    public nint RegionSize;
    public int State;
    public int Protect;
    public int Type;
}

public enum MemoryFreeType
{
    Decommit = 0x4000,
    Release = 0x8000,
}

public enum MemoryState
{
    Commit = 0x1000,
    Free = 0x10000,
    Reserve = 0x2000
}

[Flags]
public enum MemoryProtect
{
    ZeroAccess = 0,
    NoAccess = 1,
    ReadOnly = 2,
    ReadWrite = 4,
    WriteCopy = 8,
    Execute = 16,
    ExecuteRead = 32,
    ExecuteReadWrite = 64,
    ExecuteWriteCopy = 128,
    Guard = 256,
    ReadWriteGuard = 260,
    NoCache = 512
}
