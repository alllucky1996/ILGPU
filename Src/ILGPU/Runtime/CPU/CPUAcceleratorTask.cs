﻿// -----------------------------------------------------------------------------
//                                    ILGPU
//                     Copyright (c) 2016-2020 Marcel Koester
//                                www.ilgpu.net
//
// File: CPUAcceleratorTask.cs
//
// This file is part of ILGPU and is distributed under the University of
// Illinois Open Source License. See LICENSE.txt for details
// -----------------------------------------------------------------------------

using ILGPU.Resources;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ILGPU.Runtime.CPU
{
    /// <summary>
    /// Execution delegate for CPU kernels inside the runtime system.
    /// </summary>
    /// <param name="task">The referenced task.</param>
    /// <param name="groupContext">The current group context.</param>
    /// <param name="runtimeThreadOffset">The thread offset within the current group (WarpId * WarpSize + WarpThreadIdx).</param>
    /// <param name="groupSize">The group size in the scope of the runtime system.</param>
    /// <param name="chunkSize">The size of a grid-idx chunk to process.</param>
    /// <param name="chunkOffset">The offset of the current processing chunk.</param>
    /// <param name="targetDimension">The target kernel dimension.</param>
    public delegate void CPUKernelExecutionHandler(
        CPUAcceleratorTask task,
        CPURuntimeGroupContext groupContext,
        int runtimeThreadOffset,
        int groupSize,
        int chunkSize,
        int chunkOffset,
        int targetDimension);

    /// <summary>
    /// Represents a single CPU-accelerator task.
    /// </summary>
    public class CPUAcceleratorTask
    {
        #region Static

        internal const int GroupContextIndex = 1;
        internal const int RuntimeThreadOffsetIndex = 2;
        internal const int RuntimeGroupSizeIndex = 3;
        internal const int ChunkSizeIndex = 4;
        internal const int ChunkSizeOffsetIndex = 5;
        internal const int TargetDimensionIndex = 6;

        /// <summary>
        /// Contains the required parameter types of the default task constructor.
        /// </summary>
        internal static readonly Type[] ConstructorParameterTypes =
        {
            typeof(CPUKernelExecutionHandler),
            typeof(Index3),
            typeof(Index3),
            typeof(Index3),
            typeof(Index3),
            typeof(int)
        };

        /// <summary>
        /// Contains the required parameter types of the task-execution method.
        /// </summary>
        internal static readonly Type[] ExecuteParameterTypes =
        {
            typeof(CPUAcceleratorTask),     // task
            typeof(CPURuntimeGroupContext), // groupContext
            typeof(int),                    // runtimeThreadOffset
            typeof(int),                    // groupSize
            typeof(int),                    // chunkSize
            typeof(int),                    // chunkOffset
            typeof(int)                     // targetDimension
        };

        #endregion

        #region Instance

        /// <summary>
        /// Constructs a new accelerator task.
        /// </summary>
        /// <param name="kernelExecutionDelegate">The execution method.</param>
        /// <param name="userGridDim">The grid dimension that was specified by the user.</param>
        /// <param name="userGroupDim">The group dimension that was specified by the user.</param>
        /// <param name="gridDim">The grid dimension.</param>
        /// <param name="groupDim">The group dimension.</param>
        /// <param name="sharedMemSize">The required amount of shareed-memory per thread group in bytes.</param>
        public CPUAcceleratorTask(
            CPUKernelExecutionHandler kernelExecutionDelegate,
            Index3 userGridDim,
            Index3 userGroupDim,
            Index3 gridDim,
            Index3 groupDim,
            int sharedMemSize)
        {
            Debug.Assert(kernelExecutionDelegate != null, "Invalid execution delegate");
            if (sharedMemSize < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(sharedMemSize),
                    RuntimeErrorMessages.InvalidSharedMemorySize);
            if (gridDim.X < 0 | gridDim.Y < 0 | gridDim.Z < 0 | gridDim == Index3.Zero)
                throw new ArgumentOutOfRangeException(
                    nameof(gridDim),
                    RuntimeErrorMessages.InvalidGridDimension);
            if (groupDim.X < 0 | groupDim.Y < 0 | groupDim.Z < 0 | groupDim == Index3.Zero)
                throw new ArgumentOutOfRangeException(
                    nameof(groupDim),
                    RuntimeErrorMessages.InvalidGroupDimension);
            if (userGridDim.Size < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(userGridDim),
                    RuntimeErrorMessages.InvalidGridDimension);
            if (userGroupDim.Size < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(userGroupDim),
                    RuntimeErrorMessages.InvalidGroupDimension);

            KernelExecutionDelegate = kernelExecutionDelegate;
            UserGridDim = userGridDim;
            UserDimension = userGridDim.Size * userGroupDim.Size;
            GridDim = gridDim;
            GroupDim = groupDim;
            RuntimeDimension = gridDim.Size * groupDim.Size;
            SharedMemSize = sharedMemSize;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns the grid dimension that was specified by the user.
        /// </summary>
        public Index3 UserGridDim { get; }

        /// <summary>
        /// Returns the user-defined kernel dimension.
        /// </summary>
        public int UserDimension { get; }

        /// <summary>
        /// Returns the current grid dimension.
        /// </summary>
        public Index3 GridDim { get; }

        /// <summary>
        /// Returns the current group dimension.
        /// </summary>
        public Index3 GroupDim { get; }

        /// <summary>
        /// Returns the runtime-defined kernel dimension.
        /// </summary>
        public int RuntimeDimension { get; }

        /// <summary>
        /// Returns the required amount of shared-memory per thread group in bytes.
        /// </summary>
        public int SharedMemSize { get; }

        /// <summary>
        /// Returns the associated kernel-execution delegate.
        /// </summary>
        public CPUKernelExecutionHandler KernelExecutionDelegate { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Executes this task inside the runtime system.
        /// </summary>
        /// <param name="groupContext">The current group context.</param>
        /// <param name="runtimeThreadOffset">The thread offset within the current group (WarpId * WarpSize + WarpThreadIdx).</param>
        /// <param name="groupSize">The group size in the scope of the runtime system.</param>
        /// <param name="chunkSize">The size of a grid-idx chunk to process.</param>
        /// <param name="chunkOffset">The offset of the current processing chunk.</param>
        /// <param name="targetDimension">The target kernel dimension.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(
            CPURuntimeGroupContext groupContext,
            int runtimeThreadOffset,
            int groupSize,
            int chunkSize,
            int chunkOffset,
            int targetDimension)
        {
            KernelExecutionDelegate(
                this,
                groupContext,
                runtimeThreadOffset,
                groupSize,
                chunkSize,
                chunkOffset,
                targetDimension);
        }

        #endregion
    }
}
