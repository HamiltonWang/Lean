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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.Framework.Execution
{
    /// <summary>
    /// Execution model that submits orders while the current market price is more favorable that the current volume weighted average price.
    /// </summary>
    public class VolumeWeightedAveragePriceExecutionModel : BaseExecutionModel<VolumeWeightedAveragePriceExecutionModel.VwapSymbolData>
    {
        private readonly TimeSpan _minimumTimeAfterMarketOpen;
        private readonly IOrderSizingStrategy _orderSizingStrategy;

        /// <summary>
        /// Initializes a new default instance of the <see cref="VolumeWeightedAveragePriceExecutionModel"/> class
        /// using a maximum order size of 1/2% of the volume per time step.
        /// </summary>
        public VolumeWeightedAveragePriceExecutionModel()
            : this(new PercentVolumeOrderSizingStrategy(0.005m), TimeSpan.Zero)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeWeightedAveragePriceExecutionModel"/> class
        /// </summary>
        /// <param name="orderSizingStrategy">Defines the maximum order size</param>
        /// <param name="minimumTimeAfterMarketOpen">Defines the minimum time after market open before submitting orders</param>
        public VolumeWeightedAveragePriceExecutionModel(IOrderSizingStrategy orderSizingStrategy, TimeSpan minimumTimeAfterMarketOpen)
        {
            _orderSizingStrategy = orderSizingStrategy;
            _minimumTimeAfterMarketOpen = minimumTimeAfterMarketOpen;
        }

        /// <summary>
        /// Uses the model-specific symbol data to execute on the request portfolio targets.
        /// The symbol data provided here only contains the data for symbols whose portfolio
        /// targets have NOT yet been reached.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="symbolData">Data for symbols who still need to submit more orders to reach their portfolio target</param>
        protected override void ExecuteTargets(QCAlgorithmFramework algorithm, IEnumerable<VwapSymbolData> symbolData)
        {
            foreach (var data in symbolData)
            {
                var remaining = data.UnorderedQuantity;

                // VWAP at market open is always at the current price level, it's very common
                // to define a warmup period for VWAP. also, many market participants prefer to
                // wait until price stabilizes before issuing orders to the market
                var currentLocalTime = data.Security.LocalTime;
                var marketOpen = data.Security.Exchange.Hours.GetNextMarketOpen(currentLocalTime.Date, false);
                if (currentLocalTime - marketOpen < _minimumTimeAfterMarketOpen)
                {
                    continue;
                }

                if (PriceIsFavorable(data))
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
        /// Determines if the current price is better than VWAP
        /// </summary>
        private static bool PriceIsFavorable(VwapSymbolData data)
        {
            if (data.UnorderedQuantity > 0)
            {
                var price = data.Security.BidPrice == 0
                    ? data.Security.Price
                    : data.Security.BidPrice;

                if (price > data.VWAP)
                {
                    return false;
                }
            }
            else
            {
                var price = data.Security.AskPrice == 0
                    ? data.Security.AskPrice
                    : data.Security.Price;

                if (price < data.VWAP)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates the symbol data class used to track model-specific data for the specified symbol
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="symbol">The symbol</param>
        /// <returns>A new instance of the symbol data class</returns>
        protected override IExecutionModelSymbolData CreateSymbolData(QCAlgorithmFramework algorithm, Symbol symbol)
        {
            return new VwapSymbolData(algorithm, symbol);
        }

        /// <summary>
        /// VWAP model specific symbol data manages the VWAP indicator and the security's current volume
        /// </summary>
        public class VwapSymbolData : BaseExecutionModelSymbolData
        {
            private readonly IDataConsolidator _consolidator;

            public IntradayVwap VWAP { get; }
            public decimal Volume => Security.Volume;

            public VwapSymbolData(QCAlgorithmFramework algorithm, Symbol symbol)
                : base(algorithm, symbol)
            {
                var subscription = Security.Subscriptions.OrderBy(s => s.Resolution).FirstOrDefault(s =>
                    s.TickType == TickType.Trade && s.Type == typeof(Tick)
                    || s.Type == typeof(TradeBar)
                );

                if (subscription == null)
                {
                    throw new Exception($"Unable to use {nameof(VolumeWeightedAveragePriceExecutionModel)} without trade data.");
                }

                var name = algorithm.CreateIndicatorName(symbol, "VWAP", subscription.Resolution);

                // create and register our vwap indicator for price updates
                if (subscription.Type == typeof(Tick))
                {
                    VWAP = new TickIntradayVwap(name);
                }
                else
                {
                    VWAP = new TradeBarIntradayVwap(name);
                }

                _consolidator = algorithm.ResolveConsolidator(symbol, subscription.Resolution);
                _consolidator.DataConsolidated += ConsolidatorOnDataConsolidated;
                algorithm.SubscriptionManager.AddConsolidator(Symbol, _consolidator);
            }

            public override void OnRemoved()
            {
                _consolidator.DataConsolidated -= ConsolidatorOnDataConsolidated;
                Algorithm.SubscriptionManager.RemoveConsolidator(Symbol, _consolidator);
            }

            private void ConsolidatorOnDataConsolidated(object sender, IBaseData consolidated)
            {
                VWAP.Update((BaseData)consolidated);
            }
        }

        /// <summary>
        /// Defines the canonical intraday VWAP indicator
        /// </summary>
        public abstract class IntradayVwap : IndicatorBase<BaseData>
        {
            private DateTime _lastDate;
            private decimal _sumOfVolume;
            private decimal _sumOfPriceTimesVolume;

            /// <summary>
            /// Gets a flag indicating when this indicator is ready and fully initialized
            /// </summary>
            public override bool IsReady => _sumOfVolume > 0;

            /// <summary>
            /// Initializes a new instance of the <see cref="IntradayVwap"/> class
            /// </summary>
            /// <param name="name">The name of the indicator</param>
            protected IntradayVwap(string name)
                : base(name)
            {
            }

            /// <summary>
            /// Computes the new VWAP
            /// </summary>
            protected override IndicatorResult ValidateAndComputeNextValue(BaseData input)
            {
                decimal volume, averagePrice;
                if (!TryGetVolumeAndAveragePrice(input, out volume, out averagePrice))
                {
                    return new IndicatorResult(0, IndicatorStatus.InvalidInput);
                }

                // reset vwap on daily boundaries
                if (_lastDate != input.EndTime.Date)
                {
                    _sumOfVolume = 0m;
                    _sumOfPriceTimesVolume = 0m;
                    _lastDate = input.EndTime.Date;
                }

                // running totals for Σ PiVi / Σ Vi
                _sumOfVolume += volume;
                _sumOfPriceTimesVolume += averagePrice * volume;

                return _sumOfPriceTimesVolume / _sumOfVolume;
            }

            /// <summary>
            /// Computes the next value of this indicator from the given state.
            /// NOTE: This must be overriden since it's abstract in the base, but
            /// will never be invoked since we've override the validate method above.
            /// </summary>
            /// <param name="input">The input given to the indicator</param>
            /// <returns>A new value for this indicator</returns>
            protected override decimal ComputeNextValue(BaseData input)
            {
                throw new NotImplementedException($"{nameof(IntradayVwap)}.{nameof(ComputeNextValue)} should never be invoked.");
            }

            /// <summary>
            /// Determines the volume and price to be used for the current input in the VWAP computation
            /// </summary>
            protected abstract bool TryGetVolumeAndAveragePrice(BaseData input, out decimal volume, out decimal averagePrice);
        }

        /// <summary>
        /// Implements volume and price selection from tick data for the VWAP
        /// </summary>
        public class TickIntradayVwap : IntradayVwap
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TickIntradayVwap"/> class
            /// </summary>
            /// <param name="name">The name of the indicator</param>
            public TickIntradayVwap(string name)
                : base(name)
            {
            }

            /// <summary>
            /// Determines the volume and price to be used for the current input in the VWAP computation
            /// </summary>
            protected override bool TryGetVolumeAndAveragePrice(BaseData input, out decimal volume, out decimal averagePrice)
            {
                var tick = input as Tick;

                if (tick?.TickType != TickType.Trade)
                {
                    volume = 0;
                    averagePrice = 0;
                    return false;
                }

                volume = tick.Quantity;
                averagePrice = tick.LastPrice;
                return true;
            }
        }

        /// <summary>
        /// Implements volume and price selection from trade bar data for the VWAP
        /// </summary>
        public class TradeBarIntradayVwap : IntradayVwap
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TradeBarIntradayVwap"/> class
            /// </summary>
            /// <param name="name">The name of the indicator</param>
            public TradeBarIntradayVwap(string name)
                : base(name)
            {
            }

            /// <summary>
            /// Determines the volume and price to be used for the current input in the VWAP computation
            /// </summary>
            protected override bool TryGetVolumeAndAveragePrice(BaseData input, out decimal volume, out decimal averagePrice)
            {
                var tradeBar = input as TradeBar;

                if (tradeBar?.IsFillForward != false)
                {
                    volume = 0;
                    averagePrice = 0;
                    return false;
                }

                volume = tradeBar.Volume;
                averagePrice = (tradeBar.High + tradeBar.Low + tradeBar.Close) / 3m;
                return true;
            }
        }
    }
}