#region Copyright (c) 2002-2005, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net)
/*
Copyright (c) 2002-2005, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net)
All rights reserved.

Redistribution and use in source and binary forms, with or without 
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, 
this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright 
notice, this list of conditions and the following disclaimer in the 
documentation and/or other materials provided with the distribution.
* Neither the names of Bas Geertsema or Xih Solutions nor the names of its 
contributors may be used to endorse or promote products derived 
from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
THE POSSIBILITY OF SUCH DAMAGE. */
#endregion

using System;
using System.IO;
using MSNPSharp.Core;
using MSNPSharp;

namespace MSNPSharp.DataTransfer
{
	public enum P2PFlag
	{
		Normal = 0,
		Acknowledgement = 0x02,
		WaitingReply = 0x04,
		BinaryError = 0x08,
		MSNObject = 0x20,
		FileData = 0x01000030
	}
	

	[Serializable()]
	public class P2PMessage : NetworkMessage
	{
		uint sessionId = 0;
		uint identifier = 0;
		ulong offset = 0;
		ulong totalSize = 0;
		uint messageSize = 0;
		uint flags = 0;
		uint dw1;
		uint dw2 = 0;
		ulong qw1;
		uint footer = 0;

		public P2PMessage()
		{

		}
		
		public uint  SessionId
		{
			get { 
				return sessionId; 
			}
			set { 
				sessionId = value;
			}
		}

		public uint  Identifier
		{
			get { 
				return identifier; 
			}
			set { 
				identifier = value;
			}
		}

		public ulong Offset
		{
			get { 
				return offset; 
			}
			set { 
				offset = value;
			}
		}

		public ulong TotalSize
		{
			get { 
				return totalSize; 
			}
			set { 
				totalSize = value;
			}
		}

		public uint MessageSize
		{
			get { 
				return messageSize; 
			}
			set { 
				messageSize = value;
			}
		}

		public uint  Flags
		{
			get { 
				return flags; 
			}
			set { 
				flags = value;
			}
		}

		public uint DW1
		{
			get { 
				return dw1;
			}
			set { 
				dw1 = value;
			}
		}

		public uint DW2
		{
			get {
				return dw2;
			}
			set { 
				dw2 = value;
			}
		}

		public ulong QW1
		{
			get { 
				return qw1;
			}
			set { 
				qw1 = value;
			}
		}

		public uint  Footer
		{
			get { 
				return footer; 
			}
			set { 
				footer = value;
			}
		}

		public bool IsAcknowledgement
		{
			get { 
				return Flags == (uint) P2PFlag.Acknowledgement;
			}
		}

		public virtual P2PMessage CreateAcknowledgement()
		{
			P2PMessage ack = new P2PMessage();
					
			ack.TotalSize = TotalSize;
			ack.Flags = (uint) P2PFlag.Acknowledgement;

			if ((Flags & (uint) P2PFlag.WaitingReply) != 0)
				ack.Flags = 0x06;
			
			ack.DW1 = Identifier;
			ack.DW2 = DW1;
			ack.QW1 = TotalSize;
			
			return ack;
		}	

