using Unity.Netcode;
using UnityEngine;

public class RpcTest : NetworkBehaviour
{

    public static RpcTest Instance;

    public void Start()
    {
        if (Instance != null)
        {
            Destroy(Instance);
        }
        Instance = this;
    }

    [Rpc(SendTo.NotMe)]
    public void SendResetRpc()
    {
        LargePyramid.Instance.ResetTime(false);
    }

    [Rpc(SendTo.NotMe)]
    public void SendShootMessageToOthersRpc(int ticks, Vector2 rotation)
    {
        LargePyramid.Instance.ShootAtTime(ticks, false, rotation);
    }

    [Rpc(SendTo.NotMe)]
    public void SendRotateMessageToOthersRpc(Vector2 rotation)
    {
        LargePyramid.Instance.RotateOtherTurret(rotation);
    }
}
