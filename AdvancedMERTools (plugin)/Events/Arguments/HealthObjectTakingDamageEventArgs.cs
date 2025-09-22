using LabApi.Events.Arguments.Interfaces;
using LabApi.Features.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedMERTools.Events.Arguments
{
    public class HealthObjectTakingDamageEventArgs : EventArgs, IPlayerEvent, ICancellableEvent
    {
        public HODTO HealthObject { get; set; }
        public Player Player { get; set; }
        public bool IsAllowed { get; set; } = true;

        public HealthObjectTakingDamageEventArgs(HODTO healthObject, Player attacker)
        {
            HealthObject = healthObject;
            Player = attacker;
        }
    }
}
