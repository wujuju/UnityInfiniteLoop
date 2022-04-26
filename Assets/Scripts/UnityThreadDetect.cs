using System;
using System.IO;
using System.Threading;
using UnityEngine;

/// <summary>
/// 开另外一个线程检测unity是否被卡死
/// </summary>
public static class UnityThreadDetect
{
    private static int check_interval = 3000;//检测间隔
    private static string savePath = @"D:\log.txt";
    public static void Start()
    {
        if (!File.Exists(savePath))
        {
            File.CreateText(savePath);
        }

        System.Console.WriteLine("version:3");
        new Thread(CheckMainThread).Start();
    }

    static void CheckMainThread()
    {
        int frame = 0;

        while (HookUtils.isPlaying)
        {
            frame = HookUtils.currFrame;
            Thread.Sleep(check_interval);
            string str = HookUtils.OutString();
            if (str != "")
                System.Console.WriteLine(str);
            if (frame == HookUtils.currFrame)
            {
                File.WriteAllText(savePath, HookUtils.OutString());
            }
        }
    }
}
