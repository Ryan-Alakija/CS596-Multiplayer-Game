using Unity.Netcode;
using UnityEngine;

public class CoinSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkObject coinPrefab;      //NetworkObject with CoinNetwork script attached
    [SerializeField] private LayerMask platformMask;        //layer mask for the platforms

    //declare range of random movement for the spawner;
    //both horizontal and vertical
    public const float leftAndRightEdge = 23f;
    public const float upAndDownEdge = 11f;

    [SerializeField] private float spawnMinDelay = 2f;      //minimum delay between spawns
    [SerializeField] private float spawnMaxDelay = 7f;      //maximum delay between spawns

    public float speed = 10f;                   //speed of spawner's movement

    /*** PRIVATE VARIABLES ***/
    private float xVelocity;
    private float yVelocity;

    private float coinRadius;

    private bool canSpawn;                      //can the spawner spawn a coin right now?

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        xVelocity = speed;
        yVelocity = speed;

        coinRadius = coinPrefab.GetComponent<Renderer>().bounds.size.x * 0.5f;

        //Debug.Log("Coin radius: " + coinRadius);
        //Invoke(nameof(SpawnCoin), 3f);
    }

    //OnNetworkSpawn()
    //OnNetworkDespawn()
    //OnDestroy()

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;
        Invoke(nameof(SpawnCoin), 3f);
    }

    public override void OnNetworkDespawn()
    {
        CancelInvoke(nameof(SpawnCoin));
        base.OnNetworkDespawn();
    }

    public override void OnDestroy()
    {
        CancelInvoke(nameof(SpawnCoin));
        base.OnDestroy();
    }

    void SpawnCoin()
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return;
        
        canSpawn = false;

        //check if the spawner can spawn a coin at its current position
        //(i.e. it is not colliding with the platform)
        while (!canSpawn)
        {
            if (!IsServer) return;     //only the server should spawn coins

            //Random.Range(float min, float max) => returns a random float
            //number between min [inclusive] and max [inclusive]

            Vector3 spawnPos = new Vector3(
                Random.Range(-leftAndRightEdge, leftAndRightEdge),
                Random.Range(-upAndDownEdge, upAndDownEdge),
                0f
            );

            //Physics.OverlapSphere(Vector3 position, float radius, int layerMask) => returns an array of all colliders
            //that are touching or inside the sphere defined by position and radius, and that are on the layer defined by layerMask
            Collider[] hits = Physics.OverlapSphere(spawnPos, coinRadius, platformMask);
            
            //if the position is colliding with any platforms at all, do not spawn a coin there
            if (hits.Length > 0) continue;

            canSpawn = true;                                                        //set canSpawn to true to exit the loop
            var coin = Instantiate(coinPrefab, spawnPos, Quaternion.identity);      //instantiate a new coin from the prefab
            coin.Spawn();

            /* Object.Instantiate(Object original, Vector3 position, Quaternion rotation) => Clones original and returns an instance of the object
             *     original: the object you want to make a copy of
             *     position: position for the new object
             *     rotation: orientation of the new object (default: Quaternion.identity - no rotation)
             */
        }

        Invoke(nameof(SpawnCoin), Random.Range(spawnMinDelay, spawnMaxDelay));
    }
}
