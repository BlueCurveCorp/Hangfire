// This file is part of NexusForge.
// Copyright © 2026 NexusForge OÜ.
// 
// NexusForge is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.

using System;
using NexusForge.States;

namespace NexusForge
{
    public sealed class QueueDescriptor
    {
        public QueueDescriptor(string name, int priority)
        {
            EnqueuedState.ValidateQueueName(nameof(name), name);
            if (priority <= 0) throw new ArgumentOutOfRangeException(nameof(priority), "Queue priority must be a positive integer.");

            Name = name;
            Priority = priority;
        }

        public string Name { get; }

        public int Priority { get; }
    }
}
