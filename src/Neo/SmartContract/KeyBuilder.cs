// Copyright (C) 2021-2024 The EpicChain Lab's.
//
// KeyBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System;
using System.Buffers.Binary;
using System.IO;

namespace Neo.SmartContract
{
    /// <summary>
    /// Used to build storage keys for native contracts.
    /// </summary>
    public class KeyBuilder
    {
        private readonly MemoryStream stream = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyBuilder"/> class.
        /// </summary>
        /// <param name="id">The id of the contract.</param>
        /// <param name="prefix">The prefix of the key.</param>
        public KeyBuilder(int id, byte prefix)
        {
            var data = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(data, id);

            stream.Write(data);
            stream.WriteByte(prefix);
        }

        /// <summary>
        /// Adds part of the key to the builder.
        /// </summary>
        /// <param name="key">Part of the key.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        public KeyBuilder Add(byte key)
        {
            stream.WriteByte(key);
            return this;
        }

        /// <summary>
        /// Adds part of the key to the builder.
        /// </summary>
        /// <param name="key">Part of the key.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        public KeyBuilder Add(ReadOnlySpan<byte> key)
        {
            stream.Write(key);
            return this;
        }

        /// <summary>
        /// Adds part of the key to the builder.
        /// </summary>
        /// <param name="key">Part of the key.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        public KeyBuilder Add(ISerializable key)
        {
            using (BinaryWriter writer = new(stream, Utility.StrictUTF8, true))
            {
                key.Serialize(writer);
                writer.Flush();
            }
            return this;
        }

        /// <summary>
        /// Adds part of the key to the builder in BigEndian.
        /// </summary>
        /// <param name="key">Part of the key.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        public KeyBuilder AddBigEndian(int key)
        {
            var data = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(data, key);

            return Add(data);
        }

        /// <summary>
        /// Adds part of the key to the builder in BigEndian.
        /// </summary>
        /// <param name="key">Part of the key.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        public KeyBuilder AddBigEndian(uint key)
        {
            var data = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32BigEndian(data, key);

            return Add(data);
        }

        /// <summary>
        /// Adds part of the key to the builder in BigEndian.
        /// </summary>
        /// <param name="key">Part of the key.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        public KeyBuilder AddBigEndian(long key)
        {
            var data = new byte[sizeof(long)];
            BinaryPrimitives.WriteInt64BigEndian(data, key);

            return Add(data);
        }

        /// <summary>
        /// Adds part of the key to the builder in BigEndian.
        /// </summary>
        /// <param name="key">Part of the key.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        public KeyBuilder AddBigEndian(ulong key)
        {
            var data = new byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64BigEndian(data, key);

            return Add(data);
        }

        /// <summary>
        /// Gets the storage key generated by the builder.
        /// </summary>
        /// <returns>The storage key.</returns>
        public byte[] ToArray()
        {
            using (stream)
            {
                return stream.ToArray();
            }
        }

        public static implicit operator StorageKey(KeyBuilder builder)
        {
            using (builder.stream)
            {
                return new StorageKey(builder.stream.ToArray());
            }
        }
    }
}
