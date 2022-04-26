
using System.Collections.Generic;

public static class HookUtils
{
    public static bool isPlaying;
    public static int currFrame;
    static List<string> curFunList = new List<string>(2000);

    public static void Begin(string str)
    {
        curFunList.Add(str);
    }

    public static void Clear()
    {
        curFunList.Clear();
    }

    public static string OutString()
    {
        string str = "";
        for (int i = 0; i < curFunList.Count; i++)
        {
            str += curFunList[i] + "\n";
        }
        return str;
    }
}
