// Copyright© 2014 Jeroen Stemerdink. All Rights Reserved.
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

using EPiServer.Framework.Cache;

using PostSharp.Extensibility;

namespace EPiServer.Libraries.Aspects.Caching
{
    /// <summary>A cache attribute. Skip the execution of a method when its value is found in the EPiServer cache.</summary>
    /// <example>
    ///     [assembly: Cache( AttributeTargetTypes="AdventureWorks.BusinessLayer.*", AttributeTargetMemberAttributes =
    ///     MulticastAttributes.Public )]
    /// </example>
    [MulticastAttributeUsage(MulticastTargets.Method, TargetMemberAttributes = MulticastAttributes.Public)]
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    [Serializable]
    public sealed class VersionKeyCacheAttribute : EPiServerCacheBaseAttribute
    {
        #region Public Properties

        public override CacheEvictionPolicy CacheEvictionPolicy
        {
            get
            {
                return new CacheEvictionPolicy(new[] { DataFactoryCache.VersionKey });
            }
        }

        #endregion
    }
}