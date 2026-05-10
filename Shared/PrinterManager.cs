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
        public DateTime Timestamp;
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
            float x = TicketLayout.MarginLeft;
            float y = TicketLayout.MarginTop;

            string number = !string.IsNullOrEmpty(ticket.DisplayNumber)
                ? ticket.DisplayNumber
                : ticket.Number.ToString("D3");

            // Header
            g.DrawString(TicketLayout.HeaderText, TicketLayout.HeaderFont, Brushes.Black, x, y);
            y += TicketLayout.HeaderSpacing;

            // Big number
            g.DrawString(number, TicketLayout.NumberFont, Brushes.Black, x, y);
            y += TicketLayout.NumberSpacing;

            // Date label
            g.DrawString(TicketLayout.DateLabel, TicketLayout.SmallFont, Brushes.Black, x, y);
            y += TicketLayout.LabelSpacing;

            // Date value
            g.DrawString(ticket.Timestamp.ToString(TicketLayout.DateFormat),
                         TicketLayout.SmallFont, Brushes.Black, x, y);
        }
    }
}