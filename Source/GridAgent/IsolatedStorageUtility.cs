using System;
using System.IO;
using System.IO.IsolatedStorage;
using GridSharedLibs;

namespace GridAgent
{
    /// <summary>
    /// Stores and retrieves data using an <see cref="IsolatedStorageFile"/>.
    /// </summary>
    public static class IsolatedStorageUtility
    {
        /// <summary>
        /// Saves a key value pair.
        /// </summary>
        /// <param name="key">The key used to retrieve the value.</param>
        /// <param name="value">The value to be stored.</param>
        /// <exception cref="IOException">If unable to write to file.</exception>
        public static void SaveSetting(string key, object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            string fileName = key + ".txt";
            using (IsolatedStorageFile storageFile = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (value == null || value.ToString().Trim().Length == 0)
                {
                    storageFile.DeleteFile(fileName);
                }
                else
                {
                    using (var isoStream = new IsolatedStorageFileStream(fileName, FileMode.Create, storageFile))
                    {
                        using (var writer = new StreamWriter(isoStream))
                        {
                            writer.Write(value);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Tries the get a setting identified by the specified key.
        /// </summary>
        /// <param name="key">The key identifier.</param>
        /// <param name="setting">The setting value.</param>
        /// <returns><code>true</code> if the value was successfully retrieved; 
        /// <code>false</code> otherwise.</returns>
        /// <exception cref="ArgumentNullException">If key is null.</exception>
        public static bool TryGetSetting(string key, out object setting)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            try
            {
                setting = GetSetting(key);
            }
            catch (Exception)
            {
                setting = null;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the setting with the specified key.
        /// </summary>
        /// <param name="key">The key used to identify the setting.</param>
        /// <returns></returns>
        /// <exception cref="IOException">If unable to write to file.</exception>
        /// <exception cref="ArgumentNullException">If key is null.</exception>
        private static object GetSetting(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            string fileName = key + ".txt";

            using (IsolatedStorageFile storageFile
                = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var isoStream = new IsolatedStorageFileStream(fileName, FileMode.Open, storageFile))
                {
                    using (var reader = new StreamReader(isoStream))
                    {
                        /* Read the to the end of the file. */
                        String storedValue = reader.ReadToEnd();
                        return storedValue;
                    }
                }
            }
        }

        public static bool TrySerialize(string key, object obj)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }
            try
            {
                string fileName = key + ".txt";
                using (IsolatedStorageFile storageFile = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var isoStream = new IsolatedStorageFileStream(fileName, FileMode.Create, storageFile))
                    {
                        JsonNetSerializer serializer = new JsonNetSerializer();
                        serializer.SerializeToStream(obj, isoStream);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to deserialize an object specified by the type name.
        /// </summary>
        /// <param name="instanceType">Type of the instance to deserialize.</param>
        /// <param name="obj">The object result.</param>
        /// <returns><code>true</code> if desirialization succeeded; 
        /// <code>false</code> otherwise.</returns>
        public static bool TryDeserialize<T>(string instanceType, out T obj)
        {
            try
            {
                obj = Deserialize<T>(instanceType);
            }
            catch (Exception)
            {
                obj = default(T);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Deserializes the specified instance type.
        /// </summary>
        /// <param name="instanceType">Type of the instance to desierialize.</param>
        /// <returns>The desierialized instance.</returns>
        private static T Deserialize<T>(string instanceType)
        {
            string fileName = instanceType + ".txt";

            using (IsolatedStorageFile storageFile
                = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var isoStream = new IsolatedStorageFileStream(fileName, FileMode.Open, storageFile))
                {
                    JsonNetSerializer serializer = new JsonNetSerializer();
                    return serializer.DeserializeFromStream<T>(isoStream);
                }
            }
        }
    }
}