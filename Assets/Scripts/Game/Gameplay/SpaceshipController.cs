using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceshipGame
{
    public struct SequencedInputPacket
    {
        public InputPacket inputPacket;
        public uint inputSequenceID;
    }

    public class SpaceshipController : MonoBehaviour, ISpaceship
    {
        [SerializeField] uint objectID = 0;

        public float Speed { get; set; }

        List<SequencedInputPacket> inputsSent = new List<SequencedInputPacket>(MaxSequenceListSize);
        uint currentInputSequenceID = 0;

        const int MaxSequenceListSize = 20;

        void Start()
        {
            PacketsManager.Instance.AddUserPacketListener(objectID, OnDataReceived);
        }
        
        void FixedUpdate()
        {
            float horizontalInput = Input.GetAxis("Horizontal");

            if (horizontalInput != 0f)
            {
                Vector3 inputVector = transform.right * horizontalInput * Speed * Time.fixedDeltaTime;
                transform.Translate(inputVector);
                SendInputDataToServer(inputVector);
            }
        }

        void ReconcilePosition(Vector3 serverPosition, Vector3 accumulatedInput)
        {
            transform.position = serverPosition + accumulatedInput;
        }

        void SendInputDataToServer(Vector3 movementVector)
        {
            float[] movement = { movementVector.x, movementVector.y, movementVector.z };
            SequencedInputPacket queueableInputPacket;
            InputPacket inputPacket = new InputPacket();
            InputData inputData;

            inputData.movement = movement;
            inputPacket.Payload = inputData; 
            queueableInputPacket.inputPacket = inputPacket;
            queueableInputPacket.inputSequenceID = currentInputSequenceID++;

            inputsSent.Add(queueableInputPacket);
            PacketsManager.Instance.SendPacket(inputPacket, null, UdpNetworkManager.Instance.GetSenderID(), objectID, reliable: true);
        }

        void OnDataReceived(ushort userPacketTypeIndex, uint senderID, Stream stream)
        {
            if (userPacketTypeIndex != (ushort)UserPacketType.Transform)
                return;

            TransformPacket transformPacket = new TransformPacket();

            transformPacket.Deserialize(stream);

            int lastSequenceIDProcessedByServer = inputsSent.FindIndex(sip => sip.inputSequenceID == transformPacket.Payload.inputSequenceID);

            if (lastSequenceIDProcessedByServer != -1)
            {
                float[] positionReceived = transformPacket.Payload.position;
                Vector3 serverAuthoritativePosition = new Vector3(positionReceived[0], positionReceived[1], positionReceived[2]);
                Vector3 accumulatedInput = Vector3.zero;

                for (int i = lastSequenceIDProcessedByServer + 1; i < inputsSent.Count; i++)
                {
                    float[] movement = inputsSent[i].inputPacket.Payload.movement;
                    Vector3 movementVector = new Vector3(movement[0], movement[1], movement[2]);
                    
                    accumulatedInput += movementVector;
                }
                
                inputsSent.RemoveAt(lastSequenceIDProcessedByServer);
                ReconcilePosition(serverAuthoritativePosition, accumulatedInput);
            }
        }
    }
}