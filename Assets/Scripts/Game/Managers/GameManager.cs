using System.Collections.Generic;
using UnityEngine;

namespace SpaceshipGame
{
    public class GameManager : MonoBehaviourSingleton<GameManager>
    {
        [Header("Spaceships' Prefbs")]
        [SerializeField] GameObject playerSpaceshipPrefab = default;
        [SerializeField] GameObject enemySpaceshipPrefab = default;

        [Header("Spaceships' Properties")]
        [SerializeField] Vector3 playerSpawnPoint = default;
        [SerializeField] Vector3 enemySpawnPoint = default;
        [SerializeField, Range(5f, 20f)] float spaceshipsSpeed = 10f;

        public const int PlayerCount = 2;

        void Start()
        {
            if (UdpNetworkManager.Instance.IsServer)
                UdpConnectionManager.Instance.OnClientAddedByServer += (id) => AddSpaceshipToServer();
            else
                UdpConnectionManager.Instance.OnOtherClientJoined += (id) => CreateSpaceship(enemySpaceshipPrefab, enemySpawnPoint);
        }

        int GetCurrentClientCount()
        {
            return UdpConnectionManager.Instance.ClientsIDs.Count;
        }

        void CreateSpaceship(GameObject prefab, Vector3 spawnPoint)
        {
            ISpaceship spaceship = Instantiate(prefab, spawnPoint, Quaternion.identity).transform as ISpaceship;
            spaceship.Speed = spaceshipsSpeed;
        }

        void AddSpaceshipToServer()
        {
            Vector3 spawnPoint = (GetCurrentClientCount() == 0) ? playerSpawnPoint : enemySpawnPoint;
            CreateSpaceship(enemySpaceshipPrefab, spawnPoint);
        }

        public void StartGame()
        {
            if (UdpNetworkManager.Instance.IsServer)
                return;
            
            CreateSpaceship(playerSpaceshipPrefab, playerSpawnPoint);
            if (GetCurrentClientCount() > 1)
                CreateSpaceship(enemySpaceshipPrefab, enemySpawnPoint);
        }
    } 
}