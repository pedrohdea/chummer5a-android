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

// Metade dependente de WinForms de VehicleMod, separada do domínio durante a
// extração do núcleo (DEC-023). A metade de regras vive em Chummer/Backend/Equipment/VehicleMod.cs.
//
// As duas são `partial class VehicleMod`: no projeto legado voltam a ser uma classe
// só, então nenhum chamador precisou mudar. No Chummer.Core, só o domínio existe.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using NLog;

namespace Chummer.Backend.Equipment
{
    public partial class VehicleMod
    {
        /// <summary>
        /// Creates a synthetic tree node that groups vehicle mods of this mod's category.
        /// </summary>
        /// <param name="token">Cancellation token to listen to.</param>
        /// <returns>A tree node whose Tag is the category group key.</returns>
        public async Task<TreeNode> CreateCategoryGroupTreeNode(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return new TreeNode
            {
                Tag = GetCategoryGroupTag(Category),
                Text = await GetCategoryGroupDisplayNameAsync(Category, _objCharacter, token).ConfigureAwait(false)
            };
        }
        /// <summary>
        /// Adds vehicle mod tree nodes to a parent collection, optionally grouping by category.
        /// </summary>
        /// <param name="lstMods">Mods to add.</param>
        /// <param name="lstChildNodes">Parent node collection to populate.</param>
        /// <param name="cmsVehicleMod">ContextMenuStrip for Vehicle Mods.</param>
        /// <param name="cmsCyberware">ContextMenuStrip for Cyberware.</param>
        /// <param name="cmsCyberwareGear">ContextMenuStrip for Gear in Cyberware.</param>
        /// <param name="cmsVehicleWeapon">ContextMenuStrip for Vehicle Weapons.</param>
        /// <param name="cmsVehicleWeaponAccessory">ContextMenuStrip for Vehicle Weapon Accessories.</param>
        /// <param name="cmsVehicleWeaponAccessoryGear">ContextMenuStrip for Gear in Vehicle Weapon Accessories.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public static async Task AddModsToTreeNodeCollection(
            TaggedObservableCollection<VehicleMod> lstMods,
            TreeNodeCollection lstChildNodes,
            ContextMenuStrip cmsVehicleMod,
            ContextMenuStrip cmsCyberware,
            ContextMenuStrip cmsCyberwareGear,
            ContextMenuStrip cmsVehicleWeapon,
            ContextMenuStrip cmsVehicleWeaponAccessory,
            ContextMenuStrip cmsVehicleWeaponAccessoryGear,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (lstMods == null || lstChildNodes == null)
                return;

            if (!GlobalSettings.GroupVehicleModsByCategory)
            {
                await lstMods.ForEachAsync(async objMod =>
                {
                    TreeNode objLoopNode = await objMod.CreateTreeNode(cmsVehicleMod, cmsCyberware, cmsCyberwareGear,
                        cmsVehicleWeapon, cmsVehicleWeaponAccessory, cmsVehicleWeaponAccessoryGear, token).ConfigureAwait(false);
                    if (objLoopNode != null)
                        lstChildNodes.Add(objLoopNode);
                }, token).ConfigureAwait(false);
                return;
            }

            Dictionary<string, TreeNode> dicCategories = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase);
            await lstMods.ForEachAsync(async objMod =>
            {
                TreeNode objLoopNode = await objMod.CreateTreeNode(cmsVehicleMod, cmsCyberware, cmsCyberwareGear,
                    cmsVehicleWeapon, cmsVehicleWeaponAccessory, cmsVehicleWeaponAccessoryGear, token).ConfigureAwait(false);
                if (objLoopNode == null)
                    return;

                string strCategoryKey = GetCategoryGroupKey(objMod.Category);
                if (!dicCategories.TryGetValue(strCategoryKey, out TreeNode nodCategory))
                {
                    nodCategory = await objMod.CreateCategoryGroupTreeNode(token).ConfigureAwait(false);
                    dicCategories.Add(strCategoryKey, nodCategory);
                }

                nodCategory.Nodes.Add(objLoopNode);
                nodCategory.Expand();
            }, token).ConfigureAwait(false);

            foreach (string strCategoryKey in dicCategories.Keys
                         .OrderBy(GetCategoryGroupSortOrder)
                         .ThenBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                lstChildNodes.Add(dicCategories[strCategoryKey]);
            }
        }
        /// <summary>
        /// Add a piece of Armor to the Armor TreeView.
        /// </summary>
        public async Task<TreeNode> CreateTreeNode(ContextMenuStrip cmsVehicleMod, ContextMenuStrip cmsCyberware, ContextMenuStrip cmsCyberwareGear, ContextMenuStrip cmsVehicleWeapon, ContextMenuStrip cmsVehicleWeaponAccessory, ContextMenuStrip cmsVehicleWeaponAccessoryGear, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (IncludedInVehicle && !string.IsNullOrEmpty(Source) && !await (await _objCharacter.GetSettingsAsync(token).ConfigureAwait(false)).BookEnabledAsync(Source, token).ConfigureAwait(false))
                return null;

            TreeNode objNode = new TreeNode
            {
                Name = InternalId,
                Text = await GetCurrentDisplayNameAsync(token).ConfigureAwait(false),
                Tag = this,
                ContextMenuStrip = cmsVehicleMod,
                ForeColor = await GetPreferredColorAsync(token).ConfigureAwait(false),
                ToolTipText = (await GetNotesAsync(token).ConfigureAwait(false)).WordWrap()
            };

            TreeNodeCollection lstChildNodes = objNode.Nodes;
            // Cyberware.
            await Cyberware.ForEachAsync(async objCyberware =>
            {
                TreeNode objLoopNode = await objCyberware.CreateTreeNode(cmsCyberware, cmsCyberwareGear, token).ConfigureAwait(false);
                if (objLoopNode != null)
                    lstChildNodes.Add(objLoopNode);
            }, token).ConfigureAwait(false);

            // VehicleWeapons.
            await Weapons.ForEachAsync(async objWeapon =>
            {
                TreeNode objLoopNode = await objWeapon.CreateTreeNode(cmsVehicleWeapon, cmsVehicleWeaponAccessory,
                    cmsVehicleWeaponAccessoryGear, token).ConfigureAwait(false);
                if (objLoopNode != null)
                    lstChildNodes.Add(objLoopNode);
            }, token).ConfigureAwait(false);

            if (lstChildNodes.Count > 0)
                objNode.Expand();

            return objNode;
        }
        public void SetSourceDetail(Control sourceControl)
        {
            if (_objCachedSourceDetail.Language != GlobalSettings.Language)
                _objCachedSourceDetail = default;
            SourceDetail.SetControl(sourceControl);
        }
        public async Task SetSourceDetailAsync(Control sourceControl, CancellationToken token = default)
        {
            if (_objCachedSourceDetail.Language != GlobalSettings.Language)
                _objCachedSourceDetail = default;
            await (await GetSourceDetailAsync(token).ConfigureAwait(false)).SetControlAsync(sourceControl, token).ConfigureAwait(false);
        }
    }
}
