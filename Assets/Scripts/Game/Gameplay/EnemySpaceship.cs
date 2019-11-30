using System.Collections.Generic;
using UnityEngine;

namespace SpaceshipGame
{
    public class EnemySpaceship : MonoBehaviour, ISpaceship
    {
        public float Speed { get; set; }

        public void Move(Vector3 translationVector)
        {
            transform.Translate(translationVector);
        }
    }
}