using Unity.Netcode;
using UnityEngine;

public class RpcTest : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        //only send an RPC to the server on the client that owns the NetworkObject
        //that owns this NetworkBehavior instance
        if (!IsServer && IsOwner)
        {
            TestServerRpc(0, NetworkObjectId);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    void TestClientRpc(int value, ulong sourceNetworkObjectId)
    {
        Debug.Log($"Client Recieved the RPC #{value} on NetworkObject #{sourceNetworkObjectId}");
        //only send an RPC to the server on the client that owns
        //the NetworkObject that owns this NetworkBehavior instance
        if (IsOwner)
        {
            TestServerRpc(value + 1, sourceNetworkObjectId);
        }
    }

    [Rpc(SendTo.Server)]
    void TestServerRpc(int value, ulong sourceNetworkObjectId)
    {
        Debug.Log($"Server Recieved the RPC #{value} on NetworkObject #{sourceNetworkObjectId}");
        TestClientRpc(value, sourceNetworkObjectId);
    }
}
