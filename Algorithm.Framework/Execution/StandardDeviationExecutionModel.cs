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
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.Framework.Execution
{
    /// <summary>
    /// Execution model that submits orders while the current market prices is at least the configured number of standard
    /// deviations away from the mean in the favorable direction (below/above for buy/sell respectively)
    /// </summary>
    public class StandardDeviationExecutionModel : BaseExecutionModel<StandardDeviationExecutionModel.StandardDeviationSymbolData>
    {
        private readonly int _period;
        private readonly decimal _deviations;
        private readonly IOrderSizingStrategy _orderSizingStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardDeviationExecutionModel"/> class
        /// </summary>
        /// <param name="orderSizingStrategy">Defines the maximum order size</param>
        /// <param name="period">Period of the standard deviation indicator, created in the security's configured resolution</param>
        /// <param name="deviations">The number of deviations away from the mean before submitting an order</param>
        public StandardDeviationExecutionModel(IOrderSizingStrategy orderSizingStrategy, int period, decimal deviations)
        {
            _period = period;
            _deviations = deviations;
            _orderSizingStrategy = orderSizingStrategy;
        }

        /// <summary>
        /// Executes market orders if the standard deviation of price is more than the configured number of deviations
        /// in the favorable direction.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="symbolData">Data for symbols who still need to submit more orders to reach their portfolio target</param>
        protected override void ExecuteTargets(QCAlgorithmFramework algorithm, IEnumerable<StandardDeviationSymbolData> symbolData)
        {
            foreach (var data in symbolData)
            {
                var remaining = data.UnorderedQuantity;
                if (data.STD.IsReady && PriceIsFavorable(data))
                {
                    // determine order size as minimum between remaining and maximum
                    var maxOrderSize = _orderSizingStrategy.GetMaximumOrderSize(algorithm, data.Symbol);
                    var orderSize = Math.Min(maxOrderSize, Math.Abs(remaining));

                    // round down to even lot size
                    orderSize -= orderSize % data.Security.SymbolProperties.LotSize;
                    if (orderSize != 0)
                    {
                        algorithm.MarketOrder(data.Symbol, Math.Sign(remaining) * orderSize);
                    }
                }
            }
        }

        /// <summary>
        /// Creates the symbol data class used to track model-specific data for the specified symbol
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="symbol">The symbol</param>
        /// <returns>A new instance of the symbol data class</returns>
        protected override IExecutionModelSymbolData CreateSymbolData(QCAlgorithmFramework algorithm, Symbol symbol)
        {
            return new StandardDeviationSymbolData(algorithm, symbol, _period);
        }

        /// <summary>
        /// Determines if the current price is more than the configured number of standard deviations
        /// away from the mean in the favorable direction.
        /// </summary>
        private bool PriceIsFavorable(StandardDeviationSymbolData data)
        {
            if (data.UnorderedQuantity > 0)
            {
                var price = data.Security.BidPrice == 0
                    ? data.Security.Price
                    : data.Security.BidPrice;

                var threshold = data.SMA + _deviations * data.STD;

                if (price > threshold)
                {
                    return false;
                }
            }
            else
            {
                var price = data.Security.AskPrice == 0
                    ? data.Security.AskPrice
                    : data.Security.Price;

                var threshold = data.SMA - _deviations * data.STD;

                if (price < threshold)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Standard deviation model-specific data manages the standard deviation indicator
        /// </summary>
        public class StandardDeviationSymbolData : BaseExecutionModelSymbolData
        {
            private readonly QCAlgorithmFramework _algorithm;
            private readonly IDataConsolidator _consolidator;

            public StandardDeviation STD { get; }
            public SimpleMovingAverage SMA { get; }

            public StandardDeviationSymbolData(QCAlgorithmFramework algorithm, Symbol symbol, int period)
                : base(algorithm, symbol)
            {
                _algorithm = algorithm;

                var name = algorithm.CreateIndicatorName(symbol, "STD" + period, Security.Resolution);
                STD = new StandardDeviation(name, period);

                var smaName = algorithm.CreateIndicatorName(symbol, "SMA" + period, Security.Resolution);
                SMA = new SimpleMovingAverage(smaName, period);

                _consolidator = algorithm.ResolveConsolidator(symbol, Security.Resolution);
                _consolidator.DataConsolidated += ConsolidatorOnDataConsolidated;
                algorithm.SubscriptionManager.AddConsolidator(Symbol, _consolidator);
            }

            /// <summary>
            /// Invoked when this symbol's data has been removed from the model
            /// </summary>
            public override void OnRemoved()
            {
                _consolidator.DataConsolidated -= ConsolidatorOnDataConsolidated;
                _algorithm.SubscriptionManager.RemoveConsolidator(Symbol, _consolidator);
            }

            private void ConsolidatorOnDataConsolidated(object sender, IBaseData consolidated)
            {
                STD.Update(consolidated.EndTime, consolidated.Value);
                SMA.Update(consolidated.EndTime, consolidated.Value);
            }
        }
    }
}
