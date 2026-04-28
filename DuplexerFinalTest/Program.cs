using System.Windows.Forms;

namespace DuplexerFinalTest
{
    static class Program
    {
        [System.STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
