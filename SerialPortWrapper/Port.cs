using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;

namespace BetterSerial
{
    /// <summary>
    /// A Wrapper for <see cref="SerialPort"/> that only exposes the members listed as safe by 
    /// Ben Voigt here: https://www.sparxeng.com/blog/software/must-use-net-system-io-ports-serialport
    /// </summary>
    public sealed class Port
    {
        private readonly SerialPort basePort;

        #region Port ID
        ///<inheritdoc cref="SerialPort.PortName"/>
        public string PortName { get => basePort.PortName; }

        ///<inheritdoc cref="SerialPort.IsOpen"/>
        public bool IsOpen { get => basePort.IsOpen; }
        #endregion

        #region Control Lines
        ///<inheritdoc cref="SerialPort.CDHolding"/>
        public bool CDHolding { get => basePort.CDHolding; }

        ///<inheritdoc cref="SerialPort.CtsHolding"/>
        public bool CtsHolding { get => basePort.CtsHolding; }

        ///<inheritdoc cref="SerialPort.DsrHolding"/>
        public bool DsrHolding { get => basePort.DsrHolding; }

        ///<inheritdoc cref="SerialPort.DtrEnable"/>
        public bool DtrEnable
        {
            get => basePort.DtrEnable;
            set => basePort.DtrEnable = value;
        }

        ///<inheritdoc cref="SerialPort.RtsEnable"/>
        public bool RtsEnable
        {
            get => basePort.RtsEnable;
            set => basePort.RtsEnable = value;
        }

        #endregion

        #region Serial Modes
        ///<inheritdoc cref="SerialPort.Handshake"/>
        public Handshake Handshake
        {
            get => basePort.Handshake;
            set =>
                basePort.Handshake =
                    !IsOpen ? value : throw new InvalidOperationException();
        }

        ///<inheritdoc cref="SerialPort.BaudRate"/>
        public int BaudRate
        {
            get => basePort.BaudRate;
            set =>
                basePort.BaudRate =
                    !IsOpen ? value : throw new InvalidOperationException();
        }

        ///<inheritdoc cref="SerialPort.DataBits"/>
        public int DataBits
        {
            get => basePort.DataBits;
            set =>
                basePort.DataBits =
                    !IsOpen ? value : throw new InvalidOperationException();
        }

        ///<inheritdoc cref="SerialPort.Parity"/>
        public Parity Parity
        {
            get => basePort.Parity;
            set =>
                basePort.Parity =
                    !IsOpen ? value : throw new InvalidOperationException();
        }

        ///<inheritdoc cref="SerialPort.StopBits"/>
        public StopBits StopBits
        {
            get => basePort.StopBits;
            set =>
                basePort.StopBits =
                    !IsOpen ? value : throw new InvalidOperationException();
        }
        #endregion

        /// <summary>
        /// Create a new <see cref="Port"/> for a specific Port Name
        /// </summary>
        /// <param name="name">The name of the port (the same as would be used for <see cref="SerialPort.PortName"/>)</param>
        /// <param name="createSerial">Delegate for converting <paramref name="createSerial"/> to a <see cref="SerialPort"/>.
        /// Default is to use the <see cref="SerialPort"/> constructor.</param>
        public Port(string name, Func<string, SerialPort>? createSerial = default)
        {
            basePort = createSerial?.Invoke(name) ?? new SerialPort(name);
        }

        /// <summary>
        /// Begin communication over the port
        /// </summary>
        /// <returns>A bidirectional stream representing the serial connection.</returns>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="IOException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public Stream Open()
        {
            if(!IsOpen)
            {
                basePort.Open();
                return basePort.BaseStream;
            }

            throw new InvalidOperationException("Port is already open");
        }

        /// <summary>
        /// Close the port
        /// </summary>
        public void Close()
        {
            if(IsOpen)
            {
                basePort.Close();
            }
        }
    }
}
