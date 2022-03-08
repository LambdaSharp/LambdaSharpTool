/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
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

namespace LambdaSharp.Build {

    public class BuildEventArgs {

        //--- Constructors ---
        public BuildEventArgs(string message) {
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public BuildEventArgs(string message, TimeSpan duration) {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Duration = duration;
        }

        public BuildEventArgs(string message, Exception exception) {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Exception = exception;
        }

        public BuildEventArgs(bool? cached) {
            Message = "(no message)";
            Cached = cached;
        }

        //--- Properties ---
        public string Message { get; }
        public TimeSpan Duration { get; }
        public Exception? Exception { get; }
        public bool? Cached { get; }
    }

    public class BuildEventsConfig {

        //--- Class Fields ---
        public static BuildEventsConfig Instance = new BuildEventsConfig();

        //--- Events ---
        public event EventHandler<BuildEventArgs>? OnLogErrorEvent;
        public event EventHandler<BuildEventArgs>? OnLogWarnEvent;
        public event EventHandler<BuildEventArgs>? OnLogInfoEvent;
        public event EventHandler<BuildEventArgs>? OnLogInfoVerboseEvent;
        public event EventHandler<BuildEventArgs>? OnStartLogPerformanceEvent;
        public event EventHandler<BuildEventArgs>? OnStopLogPerformanceEvent;

        //--- Methods ---
        public void LogError(object sender, string message) => OnLogErrorEvent?.Invoke(this, new BuildEventArgs(message));
        public void LogError(object sender, string message, Exception exception) => OnLogErrorEvent?.Invoke(this, new BuildEventArgs(message, exception));
        public void LogWarn(object sender, string message) => OnLogWarnEvent?.Invoke(this, new BuildEventArgs(message));
        public void LogInfo(object sender, string message) => OnLogInfoEvent?.Invoke(this, new BuildEventArgs(message));
        public void LogInfoVerbose(object sender, string message) => OnLogInfoVerboseEvent?.Invoke(this, new BuildEventArgs(message));
        public void StartLogPerformance(object sender, string message) => OnStartLogPerformanceEvent?.Invoke(this, new BuildEventArgs(message));
        public void StopLogPerformance(object sender, bool? cached = null) => OnStopLogPerformanceEvent?.Invoke(this, new BuildEventArgs(cached));
    }

    public abstract class ABuildEventsSource  {

        //--- Constructors ---
        protected ABuildEventsSource(BuildEventsConfig? buildEventsConfig = null) => BuildEventsConfig = buildEventsConfig ?? BuildEventsConfig.Instance;

        //--- Properties ---
        private protected BuildEventsConfig BuildEventsConfig { get; set; }

        //--- Methods ---
        protected void LogError(string message) => BuildEventsConfig.LogError(this, message);
        protected void LogError(string message, Exception exception) => BuildEventsConfig.LogError(this, message, exception);
        protected void LogWarn(string message) => BuildEventsConfig.LogWarn(this, message);
        protected void LogInfo(string message) => BuildEventsConfig.LogInfo(this, message);
        protected void LogInfoVerbose(string message) => BuildEventsConfig.LogInfoVerbose(this, message);
        protected void StartLogPerformance(string message) => BuildEventsConfig.StartLogPerformance(this, message);
        protected void StopLogPerformance(bool? cached = null) => BuildEventsConfig.StopLogPerformance(this, cached);
    }
}