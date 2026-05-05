using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Tokenizer.Shared;

namespace Tokenizer
{
    [ComVisible(true)]
    public class ScriptManager
    {
        public bool printDebug = true;
        public string GetConfig(string key)
        {
            return Config.Get(key);
        }


        public string FetchSomething() // TODO: change
        {
            return ApiManager.Get("/ticket/types/");
        }

        public void BookTicket(string typeId, string title, string printerName)
        {
            int id;
            if (!int.TryParse(typeId, out id))
                return;

            string response = ApiManager.Get("/ticket/?ticket_type_id=" + id);

            string ticketName = title;
            int ticketNumber = 0;

            try
            {
                Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(response);
                Newtonsoft.Json.Linq.JToken ticket = obj["ticket"];

                if (ticket != null)
                {
                    if (ticket["name"] != null)
                        ticketName = ticket["name"].ToString();

                    if (ticket["number"] != null)
                        ticketNumber = Convert.ToInt32(ticket["number"].ToString());
                }
            }
            catch
            {
                // если JSON кривой — просто используем дефолт
            }

            // print
            if (string.IsNullOrEmpty(printerName) || printerName == "None")
                return;

            Ticket ticketObj = new Ticket();
            ticketObj.type = "";
            ticketObj.number = ticketNumber;
            ticketObj.displayNumber = ticketName;
            ticketObj.timestamp = DateTime.Now;

            PrinterManager pm = new PrinterManager(printerName);
            pm.PrintTicket(ticketObj, printDebug);
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