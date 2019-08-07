using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using YAXLib;
using System.Xml.Serialization;

namespace XV2SSEdit
{
    public enum Language
    {
        English,
        Spanish_ES,
        Spanish_CA,
        French,
        German,
        Italian, 
        Portuguese,
        Polish, 
        Russian, 
        Chinese_TW,
        Chinese_ZH,
        Korean
    }

    public class ToolSettings
    {
        [YAXDontSerialize]
        public static string SettingsPath = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"/" + "xv2sseditor_settings.xml";
        [YAXDontSerialize]
        public static string[] LanguageSuffix = new string[12] { "en", "es", "ca", "fr", "de", "it", "pt", "pl", "ru", "tw", "zh", "kr" };

        //Settings to save to file
        public string GameDir { get; set; }
        public Language GameLanguage { get; set; }

        [YAXDontSerialize]
        public bool IsValidGameDir
        {
            get
            {
                if (String.IsNullOrWhiteSpace(GameDir)) return false;
                if (File.Exists(String.Format("{0}/bin/DBXV2.exe", GameDir))) return true;
                return false;
            }
        }
        [YAXDontSerialize]
        public string LanguagePrefix
        {
            get
            {
                return ToolSettings.LanguageSuffix[(int)GameLanguage];
            }
        }

        public static ToolSettings Load()
        {
            XmlSerializer xml = new XmlSerializer(typeof(ToolSettings));

            if (File.Exists(SettingsPath))
            {
                YAXSerializer serializer = new YAXSerializer(typeof(ToolSettings), YAXSerializationOptions.DontSerializeNullObjects);
                ToolSettings settings = (ToolSettings)serializer.DeserializeFromFile(SettingsPath);

                if (settings == null)
                {
                    settings = new ToolSettings() { GameLanguage = Language.English };
                    settings.Save();
                }

                return settings;
            }
            else
            {
                var settings = new ToolSettings() { GameLanguage = Language.English };
                settings.Save();
                return settings;
            }
            
        }

        public void Save()
        {
            YAXSerializer serializer = new YAXSerializer(typeof(ToolSettings));
            serializer.SerializeToFile(this, SettingsPath);
        }

        
    }
}
