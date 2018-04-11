using Axinom.Toolkit;
using System;
using System.IO;
using System.Reflection;

namespace Tests
{
    /// <summary>
    /// Base class for all test classes, to set up global functionality (e.g. logging and test data resolving).
    /// </summary>
    public abstract class TestClassBase : IDisposable
    {
        private static readonly StreamWriter _logFile;

        static TestClassBase()
        {
            _logFile = new StreamWriter("TestLog.txt");

            Log.Default.RegisterListener(new StreamWriterLogListener(_logFile));
            Log.Default.RegisterListener(new TraceLogListener());
        }

        protected static string ResolveTestDataPath(string filename)
        {
            return Path.Combine(GetTestDataFolder(), filename);
        }

        private static string GetCurrentAssemblyFolder()
        {
            return Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
        }

        private static string GetTestDataFolder()
        {
            return Path.Combine(GetCurrentAssemblyFolder(), "TestData");
        }

        public virtual void Dispose()
        {
            _logFile.Flush();
        }
    }
}
