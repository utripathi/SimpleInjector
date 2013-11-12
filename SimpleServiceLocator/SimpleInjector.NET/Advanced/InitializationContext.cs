﻿#region Copyright (c) 2013 S. van Deursen
/* The Simple Injector is an easy-to-use Inversion of Control library for .NET
 * 
 * Copyright (C) 2013 S. van Deursen
 * 
 * To contact me, please visit my blog at http://www.cuttingedge.it/blogs/steven/ or mail to steven at 
 * cuttingedge.it.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
 * associated documentation files (the "Software"), to deal in the Software without restriction, including 
 * without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
 * copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the 
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial 
 * portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT 
 * LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO 
 * EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER 
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE 
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion

namespace SimpleInjector.Advanced
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    /// <summary>
    /// An instance of this type will be supplied to the <see cref="Predicate{T}" />
    /// delegate that is that is supplied to the 
    /// <see cref="Container.RegisterInitializer(Action{InstanceInitializationData}, Predicate{InitializationContext})">RegisterInitializer</see>
    /// overload that takes this delegate. This type contains contextual information about the creation and it 
    /// allows the user to examine the given instance to decide whether the instance should be initialized or 
    /// not.
    /// </summary>
    [DebuggerDisplay("OpenGenericPredicateContext ({DebuggerDisplay,nq})")]
    public class InitializationContext
    {
        internal InitializationContext(InstanceProducer producer, Registration registration)
        {
            // producer will be null when a user calls Registration.BuildExpression() directly, instead of
            // calling InstanceProducer.BuildExpression() or InstanceProducer.GetInstance(). 
            Requires.IsNotNull(registration, "registration");

            this.Producer = producer;
            this.Registration = registration;
        }

        /// <summary>
        /// Gets the <see cref="InstanceProducer"/> that is responsible for the initialization of the created
        /// instance.
        /// </summary>
        /// <value>The <see cref="InstanceProducer"/> or null (Nothing in VB) when the instance producer is
        /// unknown.</value>
        public InstanceProducer Producer { get; private set; }

        /// <summary>
        /// Gets the <see cref="Registration"/> that is responsible for the initialization of the created
        /// instance.
        /// </summary>
        /// /// <value>The <see cref="Registration"/>.</value>
        public Registration Registration { get; private set; }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode",
            Justification = "This method is called by the debugger.")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "Producer.ServiceType: {0}, Registration.Implementation: {1}",
                    this.Producer.ServiceType.ToFriendlyName(),
                    this.Registration.ImplementationType.ToFriendlyName());
            }
        }
    }
}