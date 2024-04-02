// EpicChain Copyright Project (2021-2024)
// 
// Copyright (c) 2021-2024 EpicChain
// 
// EpicChain is an innovative blockchain network developed and maintained by xmoohad. This copyright project outlines the rights and responsibilities associated with the EpicChain software and its related components.
// 
// 1. Copyright Holder:
//    - xmoohad
// 
// 2. Project Name:
//    - EpicChain
// 
// 3. Project Description:
//    - EpicChain is a decentralized blockchain network that aims to revolutionize the way digital assets are managed, traded, and secured. With its innovative features and robust architecture, EpicChain provides a secure and efficient platform for various decentralized applications (dApps) and digital asset management.
// 
// 4. Copyright Period:
//    - The copyright for the EpicChain software and its related components is valid from 2021 to 2024.
// 
// 5. Copyright Statement:
//    - All rights reserved. No part of the EpicChain software or its related components may be reproduced, distributed, or transmitted in any form or by any means, without the prior written permission of the copyright holder, except in the case of brief quotations embodied in critical reviews and certain other noncommercial uses permitted by copyright law.
// 
// 6. License:
//    - The EpicChain software is licensed under the EpicChain Software License, a custom license that governs the use, distribution, and modification of the software. The EpicChain Software License is designed to promote the free and open development of the EpicChain network while protecting the interests of the copyright holder.
// 
// 7. Open Source:
//    - EpicChain is an open-source project, and its source code is available to the public under the terms of the EpicChain Software License. Developers are encouraged to contribute to the development of EpicChain and create innovative applications on top of the EpicChain network.
// 
// 8. Disclaimer:
//    - The EpicChain software and its related components are provided "as is," without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose, and noninfringement. In no event shall the copyright holder or contributors be liable for any claim, damages, or other liability, whether in an action of contract, tort, or otherwise, arising from, out of, or in connection with the EpicChain software or its related components.
// 
// 9. Contact Information:
//    - For inquiries regarding the EpicChain copyright project, please contact xmoohad at [email address].
// 
// 10. Updates:
//     - This copyright project may be updated or modified from time to time to reflect changes in the EpicChain project or to address new legal or regulatory requirements. Users and developers are encouraged to check the latest version of the copyright project periodically.

//
// LedgerContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#pragma warning disable IDE0051

