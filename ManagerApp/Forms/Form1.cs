using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Tokenizer.Shared;

namespace Tokenizer
{
    [ComVisible(true)]
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