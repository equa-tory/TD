using System;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Printing;
using TicketApp.Shared;

namespace Tokenizer
{
    // [ComVisible(true)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class ScriptManager
    {
        public void SaveConfig(string key, string value)
        {
            Config.Set(key, value);
        }

        public string GetConfig(string key)
        {
            return Config.Get(key);
        }

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
            catch (Exception ex)
            {
                // return as JSON error so JS can show it
                return "[\"ERROR: " + ex.Message.Replace("\"", "'") + "\"]";
            }
        }
        // public void DebugPrint()
        // {
            // GetConfig.Set()
        // }
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
        }
    }
}