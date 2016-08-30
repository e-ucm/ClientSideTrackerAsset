namespace TrackerAssetUnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    // 
    using AssetPackage;
    public class TesterBridge : IBridge, ILog, IDataStorage
    {
        readonly String StorageDir = String.Format(@".{0}TestStorage", Path.DirectorySeparatorChar);
        /// <summary>
        /// Initializes a new instance of the asset_proof_of_concept_demo_CSharp.Bridge class.
        /// </summary>
        public TesterBridge()
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

    }
}