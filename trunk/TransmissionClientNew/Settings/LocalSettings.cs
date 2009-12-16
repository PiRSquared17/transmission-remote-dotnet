﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Jayrock.Json;
using Jayrock.Json.Conversion;
using Microsoft.Win32;
using System.Windows.Forms;

namespace TransmissionRemoteDotnet.Settings
{
    public class LocalSettings
    {
        /*
         * Modify this in MONO to right settings storage
         */
        public ILocalSettingsStore DefaultLocalStore = new RegistryLocalSettingsStore();
        public bool CompletedBaloon = true;
        public bool MinOnClose = false;
        public bool MinToTray = false;
        public bool AutoCheckupdate = false;
        public bool DeleteTorrentWhenAdding = false;
        public int DefaultDoubleClickAction = 0;
        public bool StartedBalloon = true;
        private bool dontsavepasswords = false;
        public string Locale = "en-GB";
        public string PlinkPath = null;
        public bool UploadPrompt = false;
        public string AutoConnect = "";
        private string currentprofile = "";

        public string CurrentProfile
        {
            get { return currentprofile; }
            set
            {
                if (Servers.ContainsKey(value))
                    currentprofile = value;
            }
        }

        public bool DontSavePasswords
        {
            get { return dontsavepasswords; }
            set
            {
                dontsavepasswords = value;
                foreach (KeyValuePair<string, TransmissionServer> s in Servers)
                {
                    s.Value.SetDontSavePasswords(dontsavepasswords);
                }
            }
        }

        public TransmissionServer Current
        {
            get
            {
                TransmissionServer ts = Servers[CurrentProfile];
                if (ts == null)
                    ts = new TransmissionServer();
                return ts;
            }
        }

        public Dictionary<string, TransmissionServer> Servers = new Dictionary<string, TransmissionServer>();
        public Dictionary<string, object> Misc = new Dictionary<string, object>();

