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

using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Execution
{
    /// <summary>
    /// Defines the maximum order size as a percentage of trading volume during the current time step.
    /// </summary>
    public class PercentVolumeOrderSizingStrategy : IOrderSizingStrategy
    {
        private readonly decimal _percent;

        /// <summary>
        /// Initializes a new instance of the <see cref="PercentVolumeOrderSizingStrategy"/> class
        /// </summary>
        /// <param name="percent"></param>
        public PercentVolumeOrderSizingStrategy(decimal percent)
        {
            _percent = percent;
        }

        /// <summary>
        /// Gets the maximum order size as a percentage of trading volume from the current time step.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="symbol">The symbol being traded</param>
        /// <returns>The maximum order size</returns>
        public decimal GetMaximumOrderSize(QCAlgorithmFramework algorithm, Symbol symbol)
        {
            Security security;
            if (!algorithm.Securities.TryGetValue(symbol, out security))
            {
                return 0m;
            }

            return _percent * security.Volume;
        }
    }
}