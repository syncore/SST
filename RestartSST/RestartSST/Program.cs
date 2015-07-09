namespace RestartSST
{
    using System;
    using System.Diagnostics;

    internal class Program
    {
        private static void Main()
        {
            foreach (var p in Process.GetProcessesByName("SST"))
            {
                try
                {
                    p.Kill();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to stop SST process - {0}", ex);
                }
            }
            try
            {
                Process.Start(new ProcessStartInfo { Arguments = "--restart", FileName = "SST.exe" });
            }
            catch (Exception ex)
            {
                Console.Write("Unable to start SST process - {0}", ex);
            }
        }
    }
}
