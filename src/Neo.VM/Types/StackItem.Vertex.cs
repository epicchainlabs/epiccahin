// Copyright (C) 2021-2024 The EpicChain Lab's.
//
// StackItem.Vertex.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.VM.Types
{
    partial class StackItem
    {
        internal class ObjectReferenceEntry
        {
            public StackItem Item;
            public int References;
            public ObjectReferenceEntry(StackItem item) => Item = item;
        }

        internal int StackReferences = 0;
        internal Dictionary<CompoundType, ObjectReferenceEntry>? ObjectReferences;
        internal int DFN = -1;
        internal int LowLink = 0;
        internal bool OnStack = false;

        internal IEnumerable<StackItem> Successors => ObjectReferences?.Values.Where(p => p.References > 0).Select(p => p.Item) ?? System.Array.Empty<StackItem>();

        internal void Reset() => (DFN, LowLink, OnStack) = (-1, 0, false);

        public override int GetHashCode() =>
            HashCode.Combine(GetSpan().ToArray());
    }
}
