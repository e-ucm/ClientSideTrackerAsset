/*
 * Copyright 2016 Open University of the Netherlands and Universidad Complutense de Madrid
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

namespace TrackerTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using SimpleJSON;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using AssetPackage;
    using AssetPackage.Utils;
    using AssetPackage.Exceptions;

    [TestClass]
    public class VisualStudioTests
    {
        #region Constants

        const string modelId = "test";
        const string restoredId = "restored";

        #endregion Constants

        #region Fields

        private TrackerAsset asset = TrackerAsset.Instance;

        private TrackerAssetSettings settings = new TrackerAssetSettings();

        private IDataStorage storage;

        private TrackerAsset.TrackerEvent trace01 = new TrackerAsset.TrackerEvent()
        {

            Target = new TrackerAsset.TrackerEvent.TraceObject(GameObjectTracker.TrackedGameObject.GameObject.ToString().ToLower(), "ObjectID"),
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Accessed)
        };

        private TrackerAsset.TrackerEvent trace02 = new TrackerAsset.TrackerEvent()
        {

            Target = new TrackerAsset.TrackerEvent.TraceObject(CompletableTracker.Completable.Game.ToString().ToLower(), "ObjectID2"),
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Initialized),
            Result = new TrackerAsset.TrackerEvent.TraceResult()
            {
                Response = "TheResponse",
                Score = 0.123f
            }
        };

        private TrackerAsset.TrackerEvent trace03 = new TrackerAsset.TrackerEvent()
        {

            Target = new TrackerAsset.TrackerEvent.TraceObject(AccessibleTracker.Accessible.Zone.ToString().ToLower(), "ObjectID3"),
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Selected),
            Result = new TrackerAsset.TrackerEvent.TraceResult()
            {
                Response = "AnotherResponse",
                Score = 123.456f,
                Success = false,
                Completion = true,
                Extensions = new Dictionary<string, object>()
                {
                    { "extension1", "value1" },
                    { "extension2", "value2" },
                    { "extension3", 3 },
                    { "extension4", 4.56f }
                }
            }
        };

        #endregion Fields

        #region Methods

        /// <summary>
        /// Cleanup() is called once during test execution after test methods in this
        /// class have executed unless this test class' Initialize() method throws an
        /// exception.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            asset.Flush();
        }

        /// <summary>
        /// Initialize() is called once during test execution before test methods in
        /// this test class are executed.
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            Debug.Print("-------");

            settings.Secure = true;
            settings.Host = "127.0.0.1";
            settings.Port = 443;
            settings.BasePath = "/api/";

            settings.UserToken = "";
            settings.TrackingCode = "";
            settings.StorageType = TrackerAsset.StorageTypes.local;
            settings.TraceFormat = TrackerAsset.TraceFormats.csv;
            settings.BatchSize = 10;

            asset.Settings = settings;

            asset.Bridge = new TrackerAssetUnitTests.TesterBridge();
            storage = (IDataStorage)asset.Bridge;
            asset.Start();

        }

        /// <summary>
        /// (Unit Test Method) Simple trace CSV
        /// </summary>
        [TestMethod]
        public void TestTrace_Generic_Csv_01()
        {
            ChangeFormat(TrackerAsset.TraceFormats.csv);

            asset.Trace(trace01);
            asset.Flush();

            string csv = storage.Load(settings.LogFile);
            string[] splitted = csv.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            Assert.AreEqual(splitted[splitted.Length-2], trace01.TimeStamp.ToString() + ",accessed,gameobject,ObjectID");
        }

        /// <summary>
        /// (Unit Test Method) Medium complexity trace CSV
        /// </summary>
        [TestMethod]
        public void TestTrace_Generic_Csv_02()
        {
            ChangeFormat(TrackerAsset.TraceFormats.csv);

            asset.Trace(trace02);
            asset.Flush();

            string csv = storage.Load(settings.LogFile);
            string[] splitted = csv.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            Assert.AreEqual(splitted[splitted.Length - 2], trace02.TimeStamp.ToString() + ",initialized,game,ObjectID2,response,TheResponse,score,0.123");
        }

        /// <summary>
        /// (Unit Test Method) Hight complexity trace with extensions inside it CSV
        /// </summary>
        [TestMethod]
        public void TestTrace_Generic_Csv_03()
        {
            ChangeFormat(TrackerAsset.TraceFormats.csv);

            asset.Trace(trace03);
            asset.Flush();

            string csv = storage.Load(settings.LogFile);
            string[] splitted = csv.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            Assert.AreEqual(splitted[splitted.Length - 2], trace02.TimeStamp.ToString() + ",selected,zone,ObjectID3,success,false,completion,true,response,AnotherResponse,score,123.456,extension1,value1,extension2,value2,extension3,3,extension4,4.56");
        }

        /// <summary>
        /// (Unit Test Method) Simple trace XAPI
        /// </summary>
        [TestMethod]
        public void TestTrace_Generic_XApi_01()
        {
            ChangeFormat(TrackerAsset.TraceFormats.xapi);

            asset.Trace(trace01);
            asset.Flush();

            JSONNode file = JSON.Parse(storage.Load(settings.LogFile));
            JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

            Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 4);
            Assert.AreEqual(tracejson["object"]["id"].Value, "ObjectID");
            Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/game-object");
            Assert.AreEqual(tracejson["verb"]["id"].Value, "https://w3id.org/xapi/seriousgames/verbs/accessed");
        }

        /// <summary>
        /// (Unit Test Method) Medium complexity trace XAPI
        /// </summary>
        [TestMethod]
        public void TestTrace_Generic_XApi_02()
        {
            ChangeFormat(TrackerAsset.TraceFormats.xapi);

            asset.Trace(trace02);
            asset.Flush();

            JSONNode file = JSON.Parse(storage.Load(settings.LogFile));
            JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

            Assert.AreEqual(tracejson.Count, 5);
            Assert.AreEqual(tracejson["object"]["id"].Value, "ObjectID2");
            Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/serious-game");
            Assert.AreEqual(tracejson["verb"]["id"].Value, "https://w3id.org/xapi/adb/verbs/initialized");
            Assert.AreEqual(tracejson["result"].Count, 2);
            Assert.AreEqual(tracejson["result"]["response"].Value, "TheResponse");
            Assert.AreEqual(tracejson["result"]["score"]["raw"].AsFloat, 0.123f);
        }

        /// <summary>
        /// (Unit Test Method) Hight complexity trace with extensions inside it XAPI
        /// </summary>
        [TestMethod]
        public void TestTrace_Generic_XApi_03()
        {
            ChangeFormat(TrackerAsset.TraceFormats.xapi);

            asset.Trace(trace03);
            asset.Flush();

            JSONNode file = JSON.Parse(storage.Load(settings.LogFile));
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

        /// <summary>
        /// (Unit Test Method) All previous generic trace methods runs once plus more tests
        /// </summary>
        [TestMethod]
        public void TestTrace_Generic_XApi_All()
        {
            ChangeFormat(TrackerAsset.TraceFormats.xapi);

            storage.Delete(settings.LogFile);

            asset.Trace(trace01);
            asset.Trace(trace02);
            asset.Trace(trace03);
            asset.Flush();

            JSONNode file = JSON.Parse(storage.Load(settings.LogFile));

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

        /// <summary>
        /// (Unit Test Method) tests accesible accessed CSV
        /// </summary>
        [TestMethod]
        public void TestAccesible_Csv_01()
        {
            ChangeFormat(TrackerAsset.TraceFormats.csv);

            asset.Accessible.Accessed("AccesibleID", AccessibleTracker.Accessible.Cutscene);
            asset.Flush();

            string csv = storage.Load(settings.LogFile);
            string[] splitted = csv.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            Assert.IsTrue(splitted[splitted.Length - 2].Contains(",accessed,cutscene,AccesibleID"));
        }

        /// <summary>
        /// (Unit Test Method) tests accesible skipped with extensions on tracker CSV
        /// </summary>
        [TestMethod]
        public void TestAccesible_Csv_02_WithExtensions()
        {
            ChangeFormat(TrackerAsset.TraceFormats.csv);

            asset.setExtension("extension1", "value1");
            asset.Accessible.Skipped("AccesibleID2", AccessibleTracker.Accessible.Screen);
            asset.Flush();

            string csv = storage.Load(settings.LogFile);
            string[] splitted = csv.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            Assert.IsTrue(splitted[splitted.Length - 2].Contains(",skipped,screen,AccesibleID2,extension1,value1"));
        }

        /// <summary>
        /// (Unit Test Method) tests accesible accesed XAPI
        /// </summary>
        [TestMethod]
        public void TestAccesible_XApi_01()
        {
            ChangeFormat(TrackerAsset.TraceFormats.xapi);

            asset.Accessible.Accessed("AccesibleID", AccessibleTracker.Accessible.Cutscene);
            asset.Flush();

            JSONNode file = JSON.Parse(storage.Load(settings.LogFile));
            JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

            Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 4);
            Assert.AreEqual(tracejson["object"]["id"].Value, "AccesibleID");
            Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/cutscene");
            Assert.AreEqual(tracejson["verb"]["id"].Value, "https://w3id.org/xapi/seriousgames/verbs/accessed");
        }

        /// <summary>
        /// (Unit Test Method) tests accesible skipped with extensions on tracker XAPI
        /// </summary>
        [TestMethod]
        public void TestAccesible_XApi_02_WithExtensions()
        {
            ChangeFormat(TrackerAsset.TraceFormats.xapi);

            asset.setExtension("extension1", "value1");
            asset.Accessible.Skipped("AccesibleID2", AccessibleTracker.Accessible.Screen);
            asset.Flush();

            JSONNode file = JSON.Parse(storage.Load(settings.LogFile));
            JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

            Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 5);
            Assert.AreEqual(tracejson["object"]["id"].Value, "AccesibleID2");
            Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/screen");
            Assert.AreEqual(tracejson["verb"]["id"].Value, "http://id.tincanapi.com/verb/skipped");
            Assert.AreEqual(tracejson["result"]["extensions"]["extension1"].Value, "value1");
        }

        /// <summary>
        /// (Unit Test Method) tests alternative selected CSV
        /// </summary>
        [TestMethod]
        public void TestAlternative_Csv_01()
        {
            ChangeFormat(TrackerAsset.TraceFormats.csv);

            asset.Alternative.Selected("AlternativeID", "SelectedOption", AlternativeTracker.Alternative.Path);
            asset.Flush();

            string csv = storage.Load(settings.LogFile);
            string[] splitted = csv.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            Assert.IsTrue(splitted[splitted.Length - 2].Contains(",selected,path,AlternativeID,response,SelectedOption"));
        }

        /// <summary>
        /// (Unit Test Method) tests alternative unlocked with extensions on tracker CSV
        /// </summary>
        [TestMethod]
        public void TestAlternative_Csv_02_WithExtensions()
        {
            ChangeFormat(TrackerAsset.TraceFormats.csv);

            asset.setVar("SubCompletableScore", 0.8);
            asset.Alternative.Unlocked("AlternativeID2", "Answer number 3", AlternativeTracker.Alternative.Question);
            asset.Flush();

            string csv = storage.Load(settings.LogFile);
            string[] splitted = csv.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            Assert.IsTrue(splitted[splitted.Length - 2].Contains(",unlocked,question,AlternativeID2,response,Answer number 3,SubCompletableScore,0.8"));
        }

        /// <summary>
        /// (Unit Test Method) tests alternative selected XAPI
        /// </summary>
        [TestMethod]
        public void TestAlternative_XApi_01()
        {
            ChangeFormat(TrackerAsset.TraceFormats.xapi);

            asset.Alternative.Selected("AlternativeID", "SelectedOption", AlternativeTracker.Alternative.Path);
            asset.Flush();

            JSONNode file = JSON.Parse(storage.Load(settings.LogFile));
            JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

            Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 5);
            Assert.AreEqual(tracejson["object"]["id"].Value, "AlternativeID");
            Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/path");
            Assert.AreEqual(tracejson["verb"]["id"].Value, "https://w3id.org/xapi/adb/verbs/selected");
            Assert.AreEqual(tracejson["result"]["response"].Value, "SelectedOption");
        }

        /// <summary>
        /// (Unit Test Method) tests alternative unlocked with extensions on tracker XAPI
        /// </summary>
        [TestMethod]
        public void TestAlternative_XApi_02_WithExtensions()
        {
            ChangeFormat(TrackerAsset.TraceFormats.xapi);

            asset.setVar("SubCompletableScore", 0.8f);
            asset.Alternative.Unlocked("AlternativeID2", "Answer number 3", AlternativeTracker.Alternative.Question);
            asset.Flush();

            JSONNode file = JSON.Parse(storage.Load(settings.LogFile));
            JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

            Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 5);
            Assert.AreEqual(tracejson["object"]["id"].Value, "AlternativeID2");
            Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "http://adlnet.gov/expapi/activities/question");
            Assert.AreEqual(tracejson["verb"]["id"].Value, "https://w3id.org/xapi/seriousgames/verbs/unlocked");
            Assert.AreEqual(tracejson["result"]["response"].Value, "Answer number 3");
            Assert.AreEqual(tracejson["result"]["extensions"]["SubCompletableScore"].AsFloat, 0.8f);
        }

        /// <summary>
        /// (Unit Test Method) tests completable initialized CSV
        /// </summary>
        [TestMethod]
        public void TestCompletable_Csv_01()
        {
            ChangeFormat(TrackerAsset.TraceFormats.csv);

            asset.Completable.Initialized("CompletableID", CompletableTracker.Completable.Quest);
            asset.Flush();

            string csv = storage.Load(settings.LogFile);
            string[] splitted = csv.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            Assert.IsTrue(splitted[splitted.Length - 2].Contains(",initialized,quest,CompletableID"));
        }

        /// <summary>
        /// (Unit Test Method) tests completable progressed CSV
        /// </summary>
        [TestMethod]
        public void TestCompletable_Csv_02()
        {
            ChangeFormat(TrackerAsset.TraceFormats.csv);

            asset.Completable.Progressed("CompletableID2", CompletableTracker.Completable.Stage ,0.34f);
            asset.Flush();

            string csv = storage.Load(settings.LogFile);
            string[] splitted = csv.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            Assert.IsTrue(splitted[splitted.Length - 2].Contains(",progressed,stage,CompletableID2,progress,0.34"));
        }

        /// <summary>
        /// (Unit Test Method) test completable completed CSV
        /// </summary>
        [TestMethod]
        public void TestCompletable_Csv_03()
        {
            ChangeFormat(TrackerAsset.TraceFormats.csv);

            asset.Completable.Completed("CompletableID3", CompletableTracker.Completable.Race, true, 0.54f);
            asset.Flush();

            string csv = storage.Load(settings.LogFile);
            string[] splitted = csv.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            Assert.IsTrue(splitted[splitted.Length - 2].Contains(",completed,race,CompletableID3,success,true,score,0.54"));
        }

        /// <summary>
        /// (Unit Test Method) tests completable initialized XAPI
        /// </summary>
        [TestMethod]
        public void TestCompletable_XApi_01()
        {
            ChangeFormat(TrackerAsset.TraceFormats.xapi);

            asset.Completable.Initialized("CompletableID", CompletableTracker.Completable.Quest);
            asset.Flush();

            JSONNode file = JSON.Parse(storage.Load(settings.LogFile));
            JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

            Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 4);
            Assert.AreEqual(tracejson["object"]["id"].Value, "CompletableID");
            Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/quest");
            Assert.AreEqual(tracejson["verb"]["id"].Value, "https://w3id.org/xapi/adb/verbs/initialized");
        }

        /// <summary>
        /// (Unit Test Method) tests completable progressed XAPI
        /// </summary>
        [TestMethod]
        public void TestCompletable_XApi_02()
        {
            ChangeFormat(TrackerAsset.TraceFormats.xapi);

            asset.Completable.Progressed("CompletableID2", CompletableTracker.Completable.Stage, 0.34f);
            asset.Flush();

            JSONNode file = JSON.Parse(storage.Load(settings.LogFile));
            JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

            Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 5);
            Assert.AreEqual(tracejson["object"]["id"].Value, "CompletableID2");
            Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/stage");
            Assert.AreEqual(tracejson["verb"]["id"].Value, "http://adlnet.gov/expapi/verbs/progressed");
            Assert.AreEqual(tracejson["result"]["extensions"]["https://w3id.org/xapi/seriousgames/extensions/progress"].AsFloat, 0.34f);
        }

        /// <summary>
        /// (Unit Test Method) tests completable completed XAPI
        /// </summary>
        [TestMethod]
        public void TestCompletable_XApi_03()
        {
            ChangeFormat(TrackerAsset.TraceFormats.xapi);

            asset.Completable.Completed("CompletableID3", CompletableTracker.Completable.Race, true, 0.54f);
            asset.Flush();

            JSONNode file = JSON.Parse(storage.Load(settings.LogFile));
            JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

            Assert.AreEqual(tracejson.Count, 5);
            Assert.AreEqual(tracejson["object"]["id"].Value, "CompletableID3");
            Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/race");
            Assert.AreEqual(tracejson["verb"]["id"].Value, "http://adlnet.gov/expapi/verbs/completed");
            Assert.AreEqual(tracejson["result"]["success"].AsBool, true);
            Assert.AreEqual(tracejson["result"]["score"]["raw"].AsFloat, 0.54f);
        }

        /// <summary>
        /// (Unit Test Method) tests gameobject interacted CSV
        /// </summary>
        [TestMethod]
        public void TestGameObject_Csv_01()
        {
            ChangeFormat(TrackerAsset.TraceFormats.csv);

            asset.GameObject.Interacted("GameObjectID", GameObjectTracker.TrackedGameObject.Npc);
            asset.Flush();

            string csv = storage.Load(settings.LogFile);
            string[] splitted = csv.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            Assert.IsTrue(splitted[splitted.Length - 2].Contains(",interacted,npc,GameObjectID"));
        }

        /// <summary>
        /// (Unit Test Method) tests gameobject used CSV
        /// </summary>
        [TestMethod]
        public void TestGameObject_Csv_02()
        {
            ChangeFormat(TrackerAsset.TraceFormats.csv);

            asset.GameObject.Used("GameObjectID2", GameObjectTracker.TrackedGameObject.Item);
            asset.Flush();

            string csv = storage.Load(settings.LogFile);
            string[] splitted = csv.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            Assert.IsTrue(splitted[splitted.Length - 2].Contains(",used,item,GameObjectID2"));
        }

        /// <summary>
        /// (Unit Test Method) tests gameobject interacted XAPI
        /// </summary>
        [TestMethod]
        public void TestGameObject_XApi_01()
        {
            ChangeFormat(TrackerAsset.TraceFormats.xapi);

            asset.GameObject.Interacted("GameObjectID", GameObjectTracker.TrackedGameObject.Npc);
            asset.Flush();

            JSONNode file = JSON.Parse(storage.Load(settings.LogFile));
            JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

            Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 4);
            Assert.AreEqual(tracejson["object"]["id"].Value, "GameObjectID");
            Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/non-player-character");
            Assert.AreEqual(tracejson["verb"]["id"].Value, "http://adlnet.gov/expapi/verbs/interacted");
        }

        /// <summary>
        /// (Unit Test Method) tests gameobject used XAPI
        /// </summary>
        [TestMethod]
        public void TestGameObject_XApi_02()
        {
            ChangeFormat(TrackerAsset.TraceFormats.xapi);

            asset.GameObject.Used("GameObjectID2", GameObjectTracker.TrackedGameObject.Item);
            asset.Flush();

            JSONNode file = JSON.Parse(storage.Load(settings.LogFile));
            JSONNode tracejson = file[new List<JSONNode>(file.Children).Count - 1];

            Assert.AreEqual(new List<JSONNode>(tracejson.Children).Count, 4);
            Assert.AreEqual(tracejson["object"]["id"].Value, "GameObjectID2");
            Assert.AreEqual(tracejson["object"]["definition"]["type"].Value, "https://w3id.org/xapi/seriousgames/activity-types/item");
            Assert.AreEqual(tracejson["verb"]["id"].Value, "https://w3id.org/xapi/seriousgames/verbs/used");
        }


        /// <summary>
        /// Changes tracker trace formatter
        /// </summary>
        public void ChangeFormat(TrackerAsset.TraceFormats format)
        {
            if (settings.TraceFormat != format)
            {
                settings.TraceFormat = format;
                asset.Settings = settings;

                if (storage.Exists(settings.LogFile))
                    storage.Delete(settings.LogFile);
            }
        }

        private string removeTimestamp(string trace)
        {
            return trace.Substring(trace.IndexOf(',') + 1);
        }

        private void CheckCSVTrace(string trace)
        {
            //TODO: this method should access the queue directly.

            CheckCSVStoredTrace(trace);
        }

        private void CheckCSVStoredTrace(string trace)
        {
            string[] lines = System.IO.File.ReadAllLines(settings.LogFile);

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
                    CollectionAssert.Contains(d1.Keys, sp2[i]);
                    Assert.AreEqual(d1[sp2[i]], sp2[i + 1]);
                }
            }
        }

        #endregion Methods
    }
}