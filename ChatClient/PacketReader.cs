using System;
using System.Linq;

namespace ChatClient
{
    [System.Diagnostics.DebuggerDisplay("{Value}")]
    public sealed class Packet : IDisposable
    {
        private string[] _params;
        int position;

#if DEBUG
        public string Value { get { return this.ToString(); } }
#endif

        public static implicit operator Packet(string pack)
        {
            return new Packet(pack);
        }

        public static explicit operator string(Packet us)
        {
            return String.Join("|", us._params);
        }

        public Packet(string[] _params)
        {
            this._params = _params;
            position = -1;
        }

        public Packet(string packet) : this(packet.Split('|')) { }

        public string Header
        {
            get { return this._params[0]; }
        }

        public int ReadInt()
        {
            return int.Parse(_params[++position]);
        }

        public byte ReadByte()
        {
            return byte.Parse(_params[++position]);
        }

        public sbyte ReadSByte()
        {
            return sbyte.Parse(_params[++position]);
        }

        public short ReadShort()
        {
            return short.Parse(_params[++position]);
        }

        public uint ReadUInt32()
        {
            return uint.Parse(_params[++position]);
        }

        public ulong ReadULong()
        {
            return ulong.Parse(_params[++position]);
        }

        public ushort ReadUShort()
        {
            return ushort.Parse(_params[++position]);
        }

        public string ReadString()
        {
            return _params[++position];
        }
		
        public void Seek(int count)
        {
            this.position += count;
        }

        public bool ReadBoolean()
        {
            var nxt = _params[++position];
            return nxt.ToLower().Equals("true") || nxt.Equals("1");
        }

        public object[] ReadAllLeft ()
        {
            return this._params.Skip(position + 1).ToArray();
        }

        public bool MoreToRead
        {
            get { return (_params.Length > (position + 1)); }
        }

        public void Dispose()
        {
            this._params = null;
        }
        public override string ToString()
        {
            return (string)this;
        }
    }
}