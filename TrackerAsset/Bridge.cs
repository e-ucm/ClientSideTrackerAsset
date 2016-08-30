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

#undef ASYNC
//#define ASYNC

namespace UserModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
#if ASYNC
    using System.Threading.Tasks;
#endif

#if PORTABLE
    //x
#else
    //x
#endif

    // 
    using AssetPackage;
    public class Bridge : IBridge, IDataStorage, IWebServiceRequest, ILog
    {
        readonly String StorageDir = String.Format(@".{0}DataStorage", Path.DirectorySeparatorChar);
        /// <summary>
        /// Initializes a new instance of the asset_proof_of_concept_demo_CSharp.Bridge class.
        /// </summary>
        public Bridge()
        {
            if (!Directory.Exists(StorageDir))
            {
                Directory.CreateDirectory(StorageDir);
            }
        }

        #region IDataStorage Members
        /// <summary>
        /// Exists the given file.
        /// </summary>
        ///
        /// <param name="fileId"> The file identifier to delete. </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>
        public bool Exists(string fileId)
        {
            return File.Exists(Path.Combine(StorageDir, fileId));
        }
        /// <summary>
        /// Gets the files.
        /// </summary>
        ///
        /// <returns>
        /// A List&lt;String&gt;
        /// </returns>
        public String[] Files()
        {
            return Directory.GetFiles(StorageDir).ToList().ConvertAll(
    new Converter<String, String>(p => p.Replace(StorageDir + Path.DirectorySeparatorChar, ""))).ToArray();
            //! EnumerateFiles not supported in Unity3D.
            // 
            //return Directory.EnumerateFiles(StorageDir).ToList().ConvertAll(
            //    new Converter<String, String>(p => p.Replace(StorageDir +  Path.DirectorySeparatorChar, ""))).ToList();
        }
        /// <summary>
        /// Saves the given file.
        /// </summary>
        ///
        /// <param name="fileId">   The file identifier to delete. </param>
        /// <param name="fileData"> Information describing the file. </param>
        public void Save(string fileId, string fileData)
        {
            File.WriteAllText(Path.Combine(StorageDir, fileId), fileData);
        }
        /// <summary>
        /// Loads the given file.
        /// </summary>
        ///
        /// <param name="fileId"> The file identifier to delete. </param>
        ///
        /// <returns>
        /// A String.
        /// </returns>
        public string Load(string fileId)
        {
            return File.ReadAllText(Path.Combine(StorageDir, fileId));
        }
        /// <summary>
        /// Deletes the given fileId.
        /// </summary>
        ///
        /// <param name="fileId"> The file identifier to delete. </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>
        public bool Delete(string fileId)
        {
            if (Exists(fileId))
            {
                File.Delete(Path.Combine(StorageDir, fileId));
                return true;
            }
            return false;
        }
        #endregion

        #region IWebServiceRequestAsync Members
        /*
 #if ASYNC
         /// <summary>
         /// Web service request.
         /// </summary>
         ///
         /// <param name="method">           The method. </param>
         /// <param name="uri">              URI of the document. </param>
         /// <param name="headers">          The headers. </param>
         /// <param name="body">             The body. </param>
         /// <param name="notifyOnResponse"> The response. </param>
         public async void WebServiceRequest(
             string method,
             Uri uri,
             Dictionary<string, string> headers,
             string body,
             IWebServiceResponse notifyOnResponse)
 #else
         /// <summary>
         /// Web service request.
         /// </summary>
         ///
         /// <param name="method">           The method. </param>
         /// <param name="uri">              URI of the document. </param>
         /// <param name="headers">          The headers. </param>
         /// <param name="body">             The body. </param>
         /// <param name="notifyOnResponse"> The response. </param>
         public void WebServiceRequest(
             string method,
             Uri uri,
             Dictionary<string, string> headers,
             string body,
             IWebServiceResponse notifyOnResponse)
 #endif
         {
             try
             {
                 //! Create might throw a silent System.IOException on .NET 3.5 (sync).
                 // 
                 HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                 request.Method = method;
                 // TODO Cookies
                 // 
                 // Both Accept and Content-Type are not allowed as Headers in a HttpWebRequest.
                 // They need to be assigned to a matching property.
                 // 
                 if (headers.ContainsKey("Accept"))
                 {
                     request.Accept = headers["Accept"];
                 }
                 if (!String.IsNullOrEmpty(body))
                 {
                     byte[] data = Encoding.UTF8.GetBytes(body);
                     if (headers.ContainsKey("Content-Type"))
                     {
                         request.ContentType = headers["Content-Type"];
                     }
                     foreach (KeyValuePair<string, string> kvp in headers)
                     {
                        if (kvp.Key.Equals("Accept") || kvp.Key.Equals("Content-Type"))
                        {
                            continue;
                        }
                        request.Headers.Add(kvp.Key, kvp.Value);
                     }

                     request.ContentLength = data.Length;

                     // See https://msdn.microsoft.com/en-us/library/system.net.servicepoint.expect100continue(v=vs.110).aspx
                     // A2 currently does not support this 100-Continue response for POST requets.
                     request.ServicePoint.Expect100Continue = false;

 #if ASYNC
                     Stream stream = await request.GetRequestStreamAsync();
                     await stream.WriteAsync(data, 0, data.Length);
                     stream.Close();
 #else
                     Stream stream = request.GetRequestStream();
                     stream.Write(data, 0, data.Length);
                     stream.Close();
 #endif
                 }
                 else
                 {
                     foreach (KeyValuePair<string, string> kvp in headers)
                     {
                        if (kvp.Key.Equals("Accept") || kvp.Key.Equals("Content-Type"))
                        {
                            continue;
                        }
                         request.Headers.Add(kvp.Key, kvp.Value);
                     }
                 }

 #if ASYNC
                 WebResponse response = await request.GetResponseAsync();
 #else
                 WebResponse response = request.GetResponse();
 #endif
                 if (notifyOnResponse != null)
                 {
                     Dictionary<string, string> responseHeaders = new Dictionary<string, string>();

                     if (response.Headers.HasKeys())
                     {
                         foreach (string key in response.Headers.AllKeys)
                         {
                            responseHeaders.Add(key, response.Headers.Get(key));
                         }
                     }

                     int responseCode = (int)(response as HttpWebResponse).StatusCode;
                     string responseBody = String.Empty;

                     using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                     {
 #if ASYNC
                         responseBody = await reader.ReadToEndAsync();
 #else
                         responseBody = reader.ReadToEnd();
 #endif
                     }

                     notifyOnResponse.Success(uri.ToString(), responseCode, responseHeaders, responseBody);
                 }
             }
             catch (Exception e)
             {
                 if (notifyOnResponse != null)
                 {
                     notifyOnResponse.Error(uri.ToString(), e.Message);
                 }
                 else
                 {
                     Log(Severity.Error, String.Format("{0} - {1}", e.GetType().Name, e.Message));
                 }
             }
         }
 */
        #endregion IWebServiceRequestAsync Members

        #region ILog Members

        /// <summary>
        /// Executes the log operation.
        /// 
        /// Implement this in Game Engine Code.
        /// </summary>
        ///
        /// <param name="severity"> The severity. </param>
        /// <param name="msg">      The message. </param>
        public void Log(Severity severity, string msg)
        {
            // if (((int)LogLevel.Info & (int)severity) == (int)severity)
            {
                if (String.IsNullOrEmpty(msg))
                {
                    Console.WriteLine("");
                }
                else
                {
                    Console.WriteLine(String.Format("{0}: {1}", severity, msg));
                }
            }
        }

        #endregion ILog Members

        #region IWebServiceRequest Members

        // See http://stackoverflow.com/questions/12224602/a-method-for-making-http-requests-on-unity-ios
        // for persistence.
        // See http://18and5.blogspot.com.es/2014/05/mono-unity3d-c-https-httpwebrequest.html

#if ASYNC
        public void WebServiceRequest(RequestSetttings requestSettings, out RequestResponse requestReponse)
        {
            // Wrap the actual method in a Task. Neccesary because we cannot:
            // 1) Make this method async (out is not allowed) 
            // 2) Return a Task<RequestResponse> as it breaks the interface (only void does not break it).
            //
            Task<RequestResponse> taskName = Task.Factory.StartNew<RequestResponse>(() =>
            {
                return WebServiceRequestAsync(requestSettings).Result;
            });

            requestReponse = taskName.Result;
        }

        /// <summary>
        /// Web service request.
        /// </summary>
        ///
        /// <param name="requestSettings"> Options for controlling the operation. </param>
        ///
        /// <returns>
        /// A RequestResponse.
        /// </returns>
        private async Task<RequestResponse> WebServiceRequestAsync(RequestSetttings requestSettings)
#else
        /// <summary>
        /// Web service request.
        /// </summary>
        ///
        /// <param name="requestSettings">  Options for controlling the operation. </param>
        /// <param name="requestResponse"> The request response. </param>
        public void WebServiceRequest(RequestSetttings requestSettings, out RequestResponse requestResponse)
        {
            requestResponse = WebServiceRequest(requestSettings);
        }

        /// <summary>
        /// Web service request.
        /// </summary>
        ///
        /// <param name="requestSettings">Options for controlling the operation. </param>
        ///
        /// <returns>
        /// A RequestResponse.
        /// </returns>
        private RequestResponse WebServiceRequest(RequestSetttings requestSettings)
#endif
        {
            RequestResponse result = new RequestResponse(requestSettings);

            try
            {
                //! Might throw a silent System.IOException on .NET 3.5 (sync).
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestSettings.uri);

                request.Method = requestSettings.method;

                // Both Accept and Content-Type are not allowed as Headers in a HttpWebRequest.
                // They need to be assigned to a matching property.

                if (requestSettings.requestHeaders.ContainsKey("Accept"))
                {
                    request.Accept = requestSettings.requestHeaders["Accept"];
                }

                if (!String.IsNullOrEmpty(requestSettings.body))
                {
                    byte[] data = Encoding.UTF8.GetBytes(requestSettings.body);

                    if (requestSettings.requestHeaders.ContainsKey("Content-Type"))
                    {
                        request.ContentType = requestSettings.requestHeaders["Content-Type"];
                    }

                    foreach (KeyValuePair<string, string> kvp in requestSettings.requestHeaders)
                    {
                        if (kvp.Key.Equals("Accept") || kvp.Key.Equals("Content-Type"))
                        {
                            continue;
                        }
                        request.Headers.Add(kvp.Key, kvp.Value);
                    }

                    request.ContentLength = data.Length;

                    // See https://msdn.microsoft.com/en-us/library/system.net.servicepoint.expect100continue(v=vs.110).aspx
                    // A2 currently does not support this 100-Continue response for POST requets.
                    request.ServicePoint.Expect100Continue = false;

#if ASYNC
                    Stream stream = await request.GetRequestStreamAsync();
                    await stream.WriteAsync(data, 0, data.Length);
                    stream.Close();
#else
                    Stream stream = request.GetRequestStream();
                    stream.Write(data, 0, data.Length);
                    stream.Close();
#endif
                }
                else
                {
                    foreach (KeyValuePair<string, string> kvp in requestSettings.requestHeaders)
                    {
                        if (kvp.Key.Equals("Accept") || kvp.Key.Equals("Content-Type"))
                        {
                            continue;
                        }
                        request.Headers.Add(kvp.Key, kvp.Value);
                    }
                }

#if ASYNC
                WebResponse response = await request.GetResponseAsync();
#else
                WebResponse response = request.GetResponse();
#endif
                if (response.Headers.HasKeys())
                {
                    foreach (string key in response.Headers.AllKeys)
                    {
                        result.responseHeaders.Add(key, response.Headers.Get(key));
                    }
                }

                result.responseCode = (int)(response as HttpWebResponse).StatusCode;

                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
#if ASYNC
                    result.body = await reader.ReadToEndAsync();
#else
                    result.body = reader.ReadToEnd();
#endif
                }
            }
            catch (Exception e)
            {
                result.responsMessage = e.Message;

                Log(Severity.Error, String.Format("{0} - {1}", e.GetType().Name, e.Message));
            }

            return result;
        }

        #endregion IWebServiceRequest Members

    }
}