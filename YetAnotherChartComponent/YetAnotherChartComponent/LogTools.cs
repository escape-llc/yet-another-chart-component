using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace eScape.Core {
	#region LogToolsExtensions
	/// <summary>
	/// Extension methods for LogTools.Flag.
	/// Allows for syntax like flag.Verbose($"the message")
	/// </summary>
	public static class LogToolsExtensions {
		/// <summary>
		/// Extension method.
		/// </summary>
		/// <param name="flag"></param>
		/// <returns></returns>
		public static bool IsVerbose(this LogTools.Flag flag) { return flag.Level <= LogTools.Level.Verbose; }
		/// <summary>
		/// Extension method.
		/// </summary>
		/// <param name="flag"></param>
		/// <returns></returns>
		public static bool IsInfo(this LogTools.Flag flag) { return flag.Level <= LogTools.Level.Info; }
		/// <summary>
		/// Extension method.
		/// </summary>
		/// <param name="flag"></param>
		/// <returns></returns>
		public static bool IsWarn(this LogTools.Flag flag) { return flag.Level <= LogTools.Level.Warn; }
		/// <summary>
		/// Extension method.
		/// </summary>
		/// <param name="flag"></param>
		/// <returns></returns>
		public static bool IsError(this LogTools.Flag flag) { return flag.Level <= LogTools.Level.Error; }
		/// <summary>
		/// Extension method.
		/// </summary>
		/// <param name="flag"></param>
		/// <returns></returns>
		public static bool IsFatal(this LogTools.Flag flag) { return flag.Level <= LogTools.Level.Fatal; }
		/// <summary>
		/// Extension method.
		/// </summary>
		/// <param name="flag"></param>
		/// <returns></returns>
		public static bool IsOff(this LogTools.Flag flag) { return flag.Level == LogTools.Level.Off; }
		/// <summary>
		/// Extension method.
		/// </summary>
		/// <param name="flag"></param>
		/// <param name="msg"></param>
		/// <returns></returns>
		public static void Verbose(this LogTools.Flag flag, String msg) { LogTools.Log.Verbose(flag, msg); }
		/// <summary>
		/// Extension method.
		/// </summary>
		/// <param name="flag"></param>
		/// <param name="msg"></param>
		/// <returns></returns>
		public static void Verbose(this LogTools.Flag flag, Func<String> msg) { LogTools.Log.Verbose(flag, msg); }
		/// <summary>
		/// Extension method.
		/// </summary>
		/// <param name="flag"></param>
		/// <param name="msg"></param>
		/// <returns></returns>
		public static void Info(this LogTools.Flag flag, String msg) { LogTools.Log.Info(flag, msg); }
		/// <summary>
		/// Extension method.
		/// </summary>
		/// <param name="flag"></param>
		/// <param name="msg"></param>
		/// <returns></returns>
		public static void Info(this LogTools.Flag flag, Func<String> msg) { LogTools.Log.Info(flag, msg); }
		/// <summary>
		/// Extension method.
		/// </summary>
		/// <param name="flag"></param>
		/// <param name="msg"></param>
		/// <returns></returns>
		public static void Warn(this LogTools.Flag flag, String msg) { LogTools.Log.Warn(flag, msg); }
		/// <summary>
		/// Extension method.
		/// </summary>
		/// <param name="flag"></param>
		/// <param name="msg"></param>
		/// <returns></returns>
		public static void Warn(this LogTools.Flag flag, Func<String> msg) { LogTools.Log.Warn(flag, msg); }
		/// <summary>
		/// Extension method.
		/// </summary>
		/// <param name="flag"></param>
		/// <param name="msg"></param>
		/// <returns></returns>
		public static void Error(this LogTools.Flag flag, String msg) { LogTools.Log.Error(flag, msg); }
		/// <summary>
		/// Extension method.
		/// </summary>
		/// <param name="flag"></param>
		/// <param name="msg"></param>
		/// <returns></returns>
		public static void Error(this LogTools.Flag flag, Func<String> msg) { LogTools.Log.Error(flag, msg); }
		/// <summary>
		/// Extension method.
		/// </summary>
		/// <param name="flag"></param>
		/// <param name="msg"></param>
		/// <returns></returns>
		public static void Fatal(this LogTools.Flag flag, String msg) { LogTools.Log.Fatal(flag, msg); }
		/// <summary>
		/// Extension method.
		/// </summary>
		/// <param name="flag"></param>
		/// <param name="msg"></param>
		/// <returns></returns>
		public static void Fatal(this LogTools.Flag flag, Func<String> msg) { LogTools.Log.Fatal(flag, msg); }
	}
	#endregion
	#region LogTools
	/// <summary>
	/// Logging helpers.
	/// </summary>
	public static class LogTools {
		#region Level
		/// <summary>
		/// Severity levels.
		/// </summary>
		public enum Level : uint {
			/// <summary>
			/// Lowest level; all messages.
			/// </summary>
			Verbose = 0,
			/// <summary>
			/// Info and higher.
			/// </summary>
			Info = 1,
			/// <summary>
			/// Warn and higher.
			/// </summary>
			Warn = 2,
			/// <summary>
			/// Error and higher.
			/// </summary>
			Error = 3,
			/// <summary>
			/// Fatal and higher.
			/// </summary>
			Fatal = 4,
			/// <summary>
			/// Highese level; no messages.
			/// </summary>
			Off = uint.MaxValue
		};
		#endregion
		#region Flag
		/// <summary>
		/// Represents the trace flag state.
		/// </summary>
		public sealed class Flag {
			/// <summary>
			/// The name of the flag.  Used in external configuration.
			/// </summary>
			public String Name { get; private set; }
			/// <summary>
			/// The flag level.  Determines whether messages are logged based on the message's level.
			/// </summary>
			public Level Level { get; set; }
			/// <summary>
			/// Whether value came from configuration.
			/// If True, the value is "locked out" from runtime initialization via e.g. LogTools.Add().
			/// </summary>
			public bool FromConfiguration { get; private set; }
			/// <summary>
			/// Create a new flag.
			/// </summary>
			/// <param name="name"></param>
			/// <param name="level"></param>
			internal Flag(String name, Level level) {
				Name = name;
				Level = level;
				FromConfiguration = false;
			}
			/// <summary>
			/// Internal use only!
			/// Used to create flags "locked" by configuration.
			/// </summary>
			/// <param name="name"></param>
			/// <param name="level"></param>
			/// <param name="fc"></param>
			internal Flag(String name, Level level, bool fc) {
				Name = name;
				Level = level;
				FromConfiguration = fc;
			}
		}
		#endregion
		#region state
		/// <summary>
		/// Holds all the flags.
		/// </summary>
		static readonly ConcurrentDictionary<String, Flag> TraceFlags = new ConcurrentDictionary<String, Flag>();
		#endregion
		#region public static
		/// <summary>
		/// Add a trace flag.
		/// If the flag was already loaded from configuration, its level is not modified.  Otherwise, its level is updated.
		/// </summary>
		/// <param name="name">Name of flag.</param>
		/// <param name="level">Initial level. Ignored if an existing flag was loaded from configuration.</param>
		/// <returns>Either a new or existing instance, according to its existence in the map.</returns>
		public static Flag Add(String name, Level level) {
			return TraceFlags.AddOrUpdate(name, new Flag(name, level), (kx, f) => { if (!f.FromConfiguration) f.Level = level; return f; });
		}
		/// <summary>
		/// Initialize flags from given container.  E.g. in JSON:
		/// { "TraceSwitches": { "Flag1": "Verbose", "Flag2": "Verbose" } }
		/// This SHOULD be called as early as possible to get all flags initialized with intended values before any are dynamically created, typically via static initializers calling LogTools.Add().
		/// Overwrites the level of any previously-loaded dynamic flags.
		/// </summary>
		/// <param name="ctr">Top-level container whose keys are flag names, values resolve to the Levels.</param>
		/// <param name="resolver">Function to resolve the dictionary's actual value type to Level. MUST NOT throw.</param>
		/// <typeparam name="ValueType">Dictionary value type.</typeparam>
		public static void FromContainer<ValueType>(IDictionary<String, ValueType> ctr, Func<ValueType, Level> resolver) {
			foreach (var key in ctr.Keys) {
				var level = resolver(ctr[key]);
				TraceFlags.AddOrUpdate(key, new Flag(key, level, true), (kx, f) => { return new Flag(kx, level, true); });
			}
		}
		#endregion
		#region Log
		/// <summary>
		/// Helper class for conditional logging based on Flag.Level.
		/// </summary>
		public static class Log {
			#region state
			/// <summary>
			/// The framework logging action.  By default, goes to System.Diagnostics.Debug.WriteLine().
			/// This action MUST BE responsible for all thread-safety it requires internally.
			/// </summary>
			internal static volatile Action<String, Level, String> logaction = (name, level, msg) => { System.Diagnostics.Debug.WriteLine($"{Prefix(name, Level.Verbose)}\t{msg}"); };
			/// <summary>
			/// Set the logging action for the framework.
			/// SHOULD only call once per AppDomain!
			/// Provides the thread-safety of volatile.
			/// The default logging action goes to System.Diagnostics.Debug.WriteLine().
			/// </summary>
			/// <param name="action">New logging action.  This code MUST BE responsible for all thread-safety it requires internally.</param>
			public static void SetLogAction(Action<String, Level, String> action) { logaction = action; }
			#endregion
			#region helpers
			/// <summary>
			/// Return the "standard" message prefix.
			/// </summary>
			/// <param name="name">name.</param>
			/// <param name="level">level; only 1st character is used.</param>
			/// <returns></returns>
			public static String Prefix(String name, Level level) {
				return $"{DateTime.Now:O} {level.ToString().Substring(0, 1)}\t{name}";
			}
			#endregion
			#region logging
			#region Verbose
			/// <summary>
			/// Log message if flag is Verbose or lower.
			/// </summary>
			/// <param name="flag">The flag to check.</param>
			/// <param name="msg">The message.</param>
			public static void Verbose(Flag flag, String msg) {
				if (flag.Level <= Level.Verbose) logaction(flag.Name, Level.Verbose, msg);
			}
			/// <summary>
			/// Deferred evaluation version.
			/// Log message if flag is Verbose or lower.
			/// </summary>
			/// <param name="flag">The flag to check.</param>
			/// <param name="msg">Generate the message.  Not called if not logging.  MUST BE thread-safe.</param>
			public static void Verbose(Flag flag, Func<String> msg) {
				if (flag.Level <= Level.Verbose) logaction(flag.Name, Level.Verbose, msg());
			}
			#endregion
			#region Info
			/// <summary>
			/// Log message if flag is Info or lower (Info, Verbose).
			/// </summary>
			/// <param name="flag">The flag to check.</param>
			/// <param name="msg">The message.</param>
			public static void Info(Flag flag, String msg) {
				if (flag.Level <= Level.Info) logaction(flag.Name, Level.Info, msg);
			}
			/// <summary>
			/// Deferred evaluation version.
			/// Log message if flag is Info or lower (Info, Verbose).
			/// </summary>
			/// <param name="flag">The flag to check.</param>
			/// <param name="msg">Generate the message.  Not called if not logging.  MUST BE thread-safe.</param>
			public static void Info(Flag flag, Func<String> msg) {
				if (flag.Level <= Level.Info) logaction(flag.Name, Level.Info, msg());
			}
			#endregion
			#region Warn
			/// <summary>
			/// Log message if flag is Warn or lower (Warn, Info, Verbose).
			/// </summary>
			/// <param name="flag">The flag to check.</param>
			/// <param name="msg">The message.</param>
			public static void Warn(Flag flag, String msg) {
				if (flag.Level <= Level.Warn) logaction(flag.Name, Level.Warn, msg);
			}
			/// <summary>
			/// Deferred evaluation version.
			/// Log message if flag is Warn or lower (Warn, Info, Verbose).
			/// </summary>
			/// <param name="flag">The flag to check.</param>
			/// <param name="msg">Generate the message.  Not called if not logging.  MUST BE thread-safe.</param>
			public static void Warn(Flag flag, Func<String> msg) {
				if (flag.Level <= Level.Warn) logaction(flag.Name, Level.Warn, msg());
			}
			#endregion
			#region Error
			/// <summary>
			/// Log message if flag is Error or lower (Error, Warn, Info, Verbose).
			/// </summary>
			/// <param name="flag">The flag to check.</param>
			/// <param name="msg">The message.</param>
			public static void Error(Flag flag, String msg) {
				if (flag.Level <= Level.Error) logaction(flag.Name, Level.Error, msg);
			}
			/// <summary>
			/// Deferred evaluation version.
			/// Log message if flag is Error or lower (Error, Warn, Info, Verbose).
			/// </summary>
			/// <param name="flag">The flag to check.</param>
			/// <param name="msg">Generate the message.  Not called if not logging.  MUST BE thread-safe.</param>
			public static void Error(Flag flag, Func<String> msg) {
				if (flag.Level <= Level.Error) logaction(flag.Name, Level.Error, msg());
			}
			#endregion
			#region Fatal
			/// <summary>
			/// Log message if flag is Fatal or lower (Fatal, Error, Warn, Info, Verbose).
			/// </summary>
			/// <param name="flag">The flag to check.</param>
			/// <param name="msg">The message.</param>
			public static void Fatal(Flag flag, String msg) {
				if (flag.Level <= Level.Fatal) logaction(flag.Name, Level.Fatal, msg);
			}
			/// <summary>
			/// Deferred evaluation version.
			/// Log message if flag is Fatal or lower (Fatal, Error, Warn, Info, Verbose).
			/// </summary>
			/// <param name="flag">The flag to check.</param>
			/// <param name="msg">Generate the message.  Not called if not logging.  MUST BE thread-safe.</param>
			public static void Fatal(Flag flag, Func<String> msg) {
				if (flag.Level <= Level.Fatal) logaction(flag.Name, Level.Fatal, msg());
			}
			#endregion
			#endregion
		}
		#endregion
	}
	#endregion
}
