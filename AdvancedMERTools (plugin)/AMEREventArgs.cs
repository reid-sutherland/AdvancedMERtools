using LabApi.Events;
using LabApi.Features.Wrappers;
using System;

namespace AdvancedMERTools;

public class HealthObjectDeadEventArgs : EventArgs  // TODO: Interface for labapi event?
{
    public HealthObjectDeadEventArgs(HODTO healthObject, Player Attacker)
    {
        HealthObject = healthObject;
        Killer = Attacker;
    }

    public HODTO HealthObject;
    public Player Killer;
}
