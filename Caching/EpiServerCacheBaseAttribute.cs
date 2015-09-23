﻿// Copyright© 2015 Jeroen Stemerdink. All Rights Reserved.
// 
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Linq;
using System.Reflection;
using System.Text;

using EPiServer.Framework.Cache;
using EPiServer.ServiceLocation;

using PostSharp.Aspects;
using PostSharp.Extensibility;

namespace EPi.Libraries.Aspects.Caching
{
    /// <summary>The base class for EPiServer caching aspects.</summary>
    [Serializable]
    public abstract class EPiServerCacheBaseAttribute : OnMethodBoundaryAspect
    {
        #region Fields

        /// <summary>This field will be set by CompileTimeInitialize and serialized at build time, then deserialized at runtime.</summary>
        private string methodName;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the cache eviction policy.
        /// </summary>
        /// <value>The cache eviction policy.</value>
        public abstract CacheEvictionPolicy CacheEvictionPolicy { get; }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the synchronized object instance cache.
        /// </summary>
        /// <value>The synchronized object instance cache.</value>
        protected Injected<ISynchronizedObjectInstanceCache> SynchronizedObjectInstanceCache { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Method invoked at build time to initialize the instance fields of the current aspect. This method is invoked before
        ///     any other build-time method.
        /// </summary>
        /// <param name="method">Method to which the current aspect is applied.</param>
        /// <param name="aspectInfo">Reserved for future usage.</param>
        /// <exception cref="System.ArgumentNullException">method</exception>
        public override void CompileTimeInitialize(MethodBase method, AspectInfo aspectInfo)
        {
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            if (method.DeclaringType != null)
            {
                this.methodName = method.DeclaringType.FullName + "." + method.Name;
            }
            else
            {
                this.methodName = method.Name;
            }
        }

        /// <summary>
        ///     Validate the attribute usage at compile time.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns><c>true</c> if [the attribute can be used], <c>false</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">method</exception>
        public override bool CompileTimeValidate(MethodBase method)
        {
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            // Don't apply to constructors.
            if (method is ConstructorInfo)
            {
                Message.Write(method, SeverityType.Error, "CX0001", "Cannot cache constructors.");
                return false;
            }

            MethodInfo methodInfo = (MethodInfo)method;

            // Don't apply to void methods.
            if (methodInfo.ReturnType.Name == "Void")
            {
                Message.Write(method, SeverityType.Error, "CX0002", "Cannot cache void methods.");
                return false;
            }

            // Does not support out parameters.
            ParameterInfo[] parameters = method.GetParameters();

            if (!parameters.Any(t => t.IsOut))
            {
                return true;
            }

            Message.Write(method, SeverityType.Error, "CX0003", "Cannot cache methods with return values.");
            return false;
        }

        /// <summary>
        ///     Method executed <b>before</b> the body of methods to which this aspect is applied.
        /// </summary>
        /// <param name="args">
        ///     Event arguments specifying which method
        ///     is being executed, which are its arguments, and how should the execution continue
        ///     after the execution of
        ///     <see cref="M:PostSharp.Aspects.IOnMethodBoundaryAspect.OnEntry(PostSharp.Aspects.MethodExecutionArgs)" />.
        /// </param>
        /// <exception cref="System.ArgumentNullException">args</exception>
        public override void OnEntry(MethodExecutionArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            // Compute the cache key. 
            string cacheKey = GetCacheKey(this.methodName, args.Instance, args.Arguments);

            // Fetch the value from the cache. 
            object value = this.SynchronizedObjectInstanceCache.Service.Get(cacheKey);

            if (value != null)
            {
                // The value was found in cache. Don't execute the method. Return immediately.
                args.ReturnValue = value;
                args.FlowBehavior = FlowBehavior.Return;
            }
            else
            {
                // The value was NOT found in cache. Continue with method execution, but store 
                // the cache key so that we don't have to compute it in OnSuccess.
                args.MethodExecutionTag = cacheKey;
            }
        }

        /// <summary>
        ///     Method executed <b>after</b> the body of methods to which this aspect is applied,
        ///     but only when the method successfully returns (i.e. when no exception flies out
        ///     the method.).
        /// </summary>
        /// <param name="args">
        ///     Event arguments specifying which method
        ///     is being executed and which are its arguments.
        /// </param>
        /// <exception cref="System.ArgumentNullException">args</exception>
        public override void OnSuccess(MethodExecutionArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            if (this.CacheEvictionPolicy == null)
            {
                return;
            }

            string cacheKey = (string)args.MethodExecutionTag;

            this.SynchronizedObjectInstanceCache.Service.Insert(cacheKey, args.ReturnValue, this.CacheEvictionPolicy);
        }

        #endregion

        #region Methods

        /// <summary>Gets the cache key.</summary>
        /// <param name="methodName">The method Name.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>The cache key.</returns>
        private static string GetCacheKey(string methodName, object instance, Arguments arguments)
        {
            // If we have no argument, return just the method name so we don't uselessly allocate memory. 
            if (instance == null && arguments.Count == 0)
            {
                return methodName;
            }

            // Add all arguments to the cache key. Note that generic arguments are not part of the cache 
            // key, so method calls that differ only by generic arguments will have conflicting cache keys.
            StringBuilder stringBuilder = new StringBuilder(methodName);

            stringBuilder.Append('(');
            if (instance != null)
            {
                stringBuilder.Append(instance);
                stringBuilder.Append("; ");
            }

            for (int i = 0; i < arguments.Count; i++)
            {
                stringBuilder.Append(arguments.GetArgument(i) ?? "null");
                stringBuilder.Append(", ");
            }

            return stringBuilder.ToString();
        }

        #endregion
    }
}