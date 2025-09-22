using LabApi.Events.Arguments.Interfaces;
using LabApi.Features.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedMERTools.Events.Arguments
{
    public class InteractableObjectInteractedEventArgs : EventArgs, IPlayerEvent
    {
        public IODTO IODTO { get; set; }
        public Player Player { get; set; }

        public InteractableObjectInteractedEventArgs(Player player, IODTO interactableObject)
        {
            Player = player;
            IODTO = interactableObject;
        }
    }
}
