using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    public int testInt = 0;

    public int testIntMethod = TestMethod3();

    public string TestProperty
    {
        get { return new object().ToString(); }
        private set { throw new System.Exception(); }
    }

    public void TestMethod1(in string msg)
    {
        Debug.Log("Test 1");
    }

    internal void TestMethod2()
    {
        Debug.Log("Test 2");
    }

    static int TestMethod3()
    {
        return -1;
    }

    public int Test() => throw new System.Exception();
}
