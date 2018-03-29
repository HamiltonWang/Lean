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
    public class PercentVolumeOrderSizingStrategyTests
    {
        [Test]
        [TestCase(0.01, 10000)]
        [TestCase(0.25, 0)]
        [TestCase(0, 10*1000)]
        [TestCase(1, 1000)]
        public void CalculatesOrderSizeAsSpecifiedPercentOfCurrentVolume(decimal percentage, decimal volume)
        {
            var algorithm = new QCAlgorithmFramework();
            var security = algorithm.AddEquity("SPY");
            security.SetMarketPrice(new TradeBar
            {
                Close = 1,
                Volume = volume
            });

            var strategy = new PercentVolumeOrderSizingStrategy(percentage);
            var orderSize = strategy.GetMaximumOrderSize(algorithm, security.Symbol);

            Assert.AreEqual(volume*percentage, orderSize);
        }
    }
}
