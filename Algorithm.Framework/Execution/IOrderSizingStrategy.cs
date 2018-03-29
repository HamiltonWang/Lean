/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

namespace QuantConnect.Algorithm.Framework.Execution
{
    /// <summary>
    /// Provides the maximum order size submittable by the execution model at the current time step.
    /// This is intended to be used by execution models to abstract away
    /// </summary>
    public interface IOrderSizingStrategy
    {
        /// <summary>
        /// Gets the maximum order size for the specified symbol. This is an absolute quantity.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="symbol">The symbol being traded</param>
        /// <returns>The maximum order size</returns>
        decimal GetMaximumOrderSize(QCAlgorithmFramework algorithm, Symbol symbol);
    }
}