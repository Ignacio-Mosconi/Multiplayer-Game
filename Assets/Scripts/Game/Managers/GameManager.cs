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
        public TransformPacket previousTransform;
        public TransformPacket lastTransform;
    }

    public class GameManager : MonoBehaviourSingleton<GameManager>
    {
        [Header("Spaceships' Prefabs")]
        [SerializeField] GameObject[] playerSpaceshipPrefabs = new GameObject[PlayerCount];
        [SerializeField] GameObject[] enemySpaceshipPrefabs = new GameObject[PlayerCount];

        [Header("Spaceships' Properties")]
        [SerializeField] Vector3[] playerSpawnPoints = new Vector3[PlayerCount];
        [SerializeField, Range(5f, 20f)] float spaceshipsSpeed = 10f;
        [SerializeField, Range(0.01f, 1f)] float serverTimeStep = 0.1f;

        Dictionary<uint, EnemySpaceshipInputData> inputsByClientID = new Dictionary<uint, EnemySpaceshipInputData>();
        Dictionary<uint, EnemySpaceshipTransformData> otherClientsTransformsByID = new Dictionary<uint, EnemySpaceshipTransformData>();
        float serverTimer = 0f;
        int localPlayerIndex; 

        public const int PlayerCount = 2;

        void FixedUpdate()
        {
            if (UdpNetworkManager.Instance.IsServer)
            {
                serverTimer += Time.fixedDeltaTime;
                if (serverTimer >= serverTimeStep)
                {
                    UpdateServerClients();
                    serverTimer -= serverTimeStep;
                }
            }
            else
                UpdateOtherClients();
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
                spawnPoint = playerSpawnPoints[0];
                enemySpaceship = CreateSpaceship(enemySpaceshipPrefabs[0], spawnPoint) as EnemySpaceship;
            }
            else
            {
                spawnPoint = playerSpawnPoints[1];
                enemySpaceship = CreateSpaceship(enemySpaceshipPrefabs[1], spawnPoint) as EnemySpaceship;
            }

            EnemySpaceshipInputData enemySpaceshipInputData;

            enemySpaceshipInputData.spaceship = enemySpaceship;
            enemySpaceshipInputData.inputs = new List<InputPacket>();
            
            inputsByClientID.Add(clientID, enemySpaceshipInputData);
        }

        void AddSpaceshipToClient(uint clientID)
        {
            if (otherClientsTransformsByID.ContainsKey(clientID))
                return;

            int playerIndex = (localPlayerIndex == 0) ? 1 : 0;

            EnemySpaceship enemySpaceship = CreateSpaceship(enemySpaceshipPrefabs[playerIndex], playerSpawnPoints[playerIndex]) as EnemySpaceship;
            EnemySpaceshipTransformData enemySpaceshipTransformData;

            enemySpaceshipTransformData.spaceship = enemySpaceship;
            enemySpaceshipTransformData.previousTransform = null;
            enemySpaceshipTransformData.lastTransform = null;

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
            if (userPacketTypeIndex != (ushort)UserPacketType.Transform || senderID == UdpConnectionManager.Instance.ClientID)
                return;

            AddSpaceshipToClient(senderID);

            TransformPacket transformPacket = new TransformPacket();

            transformPacket.Deserialize(stream);
            
            EnemySpaceshipTransformData enemySpaceshipTransformData = otherClientsTransformsByID[senderID];

            if (enemySpaceshipTransformData.lastTransform == null || 
                transformPacket.Payload.inputSequenceID > enemySpaceshipTransformData.lastTransform.Payload.inputSequenceID)
            {
                enemySpaceshipTransformData.previousTransform = enemySpaceshipTransformData.lastTransform;
                enemySpaceshipTransformData.lastTransform = transformPacket;
            }
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
                        PacketsManager.Instance.SendPacket(transformPacket, UdpConnectionManager.Instance.GetClientIP(dicIterator.Current.Key));
                    }
                }
        }

        void UpdateOtherClients()
        {
            using (var dicIterator = otherClientsTransformsByID.GetEnumerator())
                while (dicIterator.MoveNext())
                {
                    EnemySpaceshipTransformData enemySpaceshipTransformData = dicIterator.Current.Value;
                    
                    if (enemySpaceshipTransformData.previousTransform != null)
                    {
                        EnemySpaceship enemySpaceship = enemySpaceshipTransformData.spaceship;
                        Vector3 lastPosition = new Vector3(enemySpaceshipTransformData.lastTransform.Payload.position[0],
                                                            enemySpaceshipTransformData.lastTransform.Payload.position[1],
                                                            enemySpaceshipTransformData.lastTransform.Payload.position[2]);
                        Vector3 previousPosition = new Vector3(enemySpaceshipTransformData.previousTransform.Payload.position[0],
                                                                enemySpaceshipTransformData.previousTransform.Payload.position[1],
                                                                enemySpaceshipTransformData.previousTransform.Payload.position[2]);
                        Vector3 direction = (lastPosition - previousPosition).normalized;

                        enemySpaceship.Move(direction);
                    }
                }
        }

        public void StartGame(uint clientsInSession = 0)
        {
            if (UdpNetworkManager.Instance.IsServer)
                UdpConnectionManager.Instance.OnClientAddedByServer += AddSpaceshipToServer;
            else
            {
                localPlayerIndex = (clientsInSession == 1) ? 0 : 1;
                UdpConnectionManager.Instance.OnOtherClientJoined += AddSpaceshipToClient;
                CreateSpaceship(playerSpaceshipPrefabs[localPlayerIndex], playerSpawnPoints[localPlayerIndex]);
            }

            PacketsManager.Instance.AddUserPacketListener(0, OnDataReceived);
        }
    } 
}