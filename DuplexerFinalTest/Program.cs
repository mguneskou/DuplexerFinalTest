using System.Windows.Forms;
using DuplexerFinalTest.Helpers;

namespace DuplexerFinalTest
{
    static class Program
    {
        [System.STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            var mainForm = new MainForm();
            ThemeManager.ApplyDarkThemeToForm(mainForm);
            Application.Run(mainForm);
        }
    }
}
