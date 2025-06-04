using LabApi.Events;
using LabApi.Features.Wrappers;
using System;

namespace AdvancedMERTools;

// TODO: Interface for labapi event?
public class HealthObjectDeadEventArgs : EventArgs
{
    public HODTO HealthObject { get; set; }
    public Player Killer { get; set; }

    public HealthObjectDeadEventArgs(HODTO healthObject, Player attacker)
    {
        HealthObject = healthObject;
        Killer = attacker;
    }
}

public static class EventHandler
{
    // TODO: Event class?
    //public static event LabEventHandler<HealthObjectDeadEventArgs>? HealthObjectDead;
    ////public static Event<HealthObjectDeadEventArgs> HealthObjectDead { get; set; } = new Event<HealthObjectDeadEventArgs>();

    //internal static void OnHealthObjectDead(HealthObjectDeadEventArgs ev)
    //{
    //    EventHandler.HealthObjectDead.InvokeSafely(ev);
    //}
}
