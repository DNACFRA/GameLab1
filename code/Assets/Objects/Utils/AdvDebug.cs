// ReSharper disable once CommentTypo
//#define USEBADLOG
//Uncommenting this Line allows to go back to the bad log, just dont
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Entities.Scripts.Utils
{
    public enum
        LogLevel //The Higher, the more gets logged
    {
        Nothing, //You might as well just disable the whole thing
        Error, //Something went wrong
        Critical, //Really Important Stuff, something definitely went wrong
        Warning, //Important Stuff, probably something went wrong
        Notice, //Not really that important, but still important
        Inform, //Default
        Debug, //General Stuff, like "Merges and Splits"
        Verbose //Really General Stuff, like "TriggerRelay moved 1m to the right"
    }
    
    public enum LogType
    {
        Input,
        Collision,
        RayCast,
        PlayerAction,
        Raymarch,
        InspectorEdits,
        Goal,
        Click
    }

    public static class AdvDebug
    {
        public static LogLevel Lis = LogLevel.Debug;
        public static List<LogType> LogTypes = new List<LogType>();
        public static bool Log(string msg,LogType logType, LogLevel lin = LogLevel.Debug)
        {
            if (LogTypes.Contains(logType))
            {
                return UniLog(msg+ $"${logType}$", lin, Debug.Log);
            }
            return false;
        }
        public static bool LogWarning(string msg,LogType logType, LogLevel lin = LogLevel.Warning)
        {
            if (LogTypes.Contains(logType))
            {
                return UniLog(msg+ $"${logType}$", lin, Debug.Log);
            }
            return false;
        }
        public static bool Log(string msg, LogLevel lin = LogLevel.Debug)
        {
            return UniLog(msg, lin, Debug.Log);
        }

        public static bool LogWarning(string msg, LogLevel lin = LogLevel.Warning)
        {
            return UniLog(msg, lin, Debug.LogWarning);
        }

        public static bool LogError(string msg, LogLevel lin = LogLevel.Error)
        {
            return UniLog(msg, lin, Debug.LogError);
        }

        private static bool UniLog(string msg, LogLevel lin, Action<string> ac)
        {
            if (Lis >= lin)
            {
#if USEBADLOG
                ac(GetCaller(4)+":              "+GetCaller()+":  \n"+msg); //TODO: Cool, but highly costly operation and completely useless, cause Unity does that anyway
#else 
                ac(msg);
#endif
                return true;
            }

            return false;
        }

        private static string GetCaller(int level = 3)
        {
            try
            {
                var m = new StackTrace().GetFrame(level).GetMethod();
                if (m.DeclaringType != null)
                {
                    var classname = m.DeclaringType.FullName;
                    var method = m.Name;
                    return classname + " -> " + method;
                }
            }
            catch (NullReferenceException)
            {
            }


            return "?NoReferenceFound?";
        }
        
    }
}