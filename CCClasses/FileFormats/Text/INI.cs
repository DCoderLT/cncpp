using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Globalization;
using System.Diagnostics;

namespace CCClasses.FileFormats.Text {
    public class INIEntry {
        public String Key;
        public String Value;

        public INIEntry(String K, String V) {
            Key = K;
            Value = V;
        }
    }

    public class INISection {
        public String Name;
        public Dictionary<String, INIEntry> Entries = new Dictionary<string,INIEntry>();

        public INISection(String Key) {
            this.Name = Key;
        }

        public bool AddKey(String Key, String Value) {
            if (!this.ContainsKey(Key)) {
                var kv = new INIEntry(Key, Value);
                this[Key] = kv;
                return true;
            } else {
                this[Key].Value = Value;
                return false;
            }
        }

        public INIEntry this[String key] {
            get {
                return Entries[key];
            }
            set {
                Entries[key] = value;
            }
        }

        public bool ContainsKey(String key) {
            return Entries.ContainsKey(key);
        }

        public bool Remove(String key) {
            return Entries.Remove(key);
        }

        public bool TryGetValue(String key, out INIEntry value) {
            return Entries.TryGetValue(key, out value);
        }
    }

    public class INI : TextFileFormat {
        private static readonly Regex decimalRegex = new Regex("^(?<sign>-?)(?<digits>[0-9]+)");
        private static readonly Regex hexRegex1 = new Regex("^\\$(?<digits>[0-9]+)");
        private static readonly Regex hexRegex2 = new Regex("^(?<digits>[0-9]+)h$");

        public Dictionary<String, INISection> Sections = new Dictionary<string, INISection>();

        public static INI Rules_INI;
        public static INI Rules_Combined;
        public static INI Art_INI;
        //public static INI Ai_INI;

        public INI(CCFileClass ccFile = null) : base(ccFile) {
        }
        
        public void Clear() {
            Sections.Clear();
        }

        public bool AddSection(String K) {
            if (!Sections.ContainsKey(K)) {
                var sect = new INISection(K);
                Sections.Add(K, sect);
                return true;
            }
            return false;
        }

        protected override bool ReadFile(StreamReader s) {
            String line = null;
            String lastSection = "";
            while ((line = s.ReadLine()) != null) {
                String section = null;
                String key = null;
                String value = null;

                var comment = line.IndexOf(';');
                if (comment != -1) {
                    line = line.Substring(0, comment > 0 ? comment - 1 : 0);
                }
                line = line.TrimEnd();
                if (line.Length > 0) {
                    if (line[0] == '[') { // [section]
                        var ender = line.IndexOf(']');
                        if (ender != -1) {
                            section = line.Substring(1, ender - 1);
                        } else {
                            // malformed section name
                        }
                    } else {
                        var eq = line.IndexOf('=');
                        if (eq != -1) {
                            key = line.Substring(0, eq).Trim();
                            value = line.Substring(eq + 1).Trim();
                        } else {
                            // nothing useful here
                        }
                    }

                    if (section != null) {
                        AddSection(section);
                        lastSection = section;
                    } else if (key != null) {
                        AddSection(lastSection);
                        var KV = Sections[lastSection];
                        KV.AddKey(key, value);
                    }
                }
            }
            return true;
        }

        public void CombineWithFile(StreamReader r) {
            ReadFile(r);
        }

        public void CombineWithFile(INI otherINI) {
            foreach (var s in otherINI.Sections) {
                if(!SectionExists(s.Key)) {
                    AddSection(s.Key);
                }
                var sect = Sections[s.Key];
                foreach (var e in s.Value.Entries) {
                    if (!sect.ContainsKey(e.Value.Key)) {
                        sect.AddKey(e.Value.Key, e.Value.Value);
                    } else {
                        sect[e.Value.Key] = e.Value;
                    }
                }
            }
        }

        public bool SectionExists(String Section) {
            return Sections.ContainsKey(Section);
        }

        public bool KeyExists(String Section, String Key) {
            if (Sections.ContainsKey(Section)) {
                return Sections[Section].ContainsKey(Key);
            }
            return false;
        }

        public string ReadSection(string Section) {
            String allContent = "";

            if (!SectionExists(Section)) {
                throw new ArgumentException();
            }

            foreach (var Entry in Sections[Section].Entries) {
                allContent += Entry.Value.Value;
            }

            return allContent;
        }

        public bool GetString(String Section, String Key, ref String Result) {
            var Default = Result;
            return GetString(Section, Key, out Result, Default);
        }

        public bool GetString(String Section, String Key, out String Result, String Default) {
            Result = Default;
            if (!KeyExists(Section, Key)) {
                return false;
            }
            Result = Sections[Section][Key].Value;
            return true;
        }

        public bool GetInteger(String Section, String Key, ref int Result) {
            var Default = Result;
            return GetInteger(Section, Key, out Result, Default);
        }

        public bool GetInteger(String Section, String Key, out int Result, int Default) {
            String s;
            Result = Default;
            if (GetString(Section, Key, out s, "")) {
                int parsed = 0;
                var m = decimalRegex.Match(s);
                if (m.Success) {
                    if (Int32.TryParse(m.Value, out parsed)) {
                        Result = parsed;
                        return true;
                    }
                } else {
                    String hex = null;
                    m = hexRegex1.Match(s);
                    if (m.Success) {
                        hex = m.Groups["digits"].Value;
                    } else {
                        m = hexRegex2.Match(s);
                        if (m.Success) {
                            hex = m.Groups["digits"].Value;
                        }
                    }
                    if(hex != null) {
                        if (Int32.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsed)) {
                            Result = parsed;
                            return true;
                        }
                    }
                }

            }

            return false;
        }

        public bool GetDouble(String Section, String Key, ref double Result) {
            var Default = Result;
            return GetDouble(Section, Key, out Result, Default);
        }

        public bool GetDouble(String Section, String Key, out double Result, double Default) {
            String s;
            Result = Default;
            if (GetString(Section, Key, out s, "")) {
                if (Double.TryParse(s, out Result)) {
                    return true;
                }
            }
            return false;
        }

        public bool GetBool(String Section, String Key, ref bool Result) {
            var Default = Result;
            return GetBool(Section, Key, out Result, Default);
        }

        public bool GetBool(String Section, String Key, out bool Result, bool Default) {
            String s;
            Result = Default;
            if (GetString(Section, Key, out s, "")) {
                switch (s.ToUpper()[0]) {
                    case '0':
                    case 'F':
                    case 'N':
                        Result = false;
                        return true;

                    case '1':
                    case 'T':
                    case 'Y':
                        Result = true;
                        return true;
                }
            }
            return false;
        }

        public bool Get4Integers(string Section, string Key, out int[] Result, int[] Default) {
            String s;
            Result = Default;
            Debug.Assert(Default.Length == 4, "Get4Integers requires a default value of 4 integers...");

            if (GetString(Section, Key, out s, "")) {
                var tokens = s.Split(',');
                if (tokens.Length == 4) {
                    for (var i = 0; i <= 3; ++i) {
                        int num;
                        String token = tokens[i];
                        if (!Int32.TryParse(token, out num)) {
                            break;
                        }
                        Result[i] = num;
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
