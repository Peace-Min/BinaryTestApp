using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryTestApp.Interface
{
    public interface IMarshalSerialziable
    {
        byte[] Serialize();

        void Deserialize(byte[] data);
    }
}
