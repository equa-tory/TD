using System;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Printing;
using Tokenizer.Shared;

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

        public string Test()
        {
            Console.WriteLine("test");
            return "test ok";
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
                string printer = Config.Get("printer");
                bool   debug   = Config.Get("printDebug") == "true";
                ApiManager.ButtonPress(typeId, typeId, printer, debug);
                return "ok";
            }
            catch (Exception ex) { return "error:" + ex.Message; }
        }

        public string ButtonPress(string typeId, string title)
        {
            try
            {
                string printer = Config.Get("printer");
                bool   debug   = Config.Get("printDebug") == "true";
                ApiManager.ButtonPress(typeId, title, printer, debug);
                return "ok";
            }
            catch (Exception ex) { return "error:" + ex.Message; }
        }

        public void Gong()
        {
            System.Media.SystemSounds.Exclamation.Play();
        }
    }


    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            // this.Text   = "Manager";
            // this.Width  = 800;
            // this.Height = 600;

            WebBrowser browser = new WebBrowser();
            browser.Dock = DockStyle.Fill;
            this.Controls.Add(browser);

            browser.ObjectForScripting = new ScriptManager();
            browser.Url = new Uri("file:///" + Application.StartupPath + @"\Pages\manager.html");
        }
    }
}