        public JsonObject SaveToJson()
        {
            JsonObject jo = new JsonObject();
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_COMPLETEDBALLOON, CompletedBaloon);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_MINONCLOSE, MinOnClose);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_MINTOTRAY, MinToTray);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_STARTEDBALLOON, StartedBalloon);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_DONTSAVEPASSWORDS, DontSavePasswords);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_AUTOCHECKUPDATE, AutoCheckupdate);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_DELETETORRENT, DeleteTorrentWhenAdding);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_DEFAULTACTION, DefaultDoubleClickAction);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_LOCALE, Locale);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_PLINKPATH, PlinkPath);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_UPLOADPROMPT, UploadPrompt);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_AUTOCONNECT, AutoConnect);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_CURRENTPROFILE, CurrentProfile);
            JsonObject ja = new JsonObject();
            foreach (KeyValuePair<string, TransmissionServer> s in Servers)
            {
                ja.Put(s.Key, s.Value.SaveToJson());
            }
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_PROFILES, ja);
            ja = new JsonObject();
            foreach (KeyValuePair<string, object> s in Misc)
            {
                ja.Put(s.Key, s.Value);
            }
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_MISC, ja);
            return jo;
        }
        public void LoadFromJson(JsonObject o)
        {
            Toolbox.JsonGet(ref CompletedBaloon, o[SettingsKey.REGKEY_COMPLETEDBALLOON]);
            Toolbox.JsonGet(ref MinOnClose, o[SettingsKey.REGKEY_MINONCLOSE]);
            Toolbox.JsonGet(ref MinToTray, o[SettingsKey.REGKEY_MINTOTRAY]);
            Toolbox.JsonGet(ref StartedBalloon, o[SettingsKey.REGKEY_STARTEDBALLOON]);
            Toolbox.JsonGet(ref AutoCheckupdate, o[SettingsKey.REGKEY_AUTOCHECKUPDATE]);
            Toolbox.JsonGet(ref DeleteTorrentWhenAdding, o[SettingsKey.REGKEY_DELETETORRENT]);
            Toolbox.JsonGet(ref DefaultDoubleClickAction, o[SettingsKey.REGKEY_DEFAULTACTION]);
            Toolbox.JsonGet(ref Locale, o[SettingsKey.REGKEY_LOCALE]);
            Toolbox.JsonGet(ref PlinkPath, o[SettingsKey.REGKEY_PLINKPATH]);
            Toolbox.JsonGet(ref UploadPrompt, o[SettingsKey.REGKEY_UPLOADPROMPT]);
            Toolbox.JsonGet(ref AutoConnect, o[SettingsKey.REGKEY_AUTOCONNECT]);
            JsonObject ja = (JsonObject)o[SettingsKey.REGKEY_PROFILES];
            if (ja != null)
            {
                foreach (string n in ja.Names)
                {
                    Servers.Add(n, new TransmissionServer(ja[n] as JsonObject));
                }
            }
            Toolbox.JsonGet(ref currentprofile, o[SettingsKey.REGKEY_CURRENTPROFILE]);
            bool dsp = false;
            Toolbox.JsonGet(ref dsp, o[SettingsKey.REGKEY_DONTSAVEPASSWORDS]);
            DontSavePasswords = dsp;
            if (!Servers.ContainsKey(currentprofile))
                currentprofile = "";
            ja = (JsonObject)o[SettingsKey.REGKEY_MISC];
            if (ja != null)
            {
                foreach (string n in ja.Names)
                {
                    Misc.Add(n, ja[n]);
                }
            }
        }
        public LocalSettings()
        {
        }
        public LocalSettings(JsonObject o)
        {
            LoadFromJson(o);
        }
        /*
         * TODO: this sould remove!!!
         */
        public object GetObject(string key)
        {
            return Misc[key];
        }
        public bool ContainsKey(string key)
        {
            return Misc.ContainsKey(key);
        }
        public void SetObject(string key, object value)
        {
            Misc[key] = value;
        }

        public void Commit()
        {
            //            Program.Form.refreshTimer.Interval = RefreshRate * 1000;
            //            Program.Form.filesTimer.Interval = RefreshRate * 1000 * FILES_REFRESH_MULTIPLICANT;
            if (!DefaultLocalStore.Save(this.SaveToJson()))
                MessageBox.Show("Error writing settings to registry", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static LocalSettings TryLoad()
        {
            LocalSettings newsettings = null;
            ILocalSettingsStore[] SettingsSource = new ILocalSettingsStore[] { 
                new RegistryLocalSettingsStore(), 
                new RegistryJsonLocalSettingsStore(), 
                new FileLocalSettings() 
            };
            foreach (ILocalSettingsStore ls in SettingsSource)
            {
                try
                {
                    JsonObject jo = ls.Load();
                    newsettings = new LocalSettings(jo);
                    break;
                }
                catch { };
            }
            if (newsettings == null)
            { // not load from any source :(, try old mode
                LocalSettingsSingleton oldsettings = LocalSettingsSingleton.OneInstance();
                newsettings = new LocalSettings();
                newsettings.Locale = oldsettings.Locale;
                newsettings.CompletedBaloon = oldsettings.CompletedBaloon;
                newsettings.MinOnClose = oldsettings.MinOnClose;
                newsettings.MinToTray = oldsettings.MinToTray;
                newsettings.PlinkPath = oldsettings.PlinkPath;
                newsettings.StartedBalloon = oldsettings.StartedBalloon;
                newsettings.UploadPrompt = oldsettings.UploadPrompt;
                newsettings.AutoCheckupdate = oldsettings.AutoCheckupdate;
                string origcurrentprofile = oldsettings.CurrentProfile;
                foreach (string p in oldsettings.Profiles)
                {
                    oldsettings.CurrentProfile = p;
                    TransmissionServer ts = new TransmissionServer();
                    ts.CustomPath = oldsettings.CustomPath;
                    ts.DownLimit = oldsettings.DownLimit;
                    ts.UpLimit = oldsettings.UpLimit;
                    ts.Host = oldsettings.Host;
                    ts.Password = oldsettings.Pass;
                    ts.PlinkCmd = oldsettings.PlinkCmd;
                    ts.PlinkEnable = oldsettings.PlinkEnable;
                    ts.Port = oldsettings.Port;
                    ts.RefreshRate = oldsettings.RefreshRate;
                    ts.StartPaused = oldsettings.StartPaused;
                    ts.Username = oldsettings.User;
                    ts.UseSSL = oldsettings.UseSSL;
                    JsonObject mappings = oldsettings.SambaShareMappings;
                    foreach (string key in mappings.Names)
                    {
                        ts.SambaShareMappings.Add(key, (string)mappings[key]);
                    }
                    ts.destpathhistory.AddRange(oldsettings.DestPathHistory);
                    ProxyServer ps = new ProxyServer();
                    ps.Host = oldsettings.ProxyHost;
                    ps.Password = oldsettings.ProxyPass;
                    ps.Port = oldsettings.ProxyPort;
                    ps.Username = oldsettings.ProxyUser;
                    ps.ProxyMode = (ProxyMode)oldsettings.ProxyMode;
                    ts.Proxy = ps;
                    newsettings.Servers.Add(p, ts);
                    if (origcurrentprofile.Equals(p))
                        newsettings.CurrentProfile = p;
                }
                if (newsettings.CurrentProfile.Equals("") && newsettings.Servers.Count > 0)
                    newsettings.CurrentProfile = newsettings.Servers.First().Key;
                foreach (string s in oldsettings.ListObject(true))
                {
                    if (s.StartsWith("mainwindow-") || s.StartsWith("listview-"))
                        newsettings.Misc[s] = oldsettings.GetObject(s, true);
                }
                // move old stuff to backup!
                //oldsettings.BackupSettings();
                if (!newsettings.DefaultLocalStore.Save(newsettings.SaveToJson()))
                    MessageBox.Show("Failed to save settings");
            }
            return newsettings;
        }
    }
    public class Server
    {
        public string Host = "";
        public int Port = 0;
        public string Username = "";
        private string password = null;
        private bool dontsavepasswords = false;

        public virtual void LoadFromJson(JsonObject o)
        {
            Toolbox.JsonGet(ref Host, o[SettingsKey.REGKEY_HOST]);
            Toolbox.JsonGet(ref Port, o[SettingsKey.REGKEY_PORT]);
            Toolbox.JsonGet(ref Username, o[SettingsKey.REGKEY_USER]);
            Toolbox.JsonGet(ref password, o[SettingsKey.REGKEY_PASS]);
        }
        public virtual JsonObject SaveToJson()
        {
            JsonObject jo = new JsonObject();
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_HOST, Host);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_PORT, Port);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_USER, Username);
            if (!dontsavepasswords)
                Toolbox.JsonPut(jo, SettingsKey.REGKEY_PASS, password);
            return jo;
        }
        public Server()
        {
        }
        public Server(JsonObject o)
        {
            LoadFromJson(o);
        }
        public virtual void SetDontSavePasswords(bool Value)
        {
            dontsavepasswords = Value;
        }

        public string ValidPassword
        {
            get
            {
                if (password == null)
                {
                    password = InputBox.Show("Jelszo", "izeize", true);
                    if (password == null)
                        throw new PasswordEmptyException("aaa");
                }
                return this.Password;
            }
        }

        public string Password
        {
            get { return password != null ? password : ""; }
            set { password = value; }
        }
    }
    public class TransmissionServer : Server
    {
        public bool UseSSL = false;
        public string CustomPath = null;
        public bool StartPaused = false;
        public int RefreshRate = 3;
        public int RetryLimit = 3;
        public Dictionary<string, string> SambaShareMappings = new Dictionary<string, string>();
        public string DownLimit = "10,50,100,200,300,400,500,700,1000,1500,2000,3000,5000";
        public string UpLimit = "10,50,100,200,300,400,500,700,1000,1500,2000,3000,5000";
        public bool PlinkEnable = false;
        public string PlinkCmd = "ls -lh \"$DATA\"; read";
        public List<string> destpathhistory = new List<string>();
        public ProxyServer Proxy = new ProxyServer();

        public override JsonObject SaveToJson()
        {
            JsonObject jo = base.SaveToJson();
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_USESSL, UseSSL);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_REFRESHRATE, RefreshRate);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_CUSTOMPATH, CustomPath);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_RETRYLIMIT, RetryLimit);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_DOWNLIMIT, DownLimit);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_UPLIMIT, UpLimit);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_STARTPAUSED, StartPaused);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_PLINKENABLE, PlinkEnable);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_PLINKCMD, PlinkCmd);
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_PROXY, Proxy.SaveToJson());
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_DESTINATION_PATH_HISTORY, new JsonArray(destpathhistory));
            JsonObject ja = new JsonObject();
            foreach (KeyValuePair<string, string> s in SambaShareMappings)
            {
                ja.Put(s.Key, s.Value);
            }
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_SAMBASHAREMAPPINGS, ja);
            return jo;
        }

        public override void LoadFromJson(JsonObject o)
        {
            base.LoadFromJson(o);
            Toolbox.JsonGet(ref UseSSL, o[SettingsKey.REGKEY_USESSL]);
            Toolbox.JsonGet(ref RefreshRate, o[SettingsKey.REGKEY_REFRESHRATE]);
            Toolbox.JsonGet(ref CustomPath, o[SettingsKey.REGKEY_CUSTOMPATH]);
            Toolbox.JsonGet(ref RetryLimit, o[SettingsKey.REGKEY_RETRYLIMIT]);
            Toolbox.JsonGet(ref DownLimit, o[SettingsKey.REGKEY_DOWNLIMIT]);
            Toolbox.JsonGet(ref UpLimit, o[SettingsKey.REGKEY_UPLIMIT]);
            Toolbox.JsonGet(ref StartPaused, o[SettingsKey.REGKEY_STARTPAUSED]);
            Toolbox.JsonGet(ref PlinkEnable, o[SettingsKey.REGKEY_PLINKENABLE]);
            Toolbox.JsonGet(ref PlinkCmd, o[SettingsKey.REGKEY_PLINKCMD]);

            JsonArray ja = (JsonArray)JsonConvert.Import((string)o[SettingsKey.REGKEY_DESTINATION_PATH_HISTORY]);
            destpathhistory.AddRange((string[])ja.ToArray(typeof(string)));
            JsonObject jo = (JsonObject)o[SettingsKey.REGKEY_SAMBASHAREMAPPINGS];
            if (jo != null)
            {
                foreach (string n in jo.Names)
                {
                    SambaShareMappings.Add(n, jo[n] as string);
                }
            }
            jo = (JsonObject)o[SettingsKey.REGKEY_PROXY];
            if (jo != null)
            {
                Proxy = new ProxyServer(jo);
            }
        }
        public TransmissionServer()
        {
            Port = 9091;
        }
        public TransmissionServer(JsonObject o):this()
        {
            LoadFromJson(o);
        }
        public override void SetDontSavePasswords(bool Value)
        {
            base.SetDontSavePasswords(Value);
            Proxy.SetDontSavePasswords(Value);
        }

        public void RemoveSambaMapping(string unixPrefix)
        {
            if (SambaShareMappings.ContainsKey(unixPrefix))
                SambaShareMappings.Remove(unixPrefix);
        }

        public bool AddSambaMapping(string unixPrefix, string sambaPrefix)
        {
            if (SambaShareMappings.ContainsKey(unixPrefix)) return false;
            SambaShareMappings[unixPrefix] = sambaPrefix.EndsWith(@"\") ? sambaPrefix.Substring(0, sambaPrefix.Length - 1) : sambaPrefix;
            return true;
        }

        public void AddDestinationPath(string path)
        {
            if (!destpathhistory.Contains(path))
            {
                while (destpathhistory.Count > 5) destpathhistory.RemoveAt(0);
                destpathhistory.Add(path);
            }
        }
        public string[] DestPathHistory
        {
            get
            {
                return destpathhistory.ToArray();
            }
        }

        public bool AuthEnabled
        {
            get
            {
                return !Username.Equals("");
            }
        }

        public string RpcUrl
        {
            get
            {
                return String.Format("{0}://{1}:{2}{3}rpc", new object[] { UseSSL ? "https" : "http", Host, Port, CustomPath == null ? "/transmission/" : CustomPath });
            }
        }


    }
    public class ProxyServer : Server
    {
        public ProxyMode ProxyMode = ProxyMode.Auto;
        public override JsonObject SaveToJson()
        {
            JsonObject jo = base.SaveToJson();
            Toolbox.JsonPut(jo, SettingsKey.REGKEY_PROXYENABLED, (int)ProxyMode);
            return jo;
        }

        public override void LoadFromJson(JsonObject o)
        {
            base.LoadFromJson(o);
            if (o[SettingsKey.REGKEY_PROXYENABLED] != null)
            {
                ProxyMode = (ProxyMode)Toolbox.ToInt(o[SettingsKey.REGKEY_PROXYENABLED]);
            }
        }
        public ProxyServer()
        {
            Port = 8080;
        }
        public ProxyServer(JsonObject o):this()
        {
            LoadFromJson(o);
        }

        public bool AuthEnabled
        {
            get
            {
                return !Username.Equals("");
            }
        }
    }
    internal class SettingsKey
    {
        public const string
            /* Registry keys */
            REGKEY_PROFILES = "profiles",
            REGKEY_MISC = "misc",
            REGKEY_HOST = "host",
            REGKEY_PORT = "port",
            REGKEY_USESSL = "usessl",
            REGKEY_AUTOCONNECT = "autoConnect",
            REGKEY_AUTOCHECKUPDATE = "autoCheckupdate",
            REGKEY_DELETETORRENT = "deleteTorrentWhenAdding",
            REGKEY_DEFAULTACTION = "defaultDoubleClickAction",
            REGKEY_USER = "user",
            REGKEY_PASS = "pass",
            REGKEY_AUTHENABLED = "authEnabled",
            REGKEY_PROXY = "proxy",
            REGKEY_PROXYENABLED = "proxyEnabled",
            REGKEY_PROXYHOST = "proxyHost",
            REGKEY_PROXYPORT = "proxyPort",
            REGKEY_PROXYUSER = "proxyUser",
            REGKEY_PROXYPASS = "proxyPass",
            REGKEY_PROXYAUTH = "proxyAuth",
            REGKEY_STARTPAUSED = "startPaused",
            REGKEY_RETRYLIMIT = "retryLimit",
            REGKEY_MINTOTRAY = "minToTray",
            REGKEY_REFRESHRATE = "refreshRate",
            REGKEY_CURRENTPROFILE = "currentProfile",
            REGKEY_STARTEDBALLOON = "startedBalloon",
            REGKEY_DONTSAVEPASSWORDS = "dontSavePasswords",
            REGKEY_COMPLETEDBALLOON = "completedBalloon",
            REGKEY_MINONCLOSE = "minOnClose",
            REGKEY_PLINKPATH = "plinkPath",
            REGKEY_PLINKCMD = "plinkCmd",
            REGKEY_PLINKENABLE = "plinkEnable",
            REGKEY_LOCALE = "locale",
            REGKEY_CUSTOMPATH = "customPath",
            REGKEY_DOWNLIMIT = "downlimit",
            REGKEY_UPLIMIT = "uplimit",
            REGKEY_SAMBASHAREMAPPINGS = "sambaShareMappings",
            REGKEY_UPLOADPROMPT = "uploadPrompt",
            REGKEY_DESTINATION_PATH_HISTORY = "destPathHistory";
    }
    public class PasswordEmptyException : Exception
    {
        public PasswordEmptyException(string message)
            : base(message)
        {
        }
    }
}