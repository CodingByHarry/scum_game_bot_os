using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace SCUMBot
{
    public class ScumManager
    {
        private const int Delay = 300;

        private static void RunCommand(string command)
        {
            if (!User32.BringWindowToForeground()) { return; }

            SendKeys.SendWait("t");
            Thread.Sleep(Delay);

            AutoResetEvent @event = new AutoResetEvent(false);
            Thread thread = new Thread(
                () => {
                    Clipboard.SetText(command);
                    @event.Set();
                });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            @event.WaitOne();

            SendKeys.SendWait("^{v}");

            Thread.Sleep(Delay);
            SendKeys.SendWait("{enter}{escape}");
        }

        public static void Teleport(string steamID, string x, string y, string z)
        {
            Logger.LogWrite($"Teleporting drone to {steamID} at {x}, {y}, {z}");
            RunCommand($"#teleport {x} {y} {z} {steamID}");
        }

        public static void TeleportTo(string adminSteamID, string playerSteamID)
        {
            Logger.LogWrite($"Teleporting {adminSteamID} to {playerSteamID}");
            RunCommand($"#teleportto {playerSteamID} {adminSteamID}");
        }

        public static void SpawnItem(string item, int amount)
        {
            Logger.LogWrite($"Spawning item {amount} x {item}");
            RunCommand($"#spawnitem {item} {amount}");
        }

        public static void SpawnItem(string item, int amount, string location)
        {
            Logger.LogWrite($"Spawning item {amount} x {item} at location {location}");
            RunCommand($"#spawnitem {item} {amount} location {location}");
        }

        public static void Announce(string content)
        {
            Logger.LogWrite($"Announcing {content}");
            RunCommand($"#announce {content}");
        }

        public static void DumpAllSquadsInfoList()
        {
            Logger.LogWrite($"DumpAllSquadsInfoList");
            RunCommand("#DumpAllSquadsInfoList");
        }

        private class User32
        {
            [DllImport("user32.dll")]
            static extern bool SetForegroundWindow(IntPtr hWnd);

            internal static bool BringWindowToForeground()
            {
                Process[] procs = Process.GetProcessesByName("SCUM");

                if (procs.Length > 0)
                {
                    if (procs[0].MainWindowHandle != IntPtr.Zero)
                    {
                        SetForegroundWindow(procs[0].MainWindowHandle);
                        return true;
                    }
                    return false;
                }
                else
                {
                    Logger.LogWrite($"Couldnt find SCUM Window.");
                    return false;
                }
            }
        }
    }
}