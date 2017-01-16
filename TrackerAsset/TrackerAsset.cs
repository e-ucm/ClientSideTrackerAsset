/*
 * Copyright 2016 Open University of the Netherlands
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

//#define ASYNC_INTERFACE

namespace AssetPackage
{
    using AssetPackage;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text.RegularExpressions;
    using SimpleJSON;

    /// <summary>
    /// A tracker asset.
    /// 
    /// <list type="number">
    /// <item><term>TODO</term><desciption> - Add method to return the mime-type/content-type.</desciption></item>
    /// <item><term>TODO</term><desciption> - Add method to return the accept-type.</desciption></item>
    /// 
    /// <item><term>TODO</term><desciption> - Check disk based/off-line storage (local).</desciption></item>
    /// <item><term>TODO</term><desciption> - Serialize Queue for later submission (using queue.ToList()).</desciption></item>
    /// 
    /// <item><term>TODO</term><desciption> - Prevent csv/xml/json from net storage and xapi from local storage.</desciption></item>
    /// </list>
    /// </summary>
    public class TrackerAsset : BaseAsset
#if ASYNC_INTERFACE
        , IWebServiceResponseAsync
#endif
    {
        #region Fields

        public static DateTime START_DATE = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// The RegEx to extract a JSON Object. Used to extract 'actor'.
        /// </summary>
        ///
        /// <remarks>
        /// NOTE: This regex handles matching brackets by using balancing groups. This should be tested in Mono if it works there too.<br />
        /// NOTE: {} brackets must be escaped as {{ and }} for String.Format statements.<br />
        /// NOTE: \ must be escaped as \\ in strings.<br />
        /// </remarks>
        private const string ObjectRegEx =
            "\"{0}\":(" +                   // {0} is replaced by the proprty name, capture only its value in {} brackets.
            "\\{{" +                        // Start with a opening brackets.
            "(?>" +
            "    [^{{}}]+" +                // Capture each non bracket chracter.
            "    |    \\{{ (?<number>)" +   // +1 for opening bracket.
            "    |    \\}} (?<-number>)" +  // -1 for closing bracket.
            ")*" +
            "(?(number)(?!))" +             // Handle unaccounted left brackets with a fail.
            "\\}})"; // Stop at matching bracket.

        //private const string ObjectRegEx = "\"{0}\":(\\{{(?:.+?)\\}},)";
        /// <summary>
        /// Filename of the settings file.
        /// </summary>
        const String SettingsFileName = "TrackerAssetSettings.xml";

        /// <summary>
        /// The TimeStamp Format.
        /// </summary>
        private const string TimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

        /// <summary>
        /// The RegEx to extract a plain quoted JSON Value. Used to extract 'token'.
        /// </summary>
        private const string TokenRegEx = "\"{0}\":\"(.+?)\"";

        /// <summary>
        /// The instance.
        /// </summary>
        static readonly TrackerAsset _instance = new TrackerAsset();

        /// <summary>
        /// Identifier for the object.
        /// 
        /// Extracted from JSON inside Success().
        /// </summary>
        private static String ObjectId = String.Empty;

        /// <summary>
        /// A Regex to extact the actor object from JSON.
        /// </summary>
        private Regex jsonActor = new Regex(String.Format(ObjectRegEx, "actor"), RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// A Regex to extact the authentication token value from JSON.
        /// </summary>
        private Regex jsonAuthToken = new Regex(String.Format(TokenRegEx, "authToken"), RegexOptions.Singleline);

        /// <summary>
        /// A Regex to extact the objectId value from JSON.
        /// </summary>
        private Regex jsonObjectId = new Regex(String.Format(TokenRegEx, "objectId"), RegexOptions.Singleline);

        /// <summary>
        /// A Regex to extact the token value from JSON.
        /// </summary>
        private Regex jsonToken = new Regex(String.Format(TokenRegEx, "token"), RegexOptions.Singleline);

        /// <summary>
        /// A Regex to extact the status value from JSON.
        /// </summary>
        private Regex jsonHealth = new Regex(String.Format(TokenRegEx, "status"), RegexOptions.Singleline);

        /// <summary>
        /// The queue of TrackerEvents to Send.
        /// </summary>
        private Queue<TrackerEvent> queue = new Queue<TrackerEvent>();

        /// <summary>
        /// Options for controlling the operation.
        /// </summary>
        private TrackerAssetSettings settings = null;

        /// <summary>
        /// List of Extensions that have to ve added to the next trace
        /// </summary>
        private Dictionary<string, System.Object> extensions = new Dictionary<string, System.Object>();

        #region SubTracker Fields

        /// <summary>
        /// Instance of AccesibleTracker
        /// </summary>
        private AccessibleTracker accessibletracker;

        /// <summary>
        /// Instance of AlternativeTracker
        /// </summary>
        private AlternativeTracker alternativetracker;

        /// <summary>
        /// Instance of CompletableTracker
        /// </summary>
        private CompletableTracker completabletracker;

        /// <summary>
        /// Instance of GameObjectTracker
        /// </summary>
        private GameObjectTracker gameobjecttracer;

        #endregion SubTracker Fields

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Explicit static constructor tells # compiler not to mark type as
        /// beforefieldinit.
        /// </summary>
        static TrackerAsset()
        {
            // Nothing
        }

        /// <summary>
        /// Prevents a default instance of the TrackerAsset class from being created.
        /// </summary>
        private TrackerAsset()
            : base()
        {
            settings = new TrackerAssetSettings();

            if (LoadSettings(SettingsFileName))
            {
                // ok
            }
            else
            {
                settings.Secure = true;
                settings.Host = "rage.e-ucm.es";
                settings.Port = 443;
                settings.BasePath = "/api/";

                settings.UserToken = "";
                settings.TrackingCode = "";
                settings.StorageType = StorageTypes.local;
                settings.TraceFormat = TraceFormats.csv;
                settings.BatchSize = 10;

                SaveSettings(SettingsFileName);
            }
        }

        #endregion Constructors

        #region Enumerations

        /// <summary>
        /// Values that represent events.
        /// </summary>
        public enum Events
        {
            /// <summary>
            /// An enum constant representing the choice option.
            /// </summary>
            choice,
            /// <summary>
            /// An enum constant representing the click option.
            /// </summary>
            click,
            /// <summary>
            /// An enum constant representing the screen option.
            /// </summary>
            screen,
            /// <summary>
            /// An enum constant representing the variable option.
            /// </summary>
            var,
            /// <summary>
            /// An enum constant representing the zone option.
            /// </summary>
            zone,
        }

        /// <summary>
        /// Values that represent storage types.
        /// </summary>
        public enum StorageTypes
        {
            /// <summary>
            /// An enum constant representing the network option.
            /// </summary>
            net,

            /// <summary>
            /// An enum constant representing the local option.
            /// </summary>
            local
        }

        /// <summary>
        /// Values that represent trace formats.
        /// </summary>
        public enum TraceFormats
        {
            /// <summary>
            /// An enum constant representing the JSON option.
            /// </summary>
            json,
            /// <summary>
            /// An enum constant representing the XML option.
            /// </summary>
            xml,
            /// <summary>
            /// An enum constant representing the xAPI option.
            /// </summary>
            xapi,
            /// <summary>
            /// An enum constant representing the CSV option.
            /// </summary>
            csv,
        }

        /// <summary>
        /// Values that represent the available verbs for traces.
        /// </summary>
        public enum Verb
        {
            Initialized,
            Progressed,
            Completed,
            Accessed,
            Skipped,
            Selected,
            Unlocked,
            Interacted,
            Used
        }

        /// <summary>
        /// Values that represent the different extensions for traces.
        /// </summary>
        public enum Extension
        {
            /* Special extensions, 
               those extensions are stored reparatedly in xAPI, e.g.:
               result: {
                    score: {
                        raw: <score_value: float>
                    },
                    success: <success_value: bool>,
                    completion: <completion_value: bool>,
                    response: <response_value: string>
                    ...
               }


            */
            Score,
            Success,
            Response,
            Completion,

            /* Common extensions, these extensions are stored 
               in the result.extensions object (in the xAPI format), e.g.:

               result: {
                    ...
                    extensions: {
                        .../health: <value>,
                        .../position: <value>,
                        .../progress: <value>
                    }
               }
            */
            Health,
            Position,
            Progress
        }

        #endregion Enumerations

        #region Properties

        /// <summary>
        /// Visible when reflecting.
        /// </summary>
        ///
        /// <value>
        /// The instance.
        /// </value>
        public static TrackerAsset Instance
        {
            get
            {
                return _instance;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the connection active (ie the ActorObject
        /// and ObjectId have been extracted).
        /// </summary>
        ///
        /// <value>
        /// true if active, false if not.
        /// </value>
        public Boolean Active { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the connected (ie a UserToken is present and no Fail() has occurred).
        /// </summary>
        ///
        /// <value>
        /// true if connected, false if not.
        /// </value>
        public Boolean Connected { get; private set; }

        /// <summary>
        /// Gets the health.
        /// </summary>
        ///
        /// <value>
        /// The health.
        /// </value>
        public String Health { get; private set; }

        /// <summary>
        /// Gets or sets options for controlling the operation.
        /// </summary>
        ///
        /// <remarks>   Besides the toXml() and fromXml() methods, we never use this property but use
        ///                it's correctly typed backing field 'settings' instead. </remarks>
        /// <remarks> This property should go into each asset having Settings of its own. </remarks>
        /// <remarks>   The actual class used should be derived from BaseAsset (and not directly from
        ///             ISetting). </remarks>
        ///
        /// <value>
        /// The settings.
        /// </value>
        public override ISettings Settings
        {
            get
            {
                return settings;
            }
            set
            {
                settings = (value as TrackerAssetSettings);
            }
        }

        /// <summary>
        /// The actor object.
        /// 
        /// Extracted from JSON inside Success().
        /// </summary>
        private static JSONNode ActorObject
        {
            get;
            set;
        }

        #region SubTracker Properties

        /// <summary>
        /// Access point for Accesible Traces generation
        /// </summary>
        public AccessibleTracker Accesible
        {
            get
            {
                if(accessibletracker == null)
                {
                    accessibletracker = new AccessibleTracker();
                    accessibletracker.setTracker(this);
                }
                    
                return accessibletracker;
            }
        }

        /// <summary>
        /// Access point for Alternative Traces generation
        /// </summary>
        public AlternativeTracker Alternative
        {
            get
            {
                if (alternativetracker == null)
                {
                    alternativetracker = new AlternativeTracker();
                    alternativetracker.setTracker(this);
                }

                return alternativetracker;
            }
        }

        /// <summary>
        /// Access point for Completable Traces generation
        /// </summary>
        public CompletableTracker Completable
        {
            get
            {
                if (completabletracker == null)
                {
                    completabletracker = new CompletableTracker();
                    completabletracker.setTracker(this);
                }

                return completabletracker;
            }
        }

        /// <summary>
        /// Access point for Completable Traces generation
        /// </summary>
        public GameObjectTracker GameObject
        {
            get
            {
                if (gameobjecttracer == null)
                {
                    gameobjecttracer = new GameObjectTracker();
                    gameobjecttracer.setTracker(this);
                }

                return gameobjecttracer;
            }
        }

        #endregion SubTracker Properties

        #endregion Properties

        #region Methods

        /// <summary>
        /// Checks the health of the UCM Tracker.
        /// </summary>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>
        public Boolean CheckHealth()
        {
            RequestResponse response = IssueRequest("health", "GET");

            if (response.ResultAllowed)
            {
                if (jsonHealth.IsMatch(response.body))
                {
                    Health = jsonHealth.Match(response.body).Groups[1].Value;

                    Log(Severity.Information, "Health Status={0}", Health);
                }
            }
            else
            {
                Log(Severity.Error, "Request Error: {0}-{1}", response.responseCode, response.responsMessage);
            }

            return response.ResultAllowed;
        }

#if ASYNC_INTERFACE
        /// <summary>
        /// Errors.
        /// </summary>
        ///
        /// <param name="url"> URL of the document. </param>
        /// <param name="msg"> The error message. </param>
        public void Error(string url, string msg)
        {
            //Log(Severity.Error, "{0} - [{1}]", msg, url);

            //Connected = false;
        }
#endif

        /// <summary>
        /// Flushes the queue.
        /// </summary>
        public void Flush()
        {
            if (!Connected)
            {
                Log(Severity.Verbose, "Not connected yet, Can't flush.");

                // Start();
            }
            else
            {
                ProcessQueue();
            }
        }

        /// <summary>
        /// Login with a Username and Password.
        ///
        /// After this call, the Success method will extract the token from the returned .
        /// </summary>
        ///
        /// <param name="username"> The username. </param>
        /// <param name="password"> The password. </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>
        public Boolean Login(string username, string password)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();

            headers.Add("Content-Type", "application/json");
            headers.Add("Accept", "application/json");

            RequestResponse response = IssueRequest("login", "POST", headers,
                String.Format("{{\r\n \"username\": \"{0}\",\r\n \"password\": \"{1}\"\r\n}}",
                username, password));

            if (response.ResultAllowed)
            {
                if (jsonToken.IsMatch(response.body))
                {
                    settings.UserToken = jsonToken.Match(response.body).Groups[1].Value;
                    if (settings.UserToken.StartsWith("Bearer "))
                    {
                        settings.UserToken.Remove(0, "Bearer ".Length);
                    }
                    Log(Severity.Information, "Token= {0}", settings.UserToken);

                    Connected = true;
                }
            }
            else
            {
                Log(Severity.Error, "Request Error: {0}-{1}", response.responseCode, response.responsMessage);

                Connected = false;
            }

            return Connected;
        }

        /// <summary>
        /// Starts with a userToken and trackingCode.
        /// </summary>
        ///
        /// <param name="userToken">    The user token. </param>
        /// <param name="trackingCode"> The tracking code. </param>
        public void Start(String userToken, String trackingCode)
        {
            settings.UserToken = userToken;
            settings.TrackingCode = trackingCode;

            Start();
        }

        /// <summary>
        /// Starts with a trackingCode (and with the already extracted UserToken).
        /// </summary>
        ///
        /// <param name="trackingCode"> The tracking code. </param>
        public void Start(String trackingCode)
        {
            settings.TrackingCode = trackingCode;

            Start();
        }

        /// <summary>
        /// Starts Tracking with: 1) An already extracted UserToken (from Login) and
        /// 2) TrackingCode (Shown at Game on a2 server).
        /// </summary>
        public void Start()
        {
            switch (settings.StorageType)
            {
                case StorageTypes.net:
                    Dictionary<string, string> headers = new Dictionary<string, string>();

                    //! The UserToken might get swapped for a better one during response
                    //! processing. 
                    headers["Authorization"] = String.Format("Bearer {0}", settings.UserToken);

                    RequestResponse response = IssueRequest(String.Format("proxy/gleaner/collector/start/{0}", settings.TrackingCode), "POST", headers, String.Empty);

                    if (response.ResultAllowed)
                    {
                        Log(Severity.Information, "");

                        // Extract AuthToken.
                        //
                        if (jsonAuthToken.IsMatch(response.body))
                        {
                            settings.UserToken = jsonAuthToken.Match(response.body).Groups[1].Value;
                            /*
                            if (settings.UserToken.StartsWith("Bearer "))
                            {
                                //! Update UserToken.
                                settings.UserToken = settings.UserToken = settings.UserToken.Remove(0, "Bearer ".Length);
                            }
                            */
                            Log(Severity.Information, "AuthToken= {0}", settings.UserToken);

                            Connected = true;
                        }

                        // Extract AuthToken.
                        //
                        if (jsonObjectId.IsMatch(response.body))
                        {
                            ObjectId = jsonObjectId.Match(response.body).Groups[1].Value;

                            if (!ObjectId.EndsWith("/"))
                            {
                                ObjectId += "/";
                            }

                            Log(Severity.Information, "ObjectId= {0}", ObjectId);
                        }

                        // Extract Actor Json Object.
                        //
                        if (jsonActor.IsMatch(response.body))
                        {
                            ActorObject = JSONNode.Parse(jsonActor.Match(response.body).Groups[1].Value);

                            Log(Severity.Information, "Actor= {0}", ActorObject);

                            Active = true;
                        }
                    }
                    else
                    {
                        Log(Severity.Error, "Request Error: {0}-{1}", response.responseCode, response.responsMessage);

                        Active = false;
                        Connected = false;
                    }

                    break;

                case StorageTypes.local:
                    {
                        // Allow LocalStorage if a Bridge is implementing IDataStorage.
                        // 
                        IDataStorage tmp = getInterface<IDataStorage>();

                        Connected = tmp != null;
                        Active = tmp != null;
                    }
                    break;
            }
        }

#if ASYNC_INTERFACE
        /// <summary>
        /// Success.
        /// </summary>
        ///
        /// <remarks>
        /// This method also extracts information from the returned body (token,
        /// authToken, objectId and actor).
        /// </remarks>
        ///
        /// <param name="url">     URL of the document. </param>
        /// <param name="code">    The code. </param>
        /// <param name="headers"> The headers. </param>
        /// <param name="body">    The body. </param>
        public void Success(string url, int code, Dictionary<string, string> headers, string body)
        {
            //Log(Severity.Verbose, "Success: {0} - [{1}]", code, url);

            //foreach (KeyValuePair<string, string> kvp in headers)
            //{
            //    Log(Severity.Verbose, "{0}: {1}", kvp.Key, kvp.Value);
            //}
            //Log(Severity.Verbose, body);
            //Log(Severity.Verbose, "");

            //#warning the following code should be improved (is partially caused by the use of JSON instead of XML).

            // Flow:
            // 1a) If we use a: as Authorization value on the /start/ call (and do not login),
            // 1b) We have to take the 'authToken' value from the /start/ request for subsequent calls.
            // 2a) If we login with username/password, we get a temporary Authorization value from the 'token' value.
            // 2b) This Authorization value we use for /start/ and replace it inside success() with the 'authToken' value for subsequent calls.
            // 3a) The 'token' value from 2a) can also be used directly for a start() call.

            ////! /HEALTH/
            ////
            //if (url.EndsWith("/health"))
            //{
            //    Log(Severity.Information, "Health= {0}", body);
            //}

            //! /LOGIN/
            //
            //if (url.EndsWith("/login") && jsonToken.IsMatch(body))
            //{
            //    settings.UserToken = jsonToken.Match(body).Groups[1].Value;
            //    if (settings.UserToken.StartsWith("Bearer "))
            //    {
            //        settings.UserToken.Remove(0, "Bearer ".Length);
            //    }
            //    Log(Severity.Information, "Token= {0}", settings.UserToken);

            //    Connected = true;
            //}

            //! /START/
            //
            //if (url.EndsWith(String.Format("/start/{0}", (Settings as TrackerAssetSettings).TrackingCode)))
            //{
            //    Log(Severity.Information, "");

            //    // Extract AuthToken.
            //    //
            //    if (jsonAuthToken.IsMatch(body))
            //    {
            //        settings.UserToken = jsonAuthToken.Match(body).Groups[1].Value;
            //        if (settings.UserToken.StartsWith("Bearer "))
            //        {
            //            settings.UserToken = settings.UserToken = settings.UserToken.Remove(0, "Bearer ".Length);
            //        }
            //        Log(Severity.Information, "AuthToken= {0}", settings.UserToken);

            //        Connected = true;
            //    }

            //    // Extract AuthToken.
            //    //
            //    if (jsonObjectId.IsMatch(body))
            //    {
            //        ObjectId = jsonObjectId.Match(body).Groups[1].Value;

            //        if (!ObjectId.EndsWith("/"))
            //        {
            //            ObjectId += "/";
            //        }

            //        Log(Severity.Information, "ObjectId= {0}", ObjectId);
            //    }

            //    // Extract Actor Json Object.
            //    //
            //    if (jsonActor.IsMatch(body))
            //    {
            //        ActorObject = jsonActor.Match(body).Groups[1].Value;

            //        Log(Severity.Information, "Actor= {0}", ActorObject);

            //        Active = true;
            //    }
            //}

            if (url.EndsWith("/track"))
            {
                Log(Severity.Information, "Track= {0}", body);
            }

            Active = !(String.IsNullOrEmpty(ActorObject) || String.IsNullOrEmpty(ObjectId));
        }
#endif

        /// <summary>
        /// Adds the given value to the Queue.
        /// </summary>
        ///
        /// <param name="value"> New value for the variable. </param>
        public void Trace(TrackerEvent trace)
        {
            if (extensions.Count > 0)
            {
                trace.Result.Extensions = new Dictionary<string, object>(extensions);
                extensions.Clear();
            }
            queue.Enqueue(trace);
        }

        /// <summary>
        /// Issue a HTTP Webrequest.
        /// </summary>
        ///
        /// <param name="path">   Full pathname of the file. </param>
        /// <param name="method"> The method. </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>
        private bool IssueRequestAsync(string path, string method)
        {
            return IssueRequestAsync(path, method, new Dictionary<string, string>(), String.Empty);
        }

        /// <summary>
        /// Issue a HTTP Webrequest.
        /// </summary>
        ///
        /// <param name="path">    Full pathname of the file. </param>
        /// <param name="method">  The method. </param>
        /// <param name="headers"> The headers. </param>
        /// <param name="body">    The body. </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>
        private bool IssueRequestAsync(string path, string method, Dictionary<string, string> headers, string body)
        {
            return IssueRequestAsync(path, method, headers, body, settings.Port);
        }

        /// <summary>
        /// Issue a HTTP Webrequest.
        /// </summary>
        ///
        /// <param name="path">    Full pathname of the file. </param>
        /// <param name="method">  The method. </param>
        /// <param name="headers"> The headers. </param>
        /// <param name="body">    The body. </param>
        /// <param name="port">    The port. </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>
        private bool IssueRequestAsync(string path, string method, Dictionary<string, string> headers, string body, Int32 port)
        {
            IWebServiceRequestAsync ds = getInterface<IWebServiceRequestAsync>();

            if (ds != null)
            {
                //Log(LogLevel.Verbose, "****");

                Uri uri = new Uri(string.Format("http{0}://{1}{2}{3}/{4}",
                    settings.Secure ? "s" : String.Empty,
                    settings.Host,
                    port == 80 ? String.Empty : String.Format(":{0}", port),
                    String.IsNullOrEmpty(settings.BasePath.TrimEnd('/')) ? "" : settings.BasePath.TrimEnd('/'),
                    path.TrimStart('/')));

                Log(Severity.Verbose, "{0} [{1}]", method, uri.ToString());

                foreach (KeyValuePair<string, string> kvp in headers)
                {
                    Log(Severity.Verbose, "{0}: {1}", kvp.Key, kvp.Value);
                }

                if (!string.IsNullOrEmpty(body))
                {
                    Log(Severity.Verbose, body);
                }

                /*ds.WebServiceRequestAsync(
                    method,
                    uri,
                    headers,
                    body,
                    this);*/

                return true;
            }

            return false;
        }

        /// <summary>
        /// Issue a HTTP Webrequest.
        /// </summary>
        ///
        /// <param name="path">   Full pathname of the file. </param>
        /// <param name="method"> The method. </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>
        private RequestResponse IssueRequest(string path, string method)
        {
            return IssueRequest(path, method, new Dictionary<string, string>(), String.Empty);
        }

        /// <summary>
        /// Issue a HTTP Webrequest.
        /// </summary>
        ///
        /// <param name="path">    Full pathname of the file. </param>
        /// <param name="method">  The method. </param>
        /// <param name="headers"> The headers. </param>
        /// <param name="body">    The body. </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>
        private RequestResponse IssueRequest(string path, string method, Dictionary<string, string> headers, string body = "")
        {
            return IssueRequest(path, method, headers, body, settings.Port);
        }

        /// <summary>
        /// Query if this object issue request 2.
        /// </summary>
        ///
        /// <param name="path">    Full pathname of the file. </param>
        /// <param name="method">  The method. </param>
        /// <param name="headers"> The headers. </param>
        /// <param name="body">    The body. </param>
        /// <param name="port">    The port. </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>
        private RequestResponse IssueRequest(string path, string method, Dictionary<string, string> headers, string body, Int32 port)
        {
            IWebServiceRequest ds = getInterface<IWebServiceRequest>();

            RequestResponse response = new RequestResponse();

            if (ds != null)
            {
                ds.WebServiceRequest(
                   new RequestSetttings
                   {
                       method = method,
                       uri = new Uri(string.Format("http{0}://{1}{2}{3}/{4}",
                                   settings.Secure ? "s" : String.Empty,
                                   settings.Host,
                                   port == 80 ? String.Empty : String.Format(":{0}", port),
                                   String.IsNullOrEmpty(settings.BasePath.TrimEnd('/')) ? "" : settings.BasePath.TrimEnd('/'),
                                   path.TrimStart('/')
                                   )),
                       requestHeaders = headers,
                       //! allowedResponsCodes,     // TODO default is ok
                       body = body, // or method.Equals("GET")?string.Empty:body
                   }, out response);
            }

            return response;
        }

        /// <summary>
        /// Process the queue.
        /// </summary>
        private void ProcessQueue()
        {
            if (queue.Count > 0)
            {
                List<string> sb = new List<string>();

                UInt32 cnt = settings.BatchSize == 0 ? UInt32.MaxValue : settings.BatchSize;

                while (queue.Count > 0 && cnt > 0)
                {
                    TrackerEvent item = queue.Dequeue();

                    cnt -= 1;

                    switch (settings.TraceFormat)
                    {
                        case TraceFormats.json:
                            sb.Add(item.ToJson());
                            break;
                        case TraceFormats.xml:
                            sb.Add(item.ToXml());
                            break;
                        case TraceFormats.xapi:
                            sb.Add(item.ToXapi());
                            break;
                        default:
                            sb.Add(item.ToCsv());
                            break;
                    }
                }

                String data = String.Empty;

                switch (settings.TraceFormat)
                {
                    case TraceFormats.csv:
                        data = String.Join("\r\n", sb.ToArray()) + "\r\n";
                        break;
                    case TraceFormats.json:
                        data = "[\r\n" + String.Join(",\r\n", sb.ToArray()) + "\r\n]";
                        break;
                    case TraceFormats.xml:
                        data = "<TrackEvents>\r\n" + String.Join("\r\n", sb.ToArray()) + "\r\n</TrackEvent>";
                        break;
                    case TraceFormats.xapi:
                        data = "[\r\n" + String.Join(",\r\n", sb.ToArray()) + "\r\n]";
                        break;
                    default:
                        data = String.Join("\r\n", sb.ToArray());
                        break;
                }

                sb.Clear();

                Log(Severity.Verbose, data);

                switch (settings.StorageType)
                {
                    case StorageTypes.local:
                        IDataStorage storage = getInterface<IDataStorage>();

                        if (storage != null)
                        {
                            String previous = storage.Exists(settings.LogFile) ? storage.Load(settings.LogFile) : String.Empty;

                            if (previous.Length > 0) {
                                previous = previous.Replace("\r\n]", ",\r\n");
                                data = data.Replace("[\r\n", "");
                            }

#warning TODO Add Append() to IDataStorage using File.AppendAllText().

                            storage.Save(settings.LogFile, previous + data);
                        }

                        break;
                    case StorageTypes.net:
                        Dictionary<string, string> headers = new Dictionary<string, string>();

                        headers.Add("Content-Type", "application/json");
                        headers.Add("Authorization", String.Format("{0}", settings.UserToken));

                        Log(Severity.Information, "\r\n" + data);

                        RequestResponse response = IssueRequest("proxy/gleaner/collector/track",
                                "POST", headers, data);

                        if (response.ResultAllowed)
                        {
                            Log(Severity.Information, "Track= {0}", response.body);
                        }
                        else
                        {
                            Log(Severity.Error, "Request Error: {0}-{1}", response.responseCode, response.responsMessage);

                            Active = false;
                            Connected = false;
                        }

                        break;
                }
            }
            else
            {
                Log(Severity.Information, "Nothing to flush");
            }
        }

        #region Extension Methods

        public void setProgress(float progress)
        {
            setExtension(Extension.Progress.ToString().ToLower(), progress);
        }

        public void setPosition(float x, float y, float z)
        {
            setExtension(Extension.Position.ToString().ToLower(), "{\"x\":" + x + ", \"y\": " + y
                    + ", \"z\": " + z + "}");
        }

        public void setHealth(float health)
        {
            setExtension(Extension.Health.ToString().ToLower(), health);
        }

        public void setVar(string id, string value)
        {
            setExtension(id, value);
        }

        public void setExtension(string key, System.Object value)
        {
            extensions.Add(key, value);
        }

        #endregion Extension Methods

        #endregion Methods

        #region Nested Types

        /// <summary>
        /// Interface that subtrackers must implement.
        /// </summary>
        public interface IGameObjectTracker
        {
            void setTracker(TrackerAsset tracker);
        }

        /// <summary>
        /// Interface that trace formatters must implement.
        /// </summary>
        public interface ITraceFormatter
        {
            string Serialize(List<string> traces);

            void StartData(JSONClass data);
        }

        /// <summary>
        /// A tracker event.
        /// </summary>
        public class TrackerEvent
        {
            #region Fields
                private static Dictionary<string, string> verbIds;

                private static Dictionary<string, string> objectIds;

                private static Dictionary<string, string> extensionIds;
            #endregion Fields

            #region Constructors

            public TrackerEvent()
            {
                this.TimeStamp = Math.Round(System.DateTime.Now.ToUniversalTime().Subtract(START_DATE).TotalMilliseconds);
                this.Result = new TraceResult();
            }

            #endregion Constructors

            #region Properties

            private static Dictionary<string, string> VerbIDs
            {
                get
                {
                    if (verbIds == null)
                    {
                        verbIds = new Dictionary<string, string>()
                        {
                            { TrackerAsset.Verb.Initialized.ToString().ToLower(), "https://w3id.org/xapi/adb/verbs/initialized"},
                            { TrackerAsset.Verb.Progressed.ToString().ToLower(), "http://adlnet.gov/expapi/verbs/progressed"},
                            { TrackerAsset.Verb.Completed.ToString().ToLower(), "http://adlnet.gov/expapi/verbs/completed"},
                            { TrackerAsset.Verb.Accessed.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/verbs/accessed"},
                            { TrackerAsset.Verb.Skipped.ToString().ToLower(), "http://id.tincanapi.com/verb/skipped"},
                            { TrackerAsset.Verb.Selected.ToString().ToLower(), "https://w3id.org/xapi/adb/verbs/selected"},
                            { TrackerAsset.Verb.Unlocked.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/verbs/unlocked"},
                            { TrackerAsset.Verb.Interacted.ToString().ToLower(), "http://adlnet.gov/expapi/verbs/interacted"},
                            { TrackerAsset.Verb.Used.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/verbs/used"}
                        };
                    }
                    return verbIds;
                }
            }

            private static Dictionary<string, string> ObjectIDs
            {
                get
                {
                    if (objectIds == null)
                    {
                        objectIds = new Dictionary<string, string>()
                        {
                            // Completable
                            { CompletableTracker.Completable.Game.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/serious-game" },
                            { CompletableTracker.Completable.Session.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/session"},
                            { CompletableTracker.Completable.Level.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/level"},
                            { CompletableTracker.Completable.Quest.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/quest"},
                            { CompletableTracker.Completable.Stage.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/stage"},
                            { CompletableTracker.Completable.Combat.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/combat"},
                            { CompletableTracker.Completable.StoryNode.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/story-node"},
                            { CompletableTracker.Completable.Race.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/race"},
                            { CompletableTracker.Completable.Completable.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/completable"},

                            // Acceesible
                            { AccessibleTracker.Accessible.Screen.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/screen" },
                            { AccessibleTracker.Accessible.Area.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/area"},
                            { AccessibleTracker.Accessible.Zone.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/zone"},
                            { AccessibleTracker.Accessible.Cutscene.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/cutscene"},
                            { AccessibleTracker.Accessible.Accessible.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/accessible"},

                            // Alternative
                            { AlternativeTracker.Alternative.Question.ToString().ToLower(), "http://adlnet.gov/expapi/activities/question" },
                            { AlternativeTracker.Alternative.Menu.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/menu"},
                            { AlternativeTracker.Alternative.Dialog.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/dialog-tree"},
                            { AlternativeTracker.Alternative.Path.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/path"},
                            { AlternativeTracker.Alternative.Arena.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/arena"},
                            { AlternativeTracker.Alternative.Alternative.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/alternative"},

                            // GameObject
                            { GameObjectTracker.TrackedGameObject.Enemy.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/enemy" },
                            { GameObjectTracker.TrackedGameObject.Npc.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/non-player-character"},
                            { GameObjectTracker.TrackedGameObject.Item.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/item"},
                            { GameObjectTracker.TrackedGameObject.GameObject.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/activity-types/game-object"}
                        };
                    }
                    return objectIds;
                }
            }

            private static Dictionary<string, string> ExtensionIDs
            {
                get
                {
                    if (extensionIds == null)
                    {
                        extensionIds = new Dictionary<string, string>()
                        {
                            { TrackerAsset.Extension.Health.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/extensions/health"},
                            { TrackerAsset.Extension.Position.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/extensions/position"},
                            { TrackerAsset.Extension.Progress.ToString().ToLower(), "https://w3id.org/xapi/seriousgames/extensions/progress"}
                        };
                    }
                    return extensionIds;
                }
            }

            /// <summary>
            /// Gets or sets the event.
            /// </summary>
            ///
            /// <value>
            /// The event.
            /// </value>
            [DefaultValue("")]
            public TraceVerb Event { get; set; }

            /// <summary>
            /// Gets or sets the Target for the.
            /// </summary>
            ///
            /// <value>
            /// The target.
            /// </value>
            [DefaultValue("")]
            public TraceObject Target { get; set; }

            /// <summary>
            /// Gets or sets the Result for the.
            /// </summary>
            ///
            /// <value>
            /// The Result.
            /// </value>
            [DefaultValue("")]
            public TraceResult Result { get; set; }

            /// <summary>
            /// Gets the Date/Time of the time stamp.
            /// </summary>
            ///
            /// <value>
            /// The time stamp.
            /// </value>
            public double TimeStamp { get; private set; }

            #endregion Properties

            #region Methods

            /// <summary>
            /// Converts this object to a CSV Item.
            /// </summary>
            ///
            /// <returns>
            /// This object as a string.
            /// </returns>
            public string ToCsv()
            {
                return this.TimeStamp
                    + "," + Event.ToCsv()
                    + "," + Target.ToCsv() 
                    + (this.Result == null || String.IsNullOrEmpty(this.Result.ToCsv()) ?
                       String.Empty :
                        this.Result.ToCsv());
            }

            /// <summary>
            /// Converts this object to a JSON Item.
            /// </summary>
            ///
            /// <returns>
            /// This object as a string.
            /// </returns>
            public string ToJson()
            {
                JSONClass json = new JSONClass();

                json.Add("actor", (ActorObject == null) ? JSONNode.Parse("{}") : ActorObject);
                json.Add("event", Event.ToJson());
                json.Add("target", Target.ToJson());

                JSONClass result = Result.ToJson();
                if (result.Count > 0)
                    json.Add("result", result);

                json.Add("timestamp", new JSONData(new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc).AddMilliseconds(TimeStamp).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")));

                return json.ToString();
            }

            /// <summary>
            /// Converts this object to an XML Item.
            /// </summary>
            ///
            /// <returns>
            /// This object as a string.
            /// </returns>
            public string ToXml()
            {
#warning Use XMLSerializer else use proper XML Encoding.
                return "<TrackEvent \"timestamp\"=\"" + this.TimeStamp.ToString(TimeFormat) + "\"" +
                       " \"event\"=\"" + verbIds[this.Event.ToString().ToLower()] + "\"" +
                       " \"target\"=\"" + this.Target + "\"" +
                       (this.Result == null || String.IsNullOrEmpty(this.Result.ToXml()) ?
                       " />" :
                       "><![CDATA[" + this.Result.ToXml() + "]]></TrackEvent>");
            }

            /// <summary>
            /// Converts this object to an xapi.
            /// </summary>
            ///
            /// <returns>
            /// This object as a string.
            /// </returns>
            public string ToXapi()
            {
                JSONClass json = new JSONClass();

                json.Add("actor", (ActorObject == null) ? JSONNode.Parse("{}") : ActorObject);
                json.Add("verb", Event.ToXapi());
                json.Add("object", Target.ToXapi());

                if (Result != null)
                {
                    JSONClass result = Result.ToXapi();
                    if (result.Count > 0)
                        json.Add("result", result);
                }

                json.Add("timestamp", new JSONData(new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc).AddMilliseconds(TimeStamp).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")));

                return json.ToString();
            }

            /// <summary>
            /// Enquotes.
            /// </summary>
            ///
            /// <remarks>
            /// Both checks could be combined.
            /// </remarks>
            ///
            /// <param name="value"> The value. </param>
            ///
            /// <returns>
            /// A string.
            /// </returns>
            private string Enquote(string value)
            {
                if (value.Contains("\""))
                {
                    //1) Replace one quote by two quotes and enquote the whole string.
                    return string.Format("\"{0}\"", value.Replace("\"", "\"\""));
                }
                else if (value.Contains("\r\n") || value.Contains(","))
                {
                    // 2) If the string contains a CRLF or , enquote the whole string.
                    return string.Format("\"{0}\"", value);
                }

                return value;
            }

            #endregion Methods

            #region Nested Types

            /// <summary>
            /// Class for Target storage.
            /// </summary>
            public class TraceObject
            {
                public string Type
                {
                    get;
                    set;
                }

                public string ID
                {
                    get;
                    set;
                }

                public JSONClass Definition
                {
                    get;
                    set;
                }

                public TraceObject(string type, string id)
                {
                    this.Type = type;
                    this.ID = id;
                }

                public string ToCsv()
                {
                    return Type + "," + ID;
                }

                public JSONClass ToJson()
                {
                    string typeKey = Type;
                    ObjectIDs.TryGetValue(Type, out typeKey);

                    JSONClass obj = new JSONClass(), definition = new JSONClass();

                    obj["id"] = ((ActorObject!=null) ? ObjectId : "") + ID;
                    definition["type"] = typeKey;

                    obj.Add("definition", definition);

                    return obj;
                }

                public string ToXml()
                {
                    // TODO;
                    return Type + "," + ID;
                }

                public JSONClass ToXapi()
                {
                    string typeKey = Type;
                    ObjectIDs.TryGetValue(Type, out typeKey);

                    JSONClass obj = new JSONClass(), definition = new JSONClass();

                    obj["id"] = ((ActorObject != null) ? ObjectId : "") + ID;
                    definition["type"] = typeKey;

                    obj.Add("definition", definition);

                    return obj;
                }
            }

            /// <summary>
            /// Class for Verb storage.
            /// </summary>
            public class TraceVerb
            {
                public Verb Verb
                {
                    get;
                    set;
                }

                public TraceVerb(Verb verb)
                {
                    this.Verb = verb;
                }

                public string ToCsv()
                {
                    return this.Verb.ToString().ToLower();
                }

                public JSONClass ToJson()
                {
                    string id = this.Verb.ToString().ToLower();
                    VerbIDs.TryGetValue(id, out id);

                    JSONClass verb = new JSONClass();
                    verb["id"] = id;

                    return verb;
                }

                public string ToXml()
                {
                    // TODO;
                    return "";
                }

                public JSONClass ToXapi()
                {
                    string id = this.Verb.ToString().ToLower();
                    VerbIDs.TryGetValue(id, out id);

                    JSONClass verb = new JSONClass();
                    verb["id"] = id;

                    return verb;
                }
            }

            /// <summary>
            /// Class for Result storage.
            /// </summary>
            public class TraceResult
            {
                private int success = -1;
                private int completion = -1;
                private float score = float.NaN;

                public bool Success
                {
                    get { return success == 1 ? true : false; }
                    set { success = value ? 1 : 0; }
                }

                public bool Completion
                {
                    get { return completion == 1 ? true : false; }
                    set { completion = value ? 1 : 0; }
                }

                public string Response
                {
                    get;
                    set;
                }

                public float Score
                {
                    get
                    {
                        return score;
                    } 
                    set
                    {
                        score = value;
                    }
                }

                public Dictionary<string,System.Object> Extensions
                {
                    get;
                    set;
                }

                public string ToCsv()
                {
                    string result =
                        ((success>-1) ? ",success" + intToBoolString(success) : "")
                        + ((completion > -1) ? ",completion" + intToBoolString(completion) : "")
                        + ((!string.IsNullOrEmpty(Response)) ? ",response," + Response : "")
                        + ((!float.IsNaN(score)) ? ",score," + score.ToString().Replace(",",".") : "");

                    if (Extensions != null)
                        foreach (KeyValuePair<string, System.Object> extension in Extensions)
                            result += "," + extension.Key + "," + ((extension.Value != null) ? extension.Value.ToString().Replace(",",".") : "");


                    return result;
                }

                public JSONClass ToJson()
                {
                    JSONClass result = new JSONClass();

                    if (success != -1)
                        result.Add("success", new JSONData(Convert.ToBoolean(success)));

                    if (completion != -1)
                        result.Add("completion", new JSONData(Convert.ToBoolean(completion)));

                    if (!string.IsNullOrEmpty(Response))
                        result.Add("response", new JSONData(Response));

                    if (!float.IsNaN(score))
                        result.Add("score", new JSONData(score));

                    if (Extensions != null) {

                        JSONClass extensions = new JSONClass();
                        foreach(KeyValuePair <string, System.Object > extension in Extensions)
                        {
                            extensions.Add(extension.Key, new JSONData((extension.Value != null) ? extension.Value.ToString() : ""));
                        }

                        result.Add("extensions", extensions);
                    }

                    return result;
                }

                public string ToXml()
                {
                    // TODO;
                    return "";
                }

                public JSONClass ToXapi()
                {
                    JSONClass result = new JSONClass();

                    if (success != -1)
                        result.Add("success", new JSONData(Convert.ToBoolean(success)));

                    if (completion != -1)
                        result.Add("completion", new JSONData(Convert.ToBoolean(completion)));

                    if (!string.IsNullOrEmpty(Response))
                        result.Add("response", new JSONData(Response));

                    if (!float.IsNaN(score))
                        result.Add("score", new JSONData(score));

                    if (Extensions != null)
                    {

                        JSONClass extensions = new JSONClass();
                        foreach (KeyValuePair<string, System.Object> extension in Extensions)
                        {
                            extensions.Add(extension.Key, new JSONData((extension.Value != null) ? extension.Value.ToString() : ""));
                        }

                        result.Add("extensions", extensions);
                    }

                    return result;
                }

                private static string intToBoolString(int property)
                {
                    string ret = "";
                    if(property >= 1)
                    { 
                        ret = ",true";
                    }
                    else if( property == 0)
                    {
                        ret = ",false";
                    }
                    return ret;
                }
            }

            #endregion Nested Types
        }

        #endregion Nested Types
    }
}