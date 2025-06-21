using System;
using System.Runtime.InteropServices;

namespace CloudServiceBootstrapper
{
    public static class Handlers
    {
        private static HandlerRoutine s_rou = null;

        public static bool HasBeenSignaled { get; set; }

        public static void SetHandler()
        {
            if (s_rou == null)
            {
                HasBeenSignaled = false;
                s_rou = new HandlerRoutine(ConsoleCtrlCheck);
                SetConsoleCtrlHandler(s_rou, true);
            }
        }

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            switch (ctrlType)
            {
                case CtrlTypes.CTRL_C_EVENT:
                    Console.WriteLine("[" + DateTime.Now.ToString() + "] CTRL_C_EVENT received!");
                    HasBeenSignaled = true;

                    return true;
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    Console.WriteLine("[" + DateTime.Now.ToString() + "] CTRL_SHUTDOWN_EVENT received!");
                    HasBeenSignaled = true;

                    return true;
            }

            return false;
        }

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }
    }
}