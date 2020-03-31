using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CoffeeTable;
using UnityEditor.Compilation;
using System.IO;
using System;
using UnityEditor;
using System.Linq;

public class RuntimeTest : MonoBehaviour
{
    // Start is called before the first frame update
    async void Start()
    {
		Debug.Log($"CoffeeTable is online: {Table.IsOnline}");
		Table.Quit();
		;
	}

    // Update is called once per frame
    void Update()
    {
        
    }
}
