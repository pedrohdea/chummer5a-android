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

// Metade dependente de WinForms de Vehicle, separada do domínio durante a
// extração do núcleo (DEC-023). A metade de regras vive em Chummer/Backend/Equipment/Vehicle.cs.
//
// As duas são `partial class Vehicle`: no projeto legado voltam a ser uma classe
// só, então nenhum chamador precisou mudar. No Chummer.Core, só o domínio existe.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Chummer.Annotations;
using NLog;
using TreeNode = System.Windows.Forms.TreeNode;
using TreeNodeCollection = System.Windows.Forms.TreeNodeCollection;

namespace Chummer.Backend.Equipment
{
    public partial class Vehicle
    {
        /// <summary>
        /// Add a Vehicle to the TreeView.
        /// </summary>
        /// <param name="cmsVehicle">ContextMenuStrip for the Vehicle Node.</param>
        /// <param name="cmsVehicleLocation">ContextMenuStrip for Vehicle Location Nodes.</param>
        /// <param name="cmsVehicleWeapon">ContextMenuStrip for Vehicle Weapon Nodes.</param>
        /// <param name="cmsWeaponAccessory">ContextMenuStrip for Vehicle Weapon Accessory Nodes.</param>
        /// <param name="cmsWeaponAccessoryGear">ContextMenuStrip for Gear in Vehicle Weapon Accessory Nodes.</param>
        /// <param name="cmsVehicleGear">ContextMenuStrip for Vehicle Gear Nodes.</param>
        /// <param name="cmsVehicleWeaponMount">ContextMenuStrip for Vehicle Weapon Mounts.</param>
        /// <param name="cmsCyberware">ContextMenuStrip for Cyberware.</param>
        /// <param name="cmsCyberwareGear">ContextMenuStrip for Gear in Cyberware.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public async Task<TreeNode> CreateTreeNode(ContextMenuStrip cmsVehicle, ContextMenuStrip cmsVehicleLocation, ContextMenuStrip cmsVehicleWeapon, ContextMenuStrip cmsWeaponAccessory, ContextMenuStrip cmsWeaponAccessoryGear, ContextMenuStrip cmsVehicleGear, ContextMenuStrip cmsVehicleWeaponMount, ContextMenuStrip cmsCyberware, ContextMenuStrip cmsCyberwareGear, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (!string.IsNullOrEmpty(ParentID) && !string.IsNullOrEmpty(Source) && !await (await _objCharacter.GetSettingsAsync(token).ConfigureAwait(false)).BookEnabledAsync(Source, token).ConfigureAwait(false))
                return null;

            TreeNode objNode = new TreeNode
            {
                Name = InternalId,
                Text = await GetCurrentDisplayNameAsync(token).ConfigureAwait(false),
                Tag = this,
                ContextMenuStrip = cmsVehicle,
                ForeColor = await GetPreferredColorAsync(token).ConfigureAwait(false),
                ToolTipText = (await GetNotesAsync(token).ConfigureAwait(false)).WordWrap()
            };

            TreeNodeCollection lstChildNodes = objNode.Nodes;
            // Populate the list of Vehicle Locations.
            await Locations
                .ForEachAsync(
                    async objLocation =>
                        lstChildNodes.Add(await objLocation.CreateTreeNode(cmsVehicleLocation, token)
                            .ConfigureAwait(false)), token).ConfigureAwait(false);

