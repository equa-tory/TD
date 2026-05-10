using System;
using System.Text;
using System.Media;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Printing;
using Tokenizer.Shared;
using System.Collections.Generic;

namespace Tokenizer
{
    [ComVisible(true)]
    public class ScriptManager
    {
        public void SaveConfig(string key, string value) { Config.Set(key, value); }
        public string GetConfig(string key) { return Config.Get(key); }

        public string GetPrinters()
        {
            try
            {
                StringBuilder sb = new StringBuilder("[");
                bool first = true;
                foreach (string name in PrinterSettings.InstalledPrinters)
                {
                    if (!first) sb.Append(",");
                    sb.Append("\"" + name.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"");
                    first = false;
                }
                sb.Append("]");
                return sb.ToString();
            }
            catch (Exception ex) { return "[\"ERROR: " + ex.Message.Replace("\"", "'") + "\"]"; }
        }

        public string FetchTypes()
        {
            try   { return ApiManager.Get("/ticket/types/"); }
            catch (Exception ex) { return "{\"error\":\"" + ex.Message.Replace("\"","'") + "\"}"; }
        }

        public string BatchPress(string typeId)
        {
            try
            {
                ApiManager.CreateTicket(Int32.Parse(typeId));
                return "ok";
            }
            catch (Exception ex) { return "error:" + ex.Message; }
        }

        // System.Media.SystemSounds.Exclamation.Play();
        public void SetVolume(string percent)
        {
            try
            {
                int v;
                if (int.TryParse(percent, out v))
                    AudioManager.SetVolume(v);
                Config.Set("volume", percent);
            }
            catch { }
        }

        public string GetVolume()
        {
            return Config.Get("volume") ?? "50";
        }

