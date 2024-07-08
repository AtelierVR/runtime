using System.Collections.Generic;
using System.Linq;
using Nox.Scripts;
using UnityEngine;

namespace Nox.Network
{
    public class ConnectorManager : Manager<IConnector>
    {

        // ReSharper disable Unity.PerformanceAnalysis
        public static void Update()
        {
            foreach (var connector in Cache)
                connector.Update();
        }
    }
}