            // VehicleMods.
            await VehicleMod.AddModsToTreeNodeCollection(Mods, lstChildNodes, cmsVehicle, cmsCyberware, cmsCyberwareGear,
                cmsVehicleWeapon, cmsWeaponAccessory, cmsWeaponAccessoryGear, token).ConfigureAwait(false);
            if (await WeaponMounts.GetCountAsync(token).ConfigureAwait(false) > 0)
            {
                TreeNode nodMountsNode = new TreeNode
                {
                    Tag = "String_WeaponMounts",
                    Text = await LanguageManager.GetStringAsync("String_WeaponMounts", token: token).ConfigureAwait(false)
                };

                // Weapon Mounts
                await WeaponMounts.ForEachAsync(async objWeaponMount =>
                {
                    TreeNode objLoopNode = await objWeaponMount.CreateTreeNode(cmsVehicleWeaponMount, cmsVehicleWeapon,
                        cmsWeaponAccessory, cmsWeaponAccessoryGear, cmsCyberware, cmsCyberwareGear, cmsVehicle, token).ConfigureAwait(false);
                    if (objLoopNode != null)
                    {
                        nodMountsNode.Nodes.Add(objLoopNode);
                        nodMountsNode.Expand();
                    }
                }, token).ConfigureAwait(false);

                if (nodMountsNode.Nodes.Count > 0)
                {
                    if (VehicleMod.ShouldNestWeaponMountsUnderWeaponsCategory)
                    {
                        TreeNode nodWeaponsCategory = null;
                        foreach (TreeNode objChild in lstChildNodes)
                        {
                            if (objChild.Tag is string strTag
                                && strTag == VehicleMod.GetCategoryGroupTag(VehicleMod.WeaponsCategoryKey))
                            {
                                nodWeaponsCategory = objChild;
                                break;
                            }
                        }

                        if (nodWeaponsCategory == null)
                        {
                            nodWeaponsCategory = new TreeNode
                            {
                                Tag = VehicleMod.GetCategoryGroupTag(VehicleMod.WeaponsCategoryKey),
                                Text = await VehicleMod.GetCategoryGroupDisplayNameAsync(
                                    VehicleMod.WeaponsCategoryKey, _objCharacter, token).ConfigureAwait(false)
                            };
                            // Insert Weapons category in preferred order among existing category groups
                            int intInsert = lstChildNodes.Count;
                            for (int i = 0; i < lstChildNodes.Count; ++i)
                            {
                                if (!VehicleMod.IsCategoryGroupTag(lstChildNodes[i].Tag))
                                {
                                    intInsert = i;
                                    break;
                                }

                                string strExistingKey = ((string)lstChildNodes[i].Tag)
                                    .Substring(VehicleMod.CategoryGroupTagPrefix.Length);
                                if (VehicleMod.GetCategoryGroupSortOrder(strExistingKey)
                                    > VehicleMod.GetCategoryGroupSortOrder(VehicleMod.WeaponsCategoryKey))
                                {
                                    intInsert = i;
                                    break;
                                }
                            }

                            lstChildNodes.Insert(intInsert, nodWeaponsCategory);
                        }

                        nodWeaponsCategory.Nodes.Add(nodMountsNode);
                        nodWeaponsCategory.Expand();
                    }
                    else
                    {
                        lstChildNodes.Add(nodMountsNode);
                    }
                }
            }
            // Vehicle Weapons (not attached to a mount).
            await Weapons.ForEachAsync(async objWeapon =>
            {
                TreeNode objLoopNode = await objWeapon.CreateTreeNode(cmsVehicleWeapon, cmsWeaponAccessory,
                    cmsWeaponAccessoryGear, token).ConfigureAwait(false);
                if (objLoopNode != null)
                {
                    TreeNode objParent = objNode;
                    if (objWeapon.Location != null)
                    {
                        foreach (TreeNode objFind in lstChildNodes)
                        {
                            if (objFind.Tag != objWeapon.Location)
                                continue;
                            objParent = objFind;
                            break;
                        }
                    }

                    objParent.Nodes.Add(objLoopNode);
                    objParent.Expand();
                }
            }, token).ConfigureAwait(false);

            // Vehicle Gear.
            await GearChildren.ForEachAsync(async objGear =>
            {
                TreeNode objLoopNode = await objGear.CreateTreeNode(cmsVehicleGear, null, token).ConfigureAwait(false);
                if (objLoopNode != null)
                {
                    TreeNode objParent = objNode;
                    if (objGear.Location != null)
                    {
                        foreach (TreeNode objFind in lstChildNodes)
                        {
                            if (objFind.Tag != objGear.Location)
                                continue;
                            objParent = objFind;
                            break;
                        }
                    }

                    objParent.Nodes.Add(objLoopNode);
                    objParent.Expand();
                }
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
