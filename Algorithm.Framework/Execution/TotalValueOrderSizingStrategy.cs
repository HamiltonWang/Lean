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
    /// Defines maximum order size in terms of the order value in account currency units.
    /// </summary>
    public class TotalValueOrderSizingStrategy : IOrderSizingStrategy
    {
        private readonly decimal _valueInAccountCurrency;

        /// <summary>
        /// Initializes a new instance of the <see cref="TotalValueOrderSizingStrategy"/> class
        /// </summary>
        /// <param name="valueInAccountCurrency">The maximum order value in account currency</param>
        public TotalValueOrderSizingStrategy(decimal valueInAccountCurrency)
        {
            _valueInAccountCurrency = valueInAccountCurrency;
        }

        /// <summary>
        /// Gets the maximum order size as the quantity required to reach the specified order value
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

            var priceInAccountCurrency = security.Price * security.QuoteCurrency.ConversionRate;

            if (priceInAccountCurrency == 0m)
            {
                return 0m;
            }

            return _valueInAccountCurrency / priceInAccountCurrency;
        }
    }
}