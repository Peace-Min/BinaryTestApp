using BinaryTestApp.Interface;
using BinaryTestApp.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BinaryTestApp.Model
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MessageHeader : IMarshalSerialziable
    {
        /// <summary>
        /// DateTimeOffset.UtoNow.ToUnixTimeSeconds(). 메소드 사용하여 송신함.
        /// </summary>
        public UInt32 ReceiveTime;

        public byte[] Serialize() => MarshalHelper.ToBytes(this);

        public void Deserialize(byte[] data) => MarshalHelper.FromBytes(ref this, data);
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MsgModel : IMarshalSerialziable
    {
        
        public MessageHeader Header;

        public byte Flag;

        public MsgSubModel SubModel;

        public byte[] Serialize()=>MarshalHelper.ToBytes(this);

        public void Deserialize(byte[] data) => MarshalHelper.FromBytes(ref this, data);
     
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MsgSubModel : IMarshalSerialziable
    {
        public byte STS;
        public byte SAS;
        public byte SCS;
        public byte[] Serialize() => MarshalHelper.ToBytes(this);

        public void Deserialize(byte[] data) => MarshalHelper.FromBytes(ref this, data);
    }
   
}
