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

using NUnit.Framework;
using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Algorithm.Framework.Execution
{
    [TestFixture]
    public class TotalValueOrderSizingStrategyTests
    {
        [Test]
        [TestCase(10000, 10, 1)]
        [TestCase(10000, 10, 0)]
        [TestCase(10000, 0, 1)]
        [TestCase(0, 0, 1)]
        [TestCase(10000, 5.25, 1.0079)]
        public void CalculatesOrderSizeAsQuantityYieldingTotalOrderValue(decimal value, decimal price, decimal conversionRate)
        {

            var algorithm = new QCAlgorithmFramework();
            var security = algorithm.AddEquity("SPY");
            security.QuoteCurrency.ConversionRate = conversionRate;
            security.SetMarketPrice(new TradeBar
            {
                Close = price
            });

            var strategy = new TotalValueOrderSizingStrategy(value);
            var orderSize = strategy.GetMaximumOrderSize(algorithm, security.Symbol);

            var expected = price*conversionRate == 0m ? 0m : value / (price * conversionRate);
            Assert.AreEqual(expected, orderSize);
        }
    }
}