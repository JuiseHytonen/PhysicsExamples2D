using Unity.Netcode;
using UnityEngine;

public class RpcTest : NetworkBehaviour
{

    public static RpcTest Instance;

    public override void OnNetworkSpawn()
    {
        Instance = this;
        if (!IsServer && IsOwner) //Only send an RPC to the server from the client that owns the NetworkObject of this NetworkBehaviour instance
        {
            //ServerOnlyRpc(0, NetworkObjectId);
        }
    }

    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject) { }

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

    public static void SendMessageToOthers(string message)
    {
        Instance.SendMessageToOthersRpc(message);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SendMessageToOthersRpc(string message)
    {
        if (!Instance.IsHost)
        {
            Debug.Log($"Client Received the RPC {message} on NetworkObject #{Instance.NetworkObjectId}");
        }
    }

}
