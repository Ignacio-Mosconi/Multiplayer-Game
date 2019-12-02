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
        [SerializeField, Range(0.01f, 1f)] float serverTimeStep = 0.01f;

        Dictionary<uint, EnemySpaceshipInputData> inputsByClientID = new Dictionary<uint, EnemySpaceshipInputData>();
        Dictionary<uint, EnemySpaceship> enemySpaceshipsByClientID = new Dictionary<uint, EnemySpaceship>();
        Dictionary<uint, EnemySpaceshipTransformData> otherClientsTransformsByID = new Dictionary<uint, EnemySpaceshipTransformData>();
        SpaceshipController spaceshipController;
        float serverTimer = 0f;

        public const int PlayerCount = 2;
        const float NeglibiglePosDiffSqrMagnitude = 0.1f;

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

            enemySpaceshipsByClientID.Add(clientID,enemySpaceship);

            PacketsManager.Instance.AddUserPacketListener(enemySpaceship.ObjectID, OnDataReceived);
        }

        void AddSpaceshipToClient(uint clientID)
        {
            if (otherClientsTransformsByID.ContainsKey(clientID))
                return;

            int playerIndex = (spaceshipController.ObjectID == 0) ? 1 : 0;

            EnemySpaceship enemySpaceship = CreateSpaceship(enemySpaceshipPrefabs[playerIndex], 
                                                            playerSpawnPoints[playerIndex]) as EnemySpaceship;
            EnemySpaceshipTransformData enemySpaceshipTransformData;

            enemySpaceshipTransformData.spaceship = enemySpaceship;
            enemySpaceshipTransformData.previousTransform = null;
            enemySpaceshipTransformData.lastTransform = null;

            TransformPacket transformPacket = new TransformPacket();
            TransformData transformData;

            transformData.flags = (int)TransformFlag.None;
            transformData.inputSequenceID = 0;
            transformData.position = null;
            transformData.rotation = null;
            transformData.scale = null;
            transformPacket.Payload = transformData;

            PacketsManager.Instance.SendPacket(transformPacket, 
                                                null, 
                                                UdpNetworkManager.Instance.GetSenderID(), 
                                                spaceshipController.ObjectID, 
                                                reliable: true);

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
            if (userPacketTypeIndex == (ushort)UserPacketType.Input)
            {
                InputPacket inputPacket = new InputPacket();

                inputPacket.Deserialize(stream);
            
                inputsByClientID[senderID].inputs.Add(inputPacket);
            }

            if (userPacketTypeIndex == (ushort)UserPacketType.ShotInput)
            {
                ShotInputPacket shotInputPacket = new ShotInputPacket();

                shotInputPacket.Deserialize(stream);

            foreach (EnemySpaceship enemySpaceship in enemySpaceshipsByClientID.Values)
            {
               Vector3 hitPos = new Vector3(shotInputPacket.Payload.hitPosition[0], shotInputPacket.Payload.hitPosition[1], shotInputPacket.Payload.hitPosition[2]);
                if (enemySpaceship.transform.position == hitPos)
                {
                    NotificationPacket notificationPacket = new NotificationPacket();
                    notificationPacket.Deserialize(stream);
                   // PacketsManager.Instance.SendPacket(notificationPacket, null,senderID,0,true);
                }
            }
            }           
        }
        
        void OnDataReceivedByClient(ushort userPacketTypeIndex, uint senderID, Stream stream)
        {
            if (userPacketTypeIndex != (ushort)UserPacketType.Transform || senderID == UdpNetworkManager.Instance.GetSenderID())
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
                otherClientsTransformsByID[senderID] = enemySpaceshipTransformData;
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
                        PacketsManager.Instance.SendPacket(transformPacket, null, dicIterator.Current.Key, enemySpaceship.ObjectID);
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
                        float[] lastPositionValues = enemySpaceshipTransformData.lastTransform.Payload.position;
                        float[] previousPositionValues = enemySpaceshipTransformData.previousTransform.Payload.position;
                        Vector3 lastPosition = new Vector3(lastPositionValues[0], lastPositionValues[1], lastPositionValues[2]);
                        Vector3 previousPosition = new Vector3(previousPositionValues[0], previousPositionValues[1], previousPositionValues[2]);
                        
                        if ((lastPosition - enemySpaceship.transform.position).sqrMagnitude > NeglibiglePosDiffSqrMagnitude)
                        {
                            Vector3 direction = (lastPosition - previousPosition).normalized;
                            enemySpaceship.Move(direction);
                        }
                    }
                }
        }

        public void StartGame(uint clientsInSession = 0)
        {
            if (UdpNetworkManager.Instance.IsServer)
                UdpConnectionManager.Instance.OnClientAddedByServer += AddSpaceshipToServer;
            else
            {
                int playerIndex;
                int enemyIndex;
                
                if (clientsInSession == 1)
                {
                    playerIndex = 0;
                    enemyIndex = 1;
                }
                else
                {
                    enemyIndex = 0;
                    playerIndex = 1;
                }

                UdpConnectionManager.Instance.OnOtherClientJoined += AddSpaceshipToClient;
                spaceshipController = CreateSpaceship(playerSpaceshipPrefabs[playerIndex], 
                                                        playerSpawnPoints[playerIndex]) as SpaceshipController;

                PacketsManager.Instance.AddUserPacketListener(spaceshipController.ObjectID, OnDataReceived);
                PacketsManager.Instance.AddUserPacketListener((uint)enemyIndex, OnDataReceived);
            }
        }
    } 
}