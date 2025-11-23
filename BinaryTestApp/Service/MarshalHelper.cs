using BinaryTestApp.Interface;
using BinaryTestApp.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BinaryTestApp.Service
{
    public static class MarshalHelper
    {

        public static byte[] ToBytes<T>(T strct) where T : struct, IMarshalSerialziable

        {

            var size = Marshal.SizeOf<T>();

            var buffer = new byte[size];



            var ptr = Marshal.AllocHGlobal(size);

            try

            {

                Marshal.StructureToPtr(strct, ptr, false);

                Marshal.Copy(ptr, buffer, 0, size);

            }

            finally

            {

                Marshal.FreeHGlobal(ptr);

            }

            return buffer;

        }



        public static void FromBytes<T>(ref T target, byte[] data) where T : struct, IMarshalSerialziable

        {

            var size = Marshal.SizeOf<T>();

            if (data.Length < size)

                throw new ArgumentException($"Invalid data length for {typeof(T).Name}");



            var ptr = Marshal.AllocHGlobal(size);

            try

            {

                Marshal.Copy(data, 0, ptr, size);

                target = (T)Marshal.PtrToStructure(ptr, typeof(T));

            }

            finally

            {

                Marshal.FreeHGlobal(ptr);

            }

        }



        public static T DeepCopy<T>(T source) where T : struct, IMarshalSerialziable

        {

            // 1. Serialize.

            var bytes = source.Serialize();



            // 2. Deserialize into new instance.

            var clone = default(T);

            clone.Deserialize(bytes);



            // 3. return Deep-Copied object.

            return clone;

        }

    }
}
