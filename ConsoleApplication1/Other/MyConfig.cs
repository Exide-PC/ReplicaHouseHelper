using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ExideProd
{
    interface IConfig
    {
        Dictionary<string, string> KeyValueMap { get; }
        int Count { get; }
        bool IsEmpty();

        void Add(string key, string value);
        void Add(string key, int[] array);
        void Add(string key, string[] array);

        string Get(string key);
        string[] GetStrArray(string key);
        int[] GetIntArray(string key);

        bool ContainsKey(string key);
        bool ContainsValue(string value);
        bool Remove(string item);
        
        void Clear();
    }

    class MyConfig : IConfig
    {
        string link;
        Dictionary<String, String> Data;
        FileStream fs;
        Regex array_pattern = new Regex("^[^,]+(,[^,]+)+$");
        Regex line_pattern = new Regex("^[^= ]+=[^=]*$");

        public MyConfig(String link, Dictionary<String, String> DefaultData)
        {
            this.link = link;
            
            if (DefaultData != null)
                Data = DefaultData;
            else Data = new Dictionary<string, string>();

            fs = new FileStream(link, FileMode.OpenOrCreate, FileAccess.Read);
            StreamReader sr = new StreamReader(fs);
            string line;

            while ((line = sr.ReadLine()) != null) {
                if (line_pattern.IsMatch(line))
                {
                    string key = line.Split('=')[0];

                    if (Data.ContainsKey(key))
                        Data.Remove(key);

                    string value = line.Split('=')[1];
                    Data.Add(key, value);
                }
            }
            
            sr.Close();
            Save();
        }

        public MyConfig(String link):this(link, null) { }

        public int Count
        {
            get
            {
                return Data.Count;
            }
        }

        public Dictionary<string, string> KeyValueMap
        {
            get
            {
                return Data;
            }
        }

        public void Add(string key, string value)
        {
            if (Data.ContainsKey(key))
                Data.Remove(key);

            Data.Add(key, value);
            Save();
        }

        public void Add(string key, string[] array)
        {
            string values = "";

            for (int i = 0; i < array.Length; i++)
            {
                values += array[i];
                if (i != array.Length - 1) values += ',';
            }
            Add(key, values);
        }

        public void Add(string key, int[] array)
        {
            string values = "";

            for (int i = 0; i < array.Length; i++)
            {
                values += array[i];
                if (i != array.Length - 1) values += ',';
            }
            Add(key, values);
        }
        
        public void Clear()
        {
            Data.Clear();
            Save();
        }

        public bool ContainsKey(string key)
        {
            return Data.ContainsKey(key);
        }

        public bool ContainsValue(string value)
        {
            return Data.ContainsValue(value);
        }

        public string Get(string key)
        {
            string value;
            bool done = Data.TryGetValue(key, out value);
            if (done)
                return value;
            else return null;
        }

        private bool IsArray(string value)
        {
            return array_pattern.IsMatch(value);
        }

        public int[] GetIntArray(string key)
        {
            int[] intArray = null;

            string value;
            bool isDone = Data.TryGetValue(key, out value);

            if (isDone && !value.Trim().Equals(""))
                if (IsArray(value))
                {
                    string[] strArray = value.Split(',');
                    intArray = new int[strArray.Length];
                    for (int i = 0; i < strArray.Length; i++)
                        intArray[i] = int.Parse(strArray[i]);                    
                }
                else intArray = new int[] { int.Parse(value) };

            return intArray;
        }

        public string[] GetStrArray(string key)
        {
            string[] array = null;
            
            string value;
            bool isDone = Data.TryGetValue(key, out value);

            if (isDone && !value.Trim().Equals(""))
                if (IsArray(value))
                    array = value.Split(',');
                else array = new string[] { value };
                
            return array;
        }

        public bool IsEmpty()
        {
            return Data.Count == 0;
        }

        public bool Remove(string item)
        {
            bool isDone = Data.Remove(item);
            Console.WriteLine(isDone);

            if (isDone)
                Save();

            return isDone;
        }

        private void Save()
        {
            fs = new FileStream(this.link, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);

            foreach (KeyValuePair<string, string> pair in Data)
                sw.WriteLine(pair.Key + "=" + pair.Value);

            sw.Close();
        }
    }
}
