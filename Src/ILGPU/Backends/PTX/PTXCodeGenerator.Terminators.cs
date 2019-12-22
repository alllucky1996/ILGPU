﻿// -----------------------------------------------------------------------------
//                                    ILGPU
//                     Copyright (c) 2016-2019 Marcel Koester
//                                www.ilgpu.net
//
// File: PTXCodeGenerator.Terminators.cs
//
// This file is part of ILGPU and is distributed under the University of
// Illinois Open Source License. See LICENSE.txt for details
// -----------------------------------------------------------------------------

using ILGPU.IR.Values;

namespace ILGPU.Backends.PTX
{
    partial class PTXCodeGenerator
    {
        /// <summary cref="IValueVisitor.Visit(BranchTarget)"/>
        public void Visit(BranchTarget target)
        {
            // We do not have to map a branch target here
        }

        /// <summary cref="IValueVisitor.Visit(ReturnTerminator)"/>
        public void Visit(ReturnTerminator returnTerminator)
        {
            if (!returnTerminator.IsVoidReturn)
            {
                var resultRegister = Load(returnTerminator.ReturnValue);
                EmitStoreParam(ReturnParamName, resultRegister);
            }
            Command(PTXInstructions.ReturnOperation);
        }

        /// <summary cref="IValueVisitor.Visit(UnconditionalBranch)"/>
        public void Visit(UnconditionalBranch branch)
        {
            using (var command = BeginCommand(PTXInstructions.BranchOperation))
            {
                var targetLabel = GetTargetLabel(branch.Target);
                command.AppendLabel(targetLabel);
            }
        }

        /// <summary cref="IValueVisitor.Visit(ConditionalBranch)"/>
        public void Visit(ConditionalBranch branch)
        {
            var condition = LoadPrimitive(branch.Condition);
            using (var command = BeginCommand(
                PTXInstructions.BranchOperation,
                new PredicateConfiguration(condition, true)))
            {
                var trueLabel = GetTargetLabel(branch.TrueTarget);
                command.AppendLabel(trueLabel);
            }

            // Jump to false target in the else case
            using (var command = BeginCommand(PTXInstructions.BranchOperation))
            {
                var targetLabel = GetTargetLabel(branch.FalseTarget);
                command.AppendLabel(targetLabel);
            }
        }

        /// <summary cref="IValueVisitor.Visit(SwitchBranch)"/>
        public void Visit(SwitchBranch branch)
        {
            var idx = LoadPrimitive(branch.Condition);
            using (var lowerBoundsScope = new PredicateScope(this))
            {
                // Emit less than
                var lessThanCommand = PTXInstructions.GetCompareOperation(
                    CompareKind.LessThan,
                    ArithmeticBasicValueType.Int32);
                using (var command = BeginCommand(
                    lessThanCommand))
                {
                    command.AppendArgument(lowerBoundsScope.PredicateRegister);
                    command.AppendArgument(idx);
                    command.AppendConstant(0);
                }

                using (var upperBoundsScope = new PredicateScope(this))
                {
                    using (var command = BeginCommand(
                        PTXInstructions.BranchIndexRangeComparison))
                    {
                        command.AppendArgument(upperBoundsScope.PredicateRegister);
                        command.AppendArgument(idx);
                        command.AppendConstant(branch.NumCasesWithoutDefault);
                        command.AppendArgument(lowerBoundsScope.PredicateRegister);
                    }
                    using (var command = BeginCommand(
                        PTXInstructions.BranchOperation,
                        new PredicateConfiguration(upperBoundsScope.PredicateRegister, true)))
                    {
                        var defaultTarget = GetTargetLabel(branch.DefaultBlock);
                        command.AppendLabel(defaultTarget);
                    }
                }
            }

            var targetLabel = DeclareLabel();
            MarkLabel(targetLabel);
            Builder.Append('\t');
            Builder.Append(PTXInstructions.BranchTargetsDeclaration);
            Builder.Append(' ');
            for (int i = 0, e = branch.NumCasesWithoutDefault; i < e; ++i)
            {
                var caseTarget = branch.GetCaseTarget(i);
                var caseLabel = GetTargetLabel(caseTarget);
                Builder.Append(caseLabel);
                if (i + 1 < e)
                    Builder.Append(", ");
            }
            Builder.AppendLine(";");

            using (var command = BeginCommand(
                PTXInstructions.BranchIndexOperation))
            {
                command.AppendArgument(idx);
                command.AppendLabel(targetLabel);
            }
        }
    }
}
