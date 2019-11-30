using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceshipGame
{
    public struct EnemySpaceshipInputData
    {
        public EnemySpaceship spaceship;
        public List<InputPacket> inputs;
    }

    public struct EnemySpaceshipTransformData
    {
        public EnemySpaceship spaceship;
        public List<TransformPacket> transforms;
    }

    public class GameManager : MonoBehaviourSingleton<GameManager>
    {
        [Header("Spaceships' Prefabs")]
        [SerializeField] GameObject playerSpaceshipPrefab = default;
        [SerializeField] GameObject[] enemySpaceshipPrefabs = new GameObject[PlayerCount];

        [Header("Spaceships' Properties")]
        [SerializeField] Vector3 playerSpawnPoint = default;
        [SerializeField] Vector3 enemySpawnPoint = default;
        [SerializeField, Range(5f, 20f)] float spaceshipsSpeed = 10f;

        Dictionary<uint, EnemySpaceshipInputData> inputsByClientID = new Dictionary<uint, EnemySpaceshipInputData>();
        Dictionary<uint, EnemySpaceshipTransformData> otherClientsTransformsByID = new Dictionary<uint, EnemySpaceshipTransformData>();

        public const int PlayerCount = 2;

        void FixedUpdate()
        {
            if (UdpNetworkManager.Instance.IsServer)
                UpdateServerClients();
        }

        int GetCurrentClientCount()
        {
            return UdpConnectionManager.Instance.ClientsIDs.Count;
        }

        ISpaceship CreateSpaceship(GameObject prefab, Vector3 spawnPoint)
        {
            ISpaceship spaceship = Instantiate(prefab, spawnPoint, Quaternion.identity).GetComponent(typeof(ISpaceship)) as ISpaceship;
            spaceship.Speed = spaceshipsSpeed;

            return spaceship;
        }

        void AddSpaceshipToServer(uint clientID)
        {
            Vector3 spawnPoint;
            EnemySpaceship enemySpaceship;
            int clientCount = GetCurrentClientCount();

            if (clientCount == 1)
            {
                spawnPoint = playerSpawnPoint;
                enemySpaceship = CreateSpaceship(enemySpaceshipPrefabs[0], spawnPoint) as EnemySpaceship;
            }
            else
            {
                spawnPoint = enemySpawnPoint;
                enemySpaceship = CreateSpaceship(enemySpaceshipPrefabs[1], spawnPoint) as EnemySpaceship;
            }

            EnemySpaceshipInputData enemySpaceshipInputData;

            enemySpaceshipInputData.spaceship = enemySpaceship;
            enemySpaceshipInputData.inputs = new List<InputPacket>();
            
            inputsByClientID.Add(clientID, enemySpaceshipInputData);
        }

        void AddSpaceshipToClient(uint clientID)
        {
            EnemySpaceship enemySpaceship = CreateSpaceship(enemySpaceshipPrefabs[1], enemySpawnPoint) as EnemySpaceship;
            EnemySpaceshipTransformData enemySpaceshipTransformData;

            enemySpaceshipTransformData.spaceship = enemySpaceship;
            enemySpaceshipTransformData.transforms = new List<TransformPacket>();

            otherClientsTransformsByID.Add(clientID, enemySpaceshipTransformData);
        }

        void OnDataReceived(ushort userPacketTypeIndex, uint senderID, Stream stream)
        {
            if (UdpNetworkManager.Instance.IsServer)
                OnDataReceivedByServer(userPacketTypeIndex, senderID, stream);
            else
                OnDataReceivedByClient(userPacketTypeIndex, senderID, stream);
        }

        void OnDataReceivedByServer(ushort userPacketTypeIndex, uint senderID, Stream stream)
        {
            if (userPacketTypeIndex != (ushort)UserPacketType.Input)
                return;

            InputPacket inputPacket = new InputPacket();

            inputPacket.Deserialize(stream);
            
            inputsByClientID[senderID].inputs.Add(inputPacket);
        }
        
        void OnDataReceivedByClient(ushort userPacketTypeIndex, uint senderID, Stream stream)
        {

        }

        void UpdateServerClients()
        {
            using (var dicIterator = inputsByClientID.GetEnumerator())
                while (dicIterator.MoveNext())
                {
                    TransformPacket transformPacket = new TransformPacket();
                    TransformData transformData;
                    EnemySpaceship enemySpaceship = dicIterator.Current.Value.spaceship;
                    Vector3 accumulatedInputs = Vector3.zero;
                    List<InputPacket> inputs = dicIterator.Current.Value.inputs;

                    if (inputs.Count > 0)
                    {
                        for (int i = 0; i < inputs.Count; i++)
                        {
                            float[] movement = inputs[i].Payload.movement;
                            Vector3 inputVector = new Vector3(movement[0], movement[1], movement[2]);

                            accumulatedInputs += inputVector;
                        }

                        enemySpaceship.Move(accumulatedInputs);

                        float[] spaceshipPosition =
                        {
                            enemySpaceship.transform.position.x,
                            enemySpaceship.transform.position.y,
                            enemySpaceship.transform.position.z,
                        };

                        transformData.flags = (int)TransformFlag.PositionBit | (int)TransformFlag.InputSequenceIDBit;

                        transformData.inputSequenceID = inputs[inputs.Count - 1].Payload.sequenceID;
                        transformData.position = spaceshipPosition;
                        transformData.rotation = null;
                        transformData.scale = null;

                        transformPacket.Payload = transformData;

                        inputs.Clear();
                    }
                }
        }

        public void StartGame()
        {
            if (UdpNetworkManager.Instance.IsServer)
                UdpConnectionManager.Instance.OnClientAddedByServer += AddSpaceshipToServer;
            else
            {
                UdpConnectionManager.Instance.OnOtherClientJoined += AddSpaceshipToClient;
            
                CreateSpaceship(playerSpaceshipPrefab, playerSpawnPoint);
            }

            PacketsManager.Instance.AddUserPacketListener(0, OnDataReceived);
        }
    } 
}