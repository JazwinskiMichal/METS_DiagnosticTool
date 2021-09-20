using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace METS_DiagnosticTool_Utilities
{
    /// <summary>
    /// Class to help start UI from the Core
    /// </summary>
    public class UIHelper
    {
        private static readonly Process ui = new Process();

        private const string uiWindowName = "METS Diagnostic Tool UI";

        private const string uiProcessName = "METS_DiagnosticTool_UI";

        public static bool CheckUninstall(string[] givenArgs)
        {
            bool _return = false;

            foreach (string givenArg in givenArgs)
            {
                if (givenArg.Contains("uninstall"))
                {
                    _return = true;
                    break;
                }
            }

            return _return;
        }

        /// <summary>
        /// Method to pass parameters from Core to UI
        /// </summary>
        /// <param name="corePath"></param>
        /// <param name="givenArgs"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetInputParameters(string corePath, string[] givenArgs)
        {
            Dictionary<string, string> _return = new Dictionary<string, string>
            {

                // Get input Parameters for UI
                 { "-CorePath:", corePath },
                { "-UIPath:", Utility.ParseArg(givenArgs, "-UIPath:") },
                { "-ADSIp:", Utility.ParseArg(givenArgs, "-ADSIp:") },
                { "-ADSPort:", Utility.ParseArg(givenArgs, "-ADSPort:") }
            };

            return _return;
        }

        /// <summary>
        /// Method to start UI with input Parameters
        /// </summary>
        /// <param name="uiFullPath"></param>
        /// <param name="adsIp"></param>
        /// <param name="adsPort"></param>
        /// <returns></returns>
        public static bool StartUI(string coreFullPath, string uiFullPath, string adsIp, string adsPort)
        {
            bool _return = false;

            // Start Bridge UI
            // First check does the Bridge UI has been already running
            Process[] pname = Process.GetProcessesByName(uiProcessName);
            if (pname.Length == 0)
            {
                // It's not running, so start it
                ui.StartInfo.FileName = uiFullPath;
                ui.StartInfo.WorkingDirectory = Path.GetDirectoryName(uiFullPath);
                ui.StartInfo.Arguments = string.Concat(coreFullPath, " ", adsIp, " ", adsPort);
                if (ui.Start())
                    _return = true;
            }

            return _return;
        }

        /// <summary>
        /// Method to show UI window
        /// </summary>
        public static void ShowUI()
        {
            IEnumerable<IntPtr> window = FindWindowsWithText(uiWindowName);

            ShowWindow(window.FirstOrDefault(), ShowWindowEnum.ShowNormal);
        }

        /// <summary>
        /// Method to hide UI window
        /// </summary>
        public static void HideUI()
        {
            IEnumerable<IntPtr> window = FindWindowsWithText(uiWindowName);

            ShowWindow(window.FirstOrDefault(), ShowWindowEnum.Hide);
        }

        /// <summary>
        /// Method to kill every running instance of the UI
        /// </summary>
        public static void KillUI()
        {
            Process[] pname = Process.GetProcessesByName(uiProcessName);
            if (pname.Length > 0)
            {
                foreach (Process item in pname)
                {
                    item.Kill();
                }
            }
        }

        /// <summary>
        /// Method to check us the UI Running
        /// </summary>
        /// <returns></returns>
        public static bool CheckUIRunning()
        {
            bool _return = false;

            Process[] pname = Process.GetProcessesByName(uiProcessName);
            if (pname.Length > 1) // another instance of UI is already Running
            {
                _return = true;
            }

            return _return;
        }

        #region Show Hide UI Methods and Fields
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);
        private enum ShowWindowEnum
        {
            Hide = 0,
            ShowNormal = 1, ShowMinimized = 2, ShowMaximized = 3,
            Maximize = 3, ShowNormalNoActivate = 4, Show = 5,
            Minimize = 6, ShowMinNoActivate = 7, ShowNoActivate = 8,
            Restore = 9, ShowDefault = 10, ForceMinimized = 11
        };

        // Delegate to filter which windows to include 
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        /// <summary> Get the text for the window pointed to by hWnd </summary>
        private static string GetWindowText(IntPtr hWnd)
        {
            int size = GetWindowTextLength(hWnd);
            if (size > 0)
            {
                var builder = new StringBuilder(size + 1);
                GetWindowText(hWnd, builder, builder.Capacity);
                return builder.ToString();
            }

            return String.Empty;
        }

        /// <summary> Find all windows that match the given filter </summary>
        /// <param name="filter"> A delegate that returns true for windows
        ///    that should be returned and false for windows that should
        ///    not be returned </param>
        private static IEnumerable<IntPtr> FindWindows(EnumWindowsProc filter)
        {
            IntPtr found = IntPtr.Zero;
            List<IntPtr> windows = new List<IntPtr>();

            EnumWindows(delegate (IntPtr wnd, IntPtr param)
            {
                if (filter(wnd, param))
                {
                    // only add the windows that pass the filter
                    windows.Add(wnd);
                }

                // but return true here so that we iterate all windows
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        /// <summary> Find all windows that contain the given title text </summary>
        /// <param name="titleText"> The text that the window title must contain. </param>
        private static IEnumerable<IntPtr> FindWindowsWithText(string titleText)
        {
            return FindWindows(delegate (IntPtr wnd, IntPtr param)
            {
                return GetWindowText(wnd).Contains(titleText);
            });
        }
        #endregion

    }
}
