using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CoffeeTable.Logging
{
	internal class Log
	{
		[Flags]
		internal enum LogLevels
		{
			None = 0,
			Out = 1,
			Warn = 2,
			Error = 4
		}

		internal const string ApplicationName = "CoffeeTable";
		private string mLogFormat = $"<b>[{ApplicationName}]</b> {{0}}";

		public static void Out(object s) => RuntimeLog.LogOut(s);
		public static void Warn(object s) => RuntimeLog.LogWarn(s);
		public static void Error(object s) => RuntimeLog.LogError(s);

		public void Write (LogLevels level, object s)
		{
			if ((level & LogLevels.Error) != LogLevels.None)
				Debug.LogErrorFormat(mLogFormat, s.ToString());
			else if ((level & LogLevels.Warn) != LogLevels.None)
				Debug.LogWarningFormat(mLogFormat, s.ToString());
			else if ((level & LogLevels.Out) != LogLevels.None)
				Debug.LogFormat(mLogFormat, s.ToString());
		}

		public void LogOut(object s) => Write(LogLevels.Out, s);
		public void LogWarn(object s) => Write(LogLevels.Warn, s);
		public void LogError(object s) => Write(LogLevels.Error, s);

		public static Log RuntimeLog = new Log();
		public static Log BuildLog = new Log()
		{
			mLogFormat = $"<b>[{ApplicationName}] [Build]</b> {{0}}"
		};
	}
}
