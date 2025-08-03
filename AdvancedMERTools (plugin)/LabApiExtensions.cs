using InventorySystem.Items.Firearms.Modules;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;

namespace AdvancedMERTools;

public static class LabApiExtensions
{
    public static bool IsAirborne(this Player player)
    {
        return player.RoleBase is IFpcRole fpc && !fpc.FpcModule.IsGrounded;
    }

    public static bool IsJumping(this Player player)
    {
        return player.RoleBase is IFpcRole fpc && fpc.FpcModule.Motor.JumpController.IsJumping;
    }

    public static bool IsReloading(this Player player)
    {
        return player.CurrentItem is FirearmItem firearmItem && firearmItem.Base.TryGetModule(out IReloaderModule module) && module.IsReloading;
    }

    public static bool IsUsingStamina(this Player player)
    {
        return player.RoleBase is IFpcRole fpc && fpc.FpcModule.CurrentMovementState.HasFlag(PlayerMovementState.Sprinting);
    }

    public static string GetUniqueRole(this Player player)
    {
        return player.RoleBase.RoleName;
    }
}
