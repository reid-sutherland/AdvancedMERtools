using LabApi.Events;
using LabApi.Features.Wrappers;
using System;

namespace AdvancedMERTools;

public static class AMERTEvents
{
    public static event LabEventHandler<HealthObjectDeadEventArgs> HealthObjectDead;

    public static void OnHealthObjectDead(HealthObjectDeadEventArgs ev)
    {
        HealthObjectDead.InvokeEvent(ev);
    }
}

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