using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// A native contract for storing all blocks and transactions.
    /// </summary>
    public sealed class LedgerContract : NativeContract
    {
        private const byte Prefix_BlockHash = 9;
        private const byte Prefix_CurrentBlock = 12;
        private const byte Prefix_Block = 5;
        private const byte Prefix_Transaction = 11;

        internal LedgerContract()
        {
        }

        internal override ContractTask OnPersist(ApplicationEngine engine)
        {
            TransactionState[] transactions = engine.PersistingBlock.Transactions.Select(p => new TransactionState
            {
                BlockIndex = engine.PersistingBlock.Index,
                Transaction = p,
                State = VMState.NONE
            }).ToArray();
            engine.Snapshot.Add(CreateStorageKey(Prefix_BlockHash).AddBigEndian(engine.PersistingBlock.Index), new StorageItem(engine.PersistingBlock.Hash.ToArray()));
            engine.Snapshot.Add(CreateStorageKey(Prefix_Block).Add(engine.PersistingBlock.Hash), new StorageItem(Trim(engine.PersistingBlock).ToArray()));
            foreach (TransactionState tx in transactions)
            {
                // It's possible that there are previously saved malicious conflict records for this transaction.
                // If so, then remove it and store the relevant transaction itself.
                engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_Transaction).Add(tx.Transaction.Hash), () => new StorageItem(new TransactionState())).FromReplica(new StorageItem(tx));

                // Store transaction's conflicits.
                var conflictingSigners = tx.Transaction.Signers.Select(s => s.Account);
                foreach (var attr in tx.Transaction.GetAttributes<Conflicts>())
                {
                    engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_Transaction).Add(attr.Hash), () => new StorageItem(new TransactionState())).FromReplica(new StorageItem(new TransactionState() { BlockIndex = engine.PersistingBlock.Index }));
                    foreach (var signer in conflictingSigners)
                    {
                        engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_Transaction).Add(attr.Hash).Add(signer), () => new StorageItem(new TransactionState())).FromReplica(new StorageItem(new TransactionState() { BlockIndex = engine.PersistingBlock.Index }));
                    }
                }
            }
            engine.SetState(transactions);
            return ContractTask.CompletedTask;
        }

        internal override ContractTask PostPersist(ApplicationEngine engine)
        {
            HashIndexState state = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_CurrentBlock), () => new StorageItem(new HashIndexState())).GetInteroperable<HashIndexState>();
            state.Hash = engine.PersistingBlock.Hash;
            state.Index = engine.PersistingBlock.Index;
            return ContractTask.CompletedTask;
        }

        internal bool Initialized(DataCache snapshot)
        {
            return snapshot.Find(CreateStorageKey(Prefix_Block).ToArray()).Any();
        }

        private bool IsTraceableBlock(DataCache snapshot, uint index, uint maxTraceableBlocks)
        {
            uint currentIndex = CurrentIndex(snapshot);
            if (index > currentIndex) return false;
            return index + maxTraceableBlocks > currentIndex;
        }

        /// <summary>
        /// Gets the hash of the specified block.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="index">The index of the block.</param>
        /// <returns>The hash of the block.</returns>
        public UInt256 GetBlockHash(DataCache snapshot, uint index)
        {
            StorageItem item = snapshot.TryGet(CreateStorageKey(Prefix_BlockHash).AddBigEndian(index));
            if (item is null) return null;
            return new UInt256(item.Value.Span);
        }

        /// <summary>
        /// Gets the hash of the current block.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <returns>The hash of the current block.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public UInt256 CurrentHash(DataCache snapshot)
        {
            return snapshot[CreateStorageKey(Prefix_CurrentBlock)].GetInteroperable<HashIndexState>().Hash;
        }

        /// <summary>
        /// Gets the index of the current block.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <returns>The index of the current block.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public uint CurrentIndex(DataCache snapshot)
        {
            return snapshot[CreateStorageKey(Prefix_CurrentBlock)].GetInteroperable<HashIndexState>().Index;
        }

        /// <summary>
        /// Determine whether the specified block is contained in the blockchain.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="hash">The hash of the block.</param>
        /// <returns><see langword="true"/> if the blockchain contains the block; otherwise, <see langword="false"/>.</returns>
        public bool ContainsBlock(DataCache snapshot, UInt256 hash)
        {
            return snapshot.Contains(CreateStorageKey(Prefix_Block).Add(hash));
        }

        /// <summary>
        /// Determine whether the specified transaction is contained in the blockchain.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="hash">The hash of the transaction.</param>
        /// <returns><see langword="true"/> if the blockchain contains the transaction; otherwise, <see langword="false"/>.</returns>
        public bool ContainsTransaction(DataCache snapshot, UInt256 hash)
        {
            var txState = GetTransactionState(snapshot, hash);
            return txState != null;
        }

        /// <summary>
        /// Determine whether the specified transaction hash is contained in the blockchain
        /// as the hash of conflicting transaction.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="hash">The hash of the conflicting transaction.</param>
        /// <param name="signers">The list of signer accounts of the conflicting transaction.</param>
        /// <param name="maxTraceableBlocks">MaxTraceableBlocks protocol setting.</param>
        /// <returns><see langword="true"/> if the blockchain contains the hash of the conflicting transaction; otherwise, <see langword="false"/>.</returns>
        public bool ContainsConflictHash(DataCache snapshot, UInt256 hash, IEnumerable<UInt160> signers, uint maxTraceableBlocks)
        {
            // Check the dummy stub firstly to define whether there's exist at least one conflict record.
            var stub = snapshot.TryGet(CreateStorageKey(Prefix_Transaction).Add(hash))?.GetInteroperable<TransactionState>();
            if (stub is null || stub.Transaction is not null || !IsTraceableBlock(snapshot, stub.BlockIndex, maxTraceableBlocks))
                return false;

            // At least one conflict record is found, then need to check signers intersection.
            foreach (var signer in signers)
            {
                var state = snapshot.TryGet(CreateStorageKey(Prefix_Transaction).Add(hash).Add(signer))?.GetInteroperable<TransactionState>();
                if (state is not null && IsTraceableBlock(snapshot, state.BlockIndex, maxTraceableBlocks))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a <see cref="TrimmedBlock"/> with the specified hash.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="hash">The hash of the block.</param>
        /// <returns>The trimmed block.</returns>
        public TrimmedBlock GetTrimmedBlock(DataCache snapshot, UInt256 hash)
        {
            StorageItem item = snapshot.TryGet(CreateStorageKey(Prefix_Block).Add(hash));
            if (item is null) return null;
            return item.Value.AsSerializable<TrimmedBlock>();
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private TrimmedBlock GetBlock(ApplicationEngine engine, byte[] indexOrHash)
        {
            UInt256 hash;
            if (indexOrHash.Length < UInt256.Length)
                hash = GetBlockHash(engine.Snapshot, (uint)new BigInteger(indexOrHash));
            else if (indexOrHash.Length == UInt256.Length)
                hash = new UInt256(indexOrHash);
            else
                throw new ArgumentException(null, nameof(indexOrHash));
            if (hash is null) return null;
            TrimmedBlock block = GetTrimmedBlock(engine.Snapshot, hash);
            if (block is null || !IsTraceableBlock(engine.Snapshot, block.Index, engine.ProtocolSettings.MaxTraceableBlocks)) return null;
            return block;
        }

        /// <summary>
        /// Gets a block with the specified hash.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="hash">The hash of the block.</param>
        /// <returns>The block with the specified hash.</returns>
        public Block GetBlock(DataCache snapshot, UInt256 hash)
        {
            TrimmedBlock state = GetTrimmedBlock(snapshot, hash);
            if (state is null) return null;
            return new Block
            {
                Header = state.Header,
                Transactions = state.Hashes.Select(p => GetTransaction(snapshot, p)).ToArray()
            };
        }

        /// <summary>
        /// Gets a block with the specified index.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="index">The index of the block.</param>
        /// <returns>The block with the specified index.</returns>
        public Block GetBlock(DataCache snapshot, uint index)
        {
            UInt256 hash = GetBlockHash(snapshot, index);
            if (hash is null) return null;
            return GetBlock(snapshot, hash);
        }

        /// <summary>
        /// Gets a block header with the specified hash.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="hash">The hash of the block.</param>
        /// <returns>The block header with the specified hash.</returns>
        public Header GetHeader(DataCache snapshot, UInt256 hash)
        {
            return GetTrimmedBlock(snapshot, hash)?.Header;
        }

        /// <summary>
        /// Gets a block header with the specified index.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="index">The index of the block.</param>
        /// <returns>The block header with the specified index.</returns>
        public Header GetHeader(DataCache snapshot, uint index)
        {
            UInt256 hash = GetBlockHash(snapshot, index);
            if (hash is null) return null;
            return GetHeader(snapshot, hash);
        }

        /// <summary>
        /// Gets a <see cref="TransactionState"/> with the specified hash.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="hash">The hash of the transaction.</param>
        /// <returns>The <see cref="TransactionState"/> with the specified hash.</returns>
        public TransactionState GetTransactionState(DataCache snapshot, UInt256 hash)
        {
            var state = snapshot.TryGet(CreateStorageKey(Prefix_Transaction).Add(hash))?.GetInteroperable<TransactionState>();
            if (state?.Transaction is null) return null;
            return state;
        }

        /// <summary>
        /// Gets a transaction with the specified hash.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="hash">The hash of the transaction.</param>
        /// <returns>The transaction with the specified hash.</returns>
        public Transaction GetTransaction(DataCache snapshot, UInt256 hash)
        {
            return GetTransactionState(snapshot, hash)?.Transaction;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates, Name = "getTransaction")]
        private Transaction GetTransactionForContract(ApplicationEngine engine, UInt256 hash)
        {
            TransactionState state = GetTransactionState(engine.Snapshot, hash);
            if (state is null || !IsTraceableBlock(engine.Snapshot, state.BlockIndex, engine.ProtocolSettings.MaxTraceableBlocks)) return null;
            return state.Transaction;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private Signer[] GetTransactionSigners(ApplicationEngine engine, UInt256 hash)
        {
            TransactionState state = GetTransactionState(engine.Snapshot, hash);
            if (state is null || !IsTraceableBlock(engine.Snapshot, state.BlockIndex, engine.ProtocolSettings.MaxTraceableBlocks)) return null;
            return state.Transaction.Signers;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private VMState GetTransactionVMState(ApplicationEngine engine, UInt256 hash)
        {
            TransactionState state = GetTransactionState(engine.Snapshot, hash);
            if (state is null || !IsTraceableBlock(engine.Snapshot, state.BlockIndex, engine.ProtocolSettings.MaxTraceableBlocks)) return VMState.NONE;
            return state.State;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private int GetTransactionHeight(ApplicationEngine engine, UInt256 hash)
        {
            TransactionState state = GetTransactionState(engine.Snapshot, hash);
            if (state is null || !IsTraceableBlock(engine.Snapshot, state.BlockIndex, engine.ProtocolSettings.MaxTraceableBlocks)) return -1;
            return (int)state.BlockIndex;
        }

        [ContractMethod(CpuFee = 1 << 16, RequiredCallFlags = CallFlags.ReadStates)]
        private Transaction GetTransactionFromBlock(ApplicationEngine engine, byte[] blockIndexOrHash, int txIndex)
        {
            UInt256 hash;
            if (blockIndexOrHash.Length < UInt256.Length)
                hash = GetBlockHash(engine.Snapshot, (uint)new BigInteger(blockIndexOrHash));
            else if (blockIndexOrHash.Length == UInt256.Length)
                hash = new UInt256(blockIndexOrHash);
            else
                throw new ArgumentException(null, nameof(blockIndexOrHash));
            if (hash is null) return null;
            TrimmedBlock block = GetTrimmedBlock(engine.Snapshot, hash);
            if (block is null || !IsTraceableBlock(engine.Snapshot, block.Index, engine.ProtocolSettings.MaxTraceableBlocks)) return null;
            if (txIndex < 0 || txIndex >= block.Hashes.Length)
                throw new ArgumentOutOfRangeException(nameof(txIndex));
            return GetTransaction(engine.Snapshot, block.Hashes[txIndex]);
        }

        private static TrimmedBlock Trim(Block block)
        {
            return new TrimmedBlock
            {
                Header = block.Header,
                Hashes = block.Transactions.Select(p => p.Hash).ToArray()
            };
        }
    }
}
