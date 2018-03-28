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

using QuantConnect.Algorithm.Framework.Portfolio;

namespace QuantConnect.Algorithm.Framework.Execution
{
    /// <summary>
    /// Defines properties and methods invoked by the <see cref="BaseExecutionModel{T}"/>.
    /// To use your own symbol data type, recommend subclassing <see cref="BaseExecutionModelSymbolData"/>,
    /// or you can implement this interface directly.
    /// </summary>
    public interface IExecutionModelSymbolData
    {
        /// <summary>
        /// Gets the symbol
        /// </summary>
        Symbol Symbol { get; }

        /// <summary>
        /// Gets the portfolio target
        /// </summary>
        IPortfolioTarget Target { get; }

        /// <summary>
        /// Gets whether or not this target has been reached and requires no further processing by the execution model
        /// </summary>
        bool TargetReached { get; }

        /// <summary>
        /// Invoked when this symbol's data has been removed from the model
        /// </summary>
        void OnRemoved();

        /// <summary>
        /// Sets the portoflio target for this symbol
        /// </summary>
        /// <param name="target">The new portfolio target for this symbol</param>
        void SetTarget(IPortfolioTarget target);
    }
}