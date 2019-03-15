using System;
using System.Collections.Generic;
using System.Text;

namespace NEL.Simple.SDK
{
    public class LevelDbOperation
    {
        public byte tableid;
        public byte[] key;
        public byte[] value;
        public byte state;

        public static LevelDbOperation Deserialize(ref System.IO.BinaryReader stream)
        {
            LevelDbOperation levelDbOperation = new LevelDbOperation();
            //tableid
            levelDbOperation.tableid =(byte)stream.ReadByte();
            //key
            var isnull = stream.ReadByte();
            if (isnull != 1)
                levelDbOperation.key = null;
            else
            {

                byte[] bytes_keyL = new byte[4];
                stream.Read(bytes_keyL,0,4);
                var keyL = BitConverter.ToInt32(bytes_keyL,0);
                byte[] bytes_key = new byte[keyL];
                stream.Read(bytes_key,0, keyL);
                levelDbOperation.key = bytes_key;
            }

            //value
            isnull = stream.ReadByte();
            if (isnull != 1)
                levelDbOperation.value = null;
            else
            {
                byte[] bytes_valueL = new byte[4];
                stream.Read(bytes_valueL,0,4);
                var valueL = BitConverter.ToInt32(bytes_valueL,0);
                byte[] bytes_value = new byte[valueL];
                stream.Read(bytes_value,0, valueL);
                levelDbOperation.value = bytes_value;
            }

            //state
            levelDbOperation.state = (byte)stream.ReadByte();

            ////height
            //byte[] bytes_height = new byte[4];
            //stream.Read(bytes_height,0,4);
            //levelDbOperation.height = (ulong)BitConverter.ToInt32(bytes_height,0);

            return levelDbOperation;
        }

        public static byte[] Serialize(LevelDbOperation levelDbOperation)
        {
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                // xie tableid
                ms.WriteByte(levelDbOperation.tableid);


                // 写 key
                if (levelDbOperation.key != null)
                {
                    ms.WriteByte(1);
                    byte[] bytes_key = levelDbOperation.key;
                    byte[] bytes_keyL = BitConverter.GetBytes(bytes_key.Length);
                    ms.Write(bytes_keyL,0,4);
                    ms.Write(bytes_key,0, bytes_key.Length);
                }
                else
                {
                    ms.WriteByte(0);
                }


                //写 value
                if (levelDbOperation.value != null)
                {
                    ms.WriteByte(1);
                    byte[] bytes_value = levelDbOperation.value;
                    byte[] bytes_valueL = BitConverter.GetBytes(bytes_value.Length);
                    ms.Write(bytes_valueL, 0, 4);
                    ms.Write(bytes_value, 0, bytes_value.Length);
                }
                else
                {
                    ms.WriteByte(0);
                }

                //state
                ms.WriteByte(levelDbOperation.state);

                ////写blockheight
                //byte[] bytes_height = BitConverter.GetBytes(levelDbOperation.height);
                //ms.Write(bytes_height,0,4);

                return ms.ToArray();
            }
        }
    }
}
