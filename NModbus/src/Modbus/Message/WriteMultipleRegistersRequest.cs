using System;
using System.Globalization;
using System.Linq;
using System.Net;
using Modbus.Data;
using Unme.Common;
using System.Diagnostics;
using System.IO;

namespace Modbus.Message
{
	class WriteMultipleRegistersRequest : ModbusMessageWithData<RegisterCollection>, IModbusRequest
	{
		private const int _minimumFrameSize = 7;

		public WriteMultipleRegistersRequest()
		{
		}

		public WriteMultipleRegistersRequest(byte slaveAddress, ushort startAddress, RegisterCollection data)
			: base(slaveAddress, Modbus.WriteMultipleRegisters)
		{
			StartAddress = startAddress;
			NumberOfPoints = (ushort) data.Count;
			ByteCount = (byte) (data.Count * 2);
			Data = data;
		}

		public byte ByteCount
		{
			get { return MessageImpl.ByteCount; }
			set { MessageImpl.ByteCount = value; }
		}

		public ushort NumberOfPoints
		{
			get
			{
				return MessageImpl.NumberOfPoints;
			}
			set
			{
				if (value > Modbus.MaximumRegisterRequestResponseSize)
					throw new ArgumentOutOfRangeException("NumberOfPoints", String.Format(CultureInfo.InvariantCulture, "Maximum amount of data {0} registers.", Modbus.MaximumRegisterRequestResponseSize));

				MessageImpl.NumberOfPoints = value;
			}
		}
		
		public ushort StartAddress
		{
			get { return MessageImpl.StartAddress; }
			set { MessageImpl.StartAddress = value; }
		}

		public override int MinimumFrameSize
		{
			get { return _minimumFrameSize; }
		}

		public override string ToString()
		{
			return String.Format(CultureInfo.InvariantCulture, "Write {0} holding registers starting at address {1}.", NumberOfPoints, StartAddress);
		}

        public void ValidateResponse(IModbusMessage response)
        {
            var typedResponse = (WriteMultipleRegistersResponse) response;

            if (StartAddress != typedResponse.StartAddress)
            {
                throw new IOException(String.Format(CultureInfo.InvariantCulture,
                    "Unexpected start address in response. Expected {0}, received {1}.", StartAddress, typedResponse.StartAddress));
            }

            if (NumberOfPoints != typedResponse.NumberOfPoints)
            {
                throw new IOException(String.Format(CultureInfo.InvariantCulture,
                    "Unexpected number of points in response. Expected {0}, received {1}.", NumberOfPoints, typedResponse.NumberOfPoints));
            }
        }

		protected override void InitializeUnique(byte[] frame)
		{
			if (frame.Length < _minimumFrameSize + frame[6])
				throw new FormatException("Message frame does not contain enough bytes.");

			StartAddress = (ushort) IPAddress.NetworkToHostOrder(BitConverter.ToInt16(frame, 2));
			NumberOfPoints = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(frame, 4));
			ByteCount = frame[6];
			Data = new RegisterCollection(frame.Slice(7, ByteCount).ToArray());
		}
	}
}
