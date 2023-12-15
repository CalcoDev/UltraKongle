using System;
using System.Collections.Generic;
using System.Text;

namespace KongleJam.Networking;

public class Packet
{
    private readonly List<byte> _buffer = new();
    private int _readOffset = 0;

    public Packet()
    {
    }

    public Packet(IEnumerable<byte> bytes)
    {
        _buffer.AddRange(bytes);
    }

    public byte[] ToByteArray()
    {
        return _buffer.ToArray();
    }

    public void ResetRead()
    {
        _readOffset = 0;
    }

    #region Read

    public byte ReadByte()
    {
        if (_buffer.Count > _readOffset)
        {
            byte value = _buffer[_readOffset];
            _readOffset++;
            return value;
        }

        throw new InvalidOperationException("Not enough data to read a byte.");
    }

    public int ReadInt()
    {
        if (_buffer.Count >= _readOffset + sizeof(int))
        {
            int value = BitConverter.ToInt32(_buffer.ToArray(), _readOffset);
            _readOffset += sizeof(int);
            return value;
        }

        throw new InvalidOperationException("Not enough data to read an int.");
    }

    public float ReadFloat()
    {
        if (_buffer.Count >= _readOffset + sizeof(float))
        {
            float value = BitConverter.ToSingle(_buffer.ToArray(), _readOffset);
            _readOffset += sizeof(float);
            return value;
        }

        throw new InvalidOperationException("Not enough data to read a float.");
    }

    public string ReadString()
    {
        int strLength = ReadInt();
        if (_buffer.Count >= _readOffset + strLength)
        {
            string value = Encoding.UTF8.GetString(_buffer.ToArray(), _readOffset, strLength);
            _readOffset += strLength;
            return value;
        }

        throw new InvalidOperationException("Not enough data to read a string.");
    }

    #endregion

    #region Write

    public void WriteByte(byte value)
    {
        _buffer.Add(value);
    }

    public void WriteInt(int value)
    {
        _buffer.AddRange(BitConverter.GetBytes(value));
    }

    public void WriteFloat(float value)
    {
        _buffer.AddRange(BitConverter.GetBytes(value));
    }

    public void WriteString(string value)
    {
        WriteInt(value.Length);
        _buffer.AddRange(Encoding.ASCII.GetBytes(value));
    }

    #endregion
}