using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Tokenizer.Shared;

namespace Tokenizer
{
    [ComVisible(true)]
    public class ScriptManager
    {
        public void SaveConfig(string key, string value) { Config.Set(key, value); }
        public string GetConfig(string key) { return Config.Get(key); }

        public void SetFullscreen(bool full)
        {
            Form owner = Form.ActiveForm ?? Application.OpenForms[0];
            owner.Invoke((MethodInvoker)delegate {
                if (full) {
                    owner.FormBorderStyle = FormBorderStyle.None;
                    owner.WindowState     = FormWindowState.Maximized;
                } else {
                    owner.FormBorderStyle = FormBorderStyle.Sizable;
                    owner.WindowState     = FormWindowState.Normal;
                }
            });
        }

        public string FetchSomething()
        {
            try
            {
                return ApiManager.Get("/ticket/types/");
            }
            catch (Exception ex)
            {
                return "{\"error\":\"" + ex.Message.Replace("\"", "'") + "\"}";
            }
        }

        // Returns "ok" or an error message so JS can show feedback
        public string ButtonPress(string typeId, string title)
        {
            try
            {
                string printer = Config.Get("printer");
                bool   debug   = Config.Get("printDebug") == "true";
                ApiManager.ButtonPress(typeId, title, printer, debug); // force preview
                return "ok";
            }
            catch (Exception ex)
            {
                // show the REAL error in the browser
                return "error:" + ex.GetType().Name + ": " + ex.Message;
            }
        }
    }

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            
            // full screen
            // this.FormBorderStyle = FormBorderStyle.None;
            // this.WindowState     = FormWindowState.Maximized;

            WebBrowser browser = new WebBrowser();
            browser.Dock = DockStyle.Fill;
            this.Controls.Add(browser);

            browser.ObjectForScripting = new ScriptManager();
            browser.Url = new Uri("file:///" + Application.StartupPath + @"\Pages\buttons.html");
            RestorePosition();
        }

        private void RestorePosition()
        {
            try
            {
                int x = int.Parse(Config.Get("btnWindowX"));
                int y = int.Parse(Config.Get("btnWindowY"));
                int w = int.Parse(Config.Get("btnWindowW"));
                int h = int.Parse(Config.Get("btnWindowH"));

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
            Config.Set("btnWindowX", this.Location.X.ToString());
            Config.Set("btnWindowY", this.Location.Y.ToString());
            Config.Set("btnWindowW", this.Size.Width.ToString());
            Config.Set("btnWindowH", this.Size.Height.ToString());
            base.OnFormClosing(e);
        }
    }
}