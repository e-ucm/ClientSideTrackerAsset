﻿/*
 * Copyright 2016 e-UCM (http://www.e-ucm.es/)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * This project has received funding from the European Union’s Horizon
 * 2020 research and innovation programme under grant agreement No 644187.
 * You may obtain a copy of the License at
 * 
 *	 http://www.apache.org/licenses/LICENSE-2.0 (link is external)
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
/// <summary>
/// This class is a temporary class for transitioning between previous interfaces to new versions of the tracker
/// </summary>

using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using System;
using System.IO;

namespace RAGE.Analytics
{
	using AssetPackage;

	public class Tracker : MonoBehaviour
	{
		public static DateTime START_DATE = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		public bool rawCopy;
		public static bool strictMode = true;
		private float nextFlush;
		private bool flushRequested = false;
		public float flushInterval = -1;
		[Range(3, int.MaxValue)]
		public float checkInterval = 3;
		private float nextCheck;
		public string storageType = "local";
		public string traceFormat = "csv";
		public string host;
		public string trackingCode;
		public Boolean debug = false;

		public string user;
		public string pass;

		public static TrackerAsset T
		{
			get { return TrackerAsset.Instance; }
		}

		void Awake ()
		{
			string [] splitted = host.Split ('/');
			string [] host_splitted = splitted [2].Split (':');
			string domain = host_splitted [0];
			int port = (host_splitted.Length > 1) ? int.Parse(host_splitted[1]) : (splitted[0] == "https:" ? 443 : 80);

			TrackerAsset.TraceFormats format;
			switch (traceFormat) {
			case "json":
				format = TrackerAsset.TraceFormats.json;
				break;
			case "xapi":
				format = TrackerAsset.TraceFormats.xapi;
				break;
			default:
				format = TrackerAsset.TraceFormats.csv;
				break;
			}

			TrackerAsset.StorageTypes storage;
			switch (storageType) {
			case "net":
				storage = TrackerAsset.StorageTypes.net;
				break;
			default:
				storage = TrackerAsset.StorageTypes.local;
				break;
			}

			TrackerAssetSettings tracker_settings = new TrackerAssetSettings()
			{
				Host = domain,
				TrackingCode = trackingCode,
				BasePath = "/api",
				Port = port,
				Secure = splitted[0] == "https:",
				StorageType = storage,
				TraceFormat = format,
				BatchSize = 10
			};

			TrackerAsset.Instance.Bridge = new UnityBridge();
			TrackerAsset.Instance.Settings = tracker_settings;
		}
        
		/// <summary>
		/// DONT USE THIS METHOD. UNITY INTERNAL MONOBEHAVIOUR.
		/// </summary>
		public void Start ()
		{
			if (!String.IsNullOrEmpty (user))
				TrackerAsset.Instance.Login (user, pass);
			
			TrackerAsset.Instance.Start ();
			this.nextFlush = flushInterval;
			UnityEngine.Object.DontDestroyOnLoad (this);
		}

		// <summary>
		// DONT USE THIS METHOD. UNITY INTERNAL MONOBEHAVIOUR.
		/// </summary>
		public void Update ()
		{
			float delta = Time.deltaTime;
			if (flushInterval >= 0) {
					nextFlush -= delta;
					if (nextFlush <= 0) {
						flushRequested = true;
					}
					while (nextFlush <= 0) {
						nextFlush += flushInterval;
					}
			}
			if (flushRequested) {
				flushRequested = false;
				TrackerAsset.Instance.Flush ();
			}
		}
	}
}