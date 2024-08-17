using System.Collections.Generic;
using Entities.Scripts.Utils;
using UnityEngine;
using LogType = Entities.Scripts.Utils.LogType;

namespace Objects.Engine
{
    
    /// <summary>
    /// This class is used to configure the AdvDebug
    /// </summary>
    public class AdvConfigurator: MonoBehaviour
    {
        [SerializeField] private List<LogType> whatToLog = new List<LogType>();
        [SerializeField] private LogLevel logLevel = LogLevel.Debug;
        private void Start()
        {
            AdvDebug.LogTypes = whatToLog;
            AdvDebug.Lis = logLevel;
        }
    }
}