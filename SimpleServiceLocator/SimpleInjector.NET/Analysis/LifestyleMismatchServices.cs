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

namespace SimpleInjector.Analysis
{
    using System;

    using SimpleInjector.Lifestyles;

    internal static class LifestyleMismatchServices
    {
        internal static bool DependencyHasPossibleLifestyleMismatch(KnownRelationship relationship)
        {
            Lifestyle componentLifestyle = relationship.Lifestyle;
            Lifestyle dependencyLifestyle = relationship.Dependency.Lifestyle;

            // If the lifestyles are the same instance, we consider them valid, even though in theory
            // an hybrid lifestyle could screw things up. In practice this would be very unlikely, since
            // the Func<bool> test delegate would typically return the same value within a given context.
            if (object.ReferenceEquals(componentLifestyle, dependencyLifestyle) &&
                componentLifestyle != UnknownLifestyle.Instance)
            {
                return false;
            }

            return componentLifestyle.ComponentLength > dependencyLifestyle.DependencyLength;
        }
    }
}