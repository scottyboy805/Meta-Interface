using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    public int testInt = 0;

    public void SayHello(in string msg)
    {
        Debug.Log("Hello World");
    }
}
