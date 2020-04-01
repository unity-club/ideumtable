using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CoffeeTable;
using UnityEditor.Compilation;
using System.IO;
using System;
using UnityEditor;
using System.Linq;
using CoffeeTable.Common.Manifests.Networking;
using CoffeeTable.Common.Manifests;
using CoffeeTable.Publishers;

public class RuntimeTest : MonoBehaviour
{
	// Start is called before the first frame update
	async void Start()
	{
		//Debug.Log($"CoffeeTable is online: {Table.IsOnline}");
		//Table.Quit();
		;

	}

	private void ModifiedTest()
	{
		ApplicationInstanceInfo[] oldApps = new[]
{
			new ApplicationInstanceInfo()
			{
				DestinationId = 1,
				IsSimulator = true,
				Layout = ApplicationLayout.Fullscreen,
				State = ApplicationState.Running
			},
			new ApplicationInstanceInfo()
			{
				DestinationId = 2,
				IsSimulator = true,
				Layout = ApplicationLayout.Fullscreen,
				State = ApplicationState.Running
			},
			new ApplicationInstanceInfo()
			{
				DestinationId = 3,
				IsSimulator = true,
				Layout = ApplicationLayout.Fullscreen,
				State = ApplicationState.Running
			},
						new ApplicationInstanceInfo()
			{
				DestinationId = 10,
				IsSimulator = true,
				Layout = ApplicationLayout.Fullscreen,
				State = ApplicationState.Running
			},
									new ApplicationInstanceInfo()
			{
				DestinationId = 7,
				IsSimulator = true,
				Layout = ApplicationLayout.Fullscreen,
				State = ApplicationState.Running
			},
												new ApplicationInstanceInfo()
			{
				DestinationId = 5,
				IsSimulator = true,
				Layout = ApplicationLayout.Fullscreen,
				State = ApplicationState.Running
			},
		};

		ApplicationInstanceInfo[] newApps = new[]
		{
			oldApps[0],
			oldApps[1],
			new ApplicationInstanceInfo()
			{
				DestinationId = 3,
				IsSimulator = true,
				Layout = ApplicationLayout.Fullscreen,
				State = ApplicationState.Starting
			},
						new ApplicationInstanceInfo()
			{
				DestinationId = 32,
				IsSimulator = true,
				Layout = ApplicationLayout.Fullscreen,
				State = ApplicationState.Starting
			},
									new ApplicationInstanceInfo()
			{
				DestinationId = 54,
				IsSimulator = true,
				Layout = ApplicationLayout.Fullscreen,
				State = ApplicationState.Starting
			},
												new ApplicationInstanceInfo()
			{
				DestinationId = 9,
				IsSimulator = true,
				Layout = ApplicationLayout.Fullscreen,
				State = ApplicationState.Starting
			},
		};

		var modified = TableExtensions.GetModified(oldApps, newApps,
			(a, b) => ApplicationInstanceInfo.IdComparer.Equals(a, b),
			(oldApp, newApp) => !ApplicationInstanceInfo.Equals(oldApp, newApp)).ToList();
		;
		var i = modified;
		;
	}

	// Update is called once per frame
	void Update()
	{

	}
}
