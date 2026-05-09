using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Tokenizer.Shared
{
    public static class ApiManager
    {
        // Base URL pulled from Config so Manager app can change it
        private static string BaseUrl
        {
            get { return Config.Get("apiUrl"); }
        }

        static ApiManager()
        {
            ServicePointManager.ServerCertificateValidationCallback =
                new RemoteCertificateValidationCallback(AcceptCertificate);
        }

        private static bool AcceptCertificate(object sender, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        // ----------------------------------------------------------------
        // Core request methods
        // ----------------------------------------------------------------

        public static string Get(string path, Dictionary<string, string> queryParams)
        {
            string url = BaseUrl + path + BuildQueryString(queryParams);
            return SendRequest(url, "GET", null);
        }

        public static string Get(string path)
        {
            return Get(path, null);
        }

        public static string Post(string path, Dictionary<string, string> queryParams, string jsonBody)
        {
            string url = BaseUrl + path + BuildQueryString(queryParams);
            return SendRequest(url, "POST", jsonBody);
        }

        public static string Post(string path, Dictionary<string, string> queryParams)
        {
            return Post(path, queryParams, null);
        }

        public static string Post(string path)
        {
            return Post(path, null, null);
        }

        public static string Delete(string path, string rawQuery)
        {
            string url = BaseUrl + path + (string.IsNullOrEmpty(rawQuery) ? "" : "?" + rawQuery);
            return SendRequest(url, "DELETE", null);
        }

        // ----------------------------------------------------------------
        // Specific endpoints
        // ----------------------------------------------------------------

        // BOOK \/
        public static string CreateTicket(int ticketTypeId)
        {
            Dictionary<string, string> p = new Dictionary<string, string>();
            p.Add("ticket_type_id", ticketTypeId.ToString());
            return Post("/ticket/", p);
        }
    
        // PRINT \/
        public static void ButtonPress(string typeId, string title, string printerName, bool printDebug)
        {
            int id;
            if (!int.TryParse(typeId, out id))
                return;

            Dictionary<string, string> p = new Dictionary<string, string>();
            p.Add("ticket_type_id", id.ToString());

            string response = Post("/ticket/", p);  // POST with empty body, params in query string

            string displayNumber = title;
            int number = 0;

            try
            {
                string numVal  = JsonUtil.GetString(response, "number");
                string nameVal = JsonUtil.GetString(response, "name");
                if (!string.IsNullOrEmpty(numVal))  number        = Convert.ToInt32(numVal);
                if (!string.IsNullOrEmpty(nameVal)) displayNumber = nameVal;
            }
            catch { }

            Ticket ticket = new Ticket();
            ticket.Type          = "";
            ticket.Number        = number;
            ticket.DisplayNumber = displayNumber;
            ticket.Timestamp     = DateTime.Now;

            PrinterManager pm = new PrinterManager(printerName);
            pm.PrintTicket(ticket, printDebug);
        }

        // ----------------------------------------------------------------
        // Internals
        // ----------------------------------------------------------------

        private static string SendRequest(string url, string method, string jsonBody)
        {
            HttpWebRequest  request  = null;
            HttpWebResponse response = null;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.Method        = method;
                request.ContentType   = "application/json";
                request.Timeout       = 15000;
                request.ReadWriteTimeout = 15000;
                request.UserAgent     = "Tokenizer/1.0 (.NET 3.5)";
                request.KeepAlive     = false;

                if (method == "POST" || method == "DELETE")
                {
                    if (!string.IsNullOrEmpty(jsonBody))
                    {
                        byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                        request.ContentLength = bodyBytes.Length;
                        using (Stream s = request.GetRequestStream())
                            s.Write(bodyBytes, 0, bodyBytes.Length);
                    }
                    else
                    {
                        request.ContentLength = 0;
                    }
                }

                response = (HttpWebResponse)request.GetResponse();
                using (Stream rs = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(rs, System.Text.Encoding.UTF8))
                    return reader.ReadToEnd();
            }
            finally
            {
                if (response != null) response.Close();
            }
        }

        private static string BuildQueryString(Dictionary<string, string> queryParams)
        {
            if (queryParams == null || queryParams.Count == 0)
                return "";

            StringBuilder sb = new StringBuilder("?");
            foreach (KeyValuePair<string, string> kv in queryParams)
            {
                if (sb.Length > 1) sb.Append("&");
                sb.Append(Uri.EscapeDataString(kv.Key));
                sb.Append("=");
                sb.Append(Uri.EscapeDataString(kv.Value));
            }
            return sb.ToString();
        }
    }
}