using System;
using System.Net;
using System.Windows.Forms;

namespace MingxuDesktop
{

internal static class Program
{
    [STAThread]
    static void Main()
    {
        try
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }
        catch
        {
            // ignore
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += ApplicationThreadException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

        try
        {
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            ShowFatal(ex);
        }
    }

    private static void ApplicationThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
    {
        ShowFatal(e.Exception);
    }

    private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        ShowFatal(e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject == null ? "未知錯誤" : e.ExceptionObject.ToString()));
    }

    private static void ShowFatal(Exception ex)
    {
        try
        {
            MessageBox.Show(
                "程式無法啟動或發生錯誤：\n\n" + ex.Message + "\n\n" + ex.GetType().FullName,
                "名序",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
            // ignore
        }
    }
}
}
