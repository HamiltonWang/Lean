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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.Framework.Execution
{
    /// <summary>
    /// Provides a base class to manage symbol data and common tasks between execution models
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseExecutionModel<T> : IExecutionModel
        where T : IExecutionModelSymbolData
    {
        private readonly ConcurrentDictionary<Symbol, IExecutionModelSymbolData> _symbolDataBySymbol;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseExecutionModel{T}"/> class
        /// </summary>
        protected BaseExecutionModel()
        {
            _symbolDataBySymbol = new ConcurrentDictionary<Symbol, IExecutionModelSymbolData>();
        }

        /// <summary>
        /// Submit orders for the specified portolio targets.
        /// This model is free to delay or spread out these orders as it sees fit
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="targets">The portfolio targets to be ordered</param>
        public void Execute(QCAlgorithmFramework algorithm, IEnumerable<IPortfolioTarget> targets)
        {
            foreach (var target in targets)
            {
                _symbolDataBySymbol.AddOrUpdate(target.Symbol,
                    sym =>
                    {
                        // create new symbol data for target
                        var sd = CreateSymbolData(algorithm, sym);
                        sd.SetTarget(target);
                        return sd;
                    },
                    (sym, sd) =>
                    {
                        // update existing symbolata w/ new target
                        sd.SetTarget(target);
                        return sd;
                    });
            }

            // invoke the derived implementation with remaining targets, casting to the configured symbol data type
            ExecuteTargets(algorithm, _symbolDataBySymbol.Select(kvp => kvp.Value).Where(sd => !sd.TargetReached).OfType<T>());
        }

        /// <summary>
        /// Uses the model-specific symbol data to execute on the request portfolio targets.
        /// The symbol data provided here only contains the data for symbols whose portfolio
        /// targets have NOT yet been reached.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="symbolData">Data for symbols who still need to submit more orders to reach their portfolio target</param>
        protected abstract void ExecuteTargets(QCAlgorithmFramework algorithm, IEnumerable<T> symbolData);

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public void OnSecuritiesChanged(QCAlgorithmFramework algorithm, SecurityChanges changes)
        {
            foreach (var added in changes.AddedSecurities)
            {
                // don't overwrite existing entries
                _symbolDataBySymbol.GetOrAdd(added.Symbol, sym => CreateSymbolData(algorithm, sym));
            }

            foreach (var removed in changes.RemovedSecurities)
            {
                IExecutionModelSymbolData data;
                if (_symbolDataBySymbol.TryRemove(removed.Symbol, out data))
                {
                    // give symbol data a chance to clean itself up upon removal from universe
                    data.OnRemoved();
                }
            }
        }

        /// <summary>
        /// Creates the symbol data class used to track model-specific data for the specified symbol
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="symbol">The symbol</param>
        /// <returns>A new instance of the symbol data class</returns>
        protected virtual IExecutionModelSymbolData CreateSymbolData(QCAlgorithmFramework algorithm, Symbol symbol)
        {
            return new BaseExecutionModelSymbolData(algorithm, symbol);
        }

        /// <summary>
        /// Provides derived types direct access to a particular symbol's data
        /// </summary>
        /// <param name="symbol">The symbol whose data we seek</param>
        /// <returns>The symbol data</returns>
        protected T GetSymbolData(Symbol symbol)
        {
            return (T) _symbolDataBySymbol[symbol];
        }
    }
}