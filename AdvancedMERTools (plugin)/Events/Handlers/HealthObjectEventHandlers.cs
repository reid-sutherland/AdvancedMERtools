using AdvancedMERTools.Events.Arguments;
using LabApi.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedMERTools.Events.Handlers
{
    public static class HealthObjectEventHandlers
    {
        public static event LabEventHandler<HealthObjectDiedEventArgs> HealthObjectDied;

        public static event LabEventHandler<HealthObjectTakingDamageEventArgs> HealthObjectTakingDamage;

        internal static void OnHealthObjectDied(HealthObjectDiedEventArgs ev)
        {
            HealthObjectDied.InvokeEvent(ev);
        }
        internal static void OnHealthObjectTakingDamage(HealthObjectTakingDamageEventArgs ev)
        {
            HealthObjectTakingDamage.InvokeEvent(ev);
        }
    }
}
