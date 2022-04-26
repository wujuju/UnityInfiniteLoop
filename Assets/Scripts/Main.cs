using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class Main : MonoBehaviour
{
    bool isInfiniteLoop;
    // Start is called before the first frame update
    void Start()
    {
        UnitySystemConsoleRedirector.Redirect();
        UnityThreadDetect.Start();
        HookUtils.isPlaying = true;
        TestA();
    }

    void TestA()
    {
        int b = 0;
        for (int i = 0; i < 100; i++)
        {
            b++;
        }
    }

    void TestB()
    {
        int b = 0;
        while (true)
        {
            b++;
        }
    }

    void TestC()
    {
        int b = 0;
    }

    public void BtnClick()
    {
        isInfiniteLoop = true;
    }

    // Update is called once per frame
    void Update()
    {
        HookUtils.Clear();
        HookUtils.currFrame++;
        if (isInfiniteLoop)
        {
            isInfiniteLoop = false;
            TestB();
        }
        TestC();
    }

    void OnDestroy()
    {
        HookUtils.isPlaying = false;
    }
}
