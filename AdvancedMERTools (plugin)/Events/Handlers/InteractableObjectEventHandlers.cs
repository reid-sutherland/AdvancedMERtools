using AdvancedMERTools.Events.Arguments;
using LabApi.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedMERTools.Events.Handlers
{
    public static class InteractableObjectEventHandlers
    {
        // From external code, register a handler to this event to trigger when a player interacts with the object
        public static event LabEventHandler<InteractableObjectInteractedEventArgs> InteractableObjectInteracted;

        public static void OnPlayerIOInteracted(InteractableObjectInteractedEventArgs ev)
        {
            InteractableObjectInteracted.InvokeEvent(ev);
        }
    }
}
