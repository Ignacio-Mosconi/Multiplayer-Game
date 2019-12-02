using System.IO;
using System.Collections.Generic;
using UnityEngine;


namespace SpaceshipGame
{
    public class SpaceshipController : MonoBehaviour, ISpaceship
    {
        [SerializeField] uint objectID = 0;

        public float Speed { get; set; }

        List<InputPacket> inputsSent = new List<InputPacket>();

        uint currentInputSequenceID = 0;

        void Start()
        {
            PacketsManager.Instance.AddUserPacketListener(objectID, OnDataReceived);
        }
        
        void FixedUpdate()
        {
            float horizontalInput = Input.GetAxis("Horizontal");

            if (horizontalInput != 0f)
            {
                Vector3 inputVector = transform.right * horizontalInput;

                PredictMovement(inputVector);
                SendInputDataToServer(inputVector);
            }

           if (Input.GetButton("Fire1"))
           {
                RaycastHit2D hitInfo = Physics2D.Raycast(this.transform.position,Vector2.up);
                ///
                Gizmos.color = Color.green;
                Gizmos.DrawLine(this.transform.position,Vector3.up);
                ///
                //Una de las opciones es utilizar un GameObject donde colisiona el rayo.
                if (hitInfo.collider.tag == "Enemy")
                {
                    SendShotInputDataToServer(hitInfo.transform.position);
                }     

           }

               

        }

        void PredictMovement(Vector3 inputVector)
        {
            transform.Translate(inputVector * Speed * Time.fixedDeltaTime);
        }

        void ReconcilePosition(Vector3 serverPosition, Vector3 accumulatedMovement)
        {
            transform.position = serverPosition + accumulatedMovement;
        }

        void SendInputDataToServer(Vector3 movementVector)
        {
            float[] movement = { movementVector.x, movementVector.y, movementVector.z };
            InputPacket inputPacket = new InputPacket();
            InputData inputData;

            inputData.sequenceID = currentInputSequenceID++;
            inputData.movement = movement;
            inputPacket.Payload = inputData; 

            inputsSent.Add(inputPacket);
            PacketsManager.Instance.SendPacket(inputPacket, null, UdpNetworkManager.Instance.GetSenderID(), objectID, reliable: true);
        }
        void SendShotInputDataToServer(Vector3 hitPosition)
        { 
            float[] position = {hitPosition.x,hitPosition.y,hitPosition.z};
            ShotInputPacket shotInputPacket = new ShotInputPacket();
            ShotInputData shotInputData;
            shotInputData.hitPosition = position;
            PacketsManager.Instance.SendPacket(shotInputPacket, null, UdpNetworkManager.Instance.GetSenderID(), objectID, reliable: true);
        }

        void OnDataReceived(ushort userPacketTypeIndex, uint senderID, Stream stream)
        {
            if (userPacketTypeIndex != (ushort)UserPacketType.Transform || senderID != UdpNetworkManager.Instance.GetSenderID())
                return;

            TransformPacket transformPacket = new TransformPacket();

            transformPacket.Deserialize(stream);

            int lastSequenceIDProcessedByServer = inputsSent.FindIndex(ip => ip.Payload.sequenceID == transformPacket.Payload.inputSequenceID);

            if (lastSequenceIDProcessedByServer != -1)
            {
                float[] positionReceived = transformPacket.Payload.position;
                Vector3 serverAuthoritativePosition = new Vector3(positionReceived[0], positionReceived[1], positionReceived[2]);
                Vector3 accumulatedMovement = Vector3.zero;

                for (int i = lastSequenceIDProcessedByServer + 1; i < inputsSent.Count; i++)
                {
                    float[] movement = inputsSent[i].Payload.movement;
                    Vector3 movementVector = new Vector3(movement[0], movement[1], movement[2]);
                    
                    accumulatedMovement += movementVector * Speed * Time.fixedDeltaTime;
                }
                
                inputsSent.RemoveRange(0, lastSequenceIDProcessedByServer);
                ReconcilePosition(serverAuthoritativePosition, accumulatedMovement);
            }
        }

        public uint ObjectID
        {
            get { return objectID; }
        }
    }
}