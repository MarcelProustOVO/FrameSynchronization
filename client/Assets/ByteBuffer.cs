using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

//IDisposable接口可以在不再使用时显式地释放资源，而不必依赖垃圾回收器的自动回收
public class ByteBuffer : IDisposable
{
    private byte[] m_Bytes = new byte[0];

    public byte[] mBytes
    {
        get { return m_Bytes; }
    }

    //private int position = 0;

    private int m_EffectiveHead = 0;

    public int mEffectiveHead
    {
        get { return m_EffectiveHead; }
    }

    public int m_RemainSize
    {
        get { return m_Bytes.Length - m_EffectiveHead; }
    }


    public byte[] mGetbuffer() { return m_Source; }

    public byte[] m_Source
    {
        get { return m_Bytes; }
        set
        {
            m_Bytes = value;
        }
    }


    public ByteBuffer()
    {

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="effectiveHead">有效数据开头位置</param>
    public ByteBuffer(byte[] value, int effectiveHead = 0)
    {
        m_Source = value;
        m_EffectiveHead = effectiveHead;
    }
    public void SetEffectiveHead(int count)
    {
        m_EffectiveHead += count;
    }
    //向字节缓冲区写入一个 16 位有符号整数short
    //返回 this即当前的 ByteBuffer 对象，以支持方法链式调用。
    public ByteBuffer WriteInt16(short value)
    {
        copy(BitConverter.GetBytes(value));
        return this;
    }

    public ByteBuffer WriteInt32(int value, bool after = true)
    {
        copy(BitConverter.GetBytes(value), after);
        return this;
    }

    public int ReadInt32()
    {
        byte[] key = get(4);
        if (key == null)
            return -1;
        return BitConverter.ToInt32(key, 0);
    }

    public void Clear()
    {
        m_Bytes = new byte[m_Bytes.Length];
        m_EffectiveHead = 0;
    }

    public byte[] ReadByte(int length)
    {
        byte[] key = get(length);
        return key;
    }

    public ByteBuffer WriteUInt32(uint value)
    {
        copy(BitConverter.GetBytes(value));
        return this;
    }

    public ByteBuffer WriteUInt16(ushort value)
    {
        copy(BitConverter.GetBytes(value));
        return this;
    }

    public ByteBuffer WriteComplexCollection<T>(System.Collections.Generic.List<T> collection, Func<T, byte[]> forEach)
    {
        int count = collection.Count;
        WriteInt32(count);
        for (int i = 0; i < count; ++i)
        {
            WriteBytes(forEach(collection[i]));
        }
        return this;
    }
    public ByteBuffer WriteSimpleCollection<T>(System.Collections.Generic.List<T> collection, Action<T> forEach)
    {
        int count = collection.Count;
        WriteInt32(count);
        for (int i = 0; i < count; ++i)
        {
            forEach(collection[i]);
        }
        return this;
    }

    public ByteBuffer WirteBuffer(ByteBuffer bf)
    {

        byte[] temps = new byte[m_Bytes.Length + bf.m_EffectiveHead];

        Buffer.BlockCopy(m_Bytes, 0, temps, 0, m_Bytes.Length);
        Buffer.BlockCopy(bf.mGetbuffer(), 0, temps, m_EffectiveHead, bf.m_EffectiveHead);

        m_Bytes = temps;
        m_EffectiveHead += bf.m_EffectiveHead;
        return this;
    }

    //public System.Collections.Generic.List<T> ReadSimpleCollection<T>(Func<T> forEach)
    //{
    //    System.Collections.Generic.List<T> collection = new System.Collections.Generic.List<T>();
    //    int count = ReadInt32();
    //    for (int i = 0; i < count; ++i)
    //    {
    //        collection.Add(forEach());
    //    }
    //    return collection;
    //}
    //public System.Collections.Generic.List<T> ReadComplexCollection<T>(Func<byte[], T> forEach)
    //{
    //    System.Collections.Generic.List<T> collection = new System.Collections.Generic.List<T>();
    //    int count = ReadInt32();
    //    for (int i = 0; i < count; ++i)
    //    {
    //        collection.Add(forEach(ReadBytes()));
    //    }

    //    return collection;
    //}
    public ByteBuffer WriteString(string value)
    {
        byte[] data = Encoding.UTF8.GetBytes(value);
        WriteInt32(data.Length);
        copy(data);
        return this;
    }

    //不需要写入长度
    public ByteBuffer WriteBytesNoLength(byte[] value, bool after = true)
    {
        if (value.Length == 0)
            return this;

        if (after)
        {
            copy(value);
        }
        else
        {
            copy(value, false);
        }
        return this;
    }

    //需要写入长度
    public ByteBuffer WriteBytes(byte[] value, bool after = true)
    {
        if (value.Length == 0)
            return this;

        if (after)
        {
            WriteInt32(value.Length);
            copy(value);
        }
        else
        {
            copy(value, false);
            WriteInt32(value.Length, false);
        }
        return this;
    }


    public ByteBuffer WriteByte(byte value)
    {
        copy(new byte[] { value });
        return this;
    }

    public ByteBuffer WriteBool(bool value)
    {
        WriteByte(Convert.ToByte(value));
        return this;
    }

    public ByteBuffer WriteInt64(long value)
    {
        copy(BitConverter.GetBytes(value));
        return this;
    }

    public ByteBuffer WriteUInt64(ulong value)
    {
        copy(BitConverter.GetBytes(value));
        return this;
    }

    public ByteBuffer WriteFloat(float value)
    {
        copy(BitConverter.GetBytes(value));
        return this;
    }


    private void copy(byte[] value, bool after = true)
    {
        byte[] temps = new byte[m_Bytes.Length + value.Length];
        if (after)
        {
            Buffer.BlockCopy(m_Bytes, 0, temps, 0, m_Bytes.Length);
            Buffer.BlockCopy(value, 0, temps, m_EffectiveHead, value.Length);
        }
        else
        {
            Buffer.BlockCopy(value, 0, temps, 0, value.Length);
            Buffer.BlockCopy(m_Bytes, 0, temps, value.Length, m_Bytes.Length);
        }
        m_EffectiveHead += value.Length;
        m_Bytes = temps;
    }
    private byte[] get(int length)
    {
        if (m_EffectiveHead < length)
            return null;

        byte[] data = new byte[length];
        Buffer.BlockCopy(m_Bytes, 0, data, 0, length);
        Buffer.BlockCopy(m_Bytes, length, m_Bytes, 0, m_Bytes.Length - length);
        m_EffectiveHead -= length;
        return data;
    }
    public void Dispose()
    {
        //position = 0;
        m_EffectiveHead = 0;
        m_Bytes = null;
    }

    public void Destory()
    {
        m_Bytes = new byte[0];
        m_EffectiveHead = 0;
    }


    //压缩字节
    //1.创建压缩的数据流 
    //2.设定compressStream为存放被压缩的文件流,并设定为压缩模式
    //3.将需要压缩的字节写到被压缩的文件流
    public static byte[] CompressBytes(byte[] bytes)
    {
        using (MemoryStream compressStream = new MemoryStream())
        {
            using (var zipStream = new GZipStream(compressStream, CompressionMode.Compress))
                zipStream.Write(bytes, 0, bytes.Length);
            return compressStream.ToArray();
        }
    }
    //解压缩字节
    //1.创建被压缩的数据流
    //2.创建zipStream对象，并传入解压的文件流
    //3.创建目标流
    //4.zipStream拷贝到目标流
    //5.返回目标流输出字节
    public static byte[] Decompress(byte[] bytes)
    {
        using (var compressStream = new MemoryStream(bytes))
        {
            using (var zipStream = new GZipStream(compressStream, CompressionMode.Decompress))
            {
                using (var resultStream = new MemoryStream())
                {
                    zipStream.CopyTo(resultStream);
                    return resultStream.ToArray();
                }
            }
        }
    }

}