		public override string ToString()
		{			
			string debugLine =
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "SessionId     : {1:x} ({0})\r\n", SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture), SessionId) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "Identifier    : {1:x} ({0})\r\n", Identifier.ToString(System.Globalization.CultureInfo.InvariantCulture), Identifier) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "Offset        : {1:x} ({0})\r\n", Offset.ToString(System.Globalization.CultureInfo.InvariantCulture), Offset) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "TotalSize     : {1:x} ({0})\r\n", TotalSize.ToString(System.Globalization.CultureInfo.InvariantCulture), TotalSize) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "MessageSize   : {1:x} ({0})\r\n", MessageSize.ToString(System.Globalization.CultureInfo.InvariantCulture), MessageSize) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "Flags         : {1:x} ({0})\r\n", Flags.ToString(System.Globalization.CultureInfo.InvariantCulture), Flags) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "DW1	: {1:x} ({0})\r\n", DW1.ToString(System.Globalization.CultureInfo.InvariantCulture), DW1) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "DW2 : {1:x} ({0})\r\n", DW2.ToString(System.Globalization.CultureInfo.InvariantCulture), DW2) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "QW1  : {1:x} ({1})\r\n", QW1.ToString(System.Globalization.CultureInfo.InvariantCulture), QW1) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "Footer        : {1:x} ({1})\r\n", Footer.ToString(System.Globalization.CultureInfo.InvariantCulture), Footer);
			return "[P2PMessage]\r\n" + debugLine;
		}


		#region Protected helper methods
		/// Flips the bytes of the unsigned integer.
		protected uint FlipEndian(uint number)
		{
			return (uint)  (((number & 0x000000ff) << 24) +
				((number & 0x0000ff00) << 8) +
				((number & 0x00ff0000) >> 8) +
				((number & 0xff000000) >> 24));
		}

		/// Flips the bytes of the unsigned long.
		protected ulong FlipEndian(ulong number)
		{
			return (ulong) 
				(((number & 0x00000000000000ff) << 56) +
				((number & 0x000000000000ff00) << 40) +
				((number & 0x0000000000ff0000) << 24) +
				((number & 0x00000000ff000000) <<  8) +
				((number & 0x000000ff00000000) >>  8) +
				((number & 0x0000ff0000000000) >> 24) +
				((number & 0x00ff000000000000) >> 40) +
				((number & 0xff00000000000000) >> 56));			
		}
		#endregion

		
		/// Sets the D as acknowledgement in the ParentMessage.ParentMessage. This should be a SBMessage object.
		public override void PrepareMessage()
		{
			base.PrepareMessage ();
			if(ParentMessage != null && ParentMessage is MSGMessage)
				if(ParentMessage.ParentMessage != null && ParentMessage.ParentMessage is SBMessage)
					((SBMessage)ParentMessage.ParentMessage).Acknowledgement = "D";
		}

		/// Parses the given message.
		public override void ParseBytes(byte[] data)
		{								
			Stream memStream = new System.IO.MemoryStream(data);
			BinaryReader reader = new System.IO.BinaryReader(memStream);

			if(BitConverter.IsLittleEndian == false)
			{
				SessionId		= FlipEndian(reader.ReadUInt32());
				Identifier		= FlipEndian(reader.ReadUInt32());
				Offset			= FlipEndian(reader.ReadUInt64());
				TotalSize		= FlipEndian(reader.ReadUInt64());
				MessageSize		= FlipEndian(reader.ReadUInt32());
				Flags			= FlipEndian(reader.ReadUInt32());
				DW1	= FlipEndian(reader.ReadUInt32());
				DW2	= FlipEndian(reader.ReadUInt32());
				QW1	= FlipEndian(reader.ReadUInt64());
			}
			else
			{
				SessionId		= reader.ReadUInt32();
				Identifier		= reader.ReadUInt32();
				Offset			= reader.ReadUInt64();
				TotalSize		= reader.ReadUInt64();
				MessageSize		= reader.ReadUInt32();
				Flags			= reader.ReadUInt32();
				DW1	= reader.ReadUInt32();
				DW2	= reader.ReadUInt32();
				QW1	= reader.ReadUInt64();
			}

			// now move to the footer while reading the message contents
			InnerBody = new byte[MessageSize];
			memStream.Read(InnerBody, 0, (int)MessageSize);

			// this is in little-endian
			if(BitConverter.IsLittleEndian == false)
				Footer = reader.ReadUInt32();
			else
				Footer = FlipEndian(reader.ReadUInt32());
			
			// clean up
			reader.Close();
			memStream.Close();			
		}
		
		/// Returns the inner message as a byte array.
		protected virtual byte[] GetInnerBytes()
		{
			// if there is a message we contain get the contents
			if(InnerMessage != null)
				return InnerMessage.GetBytes();
			else if(InnerBody != null)
				return InnerBody;
			else
				return new byte[0];
		}

		/// Creates a P2P Message. This sets the MessageSize properly.
		public override byte[] GetBytes()
		{
			// get the inner contents and set the message size
			byte[] innerBytes = new byte[0];
			
			innerBytes = GetInnerBytes();

			MessageSize = (uint)innerBytes.Length;

			// if no total size is specified, then we assume this is the whole message.
			if(TotalSize == 0)
				TotalSize = MessageSize;

			// total size is header (48) + footer (4) + messagesize
			byte[] ppMessage = new byte[52 + MessageSize];

			Stream memStream = new System.IO.MemoryStream(ppMessage, true);
			BinaryWriter writer = new System.IO.BinaryWriter(memStream);

			if(BitConverter.IsLittleEndian == false)
			{
				writer.Write(FlipEndian(SessionId));
				writer.Write(FlipEndian(Identifier));
				writer.Write(FlipEndian(Offset));
				writer.Write(FlipEndian(TotalSize));
				writer.Write(FlipEndian(MessageSize));
				writer.Write(FlipEndian(Flags));
				writer.Write(FlipEndian(DW1));
				writer.Write(FlipEndian(DW2));
				writer.Write(FlipEndian(QW1));
			}
			else
			{
				writer.Write(SessionId);
				writer.Write(Identifier);
				writer.Write(Offset);
				writer.Write(TotalSize);
				writer.Write(MessageSize);
				writer.Write(Flags);
				writer.Write(DW1);
				writer.Write(DW2);
				writer.Write(QW1);
			}
			
			writer.Write(innerBytes);

			if(BitConverter.IsLittleEndian == false)
				writer.Write(Footer);
			else
				writer.Write(FlipEndian(Footer));

			// clean up
			writer.Close();
			memStream.Close();

			// return the total message
			return ppMessage;
		}
	}




	/// <summary>
	/// Represents a single P2PDataMessage which is used for the actual data transfer. No negotiation handling.
	/// </summary>
	/// <remarks>
	/// A p2p data message can be identified by looking at the footer in the P2P Message. When this value is > 0 a
	/// data message is send. When this value is 0 a normal, and more complex, MSNSLPMessage is send.
	/// This class is created to provide a fast way of sending messages.
	/// </remarks>
	[Serializable()]
	public class P2PDataMessage : P2PMessage
	{	
		/// <summary>
		/// Constructs a P2P data message.
		/// </summary>
		public P2PDataMessage()
		{
			Footer = 1;			
		}

		/// <summary>
		/// Writes 4 nul-bytes in the inner body. This message can then be used as a data preparation message.
		/// </summary>
		public void WritePreparationBytes()
		{
			InnerBody = new byte[4] { 0, 0, 0, 0 };
		}

		/// <summary>
		/// Writes data in the inner message buffer.
		/// </summary>
		/// <param name="ioStream"></param>
		/// <param name="maxLength"></param>
		public int WriteBytes(Stream ioStream, int maxLength)
		{
			long readLength = Math.Min(maxLength, ioStream.Length - ioStream.Position);

			InnerBody = new byte[readLength];
			return ioStream.Read(InnerBody, 0, (int)readLength);
		}


		/// <summary>
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			string debugLine =
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "SessionId     : {1:x} ({0})\r\n", SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture), SessionId) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "Identifier    : {1:x} ({0})\r\n", Identifier.ToString(System.Globalization.CultureInfo.InvariantCulture), Identifier) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "Offset        : {1:x} ({0})\r\n", Offset.ToString(System.Globalization.CultureInfo.InvariantCulture), Offset) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "TotalSize     : {1:x} ({0})\r\n", TotalSize.ToString(System.Globalization.CultureInfo.InvariantCulture), TotalSize) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "MessageSize   : {1:x} ({0})\r\n", MessageSize.ToString(System.Globalization.CultureInfo.InvariantCulture), MessageSize) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "Flags         : {1:x} ({0})\r\n", Flags.ToString(System.Globalization.CultureInfo.InvariantCulture), Flags) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "DW1	: {1:x} ({0})\r\n", DW1.ToString(System.Globalization.CultureInfo.InvariantCulture), DW1) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "DW2 : {1:x} ({0})\r\n", DW2.ToString(System.Globalization.CultureInfo.InvariantCulture), DW2) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "QW1  : {1:x} ({1})\r\n", QW1.ToString(System.Globalization.CultureInfo.InvariantCulture), QW1) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "Footer        : {1:x} ({1})\r\n", Footer.ToString(System.Globalization.CultureInfo.InvariantCulture), Footer);
			return "[P2PDataMessage]\r\n" + debugLine;
		}

	}


	/// <summary>
	/// A P2P Message which is send in a direct-connection.
	/// </summary>
	/// <remarks>The innerbody contents are used as message contents (data). The InnerMessage object is ignored.</remarks>
	[Serializable()]
	public class P2PDCMessage : P2PDataMessage
	{
		/// <summary>
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			string debugLine =
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "SessionId     : {1:x} ({0})\r\n", SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture), SessionId) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "Identifier    : {1:x} ({0})\r\n", Identifier.ToString(System.Globalization.CultureInfo.InvariantCulture), Identifier) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "Offset        : {1:x} ({0})\r\n", Offset.ToString(System.Globalization.CultureInfo.InvariantCulture), Offset) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "TotalSize     : {1:x} ({0})\r\n", TotalSize.ToString(System.Globalization.CultureInfo.InvariantCulture), TotalSize) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "MessageSize   : {1:x} ({0})\r\n", MessageSize.ToString(System.Globalization.CultureInfo.InvariantCulture), MessageSize) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "Flags         : {1:x} ({0})\r\n", Flags.ToString(System.Globalization.CultureInfo.InvariantCulture), Flags) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "DW1	: {1:x} ({0})\r\n", DW1.ToString(System.Globalization.CultureInfo.InvariantCulture), DW1) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "DW2 : {1:x} ({0})\r\n", DW2.ToString(System.Globalization.CultureInfo.InvariantCulture), DW2) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "QW1  : {1:x} ({1})\r\n", QW1.ToString(System.Globalization.CultureInfo.InvariantCulture), QW1) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "Footer        : {1:x} ({1})\r\n", Footer.ToString(System.Globalization.CultureInfo.InvariantCulture), Footer);
			return "[P2PDCMessage]\r\n" + debugLine;
		}

		
		/// <summary>
		/// Basic constructor.
		/// </summary>
		public P2PDCMessage()
		{
		}

		/// <summary>
		/// Copy constructor. Creates a shallow copy of the properties of the P2PMessage.
		/// </summary>
		/// <param name="message"></param>
		public P2PDCMessage(P2PMessage message)
		{
			SessionId		= message.SessionId;
			Identifier		= message.Identifier;
			Offset			= message.Offset;
			TotalSize		= message.TotalSize;
			MessageSize		= message.MessageSize;
			Flags			= message.Flags;
			DW1	= message.DW1;
			DW2	= message.DW2;
			QW1	= message.QW1;
			InnerMessage	= message.InnerMessage;
			InnerBody		= message.InnerBody;
		}

		/// <summary>
		/// Writes no footer, but a 4 byte length size in front of the header.
		/// </summary>
		/// <returns></returns>
		public override byte[] GetBytes()
		{
			// get the inner contents and set the message size
			byte[] innerBytes = null;
						
			innerBytes = GetInnerBytes();

			// check for valid inner data
			if(innerBytes == null)
				innerBytes = new byte[0];

			MessageSize = (uint)innerBytes.Length;

			// if no total size is specified, then we assume this is the whole message.
			if(TotalSize == 0)
				TotalSize = MessageSize;

			// total size is size(4) + header (48) + messagesize
			byte[] ppMessage = new byte[52 + MessageSize];

			Stream memStream = new System.IO.MemoryStream(ppMessage, true);
			BinaryWriter writer = new System.IO.BinaryWriter(memStream);

			
			if(BitConverter.IsLittleEndian == false)
			{
				writer.Write(FlipEndian((uint)(48 + MessageSize)));
				writer.Write(FlipEndian(SessionId));
				writer.Write(FlipEndian(Identifier));
				writer.Write(FlipEndian(Offset));
				writer.Write(FlipEndian(TotalSize));
				writer.Write(FlipEndian(MessageSize));
				writer.Write(FlipEndian(Flags));
				writer.Write(FlipEndian(DW1));
				writer.Write(FlipEndian(DW2));
				writer.Write(FlipEndian(QW1));
			}
			else
			{
				writer.Write((uint)(48 + MessageSize));
				writer.Write(SessionId);
				writer.Write(Identifier);
				writer.Write(Offset);
				writer.Write(TotalSize);
				writer.Write(MessageSize);
				writer.Write(Flags);
				writer.Write(DW1);
				writer.Write(DW2);
				writer.Write(QW1);
			}
			
			writer.Write(innerBytes);			

			// clean up
			writer.Close();
			memStream.Close();

			// return the total message
			return ppMessage;
		}


		/// <summary>
		/// Parses a data message without the 4-byte length header and without a 4 byte footer.
		/// </summary>
		/// <param name="data"></param>
		public override void ParseBytes(byte[] data)
		{
			Stream memStream = new System.IO.MemoryStream(data);
			BinaryReader reader = new System.IO.BinaryReader(memStream);			

			if(BitConverter.IsLittleEndian == false)
			{
				SessionId		= FlipEndian(reader.ReadUInt32());
				Identifier		= FlipEndian(reader.ReadUInt32());
				Offset			= FlipEndian(reader.ReadUInt64());
				TotalSize		= FlipEndian(reader.ReadUInt64());
				MessageSize		= FlipEndian(reader.ReadUInt32());
				Flags			= FlipEndian(reader.ReadUInt32());
				DW1	= FlipEndian(reader.ReadUInt32());
				DW2	= FlipEndian(reader.ReadUInt32());
				QW1	= FlipEndian(reader.ReadUInt64());
			}
			else
			{
				SessionId		= reader.ReadUInt32();
				Identifier		= reader.ReadUInt32();
				Offset			= reader.ReadUInt64();
				TotalSize		= reader.ReadUInt64();
				MessageSize		= reader.ReadUInt32();
				Flags			= reader.ReadUInt32();
				DW1	= reader.ReadUInt32();
				DW2	= reader.ReadUInt32();
				QW1	= reader.ReadUInt64();
			}

			// now read the message contents
			InnerBody = new byte[MessageSize];
			memStream.Read(InnerBody, 0, (int)MessageSize);

			// there is no footer
			
			// clean up
			reader.Close();
			memStream.Close();		
		}

	}

	/// <summary>
	/// A P2P Message which is send in a direct-connection.
	/// </summary>
	/// <remarks>The innerbody contents are used as message contents (data). The InnerMessage object is ignored.</remarks>
	[Serializable()]
	public class P2PDCHandshakeMessage : P2PDCMessage
	{
		private Guid	guid;

		/// <summary>
		/// Basic constructor
		/// </summary>
		/// <remarks>
		/// Defaults the Flags property to 0x100
		/// </remarks>
		public P2PDCHandshakeMessage()
		{
			Flags = 0x100;
		}

		/// <summary>
		/// Copy constructor. Creates a shallow copy of the properties of the P2PMessage.
		/// </summary>
		/// <param name="message"></param>
		public P2PDCHandshakeMessage(P2PMessage message)
			: base(message)
		{
			Guid = new Guid(
				(int)message.DW1,
				(short)(message.DW2 & 0x0000FFFF),
				(short)((message.DW2 & 0xFFFF0000) >> 16),								
				(byte)((message.QW1 & 0x00000000000000FF)),
				(byte)((message.QW1 & 0x000000000000FF00) >>  8),
				(byte)((message.QW1 & 0x0000000000FF0000) >> 16),
				(byte)((message.QW1 & 0x00000000FF000000) >> 24),								
				(byte)((message.QW1 & 0x000000FF00000000) >> 32),
				(byte)((message.QW1 & 0x0000FF0000000000) >> 40),
				(byte)((message.QW1 & 0x00FF000000000000) >> 48),
				(byte)((message.QW1 & 0xFF00000000000000) >> 56));							
		}

		/// <summary>
		/// Creates an acknowledgement message to a handshake message. This will only set the flag to 0 and
		/// </summary>
		/// <returns></returns>
		public override P2PMessage CreateAcknowledgement()
		{						
			// create a copy of this message
			P2PDCHandshakeMessage ackMessage = new P2PDCHandshakeMessage(this);

			// set the identifier to 0 to set our own local identifier
			ackMessage.Identifier = 0;						
			return ackMessage;
		}	

		/// <summary>
		/// The Guid to use in the handshake message.
		/// </summary>
		public Guid	Guid
		{
			get { return guid; }
			set 
			{ 
				guid = value;
			}
		}

		
		/// <summary>
		/// Writes no footer, but a 4 byte length size in front of the header.
		/// </summary>
		/// <returns></returns>
		public override byte[] GetBytes()
		{						

			// first get the bytes for the handshake
			byte[] handshakeMessage = base.GetBytes();

			byte[] totalMessage = new byte[handshakeMessage.Length + 8];
			byte[] fooMessage = new byte[]{ 0x04, 0x00, 0x00, 0x00, 0x66, 0x6f, 0x6f, 0x00 };
			byte[] guidMessage = guid.ToByteArray();
			Array.Copy(fooMessage, 0, totalMessage, 0, 8);
			Array.Copy(handshakeMessage, 0, totalMessage, 8, handshakeMessage.Length);
			Array.Copy(guidMessage, 0, totalMessage, totalMessage.Length - 16, 16);

			// return the total message
			return totalMessage;
		}

		/// <summary>
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			string debugLine =
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "SessionId    : {1:x} ({0})\r\n", SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture), SessionId) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "Identifier   : {1:x} ({0})\r\n", Identifier.ToString(System.Globalization.CultureInfo.InvariantCulture), Identifier) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "Offset       : {1:x} ({0})\r\n", Offset.ToString(System.Globalization.CultureInfo.InvariantCulture), Offset) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "TotalSize    : {1:x} ({0})\r\n", TotalSize.ToString(System.Globalization.CultureInfo.InvariantCulture), TotalSize) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "MessageSize  : {1:x} ({0})\r\n", MessageSize.ToString(System.Globalization.CultureInfo.InvariantCulture), MessageSize) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "Flags        : {1:x} ({0})\r\n", Flags.ToString(System.Globalization.CultureInfo.InvariantCulture), Flags) +
				String.Format(System.Globalization.CultureInfo.InvariantCulture, "Guid         : {0}\r\n", this.Guid.ToString());				
			return "[P2PDCHandshakeMessage]\r\n" + debugLine;
		}
	}
}
