using System;
using System.IO;
using System.Windows.Controls;

namespace DependencyResolver {
    public static class Logger {
        static TextBox outputControl;
       static StreamWriter outputFile;

        public static void Init(TextBox loggerOutput) {
            outputControl = loggerOutput;
            outputFile = new StreamWriter("log.txt");
        }

        public static void Log(string message) {
            outputControl.Text = message + Environment.NewLine + outputControl.Text;
            LogToFile(message);
        }

        public static void LogToFile(string message) {
            outputFile.WriteLine(message);
        }

        public static void Error(string message) {
            Log("ERROR:   " + message);
        }

        public static void Finalise() {
            outputFile.Close();
        }

        public static void Clear() {
            outputControl.Text = string.Empty;
        }
    }
}