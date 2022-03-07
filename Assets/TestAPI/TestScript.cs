using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    public int testInt = 0;

    public string TestProperty
    {
        get { return new object().ToString(); }
        private set { throw new System.Exception(); }
    }

    public void SayHello(in string msg)
    {
        Debug.Log("Hello World");
    }

    internal void TestMethod1()
    {
        Debug.Log("Test 1");
    }
}
