using Korn.Hooking;

public unsafe class Program
{
    public static void Main()
    {
        var writeLineHook = MethodHook.Create((Action<string?>)Console.WriteLine);
        writeLineHook.AddHook((Delegate)HookedWriteLine);
        writeLineHook.AddHook((Delegate)Hooked2WriteLine);
        writeLineHook.Hook();

        /*
        var readLineHook = MethodHook.Create((Func<string?>)Console.ReadLine);
        readLineHook.AddHook((Delegate)HookedReadLine);
        readLineHook.AddHook((Delegate)Hooked2ReadLine);
        readLineHook.Hook();
        */

        Console.WriteLine("a");

        Console.ReadLine();
    }

    public static bool HookedWriteLine(ref string? text)
    {
        Console.WriteLine("Hello from 1-th hook!");

        if (text is null)
            return true;

        text += " hooked!";

        return true;
    }

    public static bool Hooked2WriteLine(ref string? text)
    {
        Console.WriteLine("Hello from 2-th hook!");

        if (text is null)
            return true;

        text = text.Substring(4);

        return true;
    }

    public static bool HookedReadLine(ref string? result)
    {
        if (DateTime.Now.Ticks % 3 == 0)
        {
            result = "bad data!";
            return false;
        }

        return true;
    }

    public static bool Hooked2ReadLine(ref string? result)
    {
        if (result is null)
            result = "hooked text!";

        return false;
    }
}