using LabApi.Events.Arguments.Interfaces;
using LabApi.Features.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedMERTools.Events.Arguments
{
    public class HealthObjectDiedEventArgs : EventArgs, IPlayerEvent
    {
        public HODTO HealthObject { get; set; }
        public Player Player { get; set; }

        public HealthObjectDiedEventArgs(HODTO healthObject, Player attacker)
        {
            HealthObject = healthObject;
            Player = attacker;
        }
    }
}
