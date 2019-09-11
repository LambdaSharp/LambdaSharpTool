/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2019
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
using System.Collections.Generic;
using System.Linq;
using LambdaSharp.ConfigSource;
using LambdaSharp.Exceptions;

namespace LambdaSharp {

    /// <summary>
    /// The <see cref="LambdaConfig"/> class is used by the Lambda function to read
    /// its configuration settings. <see cref="LambdaConfig"/> provides hiearachical
    /// access to scoped module values using the
    /// <see cref="LambdaConfig.this[string]"/> operator.
    /// Individual values can be retrieved using the basic
    /// <see cref="LambdaConfig.Read{T}(string, Func{string, T}, Func{string, T}, Action{T})"/> method
    /// or one of the <c>Read*</c> methods.
    /// </summary>
    public sealed class LambdaConfig {

        //--- Class Methods ---

        /// <summary>
        /// Validates that the value is positive.
        /// </summary>
        /// <param name="value">Value to validate.</param>
        /// <exception cref="LambdaConfigValidationException">
        /// Thrown when the value fails validation.
        /// </exception>
        public static void ValidateIsPositive(int value) {
            if(value <= 0) {
                throw new LambdaConfigValidationException("value is not positive");
            }
        }

        /// <summary>
        /// Validates that the value is non-positive.
        /// </summary>
        /// <param name="value">Value to validate.</param>
        /// <exception cref="LambdaConfigValidationException">
        /// Thrown when the value fails validation.
        /// </exception>
        public static void ValidateIsNonPositive(int value) {
            if(value > 0) {
                throw new LambdaConfigValidationException("value is not non-positive");
            }
        }

        /// <summary>
        /// Validates that the value is negative.
        /// </summary>
        /// <param name="value">Value to validate.</param>
        /// <exception cref="LambdaConfigValidationException">
        /// Thrown when the value fails validation.
        /// </exception>
        public static void ValidateIsNegative(int value) {
            if(value >= 0) {
                throw new LambdaConfigValidationException("value is not negative");
            }
        }

        /// <summary>
        /// Validates that the value is non-negative.
        /// </summary>
        /// <param name="value">Value to validate.</param>
        /// <exception cref="LambdaConfigValidationException">
        /// Thrown when the value fails validation.
        /// </exception>
        public static void ValidateIsNonNegative(int value) {
            if(value < 0) {
                throw new LambdaConfigValidationException("value is not non-negative");
            }
        }

        /// <summary>
        /// Creates a callback that validates that a value is in range
        /// of the specified lower and upper bounds, inclusively.
        /// </summary>
        /// <param name="lowerValue">Lower bound in range.</param>
        /// <param name="upperValue">Upper bound in range.</param>
        /// <returns>The validation callback to validate that a value is within the provided lower and upper bounds.</returns>
        public static Action<int> ValidateIsInRange(int lowerValue, int upperValue) => value => {
            if((value < lowerValue) || (value > upperValue)) {
                throw new LambdaConfigValidationException("value is not in range: [{0}..{1}]", lowerValue, upperValue);
            }
        };

        private static string CombinePathWithKey(string path, string key) => ((path ?? "").Length > 0) ? (path + "::" + key) : key;
        private static bool IsValidKey(string key) => key.Any() && key.All(c => char.IsLetterOrDigit(c) || (c == ':'));

        //--- Fields ---
        private readonly LambdaConfig _parent;
        private readonly string _key;
        private readonly ILambdaConfigSource _source;

        //--- Constructors ---

        /// <summary>
        /// Create a new instance using the provided <see cref="ILambdaConfigSource"/> instance to read configuration values.
        /// </summary>
        /// <param name="source">The <see cref="ILambdaConfigSource"/> instance to read configuration values from.</param>
        /// <returns>A new <see cref="LambdaConfig"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="source"/> is null.
        /// </exception>
        public LambdaConfig(ILambdaConfigSource source) => _source = source ?? throw new ArgumentNullException(nameof(source));

