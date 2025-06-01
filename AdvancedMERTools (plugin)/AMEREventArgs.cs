using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exiled.API.Features;
using Exiled.API.Enums;

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
