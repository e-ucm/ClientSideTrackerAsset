/*
 * Copyright 2017 e-UCM research group. Universidad Complutense de Madrid
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * This project has received funding from the European Union’s Horizon
 * 2020 research and innovation programme under grant agreement No 644187.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// ############################################################################
// ######### THIS TESTS ONLY WORK WHEN TRAKCER IS IN SYNCHRONOUS MODE #########
// ############################################################################

using NUnit.Framework;
using System.Collections.Generic;
using System;
using AssetPackage;
using AssetPackage.Utils;
using AssetPackage.Exceptions;
using SimpleJSON;
using System.IO;

public class TrackerTest
{
    TrackerAssetSettings settings = new TrackerAssetSettings()
    {
        Secure = true,
        Host = "127.0.0.1",
        Port = 443,
        BasePath = "/api/",

        UserToken = "",
        TrackingCode = "",
        StorageType = TrackerAsset.StorageTypes.local,
        TraceFormat = TrackerAsset.TraceFormats.csv,
        BatchSize = 10
    };

    IDataStorage storage;

    private void initTracker(string format)
    {
        string current = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if(!Directory.Exists(current))
            Directory.CreateDirectory(current);

        Directory.SetCurrentDirectory(current);

        TrackerAsset.TraceFormats f;
        if(TrackerAssetUtils.TryParseEnum<TrackerAsset.TraceFormats>(format, out f))
        {
            settings.TraceFormat = f;
        }
        TrackerAsset.Instance.Settings = settings;
        TrackerAsset.Instance.Bridge = new TrackerAssetUnitTests.TesterBridge();
        storage = (IDataStorage) TrackerAsset.Instance.Bridge;
        TrackerAsset.Instance.StrictMode = true;
        TrackerAsset.Instance.Clear();
        TrackerAsset.Instance.Start();
    }

    [Test]
    public void ActionTraceTest()
    {
        initTracker("csv");
        TrackerAsset.Instance.StrictMode = false;

        TrackerAsset.Instance.ActionTrace("Verb", "Type", "ID");
        CheckCSVTrace("Verb,Type,ID");

        TrackerAsset.Instance.ActionTrace("Verb", "Ty,pe", "ID");
        CheckCSVTrace("Verb,Ty\\,pe,ID");

        TrackerAsset.Instance.ActionTrace("Verb", "Type", "I,D");
        CheckCSVTrace("Verb,Type,I\\,D");

        TrackerAsset.Instance.ActionTrace("Ve,rb", "Type", "ID");
        CheckCSVTrace("Ve\\,rb,Type,ID");

        initTracker("csv");
        TrackerAsset.Instance.StrictMode = true;

        //Check that null and empty string throw a controled exception
        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.ActionTrace(null, "Type", "ID"); });
        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.ActionTrace("Verb", null, "ID"); });
        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.ActionTrace("Verb", "Type", null); });

        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.ActionTrace("", "Type", "ID"); });
        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.ActionTrace("Verb", "", "ID"); });
        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.ActionTrace("Verb", "Type", ""); });

        deleteTracesFile();
        initTracker("xapi");
        TrackerAsset.Instance.StrictMode = false;

        TrackerAsset.Instance.ActionTrace("Verb", "Type", "ID");
        TrackerAsset.Instance.Flush();

        string text = storage.Load(settings.LogFile);

        if (text.IndexOf("M\n") != -1)
            text = text.Substring(text.IndexOf("M\n") + 2);

        JSONNode file = JSON.Parse(text);
        JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

        Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 4);
        Assert.AreEqual(tracejson["object"]["id"].Value, "ID");
        Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "Type");
        Assert.AreEqual(tracejson["verb"]["id"].Value, "Verb");
    }

    [Test]
    public void TestNullImputs()
    {
        initTracker("xapi");

        //Check that null and empty string throw a controled exception
        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.ActionTrace(null, "Type", "ID"); });
        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.ActionTrace("Verb", null, "ID"); });
        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.ActionTrace("Verb", "Type", null); });

        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.ActionTrace("", "Type", "ID"); });
        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.ActionTrace("Verb", "", "ID"); });
        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.ActionTrace("Verb", "Type", ""); });

        Assert.Throws(typeof(VerbXApiException), delegate { TrackerAsset.Instance.ActionTrace("Verb", "Type", "ID"); });

        Assert.Throws(typeof(TargetXApiException), delegate { TrackerAsset.Instance.completable.Initialized(null); });
        Assert.Throws(typeof(TargetXApiException), delegate { TrackerAsset.Instance.completable.Progressed(null, 0.1f); });
        Assert.Throws(typeof(TargetXApiException), delegate { TrackerAsset.Instance.completable.Completed(null); });

        Assert.Throws(typeof(TargetXApiException), delegate { TrackerAsset.Instance.accessible.Accessed(null); });
        Assert.Throws(typeof(TargetXApiException), delegate { TrackerAsset.Instance.accessible.Skipped(null); });

        Assert.Throws(typeof(TargetXApiException), delegate { TrackerAsset.Instance.alternative.Selected(null, null); });
        Assert.Throws(typeof(TargetXApiException), delegate { TrackerAsset.Instance.alternative.Selected(null, "o"); });
        Assert.Throws(typeof(ValueExtensionException), delegate { TrackerAsset.Instance.alternative.Selected("k", null); });
        Assert.Throws(typeof(TargetXApiException), delegate { TrackerAsset.Instance.alternative.Unlocked(null, null); });
        Assert.Throws(typeof(TargetXApiException), delegate { TrackerAsset.Instance.alternative.Unlocked(null, "o"); });
        Assert.Throws(typeof(ValueExtensionException), delegate { TrackerAsset.Instance.alternative.Unlocked("k", null); });

        Assert.Throws(typeof(TargetXApiException), delegate { TrackerAsset.Instance.trackedGameObject.Interacted(null); });
        Assert.Throws(typeof(TargetXApiException), delegate { TrackerAsset.Instance.trackedGameObject.Used(null); });

        Assert.Throws(typeof(KeyExtensionException), delegate { TrackerAsset.Instance.setVar("", ""); });
        Assert.Throws(typeof(KeyExtensionException), delegate { TrackerAsset.Instance.setVar(null, null); });
        Assert.Throws(typeof(KeyExtensionException), delegate { TrackerAsset.Instance.setVar(null, "v"); });
        Assert.Throws(typeof(ValueExtensionException), delegate { TrackerAsset.Instance.setVar("k", null); });
        Assert.Throws(typeof(ValueExtensionException), delegate { TrackerAsset.Instance.setVar("k", ""); });

        Assert.DoesNotThrow(delegate { TrackerAsset.Instance.setVar("k", "v"); });
    }

    [Test]
    public void TestObsoleteMethods()
    {
        initTracker("xapi");

        //Check that null and empty string throw a controled exception
        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.Trace(""); });
        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.Trace("1"); });
        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.Trace("1,2"); });
        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.Trace("1,2,3,4"); });
        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.Trace("1", "2"); });
        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.Trace("1", "2", null); });
        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.Trace("1", "2", ""); });
        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.Trace("", "", ""); });
        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.Trace("1", "2", "3", "4"); });
        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.Trace(null, null); });
        Assert.Throws(typeof(TraceException), delegate { TrackerAsset.Instance.Trace("1,2,3,4"); });
        Assert.Throws(typeof(TargetXApiException), delegate { TrackerAsset.Instance.Trace("1,2,3"); TrackerAsset.Instance.RequestFlush(); });

        initTracker("csv");
        TrackerAsset.Instance.StrictMode = false;

        TrackerAsset.Instance.Trace("Verb", "Type", "ID");
        CheckCSVTrace("Verb,Type,ID");

        TrackerAsset.Instance.Trace("Verb", "Ty,pe", "ID");
        CheckCSVTrace("Verb,Ty\\,pe,ID");

        TrackerAsset.Instance.Trace("Verb", "Type", "I,D");
        CheckCSVTrace("Verb,Type,I\\,D");

        TrackerAsset.Instance.Trace("Ve,rb", "Type", "ID");
        CheckCSVTrace("Ve\\,rb,Type,ID");

        TrackerAsset.Instance.Trace("Verb,Type,ID");
        CheckCSVTrace("Verb,Type,ID");

        TrackerAsset.Instance.Trace("Verb,Ty\\,pe,ID");
        CheckCSVTrace("Verb,Ty\\,pe,ID");

        TrackerAsset.Instance.Trace("Verb,Type,I\\,D");
        CheckCSVTrace("Verb,Type,I\\,D");

        TrackerAsset.Instance.Trace("Ve\\,rb,Type,ID");
        CheckCSVTrace("Ve\\,rb,Type,ID");

        Assert.DoesNotThrow(delegate { TrackerAsset.Instance.ActionTrace("Verb", "Type", "ID"); });

        initTracker("csv");
        TrackerAsset.Instance.StrictMode = true;

        Assert.Throws(typeof(KeyExtensionException), delegate { TrackerAsset.Instance.setExtension(null, null); });
        Assert.Throws(typeof(KeyExtensionException), delegate { TrackerAsset.Instance.setExtension("", null); });
        Assert.Throws(typeof(ValueExtensionException), delegate { TrackerAsset.Instance.setExtension("k", null); });
        Assert.Throws(typeof(ValueExtensionException), delegate { TrackerAsset.Instance.setExtension("k", ""); });
        Assert.DoesNotThrow(delegate { TrackerAsset.Instance.setExtension("k", 1); });
        Assert.DoesNotThrow(delegate { TrackerAsset.Instance.setExtension("k", 1.1f); });
        Assert.DoesNotThrow(delegate { TrackerAsset.Instance.setExtension("k", 1.1d); });
        Assert.DoesNotThrow(delegate { TrackerAsset.Instance.setExtension("k", "v"); });
    }

    [Test]
    public void AlternativeTraceTest()
    {
        initTracker("csv");

        TrackerAsset.Instance.alternative.Selected("question", "alternative");
        CheckCSVTrace("selected,alternative,question,response,alternative");
    }

    [Test]
    public void TestTrace_Generic_Csv_Stored_01()
    {
        initTracker("csv");

        enqueueTrace01();
        TrackerAsset.Instance.RequestFlush();

        CheckCSVStoredTrace("accessed,gameobject,ObjectID");
    }

    [Test]
    public void TestTrace_Generic_Csv_Stored_02()
    {
        initTracker("csv");

        enqueueTrace02();
        TrackerAsset.Instance.RequestFlush();

        CheckCSVStoredTrace("initialized,game,ObjectID2,response,TheResponse,score,0.123");
    }

    [Test]
    public void TestTrace_Generic_Csv_Stored_03()
    {
        initTracker("csv");

        enqueueTrace03();
        TrackerAsset.Instance.RequestFlush();

        CheckCSVStoredTrace("selected,zone,ObjectID3,success,false,completion,true,response,AnotherResponse,score,123.456,extension1,value1,extension2,value2,extension3,3,extension4,4.56");
    }

    [Test]
    public void TestTrace_Generic_Csv_Stored_WithComma()
    {
        initTracker("csv");
        TrackerAsset.Instance.StrictMode = false;

        TrackerAsset.Instance.setVar("e1", "ex,2");
        TrackerAsset.Instance.setVar("e,1", "ex,2,");
        TrackerAsset.Instance.setVar("e3", "e3");
        TrackerAsset.Instance.ActionTrace("verb", "target", "id");
        TrackerAsset.Instance.RequestFlush();

        CheckCSVStoredTrace("verb,target,id,e1,ex\\,2,e\\,1,ex\\,2\\,,e3,e3");
    }

    [Test]
    public void TestTrace_Generic_XApi_Stored_01()
    {
        deleteTracesFile();

        initTracker("xapi");

        enqueueTrace01();
        TrackerAsset.Instance.RequestFlush();

        string text = storage.Load(settings.LogFile);
        if (text.IndexOf("M\n") != -1)
            text = text.Substring(text.IndexOf("M\n") + 2);

        JSONNode file = JSON.Parse(text);
        JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

        Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 4);
        Assert.AreEqual(tracejson["object"]["id"].Value, "ObjectID");
        Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/game-object");
        Assert.AreEqual(tracejson["verb"]["id"].Value, "https://w3id.org/xapi/seriousgames/verbs/accessed");
    }

    [Test]
    public void TestTrace_Generic_XApi_Stored_02()
    {
        deleteTracesFile();

        initTracker("xapi");
        enqueueTrace02();
        TrackerAsset.Instance.RequestFlush();

        string text = storage.Load(settings.LogFile);
        if (text.IndexOf("M\n") != -1)
            text = text.Substring(text.IndexOf("M\n") + 2);

        JSONNode file = JSON.Parse(text);
        JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

        Assert.AreEqual(tracejson.Count, 5);
        Assert.AreEqual(tracejson["object"]["id"].Value, "ObjectID2");
        Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/serious-game");
        Assert.AreEqual(tracejson["verb"]["id"].Value, "https://w3id.org/xapi/adb/verbs/initialized");
        Assert.AreEqual(tracejson["result"].Count, 2);
        Assert.AreEqual(tracejson["result"]["response"].Value, "TheResponse");
        Assert.AreEqual(tracejson["result"]["score"]["raw"].AsFloat, 0.123f);
    }

    [Test]
    public void TestTrace_Generic_XApi_Stored_03()
    {
        deleteTracesFile();

        initTracker("xapi");
        enqueueTrace03();
        TrackerAsset.Instance.RequestFlush();

        string text = storage.Load(settings.LogFile);
        if (text.IndexOf("M\n") != -1)
            text = text.Substring(text.IndexOf("M\n") + 2);

        JSONNode file = JSON.Parse(text);
        JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

        Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 5);
        Assert.AreEqual(tracejson["object"]["id"].Value, "ObjectID3");
        Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/zone");
        Assert.AreEqual(tracejson["verb"]["id"].Value, "https://w3id.org/xapi/adb/verbs/selected");

        Assert.AreEqual(new List<JSONNode>(tracejson["result"].Children).Count, 5);
        Assert.AreEqual(tracejson["result"]["response"].Value, "AnotherResponse");
        Assert.AreEqual(tracejson["result"]["score"]["raw"].AsFloat, 123.456f);
        Assert.AreEqual(tracejson["result"]["completion"].AsBool, true);
        Assert.AreEqual(tracejson["result"]["success"].AsBool, false);

        Assert.AreEqual(new List<JSONNode>(tracejson["result"]["extensions"].Children).Count, 4);
        Assert.AreEqual(tracejson["result"]["extensions"]["extension1"].Value, "value1");
        Assert.AreEqual(tracejson["result"]["extensions"]["extension2"].Value, "value2");
        Assert.AreEqual(tracejson["result"]["extensions"]["extension3"].AsInt, 3);
        Assert.AreEqual(tracejson["result"]["extensions"]["extension4"].AsFloat, 4.56f);
    }

    [Test]
    public void TestTrace_Generic_XApi_All()
    {
        deleteTracesFile();

        initTracker("xapi");

        enqueueTrace01();
        enqueueTrace02();
        enqueueTrace03();
        TrackerAsset.Instance.RequestFlush();

        string text = storage.Load(settings.LogFile);
        if (text.IndexOf("M\n") != -1)
            text = text.Substring(text.IndexOf("M\n") + 2);

        JSONNode file = JSON.Parse(text);

        Assert.AreEqual(new List<JSONNode>(file.Children).Count, 3);

        //CHECK THE 1ST TRACe
        JSONNode tracejson = file[0];

        Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 4);
        Assert.AreEqual(tracejson["object"]["id"].Value, "ObjectID");
        Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/game-object");
        Assert.AreEqual(tracejson["verb"]["id"].Value, "https://w3id.org/xapi/seriousgames/verbs/accessed");

        //CHECK THE 2ND TRACE
        tracejson = file[1];

        Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 5);
        Assert.AreEqual(tracejson["object"]["id"].Value, "ObjectID2");
        Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/serious-game");
        Assert.AreEqual(tracejson["verb"]["id"].Value, "https://w3id.org/xapi/adb/verbs/initialized");
        Assert.AreEqual(new List<JSONNode>(tracejson["result"].Children).Count, 2);
        Assert.AreEqual(tracejson["result"]["response"].Value, "TheResponse");
        Assert.AreEqual(tracejson["result"]["score"]["raw"].AsFloat, 0.123f);

        //CHECK THE 3RD TRACE
        tracejson = file[2];

        Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 5);
        Assert.AreEqual(tracejson["object"]["id"].Value, "ObjectID3");
        Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/zone");
        Assert.AreEqual(tracejson["verb"]["id"].Value, "https://w3id.org/xapi/adb/verbs/selected");

        Assert.AreEqual(new List<JSONNode>(tracejson["result"].Children).Count, 5);
        Assert.AreEqual(tracejson["result"]["response"].Value, "AnotherResponse");
        Assert.AreEqual(tracejson["result"]["score"]["raw"].AsFloat, 123.456f);
        Assert.AreEqual(tracejson["result"]["completion"].AsBool, true);
        Assert.AreEqual(tracejson["result"]["success"].AsBool, false);

        Assert.AreEqual(new List<JSONNode>(tracejson["result"]["extensions"].Children).Count, 4);
        Assert.AreEqual(tracejson["result"]["extensions"]["extension1"].Value, "value1");
        Assert.AreEqual(tracejson["result"]["extensions"]["extension2"].Value, "value2");
        Assert.AreEqual(tracejson["result"]["extensions"]["extension3"].AsInt, 3);
        Assert.AreEqual(tracejson["result"]["extensions"]["extension4"].AsFloat, 4.56f);
    }

    [Test]
    public void TestAccesible_Csv_01()
    {
        initTracker("csv");

        TrackerAsset.Instance.accessible.Accessed("AccesibleID", AccessibleTracker.Accessible.Cutscene);

        CheckCSVTrace("accessed,cutscene,AccesibleID");
    }

    [Test]
    public void TestAccesible_Csv_02_WithExtensions()
    {
        initTracker("csv");

        TrackerAsset.Instance.setVar("extension1", "value1");
        TrackerAsset.Instance.accessible.Skipped("AccesibleID2", AccessibleTracker.Accessible.Screen);

        CheckCSVTrace("skipped,screen,AccesibleID2,extension1,value1");
    }

    [Test]
    public void TestAccesible_XApi_01()
    {
        deleteTracesFile();

        initTracker("xapi");

        TrackerAsset.Instance.accessible.Accessed("AccesibleID", AccessibleTracker.Accessible.Cutscene);
        TrackerAsset.Instance.RequestFlush();

        string text = storage.Load(settings.LogFile);

        if(text.IndexOf("M\n") != -1)
            text = text.Substring(text.IndexOf("M\n") + 2);

        JSONNode file = JSON.Parse(text);
        JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

        Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 4);
        Assert.AreEqual(tracejson["object"]["id"].Value, "AccesibleID");
        Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/cutscene");
        Assert.AreEqual(tracejson["verb"]["id"].Value, "https://w3id.org/xapi/seriousgames/verbs/accessed");
    }

    [Test]
    public void TestAccesible_XApi_02_WithExtensions()
    {
        deleteTracesFile();

        initTracker("xapi");

        TrackerAsset.Instance.setVar("extension1", "value1");
        TrackerAsset.Instance.accessible.Skipped("AccesibleID2", AccessibleTracker.Accessible.Screen);
        TrackerAsset.Instance.RequestFlush();

        string text = storage.Load(settings.LogFile);
        if (text.IndexOf("M\n") != -1)
            text = text.Substring(text.IndexOf("M\n") + 2);

        JSONNode file = JSON.Parse(text);
        JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

        Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 5);
        Assert.AreEqual(tracejson["object"]["id"].Value, "AccesibleID2");
        Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/screen");
        Assert.AreEqual(tracejson["verb"]["id"].Value, "http://id.tincanapi.com/verb/skipped");
        Assert.AreEqual(tracejson["result"]["extensions"]["extension1"].Value, "value1");
    }

    [Test]
    public void TestAlternative_Csv_01()
    {
        initTracker("csv");

        TrackerAsset.Instance.alternative.Selected("AlternativeID", "SelectedOption", AlternativeTracker.Alternative.Path);

        CheckCSVTrace("selected,path,AlternativeID,response,SelectedOption");
    }

    [Test]
    public void TestAlternative_Csv_02_WithExtensions()
    {
        initTracker("csv");

        TrackerAsset.Instance.setVar("SubCompletableScore", 0.8);
        TrackerAsset.Instance.alternative.Unlocked("AlternativeID2", "Answer number 3", AlternativeTracker.Alternative.Question);

        CheckCSVTrace("unlocked,question,AlternativeID2,response,Answer number 3,SubCompletableScore,0.8");
    }

    [Test]
    public void TestAlternative_XApi_01()
    {
        deleteTracesFile();

        initTracker("xapi");

        TrackerAsset.Instance.alternative.Selected("AlternativeID", "SelectedOption", AlternativeTracker.Alternative.Path);
        TrackerAsset.Instance.RequestFlush();

        string text = storage.Load(settings.LogFile);
        if (text.IndexOf("M\n") != -1)
            text = text.Substring(text.IndexOf("M\n") + 2);

        JSONNode file = JSON.Parse(text);
        JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

        Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 5);
        Assert.AreEqual(tracejson["object"]["id"].Value, "AlternativeID");
        Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/path");
        Assert.AreEqual(tracejson["verb"]["id"].Value, "https://w3id.org/xapi/adb/verbs/selected");
        Assert.AreEqual(tracejson["result"]["response"].Value, "SelectedOption");
    }

    [Test]
    public void TestAlternative_XApi_02_WithExtensions()
    {
        deleteTracesFile();

        initTracker("xapi");

        TrackerAsset.Instance.setVar("SubCompletableScore", 0.8);
        TrackerAsset.Instance.alternative.Unlocked("AlternativeID2", "Answer number 3", AlternativeTracker.Alternative.Question);
        TrackerAsset.Instance.RequestFlush();
        

        string text = storage.Load(settings.LogFile);
        if (text.IndexOf("M\n") != -1)
            text = text.Substring(text.IndexOf("M\n") + 2);

        JSONNode file = JSON.Parse(text);
        JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

        Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 5);
        Assert.AreEqual(tracejson["object"]["id"].Value, "AlternativeID2");
        Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "http://adlnet.gov/expapi/activities/question");
        Assert.AreEqual(tracejson["verb"]["id"].Value, "https://w3id.org/xapi/seriousgames/verbs/unlocked");
        Assert.AreEqual(tracejson["result"]["response"].Value, "Answer number 3");
        Assert.AreEqual(tracejson["result"]["extensions"]["SubCompletableScore"].AsFloat, 0.8f);
    }

    [Test]
    public void TestCompletable_Csv_01()
    {
        initTracker("csv");

        TrackerAsset.Instance.completable.Initialized("CompletableID", CompletableTracker.Completable.Quest);

        CheckCSVTrace("initialized,quest,CompletableID");
    }

    [Test]
    public void TestCompletable_Csv_02()
    {
        initTracker("csv");

        TrackerAsset.Instance.completable.Progressed("CompletableID2", CompletableTracker.Completable.Stage, 0.34f);

        CheckCSVTrace("progressed,stage,CompletableID2,progress,0.34");
    }

    [Test]
    public void TestCompletable_Csv_03()
    {
        initTracker("csv");

        TrackerAsset.Instance.completable.Completed("CompletableID3", CompletableTracker.Completable.Race, true, 0.54f);

        CheckCSVTrace("completed,race,CompletableID3,success,true,score,0.54");
    }

    [Test]
    public void TestCompletable_XApi_01()
    {
        deleteTracesFile();

        initTracker("xapi");

        TrackerAsset.Instance.completable.Initialized("CompletableID", CompletableTracker.Completable.Quest);
        TrackerAsset.Instance.RequestFlush();

        string text = storage.Load(settings.LogFile);
        if (text.IndexOf("M\n") != -1)
            text = text.Substring(text.IndexOf("M\n") + 2);

        JSONNode file = JSON.Parse(text);
        JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

        Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 4);
        Assert.AreEqual(tracejson["object"]["id"].Value, "CompletableID");
        Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/quest");
        Assert.AreEqual(tracejson["verb"]["id"].Value, "https://w3id.org/xapi/adb/verbs/initialized");
    }

    [Test]
    public void TestCompletable_XApi_02()
    {
        deleteTracesFile();

        initTracker("xapi");

        TrackerAsset.Instance.completable.Progressed("CompletableID2", CompletableTracker.Completable.Stage, 0.34f);
        TrackerAsset.Instance.RequestFlush();

        string text = storage.Load(settings.LogFile);
        if (text.IndexOf("M\n") != -1)
            text = text.Substring(text.IndexOf("M\n") + 2);

        JSONNode file = JSON.Parse(text);
        JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

        Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 5);
        Assert.AreEqual(tracejson["object"]["id"].Value, "CompletableID2");
        Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/stage");
        Assert.AreEqual(tracejson["verb"]["id"].Value, "http://adlnet.gov/expapi/verbs/progressed");
        Assert.AreEqual(tracejson["result"]["extensions"]["https://w3id.org/xapi/seriousgames/extensions/progress"].AsFloat, 0.34f);
    }

    [Test]
    public void TestCompletable_XApi_03()
    {
        deleteTracesFile();

        initTracker("xapi");

        TrackerAsset.Instance.completable.Completed("CompletableID3", CompletableTracker.Completable.Race, true, 0.54f);
        TrackerAsset.Instance.RequestFlush();

        string text = storage.Load(settings.LogFile);
        if (text.IndexOf("M\n") != -1)
            text = text.Substring(text.IndexOf("M\n") + 2);

        JSONNode file = JSON.Parse(text);
        JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

        Assert.AreEqual(tracejson.Count, 5);
        Assert.AreEqual(tracejson["object"]["id"].Value, "CompletableID3");
        Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/race");
        Assert.AreEqual(tracejson["verb"]["id"].Value, "http://adlnet.gov/expapi/verbs/completed");
        Assert.AreEqual(tracejson["result"]["success"].AsBool, true);
        Assert.AreEqual(tracejson["result"]["score"]["raw"].AsFloat, 0.54f);
    }

    [Test]
    public void TestGameObject_Csv_01()
    {
        initTracker("csv");

        TrackerAsset.Instance.trackedGameObject.Interacted("GameObjectID", GameObjectTracker.TrackedGameObject.Npc);

        CheckCSVTrace("interacted,npc,GameObjectID");
    }

    [Test]
    public void TestGameObject_Csv_02()
    {
        initTracker("csv");

        TrackerAsset.Instance.trackedGameObject.Used("GameObjectID2", GameObjectTracker.TrackedGameObject.Item);

        CheckCSVTrace("used,item,GameObjectID2");
    }

    [Test]
    public void TestGameObject_XApi_01()
    {
        deleteTracesFile();

        initTracker("xapi");

        TrackerAsset.Instance.trackedGameObject.Interacted("GameObjectID", GameObjectTracker.TrackedGameObject.Npc);
        TrackerAsset.Instance.RequestFlush();

        string text = storage.Load(settings.LogFile);
        if (text.IndexOf("M\n") != -1)
            text = text.Substring(text.IndexOf("M\n") + 2);

        JSONNode file = JSON.Parse(text);
        JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

        Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 4);
        Assert.AreEqual(tracejson["object"]["id"].Value, "GameObjectID");
        Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/non-player-character");
        Assert.AreEqual(tracejson["verb"]["id"].Value, "http://adlnet.gov/expapi/verbs/interacted");
    }

    [Test]
    public void TestGameObject_XApi_02()
    {
        deleteTracesFile();

        initTracker("xapi");

        TrackerAsset.Instance.trackedGameObject.Used("GameObjectID2", GameObjectTracker.TrackedGameObject.Item); TrackerAsset.Instance.RequestFlush();

        string text = storage.Load(settings.LogFile);
        if (text.IndexOf("M\n") != -1)
            text = text.Substring(text.IndexOf("M\n") + 2);

        JSONNode file = JSON.Parse(text);
        JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

        Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 4);
        Assert.AreEqual(tracejson["object"]["id"].Value, "GameObjectID2");
        Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/item");
        Assert.AreEqual(tracejson["verb"]["id"].Value, "https://w3id.org/xapi/seriousgames/verbs/used");
    }

    private void enqueueTrace01()
    {
        TrackerAsset.Instance.ActionTrace("accessed", "gameobject", "ObjectID");
    }

    private void enqueueTrace02()
    {
        TrackerAsset.Instance.setResponse("TheResponse");
        TrackerAsset.Instance.setScore(0.123f);
        TrackerAsset.Instance.ActionTrace("initialized", "game", "ObjectID2");
    }

    private void enqueueTrace03()
    {
        TrackerAsset.Instance.setResponse("AnotherResponse");
        TrackerAsset.Instance.setScore(123.456f);
        TrackerAsset.Instance.setSuccess(false);
        TrackerAsset.Instance.setCompletion(true);
        TrackerAsset.Instance.setVar("extension1", "value1");
        TrackerAsset.Instance.setVar("extension2", "value2");
        TrackerAsset.Instance.setVar("extension3", 3);
        TrackerAsset.Instance.setVar("extension4", 4.56f);
        TrackerAsset.Instance.ActionTrace("selected", "zone", "ObjectID3");
    }

    private void CheckCSVTrace(string trace)
    {
        //TODO: this method should access the queue directly.

        TrackerAsset.Instance.Flush();
        CheckCSVStoredTrace(trace);
    }

    private void CheckCSVStoredTrace(string trace)
    {
        string[] stringSeparators = new string[] { "\r\n" };

        while (!storage.Exists(settings.LogFile)) ;

        string[] lines = storage.Load(settings.LogFile).Split(stringSeparators, StringSplitOptions.None);

        string traceWithoutTimestamp = removeTimestamp(lines[lines.Length - 2]);

        CompareCSV(traceWithoutTimestamp, trace);
    }

    private void CheckXAPIStoredTrace(string trace)
    {
        string[] stringSeparators = new string[] { "\r\n" };
        string[] lines = storage.Load(settings.LogFile).Split(stringSeparators, StringSplitOptions.None);

        string traceWithoutTimestamp = removeTimestamp(lines[lines.Length - 1]);

        CompareCSV(traceWithoutTimestamp, trace);
    }

    private void CompareCSV(string t1, string t2)
    {
        string[] sp1 = TrackerAssetUtils.parseCSV(t1);
        string[] sp2 = TrackerAssetUtils.parseCSV(t2);

        Assert.AreEqual(sp1.Length, sp2.Length);

        for (int i = 0; i < 3; i++)
            Assert.AreEqual(sp1[i], sp2[i]);

        Dictionary<string, string> d1 = new Dictionary<string, string>();

        if (sp1.Length > 3)
        {
            for (int i = 3; i < sp1.Length; i += 2)
            {
                d1.Add(sp1[i], sp1[i + 1]);
            }

            for (int i = 3; i < sp2.Length; i += 2)
            {
                Assert.Contains(sp2[i], d1.Keys);
                Assert.AreEqual(d1[sp2[i]], sp2[i + 1]);
            }
        }
    }

    private string removeTimestamp(string trace)
    {
        return trace.Substring(trace.IndexOf(',') + 1);
    }

    private void deleteTracesFile()
    {
        if (settings !=null && storage != null && settings.LogFile != null && storage.Exists(settings.LogFile))
        {
            storage.Delete(settings.LogFile);
        }
    }
}