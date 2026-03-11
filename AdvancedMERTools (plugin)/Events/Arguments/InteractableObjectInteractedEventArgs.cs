using LabApi.Events.Arguments.Interfaces;
using LabApi.Features.Wrappers;
using System;

namespace AdvancedMERTools.Events.Arguments
{
    public class InteractableObjectInteractedEventArgs : EventArgs, IPlayerEvent
    {
        public Player Player { get; set; }
        public IODTO IODTO { get; set; }
        public string ObjectName { get; set; }

        public InteractableObjectInteractedEventArgs(Player player, IODTO interactableObject, string objectName)
        {
            Player = player;
            IODTO = interactableObject;
            ObjectName = objectName;
        }
    }
}
