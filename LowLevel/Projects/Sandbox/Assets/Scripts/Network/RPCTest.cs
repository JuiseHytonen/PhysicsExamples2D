using Unity.Netcode;
using UnityEngine;

public class RpcTest : NetworkBehaviour
{

    public static RpcTest Instance;

    public void Start()
    {
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

    public static void SendMessageToOthers(long ticks)
    {
        Instance?.SendMessageToOthersRpc(ticks, Instance.NetworkObjectId);
    }

    [Rpc(SendTo.NotMe)]
    private void SendMessageToOthersRpc(long ticks, ulong id)
    {
    //    if (id != Instance.NetworkObjectId)
      //  {
            LargePyramid.Instance.ShootAtTime(ticks, false);
           // Debug.Log($"Client Received the RPC {millis} on NetworkObject #{Instance.NetworkObjectId}");
        //}
    }

}