        public void Gong(string ticketNumber)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                try
                {
                    int v;
                    if (int.TryParse(Config.Get("volume"), out v))
                        AudioManager.SetVolume(v);

                    string voiceSkin = Config.Get("voiceSkin") ?? "Default";
                    string gongSkin  = Config.Get("gongSkin")  ?? "";
                    string voiceDir  = Application.StartupPath + @"\Audio\Voices\" + voiceSkin + @"\";
                    string gongDir   = Application.StartupPath + @"\Audio\Gongs\";

                    if (ticketNumber == "" || ticketNumber == null || ticketNumber == "null" || ticketNumber == "undefined" || ticketNumber == "undefined") {
                        // play gong first if set
                        if (!string.IsNullOrEmpty(gongSkin))
                            AudioManager.PlaySync(gongDir + gongSkin + ".wav");
                    } else {
                        // then announce number
                        AudioManager.PlaySync(voiceDir + "welcome.wav");
                        foreach (char c in ticketNumber)
                            if (c >= '0' && c <= '9')
                                AudioManager.PlaySync(voiceDir + c + ".wav");
                    }
                }
                catch { }
            });
        }        
        
        private void PlaySync(string path)
        {
            if (!System.IO.File.Exists(path)) return;
            using (SoundPlayer player = new SoundPlayer(path))
            {
                player.PlaySync(); // blocks until file finishes
            }
        }

        public string GetVoiceSkins()
        {
            try
            {
                string dir = Application.StartupPath + @"\Audio\Voices\";
                if (!System.IO.Directory.Exists(dir)) return "[]";
                string[] folders = System.IO.Directory.GetDirectories(dir);
                System.Text.StringBuilder sb = new System.Text.StringBuilder("[");
                bool first = true;
                foreach (string folder in folders)
                {
                    string name = System.IO.Path.GetFileName(folder);
                    if (!first) sb.Append(",");
                    sb.Append("\"" + name.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"");
                    first = false;
                }
                sb.Append("]");
                return sb.ToString();
            }
            catch (Exception ex) { return "[]"; }
        }

        public string GetGongSkins()
        {
            try
            {
                string dir = Application.StartupPath + @"\Audio\Gongs\";
                if (!System.IO.Directory.Exists(dir)) return "[]";
                string[] files = System.IO.Directory.GetFiles(dir, "*.wav");
                System.Text.StringBuilder sb = new System.Text.StringBuilder("[");
                bool first = true;
                foreach (string file in files)
                {
                    string name = System.IO.Path.GetFileNameWithoutExtension(file);
                    if (!first) sb.Append(",");
                    sb.Append("\"" + name.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"");
                    first = false;
                }
                sb.Append("]");
                return sb.ToString();
            }
            catch (Exception ex) { return "[]"; }
        }

        public string PrintTicket(string id, string title)
        {
            try
            {
                string printer = Config.Get("printer");
                bool   debug   = Config.Get("printDebug") == "true";

                Ticket ticket = new Ticket();
                ticket.Type          = "";
                ticket.Number        = 0;
                ticket.DisplayNumber = title;  // use the display name directly
                ticket.Timestamp     = DateTime.Now;

                PrinterManager pm = new PrinterManager(printer);
                pm.PrintTicket(ticket, debug);
                return "ok";
            }
            catch (Exception ex)
            {
                return "error:" + ex.GetType().Name + ": " + ex.Message;
            }
        }

        public string GetTickets()
        {
            try   { return ApiManager.Get("/ticket/list/"); }
            catch (Exception ex) { return "{\"error\":\"" + ex.Message.Replace("\"","'") + "\"}"; }
        }

        public string DeleteTickets(string ids)
        {
            try
            {
                // ids = "236,237" → "ticket_ids=236&ticket_ids=237"
                string[] parts = ids.Split(',');
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                for (int i = 0; i < parts.Length; i++)
                {
                    if (i > 0) sb.Append("&");
                    sb.Append("ticket_ids=");
                    sb.Append(parts[i].Trim());
                }
                return ApiManager.Delete("/delete/", sb.ToString());
            }
            catch (Exception ex) { return "error:" + ex.Message; }
        }
        
        public string UpdateStatus(string id, string status)
        {
            try
            {
                System.Collections.Generic.Dictionary<string, string> p = 
                    new System.Collections.Generic.Dictionary<string, string>();
                p.Add("id", id);
                p.Add("status", status);
                // 2026-05-10T17:09:03.218261
                p.Add("timestamp", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffff"));
                return ApiManager.Post("/ticket/", p);
            }
            catch (Exception ex) { return "error:" + ex.Message; }
        }
    }


    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            WebBrowser browser = new WebBrowser();
            browser.Dock = DockStyle.Fill;
            this.Controls.Add(browser);

            browser.ObjectForScripting = new ScriptManager();
            browser.Url = new Uri("file:///" + Application.StartupPath + @"\Pages\manager.html");
            RestorePosition();

            StartConfig();
        }



        private void StartConfig() {
            // api ip
            string apiUrl = Config.Get("apiUrl");
            if (string.IsNullOrEmpty(apiUrl))
            {
                Config.Set("apiUrl", "http://192.168..:9009");
            }

            // Voice skin
            string voiceSkin = Config.Get("voiceSkin");
            if (string.IsNullOrEmpty(voiceSkin))
            {
                Config.Set("voiceSkin", "Default");
            }

            // Gong skin
            string gongSkin = Config.Get("gongSkin");
            if (string.IsNullOrEmpty(gongSkin))
            {
                Config.Set("gongSkin", "rjd");
            }

            // start sound
            string loginSound = Config.Get("loginSound");
            if (string.IsNullOrEmpty(loginSound))
            {
                Config.Set("loginSound", "True");
                loginSound = "True";
            }
            if (loginSound == "True")
            {
                PlayLoginSound();
            }

            // volume
            string volume = Config.Get("volume");
            if (string.IsNullOrEmpty(volume))
            {
                Config.Set("volume", "100");
            }

            // refresh rate
            string refreshRate = Config.Get("refreshRate");
            if (string.IsNullOrEmpty(refreshRate))
            {
                Config.Set("refreshRate", "3000");
            }
        }

        private void PlayLoginSound()
        {
            if (Config.Get("loginSound") != "True") return;
            System.Threading.ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                try
                {
                    int v;
                    if (int.TryParse(Config.Get("volume"), out v))
                        AudioManager.SetVolume(v);

                    string dir = Application.StartupPath + @"\Audio\misc\";
                    AudioManager.PlaySync(dir + "accept.wav");
                }
                catch { }
            });
        }

        private void RestorePosition()
        {
            try
            {
                int x = int.Parse(Config.Get("windowX"));
                int y = int.Parse(Config.Get("windowY"));
                int w = int.Parse(Config.Get("windowW"));
                int h = int.Parse(Config.Get("windowH"));

                // check the saved position is still on a valid screen
                System.Drawing.Point p = new System.Drawing.Point(x, y);
                bool onScreen = false;
                foreach (Screen s in Screen.AllScreens)
                {
                    if (s.WorkingArea.Contains(p)) { onScreen = true; break; }
                }

                if (onScreen)
                {
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = new System.Drawing.Point(x, y);
                    this.Size     = new System.Drawing.Size(w, h);
                }
            }
            catch { } // no saved position yet — use default
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // save position and size
            Config.Set("windowX", this.Location.X.ToString());
            Config.Set("windowY", this.Location.Y.ToString());
            Config.Set("windowW", this.Size.Width.ToString());
            Config.Set("windowH", this.Size.Height.ToString());
            base.OnFormClosing(e);
        }

    }
}