        private LambdaConfig(LambdaConfig parent, string key, ILambdaConfigSource source) {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _key = key ?? throw new ArgumentNullException(nameof(key));
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        //--- Properties ---

        /// <summary>
        /// Create a new <see cref="LambdaConfig"/> scoped to a nested section of values.
        /// </summary>
        /// <param name="name">Name of the nested section.</param>
        /// <value>A newly scoped <see cref="LambdaConfig"/> instance.</value>
        public LambdaConfig this[string name] {
            get {

                // validate name
                if(!IsValidKey(name)) {
                    throw new LambdaConfigIllegalKeyException(name);
                }

                // attempt to open nested section
                ILambdaConfigSource childConfig;
                try {
                    childConfig = _source.Open(name) ?? new EmptyLambdaConfigSource();
                } catch(Exception e) when (!(e is ALambdaConfigException)) {
                    throw new LambdaConfigUnexpectedException(e, CombinePathWithKey(Path, name), "opening child config");
                }
                return new LambdaConfig(this, name, childConfig);
            }
        }

        /// <summary>
        /// Retrieve the current hierarchy path.
        /// </summary>
        /// <value>Current path.</value>
        public string Path => CombinePathWithKey(_parent?.Path ?? "", _key);

        /// <summary>
        /// Enumerate all child keys at the current path in the hierarchy.
        /// </summary>
        /// <value>Enumeration of all nested keys.</value>
        public IEnumerable<string> Keys {
            get {
                try {
                    return _source.ReadAllKeys().OrderBy(key => key.ToLowerInvariant()).ToArray();
                } catch(Exception e) when(!(e is ALambdaConfigException)) {
                    throw new LambdaConfigUnexpectedException(e, Path, "reading all config keys");
                }
            }
        }

        //--- Methods ---

        /// <summary>
        /// Read the value for a key from <see cref="ILambdaConfigSource"/> at the current path in the hierarchy.
        /// </summary>
        /// <remarks>
        /// This method should rarely be invoked directly. Instead use <see cref="ReadText(string, Action{string})"/>
        /// and the other convenience methods.
        /// </remarks>
        /// <param name="key">The name of the value to read.</param>
        /// <param name="fallback">An optional callback when the value could not be found at the current path.</param>
        /// <param name="convert">An optional conversion from string to another type when the value was found.</param>
        /// <param name="validate">An optional callback to validate the converted value.</param>
        /// <typeparam name="T">The type of the value after conversion.</typeparam>
        /// <returns>The found value.</returns>
        /// <exception cref="LambdaConfigMissingKeyException">
        /// Thrown when no value is found at <paramref name="key"/> and <paramref name="fallback"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="LambdaConfigBadValueException">
        /// Thrown when the value fails validation.
        /// </exception>
        public T Read<T>(string key, Func<string, T> fallback, Func<string, T> convert, Action<T> validate) {

            // validate key
            if(!IsValidKey(key)) {
                throw new LambdaConfigIllegalKeyException(key);
            }

            // open nested scopes as needed
            var nestedSource = _source;
            var keys = key.Split("::");
            for(var i = 0; i < keys.Length - 1; ++i) {
                nestedSource = nestedSource.Open(keys[i]);
            }
            var nestedKey = keys[keys.Length - 1];

            // attempt to read the requested key
            string textValue;
            try {
                textValue = nestedSource.Read(nestedKey);
            } catch(Exception e) when(!(e is ALambdaConfigException)) {
                throw new LambdaConfigUnexpectedException(e, CombinePathWithKey(Path, key), "reading config key");
            }

            // check if we were unable to find a value
            T value;
            if(textValue == null) {

                // check if we have a fallback option for getting the value
                if(fallback == null) {
                    throw new LambdaConfigMissingKeyException(CombinePathWithKey(Path, key));
                }

                // attempt to get the key value using the fallback callback
                try {
                    value = fallback(key);
                } catch(Exception e) when(!(e is ALambdaConfigException)) {
                    throw new LambdaConfigUnexpectedException(e, CombinePathWithKey(Path, key), "invoking fallback");
                }
            } else {

                // attempt to convert value to desired type
                try {
                    if(convert == null) {
                        value = (T)Convert.ChangeType(textValue, typeof(T));
                    } else {
                        value = convert(textValue);
                    }
                } catch(Exception e) when(!(e is ALambdaConfigException)) {
                    throw new LambdaConfigUnexpectedException(e, CombinePathWithKey(Path, key), "validating value");
                }
            }

            // optionally validate converted value
            if(validate == null) {
                return value;
            }
            try {
                validate(value);
            } catch(Exception e) when(!(e is ALambdaConfigException)) {
                throw new LambdaConfigBadValueException(e, CombinePathWithKey(Path, key));
            }
            return value;
        }

        /// <summary>
        /// Read the <c>string</c> value for a key at the current path in the hierarchy.
        /// </summary>
        /// <param name="key">The name of the value to read.</param>
        /// <param name="validate">An optional callback to validate the value.</param>
        /// <returns>The found <c>string</c> value.</returns>
        /// <exception cref="LambdaConfigMissingKeyException">
        /// Thrown when no value is found at <paramref name="key"/>.
        /// </exception>
        /// <exception cref="LambdaConfigBadValueException">
        /// Thrown when the value fails validation.
        /// </exception>
        public string ReadText(string key, Action<string> validate = null)
            => Read(key, fallback: null, convert: v => v, validate: validate);

        /// <summary>
        /// Read the <c>string</c> value for a key at the current path in the hierarchy.
        /// </summary>
        /// <param name="key">The name of the value to read.</param>
        /// <param name="defaultValue">A value to return if <paramref name="key"/> doesn't exist.</param>
        /// <param name="validate">An optional callback to validate the value.</param>
        /// <returns>The found <c>string</c> value.</returns>
        /// <exception cref="LambdaConfigBadValueException">
        /// Thrown when the value fails validation.
        /// </exception>
        public string ReadText(string key, string defaultValue, Action<string> validate = null)
            => Read(key, fallback: _ => defaultValue, convert: v => v, validate: validate);

        /// <summary>
        /// Read an enumeration of comma-delimited <c>string</c> values for a
        /// key at the current path in the hierarchy.
        /// </summary>
        /// <param name="key">The name of the value to read.</param>
        /// <param name="validate">An optional callback to validate the value.</param>
        /// <returns>The found enumeration of <c>string</c> values.</returns>
        /// <exception cref="LambdaConfigMissingKeyException">
        /// Thrown when no value is found at <paramref name="key"/>.
        /// </exception>
        /// <exception cref="LambdaConfigBadValueException">
        /// Thrown when the value fails validation.
        /// </exception>
        public IEnumerable<string> ReadCommaDelimitedList(string key, Action<IEnumerable<string>> validate = null)
            => Read(key, fallback: null, convert: v => v.Split(",", StringSplitOptions.RemoveEmptyEntries), validate: validate);

        /// <summary>
        /// Read the <c>int</c> value for a key at the current path in the hierarchy.
        /// </summary>
        /// <param name="key">The name of the value to read.</param>
        /// <param name="validate">An optional callback to validate the value.</param>
        /// <returns>The found <c>int</c> value.</returns>
        /// <exception cref="LambdaConfigMissingKeyException">
        /// Thrown when no value is found at <paramref name="key"/>.
        /// </exception>
        /// <exception cref="LambdaConfigBadValueException">
        /// Thrown when the value fails validation.
        /// </exception>
        public int ReadInt(string key, Action<int> validate = null)
            => Read(key, fallback: null, convert: int.Parse, validate: validate);

        /// <summary>
        /// Read the <c>int</c> value for a key at the current path in the hierarchy.
        /// </summary>
        /// <param name="key">The name of the value to read.</param>
        /// <param name="defaultValue">A value to return if <paramref name="key"/> doesn't exist.</param>
        /// <param name="validate">An optional callback to validate the value.</param>
        /// <returns>The found <c>int</c> value.</returns>
        /// <exception cref="LambdaConfigBadValueException">
        /// Thrown when the value fails validation.
        /// </exception>
        public int ReadInt(string key, int defaultValue, Action<int> validate = null)
            => Read(key, fallback: _ => defaultValue, convert: int.Parse, validate: validate);

        /// <summary>
        /// Read the <c>TimeSpan</c> value for a key at the current path in the hierarchy.
        /// </summary>
        /// <param name="key">The name of the value to read.</param>
        /// <param name="validate">An optional callback to validate the value.</param>
        /// <returns>The found <c>TimeSpan</c> value.</returns>
        /// <exception cref="LambdaConfigMissingKeyException">
        /// Thrown when no value is found at <paramref name="key"/>.
        /// </exception>
        /// <exception cref="LambdaConfigBadValueException">
        /// Thrown when the value fails validation.
        /// </exception>
        public TimeSpan ReadTimeSpan(string key, Action<TimeSpan> validate = null)
            => Read(key, fallback: null, convert: value => TimeSpan.FromSeconds(float.Parse(value)), validate: validate);

        /// <summary>
        /// Read the <c>TimeSpan</c> value for a key at the current path in the hierarchy.
        /// </summary>
        /// <param name="key">The name of the value to read.</param>
        /// <param name="defaultValue">A value to return if <paramref name="key"/> doesn't exist.</param>
        /// <param name="validate">An optional callback to validate the value.</param>
        /// <returns>The found <c>TimeSpan</c> value.</returns>
        /// <exception cref="LambdaConfigBadValueException">
        /// Thrown when the value fails validation.
        /// </exception>
        public TimeSpan ReadTimeSpan(string key, TimeSpan defaultValue, Action<TimeSpan> validate = null)
            => Read(key, fallback: _ => defaultValue, convert: value => TimeSpan.FromSeconds(float.Parse(value)), validate: validate);
    }
}
