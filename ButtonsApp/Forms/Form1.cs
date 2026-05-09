using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Tokenizer.Shared;

namespace Tokenizer
{
    [ComVisible(true)]
    public class ScriptManager
    {
        public string GetConfig(string key)
        {
            return Config.Get(key);
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

            WebBrowser browser = new WebBrowser();
            browser.Dock = DockStyle.Fill;
            this.Controls.Add(browser);

            browser.ObjectForScripting = new ScriptManager();
            browser.Url = new Uri("file:///" + Application.StartupPath + @"\Pages\buttons.html");
        }
    }
}