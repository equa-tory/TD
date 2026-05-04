using System;
using System.Collections.Generic;
using System.IO;

namespace Tokenizer.Shared
{
    public static class Config
    {
        private static string ConfigPath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TokenizerDesktop"
                );
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return Path.Combine(dir, "config.ini");
            }
        }

        public static void Set(string key, string value)
        {
            Dictionary<string, string> data = Load();
            data[key] = value;
            Save(data);
        }

        public static string Get(string key)
        {
            Dictionary<string, string> data = Load();
            return data.ContainsKey(key) ? data[key] : "";
        }

        private static Dictionary<string, string> Load()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (!File.Exists(ConfigPath)) return result;
            foreach (string line in File.ReadAllLines(ConfigPath))
            {
                int idx = line.IndexOf('=');
                if (idx > 0)
                    result[line.Substring(0, idx)] = line.Substring(idx + 1);
            }
            return result;
        }

        private static void Save(Dictionary<string, string> data)
        {
            List<string> lines = new List<string>();
            foreach (KeyValuePair<string, string> kv in data)
                lines.Add(kv.Key + "=" + kv.Value);
            File.WriteAllLines(ConfigPath, lines.ToArray());
        }
    }
}