/*  This file is part of Chummer5a.
 *
 *  Chummer5a is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Chummer5a is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Chummer5a.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  You can obtain the full source code for Chummer5a at
 *  https://github.com/chummer5a/chummer5a
 */
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chummer
{
    public interface IHasMatrixAttributes : IHasCharacterObject
    {
        int GetBaseMatrixAttribute(string strAttributeName);

        Task<int> GetBaseMatrixAttributeAsync(string strAttributeName, CancellationToken token = default);

        int GetBonusMatrixAttribute(string strAttributeName);

        Task<int> GetBonusMatrixAttributeAsync(string strAttributeName, CancellationToken token = default);

        /// <summary>
        /// Whether the Gear qualifies as a Program in the printout XML.
        /// </summary>
        bool IsProgram { get; }
        string Attack { get; set; }
        string Sleaze { get; set; }
        string DataProcessing { get; set; }
        string Firewall { get; set; }
        string ModAttack { get; set; }
        string ModSleaze { get; set; }
        string ModDataProcessing { get; set; }
        string ModFirewall { get; set; }

        /// <summary>
        /// ASDF attribute boosted by Overclocker.
        /// </summary>
        string Overclocked { get; set; }
        /// <summary>
        /// ASDF attribute boosted by Overclocker.
        /// </summary>
        Task<string> GetOverclockedAsync(CancellationToken token = default);
        /// <summary>
        /// String to determine if the device can form persona or grants persona forming to its parent.
        /// </summary>
        string CanFormPersona { get; set; }
        /// <summary>
        /// String to determine if the device can form persona or grants persona forming to its parent.
        /// </summary>
        Task<string> GetCanFormPersonaAsync(CancellationToken token = default);
        /// <summary>
        /// Is this device one that can form a persona?
        /// </summary>
        bool IsCommlink { get; }
        /// <summary>
        /// Is this device one that can form a persona?
        /// </summary>
        Task<bool> GetIsCommlinkAsync(CancellationToken token = default);

        string DeviceRating { get; set; }
        int BaseMatrixBoxes { get; }
        int BonusMatrixBoxes { get; set; }
        int TotalBonusMatrixBoxes { get; }
        int MatrixCM { get; }
        int MatrixCMFilled { get; set; }
        string ProgramLimit { get; set; }

        bool CanSwapAttributes { get; set; }
        string AttributeArray { get; set; }
        string ModAttributeArray { get; set; }

        IEnumerable<IHasMatrixAttributes> ChildrenWithMatrixAttributes { get; }
    }
}
