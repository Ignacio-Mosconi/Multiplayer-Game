using System.Collections.Generic;
using UnityEngine;

namespace SpaceshipGame
{
    public class EnemySpaceship : MonoBehaviour, ISpaceship
    {
        [SerializeField] uint objectID = 0;
        
        public float Speed { get; set; }

        public void Move(Vector3 accumulatedInputs)
        {
            transform.Translate(accumulatedInputs * Speed * Time.fixedDeltaTime);
        }

        public uint ObjectID
        {
            get { return objectID; }
        }
    }
}