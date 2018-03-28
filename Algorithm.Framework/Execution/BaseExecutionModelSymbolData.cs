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

using System;
using System.Linq;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Execution
{
    /// <summary>
    /// Provides a default implementation of <see cref="IExecutionModelSymbolData"/>
    /// </summary>
    public class BaseExecutionModelSymbolData : IExecutionModelSymbolData
    {
        protected readonly QCAlgorithmFramework Algorithm;

        /// <summary>
        /// Gets the symbol
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the security object
        /// </summary>
        public Security Security { get; }

        /// <summary>
        /// Gets the portfolio target for this symbol
        /// </summary>
        public IPortfolioTarget Target { get; private set; }

        /// <summary>
        /// Gets the algorithm's current holdings quantity for this symbol
        /// </summary>
        public virtual decimal HoldingsQuantity => Security.Holdings.Quantity;

        /// <summary>
        /// Gets the remaining quantity still requiring order placement
        /// </summary>
        public virtual decimal UnorderedQuantity => (Target?.Quantity ?? 0) - HoldingsQuantity - OpenOrderQuantity;

        /// <summary>
        /// Gets the unordered quantity in open orders for this symbol
        /// </summary>
        public virtual decimal OpenOrderQuantity => Algorithm.Transactions.GetOpenOrders(Symbol).Sum(o => o.Quantity);

        /// <summary>
        /// Gets whether or not this target has been reached and requires no further processing by the execution model
        /// </summary>
        public virtual bool TargetReached => Math.Abs(UnorderedQuantity) < Security.SymbolProperties.LotSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseExecutionModelSymbolData"/> class
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="symbol">The symbol</param>
        public BaseExecutionModelSymbolData(QCAlgorithmFramework algorithm, Symbol symbol)
        {
            Algorithm = algorithm;

            Symbol = symbol;
            Security = algorithm.Securities[symbol];
        }

        public virtual void SetTarget(IPortfolioTarget target)
        {
            Target = target;
        }

        public virtual void OnRemoved()
        {
            //NOP -- derived types can use this to perform any security-specific clean up, such as removing indicators
        }
    }
}