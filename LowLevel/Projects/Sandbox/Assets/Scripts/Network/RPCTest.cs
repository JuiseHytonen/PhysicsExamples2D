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
    public override void OnNetworkSpawn()
    {
        if (!IsServer && IsOwner) //Only send an RPC to the server from the client that owns the NetworkObject of this NetworkBehaviour instance
        {
            //ServerOnlyRpc(0, NetworkObjectId);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    void ClientAndHostRpc(int value, ulong sourceNetworkObjectId)
    {
        Debug.Log($"Client Received the RPC #{value} on NetworkObject #{sourceNetworkObjectId}");
        if (IsOwner) //Only send an RPC to the owner of the NetworkObject
        {
            ServerOnlyRpc(value + 1, sourceNetworkObjectId);
        }
    }

    [Rpc(SendTo.Server)]
    void ServerOnlyRpc(int value, ulong sourceNetworkObjectId)
    {
        Debug.Log($"Server Received the RPC #{value} on NetworkObject #{sourceNetworkObjectId}");
        ClientAndHostRpc(value, sourceNetworkObjectId);
    }

    public static void SendShootMessageToOthers(int frames, Vector2 rotation)
    {
        Instance?.SendShootMessageToOthersRpc(frames, rotation, Instance.NetworkObjectId);
    }

    [Rpc(SendTo.NotMe)]
    public void SendResetRpc()
    {
        LargePyramid.Instance.ResetTime(false);
    }

    [Rpc(SendTo.NotMe)]
    private void SendShootMessageToOthersRpc(int ticks, Vector2 rotation, ulong id)
    {
        LargePyramid.Instance.ShootAtTime(ticks, false, rotation);
    }

    public static void SendRotateMessageToOthers(Vector2 rotation)
    {
        Instance?.SendRotateMessageToOthersRpc(rotation, Instance.NetworkObjectId);
    }

    [Rpc(SendTo.NotMe)]
    private void SendRotateMessageToOthersRpc(Vector2 rotation, ulong id)
    {
        LargePyramid.Instance.RotateOtherTurret(rotation);
    }
}
