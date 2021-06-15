/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
 * lambdasharp.net
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using LambdaSharp.CloudFormation.Reporting;

namespace LambdaSharp.CloudFormation.Syntax.Validators.Rules {

    internal interface ISyntaxRule {

        //--- Properties ---
        Type NodeType { get; }
        SyntaxProcessorState State { get; set; }

        //--- Methods ---
        void InvokeValidate(object value);
    }

    internal abstract class ASyntaxRule<TNode> : ISyntaxRule {

        //--- Fields ---
        private SyntaxProcessorState? _state;

        //--- Properties ---
        public Type NodeType => typeof(TNode);

        public SyntaxProcessorState State {
            get => _state ?? throw new NullValueException();
            set => _state = value ?? throw new ArgumentNullException();
        }

        //--- Abstract Methods ---
        public abstract void Validate(TNode node);

        //--- Methods ---
        public void InvokeValidate(object node) => Validate((TNode)node);
        protected void Add(IReportEntry entry) => State.Add(entry);
    }
}