using System.Collections.Generic;
using UnityEngine;

namespace SpaceshipGame
{
    public class SpaceshipController : MonoBehaviour, ISpaceship
    {
        [SerializeField] uint objectID = 0;

        public float Speed { get; set; }

        Queue<InputPacket> inputsSent = new Queue<InputPacket>(MaxQueuSize);

        const int MaxQueuSize = 20;
        
        void FixedUpdate()
        {
            float horizontalInput = Input.GetAxis("Horizontal");

            if (horizontalInput != 0f)
            {
                Vector3 inputVector = PredictMovement(horizontalInput);
                transform.Translate(inputVector);
                SendInputDataToServer(inputVector);
            }
        }

        Vector3 PredictMovement(float horizontalInput)
        {
            return (transform.right * horizontalInput * Speed * Time.fixedDeltaTime);
        }

        void SendInputDataToServer(Vector3 movementVector)
        {
            float[] movement = { movementVector.x, movementVector.y, movementVector.z };
            InputPacket inputPacket = new InputPacket();
            InputData inputData;

            inputData.movement = movement;
            inputPacket.Payload = inputData; 

            PacketsManager.Instance.SendPacket(inputPacket, null, UdpNetworkManager.Instance.GetSenderID(), objectID, reliable: true);
        }
    }
}