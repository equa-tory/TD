using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace Tokenizer.Shared
{
    public class Ticket
    {
        public string Type;
        public int Number;
        public string DisplayNumber;
        public DateTime Timestamp;   // assigned time slot (which slot it was booked on)
        public DateTime Created;     // issue time (when the ticket was printed)
    }

    // =====================================================================
    // EDIT THIS to change ticket appearance
    // =====================================================================
    public static class TicketLayout
    {
        public static float MarginLeft    = 10f;
        public static float MarginTop     = 20f;

        public static Font  HeaderFont    = new Font("Arial", 14, FontStyle.Bold);
        public static Font  NumberFont    = new Font("Arial", 40, FontStyle.Bold);
        public static Font  SmallFont     = new Font("Arial", 10);

        public static string HeaderText   = "Номер в очереди:";
        public static string DateLabel    = "Дата и время выдачи:";
        public static string DateFormat   = "dd.MM.yyyy HH:mm";

        public static float HeaderSpacing = 40f;
        public static float NumberSpacing = 80f;
        public static float LabelSpacing  = 20f;
    }
    // =====================================================================

    public class PrinterManager
    {
        private readonly string _printerName;

        public PrinterManager(string printerName)
        {
            _printerName = printerName;
        }

        public void PrintTicket(Ticket ticket, bool preview) // TODO: doesn't print but shows
        {
            PrintDocument doc = new PrintDocument();

            // Только для реальной печати
            if (!preview)
            {
                if (string.IsNullOrEmpty(_printerName) || _printerName == "None")
                    return;

                doc.PrinterSettings.PrinterName = _printerName;
            }

            doc.PrintPage += delegate(object sender, PrintPageEventArgs e)
            {
                DrawTicket(e.Graphics, ticket);
            };

            if (preview)
            {
                Form owner = Form.ActiveForm ?? Application.OpenForms[0];

                owner.Invoke((MethodInvoker)delegate
                {
                    PrintPreviewDialog dlg = new PrintPreviewDialog();
                    dlg.Document = doc;
                    dlg.Width = 800;
                    dlg.Height = 600;
                    dlg.ShowDialog(owner);
                });
            }
            else
            {
                doc.Print();
            }
        }
        private static void DrawTicket(Graphics g, Ticket ticket)
        {
            // load from config every print — changes take effect immediately
            float marginLeft    = ParseFloat(Config.Get("print_marginLeft"),   10f);
            float marginTop     = ParseFloat(Config.Get("print_marginTop"),    20f);
            float headerSpacing = ParseFloat(Config.Get("print_headerSpacing"), 40f);
            float numberSpacing = ParseFloat(Config.Get("print_numberSpacing"), 80f);
            float labelSpacing  = ParseFloat(Config.Get("print_labelSpacing"),  20f);

            float headerSize    = ParseFloat(Config.Get("print_headerSize"),   14f);
            float numberSize    = ParseFloat(Config.Get("print_numberSize"),   40f);
            float smallSize     = ParseFloat(Config.Get("print_smallSize"),    10f);

            string headerText   = OrDefault(Config.Get("print_headerText"), "Номер в очереди:");
            string dateLabel    = OrDefault(Config.Get("print_dateLabel"),  "Дата и время выдачи:");
            string dateFormat   = OrDefault(Config.Get("print_dateFormat"), "dd.MM.yyyy HH:mm");
            string timestampLabel    = "Дата и время назначения:";
            // string timestampFormat   = "dd.MM.yyyy HH:mm";

            Font headerFont = new Font("Arial", headerSize, FontStyle.Bold);
            Font numberFont = new Font("Arial", numberSize, FontStyle.Bold);
            Font smallFont  = new Font("Arial", smallSize);

            float x = marginLeft;
            float y = marginTop;

            string number = !string.IsNullOrEmpty(ticket.DisplayNumber)
                ? ticket.DisplayNumber
                : ticket.Number.ToString("D3");

            g.DrawString(headerText, headerFont, Brushes.Black, x, y);
            y += headerSpacing;

            g.DrawString(number, numberFont, Brushes.Black, x, y);
            y += numberSpacing;

            // --- Issue date/time (when the ticket was printed) ---
            g.DrawString(dateLabel, smallFont, Brushes.Black, x, y);
            y += labelSpacing;
            g.DrawString(ticket.Created.ToString(dateFormat), smallFont, Brushes.Black, x, y);
            y += labelSpacing;

            // --- Assigned time slot (which slot it was booked on) ---
            g.DrawString(timestampLabel, smallFont, Brushes.Black, x, y);
            y += labelSpacing;
            g.DrawString(ticket.Timestamp.ToString(dateFormat), smallFont, Brushes.Black, x, y);

            headerFont.Dispose();
            numberFont.Dispose();
            smallFont.Dispose();
        }

        private static float ParseFloat(string s, float def)
        {
            float v;
            return float.TryParse(s, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out v) ? v : def;
        }

        private static string OrDefault(string s, string def)
        {
            return string.IsNullOrEmpty(s) ? def : s;
        }
    }
}