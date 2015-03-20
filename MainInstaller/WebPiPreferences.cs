using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Installer
{
    /// <summary>
    /// This class defines the properties of the WPI preferences file.
    /// </summary>
    public class WebPiPreferences : WebPiPReferencesBase
    {
        /// <summary>
        /// Gets or sets the custom feed. TODO: handle case of multiple feeds.
        /// </summary>
        public string SelectedFeeds { get { return GetProperty<string>(); } set { SetProperty(value); } }

        /// <summary>
        /// Gets or sets a value indicating whether to use IIS Express instead of IIS.
        /// </summary>
        public bool UseIisExpress { get { return GetProperty<bool>(); } set { SetProperty(value); } }

        /// <summary>
        /// Gets or sets a value indicating whether remote database support is enabled.
        /// </summary>
        public bool RemoteDbServerEnabled { get { return GetProperty<bool>(); } set { SetProperty(value); } }

        /// <summary>
        /// I'm not sure what this does - seems to be something internal. Setting it to <c>true</c> seems to work anyway. 
        /// </summary>
        private bool HasStarted { get { return GetProperty<bool>(); } set { SetProperty(value); } }

        /// <summary>
        /// Gets or sets a value indicating whether the manual configuration of all web application parameters is enabled.
        /// </summary>
        public bool ShowAllAppParametersEnabled { get { return GetProperty<bool>(); } set { SetProperty(value); } }

        /// <summary>
        /// Gets or sets the remote database server name.
        /// </summary>
        public string RemoteDbServer { get { return GetProperty<string>(); } set { SetProperty(value); } }

        /// <summary>
        /// Gets or sets the preferred language for software installations.
        /// </summary>
        public string InstallerLanguage { get { return GetProperty<string>(); } set { SetProperty(value); } }

        /// <summary>
        /// I'm not sure what this does - seems to be something internal. Just don't mess with it. 
        /// </summary>
        private string PrimaryFeedList { get { return GetProperty<string>(); } set { SetProperty(value); } }

        /// <summary>
        /// Gets the names of the WPI properties.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        /// <remarks>
        /// The order of the properties is important. The properties are written in this order to file. Any other order will not be recognized by WPI.
        /// </remarks>
        protected override string[] Properties
        {
            get
            {
                return new[]
                {
                    "SelectedFeeds",
                    "UseIisExpress",
                    "HasStarted",
                    "RemoteDbServerEnabled",
                    "ShowAllAppParametersEnabled",
                    "RemoteDbServer",
                    "InstallerLanguage",
                    "PrimaryFeedList"
                };
            }
        }

        /// <summary>
        /// Performs any necessary operations with the properties before saving..
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void FilterProperties()
        {
            //
            // If no custom feed is specified, we'll need to remove the property from the WPI preferences file
            // or else the user will get an ugly error message (but WPI will still work). 
            //
            if (string.IsNullOrWhiteSpace(SelectedFeeds) && Data.ContainsKey("SelectedFeeds"))
            {
                Data.Remove("SelectedFeeds");
            }
        }

        /// <summary>
        /// Applies the default properties to the WPI preferences file in case it hasn't existed, yet.
        /// </summary>
        protected override void ApplyDefault()
        {
            SelectedFeeds = null;
            UseIisExpress = false;
            HasStarted = true;
            RemoteDbServerEnabled = false;
            ShowAllAppParametersEnabled = false;
            RemoteDbServer = null;
            InstallerLanguage = "en";
            PrimaryFeedList = null;
        }
    }

    /// <summary>
    /// This class encapsulates reading and writing of the WPI preferences file.
    /// </summary>
    public abstract class WebPiPReferencesBase
    {
        /// <summary>
        /// The default preamble for the WPI preferences file in case the file hasn't existed yet. 
        /// </summary>
        /// <remarks>Normally, we'd just copy the preamble from the loaded file.</remarks>
        private readonly static byte[] DefaultPreamble = { 0x01, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0xD7, 0x2A, 0xC8, 0x07, 0x31, 0x67, 0x1D, 0x44, 0x8A, 0xD0, 0x69, 0x8C, 0xCE, 0xC5, 0x77, 0x2F, 0xFF, 0x01, 0x14, 0x2B, 0x00, 0x10 };

        /// <summary>
        /// Gets the property data of the WPI preferences file.
        /// </summary>
        protected Dictionary<string, object> Data { get; private set; }

        /// <summary>
        /// Gets the names of the WPI properties.
        /// </summary>
        /// <remarks>The order of the properties is important. The properties are written in this order to file. Any other order will not be recognized by WPI.</remarks>
        protected abstract string[] Properties { get; }

        /// <summary>
        /// The preamble of the loaded WPI preferences file.
        /// </summary>
        private byte[] _preamble;

        /// <summary>
        /// The path of the WPI preferences file.
        /// </summary>
        private string _filePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebPiPReferencesBase"/> class.
        /// </summary>
        protected WebPiPReferencesBase()
        {
            Load();
        }

        /// <summary>
        /// Loads the WPI preferences file from the current user's roaming app data.
        /// </summary>
        public void Load()
        {
            Data = new Dictionary<string, object>();
            var roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _filePath = Path.Combine(roamingAppData, "Microsoft", "Web Platform Installer", "webpi.preferences");

            _preamble = DefaultPreamble;

            if (!File.Exists(_filePath))
            {
                ApplyDefault();
                return;
            }

            using (var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read))
            using (var r = new BinaryReader(stream))
            {
                /*_preamble = */

                do
                {
                    Seek(r, 0x05);
                    if (stream.Position >= stream.Length)
                    {
                        break;
                    }

                    var key = ReadString(r);
                    if (stream.Position >= stream.Length)
                    {
                        break;
                    }

                    var value = ReadValue(r);

                    Data.Add(key, value);
                } while (stream.Position < stream.Length);
            }
        }

        /// <summary>
        /// Loads the WPI preferences file to the current user's roaming app data.
        /// </summary>
        public void Save()
        {
            FilterProperties();

            using (var stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write))
            using (var w = new BinaryWriter(stream))
            {
                w.Write(_preamble);

                foreach (var key in Properties)
                {
                    object value;
                    Data.TryGetValue(key, out value);

                    WriteString(w, key);
                    WriteValue(w, value);
                }
            }
        }

        /// <summary>
        /// Performs any necessary operations with the properties before saving..
        /// </summary>
        protected abstract void FilterProperties();

        /// <summary>
        /// Applies the default properties to the WPI preferences file in case it hasn't existed, yet.
        /// </summary>
        protected abstract void ApplyDefault();

        /// <summary>
        /// Move forward on stream until specified byte has been found.
        /// </summary>
        /// <remarks>At the end of this operation, the stream position is AFTER the occurrence of the byte or at the end of the stream if the byte has not occurred.</remarks>
        /// <returns>The bytes that were skipped during this operation.</returns>
        private static byte[] Seek(BinaryReader r, byte value)
        {
            var stream = r.BaseStream;
            var result = new List<byte>();

            while (stream.Position < stream.Length)
            {
                var b = r.ReadByte();
                if (b == value) break;
                result.Add(b);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Reads the next value from the stream.
        /// </summary>
        /// <remarks>The operation reads the next single byte. This byte indicates the data type (somewhat). 
        /// Depending on it's value the stream reader might be advanced to read the actual value.</remarks>
        private static object ReadValue(BinaryReader r)
        {
            object value;
            var x = r.ReadByte();

            switch (x)
            {
                case 0x65:
                    value = null;
                    break;
                case 0x67:
                    value = true;
                    break;
                case 0x68:
                    value = false;
                    break;
                case 0x05:
                    value = ReadString(r);
                    break;
                default:
                    throw new Exception("Invalid file");
            }
            return value;
        }

        /// <summary>
        /// Writes the specified value along with any data type indicator.
        /// </summary>
        public static void WriteValue(BinaryWriter w, object value)
        {
            if (value == null)
            {
                w.Write((byte)0x065);
            }
            else if (Equals(value, true))
            {
                w.Write((byte)0x067);
            }
            else if (Equals(value, false))
            {
                w.Write((byte)0x068);
            }
            else if (value is string)
            {
                WriteString(w, (string)value);
            }
        }

        /// <summary>
        /// Reads a string from the current stream position.
        /// </summary>
        /// <remarks>The operation reads the next single byte. This byte indicates the string length.
        /// The method returns the next n bytes with n being the indicated string length (ASCII-encoded).
        /// TODO: figure out actual encoding. ASCII works for me, but might not work everywhere.</remarks>
        private static string ReadString(BinaryReader r)
        {
            var length = r.Read();
            if (length == -1) return null;

            var buffer = new byte[length];
            r.Read(buffer, 0, buffer.Length);
            return Encoding.ASCII.GetString(buffer);
        }

        /// <summary>
        /// Writes the specified text to the WPI preferences file (ASCII-encoded).
        /// </summary>
        /// TODO: figure out actual encoding. ASCII works for me, but might not work everywhere.
        private static void WriteString(BinaryWriter w, string text)
        {
            w.Write((byte)0x05);
            w.Write((byte)text.Length);
            w.Write(Encoding.ASCII.GetBytes(text));
        }

        /// <summary>
        /// Gets the property with the specified name.
        /// </summary>
        protected T GetProperty<T>([CallerMemberName] string key = null)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            object value;
            Data.TryGetValue(key, out value);
            return value is T ? (T)value : default(T);
        }

        /// <summary>
        /// Sets the property with the specified name to the specified value.
        /// </summary>
        protected void SetProperty(object value, [CallerMemberName] string key = null)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (Data.ContainsKey(key))
            {
                Data[key] = value;
            }
            else
            {
                Data.Add(key, value);
            }
        }
    }
}