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

// Metade dependente de WinForms de Weapon, separada do domínio durante a
// extração do núcleo (DEC-023). A metade de regras vive em Chummer/Backend/Equipment/Weapon.cs.
//
// As duas são `partial class Weapon`: no projeto legado voltam a ser uma classe
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
using Chummer.Backend.Attributes;
using Chummer.Backend.Enums;
using Chummer.Backend.Skills;
using Microsoft.VisualStudio.Threading;
using NLog;
using IAsyncDisposable = System.IAsyncDisposable;

namespace Chummer.Backend.Equipment
{
    public partial class Weapon
    {
        public async Task Reload(IAsyncCollection<Gear> lstGears, TreeView treGearView,
                                      CancellationToken token = default)
        {
            List<string> lstCount = new List<string>(1);
            string ammoString
                = await CalculatedAmmoAsync(GlobalSettings.InvariantCultureInfo, GlobalSettings.DefaultLanguage, token)
                    .ConfigureAwait(false);
            if (!RequireAmmo)
            {
                // For weapons that have ammo capacities but no requirement for ammo, these are charges
                // We treat this function differently for them, letting the character reload as many charges as they see fit

                Clip objInternalClip = GetClip(ActiveAmmoSlot);
                if (objInternalClip == null)
                {
                    await RecreateInternalClipAsync(token).ConfigureAwait(false);
                    objInternalClip = GetClip(ActiveAmmoSlot) ?? throw new InvalidOperationException(nameof(objInternalClip));
                }

                int intCurrentAmmoCount = objInternalClip.Ammo;

                // Determine which loading methods are available to the Weapon.
                if (ammoString.IndexOfAny('×', 'x', '+') != -1 ||
                    ammoString.Contains(" or ", StringComparison.OrdinalIgnoreCase) ||
                    ammoString.Contains("Special", StringComparison.OrdinalIgnoreCase) ||
                    ammoString.Contains("External Source", StringComparison.OrdinalIgnoreCase))
                {
                    string strWeaponAmmo = ammoString.FastEscape("External Source", StringComparison.OrdinalIgnoreCase);
                    strWeaponAmmo = strWeaponAmmo.ToLowerInvariant();
                    // Get rid of or belt, and + energy.
                    strWeaponAmmo = strWeaponAmmo.FastEscapeOnceFromEnd(" + energy", eComparison: StringComparison.OrdinalIgnoreCase)
                                                 .Replace(" or belt", " or 250(belt)");

                    foreach (string strAmmo in
                             strWeaponAmmo.SplitNoAlloc(" or ", StringSplitOptions.RemoveEmptyEntries, StringComparison.OrdinalIgnoreCase))
                    {
                        lstCount.Add(AmmoCapacity(strAmmo));
                    }
                }
                else
                {
                    // Nothing weird in the ammo string, so just use the number given.
                    string strAmmo = ammoString;
                    int intPos = strAmmo.IndexOf('(');
                    if (intPos != -1)
                        strAmmo = strAmmo.Substring(0, intPos);
                    lstCount.Add(strAmmo);
                }

                int intMaxAmmoCount = 0;

                foreach (string strAmmo in lstCount)
                {
                    if (int.TryParse(strAmmo, NumberStyles.Any, GlobalSettings.InvariantCultureInfo, out intMaxAmmoCount))
                    {
                        break;
                    }
                }

                if (intMaxAmmoCount <= intCurrentAmmoCount)
                    return;

                string strDescription = string.Format(
                    GlobalSettings.CultureInfo,
                    await LanguageManager.GetStringAsync("Message_SelectNumberOfCharges", token: token)
                                         .ConfigureAwait(false),
                    await GetCurrentDisplayNameAsync(token).ConfigureAwait(false));
                using (ThreadSafeForm<SelectNumber> frmNewAmmoCount = await ThreadSafeForm<SelectNumber>.GetAsync(
                           () => new SelectNumber(0)
                           {
                               AllowCancel = true,
                               Maximum = intMaxAmmoCount,
                               Minimum = intCurrentAmmoCount,
                               Description = strDescription
                           }, token).ConfigureAwait(false))
                {
                    if (await frmNewAmmoCount.ShowDialogSafeAsync(_objCharacter, token).ConfigureAwait(false)
                        != DialogResult.OK)
                        return;

                    objInternalClip.Ammo = frmNewAmmoCount.MyForm.SelectedValue.ToInt32();
                }

                return;
            }

            bool blnExternalSource = false;
            List<Gear> lstAmmo = new List<Gear>(1);
            // Determine which loading methods are available to the Weapon.
            if (ammoString.IndexOfAny('×', 'x', '+') != -1 ||
                ammoString.Contains(" or ", StringComparison.OrdinalIgnoreCase) ||
                ammoString.Contains("Special", StringComparison.OrdinalIgnoreCase) ||
                ammoString.Contains("External Source", StringComparison.OrdinalIgnoreCase))
            {
                string strWeaponAmmo = ammoString;
                if (strWeaponAmmo.Contains("External Source", StringComparison.OrdinalIgnoreCase))
                {
                    blnExternalSource = true;
                    strWeaponAmmo = strWeaponAmmo.FastEscape("External Source", StringComparison.OrdinalIgnoreCase);
                }

                strWeaponAmmo = strWeaponAmmo.ToLowerInvariant();
                // Get rid of or belt, and + energy.
                strWeaponAmmo = strWeaponAmmo.FastEscapeOnceFromEnd(" + energy", eComparison: StringComparison.OrdinalIgnoreCase)
                                             .Replace(" or belt", " or 250(belt)");

                foreach (string strAmmo in
                         strWeaponAmmo.SplitNoAlloc(" or ", StringSplitOptions.RemoveEmptyEntries, StringComparison.OrdinalIgnoreCase))
                {
                    lstCount.Add(AmmoCapacity(strAmmo));
                }
            }
            else
            {
                // Nothing weird in the ammo string, so just use the number given.
                string strAmmo = ammoString;
                int intPos = strAmmo.IndexOf('(');
                if (intPos != -1)
                    strAmmo = strAmmo.Substring(0, intPos);
                lstCount.Add(strAmmo);
            }

            Gear objExternalSource = null;
            if (blnExternalSource)
            {
                lstCount.Add(await LanguageManager.GetStringAsync("String_ExternalSource", token: token)
                                                  .ConfigureAwait(false));
                objExternalSource = new Gear(_objCharacter);
                try
                {
                    objExternalSource.Name = await LanguageManager.GetStringAsync("String_ExternalSource", token: token)
                                                .ConfigureAwait(false);
                    objExternalSource.SourceID = Guid.Empty;
                }
                catch
                {
                    await objExternalSource.DeleteGearAsync(token: CancellationToken.None).ConfigureAwait(false);
                    throw;
                }
            }

            if (RequireAmmo)
            {
                lstAmmo.AddRange(GetAmmoReloadable(lstGears));
                // Make sure the character has some form of Ammunition for this Weapon.
                if (lstAmmo.Count == 0)
                {
                    await Program.ShowScrollableMessageBoxAsync(string.Format(GlobalSettings.CultureInfo,
                            await LanguageManager
                                .GetStringAsync("Message_OutOfAmmoType", token: token)
                                .ConfigureAwait(false),
                            await GetCurrentDisplayNameAsync(token).ConfigureAwait(false)),
                        await LanguageManager.GetStringAsync("Message_OutOfAmmo", token: token)
                            .ConfigureAwait(false),
                        icon: MessageBoxIcon.Warning, token: token).ConfigureAwait(false);
                    return;
                }
            }

            if (objExternalSource != null)
                lstAmmo.Add(objExternalSource);

            // Show the Ammunition Selection window.
            using (ThreadSafeForm<ReloadWeapon> frmReloadWeapon = await ThreadSafeForm<ReloadWeapon>.GetAsync(
                       () => new ReloadWeapon(this)
                       {
                           Ammo = lstAmmo,
                           Count = lstCount
                       }, token).ConfigureAwait(false))
            {
                if (await frmReloadWeapon.ShowDialogSafeAsync(_objCharacter, token).ConfigureAwait(false)
                    != DialogResult.OK)
                    return;

                Gear objCurrentlyLoadedAmmo = AmmoLoaded;
                Gear objSelectedAmmo;
                decimal decQty = await frmReloadWeapon.MyForm.GetSelectedCountAsync(token).ConfigureAwait(false);
                string strSelectedAmmo = await frmReloadWeapon.MyForm.GetSelectedAmmoAsync(token).ConfigureAwait(false);
                // If an External Source is not being used, consume ammo.
                if (strSelectedAmmo != objExternalSource?.InternalId)
                {
                    objSelectedAmmo = await lstGears.DeepFindByIdAsync(strSelectedAmmo, token: token).ConfigureAwait(false);

                    // If the Ammo is coming from a Spare Clip, reduce the container quantity instead of the plugin quantity.
                    if (objSelectedAmmo.Parent is Gear objParent &&
                        (objParent.Name.StartsWith("Spare Clip", StringComparison.Ordinal)
                         || objParent.Name.StartsWith("Speed Loader", StringComparison.Ordinal)))
                    {
                        if (objParent.Quantity > 1)
                        {
                            // Duplicate the clip into a new entry where we can directly deduct from the quantity as we fire
                            Gear objDuplicatedParent = new Gear(_objCharacter);
                            try
                            {
                                await objDuplicatedParent.CopyAsync(objParent, token).ConfigureAwait(false);
                                await objDuplicatedParent.SetQuantityAsync(1, token).ConfigureAwait(false);
                                await lstGears.AddAsync(objDuplicatedParent, token).ConfigureAwait(false);
                                await objParent.SetQuantityAsync(objParent.Quantity - 1, token).ConfigureAwait(false);
                                Gear objNewSelectedAmmo
                                    = await objDuplicatedParent.Children.DeepFindByIdAsync(strSelectedAmmo, token: token).ConfigureAwait(false);
                                if (objNewSelectedAmmo == null)
                                {
                                    objNewSelectedAmmo = new Gear(_objCharacter);
                                    try
                                    {
                                        await objNewSelectedAmmo.CopyAsync(objSelectedAmmo, token).ConfigureAwait(false);
                                        await objDuplicatedParent.Children.AddAsync(objNewSelectedAmmo, token)
                                                                 .ConfigureAwait(false);
                                    }
                                    catch
                                    {
                                        await objNewSelectedAmmo.DeleteGearAsync(token: CancellationToken.None).ConfigureAwait(false);
                                        throw;
                                    }
                                }

                                objSelectedAmmo = objNewSelectedAmmo;
                            }
                            catch
                            {
                                await objDuplicatedParent.DeleteGearAsync(token: CancellationToken.None).ConfigureAwait(false);
                                throw;
                            }
                        }

                        string strParentText = await objParent.GetCurrentDisplayNameAsync(token).ConfigureAwait(false);
                        await treGearView.DoThreadSafeAsync(x =>
                        {
                            TreeNode objNode = x.FindNode(objParent.InternalId);
                            if (objNode != null)
                                objNode.Text = strParentText;
                        }, token: token).ConfigureAwait(false);
                    }

                    if (objSelectedAmmo.IsIdenticalToOtherGear(objCurrentlyLoadedAmmo))
                    {
                        // Just top up the currently loaded ammo
                        decimal decTopUp = decQty - objCurrentlyLoadedAmmo.Quantity;
                        if (decTopUp > objSelectedAmmo.Quantity)
                        {
                            // We need more ammo for a full top-up than the quantity of gear, so just merge the gears and delete the old gear.
                            await objCurrentlyLoadedAmmo.SetQuantityAsync(objCurrentlyLoadedAmmo.Quantity - objSelectedAmmo.Quantity, token).ConfigureAwait(false);
                            await objSelectedAmmo.DeleteGearAsync(token: token).ConfigureAwait(false);
                            GetClip(_intActiveAmmoSlot).Ammo
                                = objCurrentlyLoadedAmmo.Quantity
                                                        .ToInt32(); // Bypass AmmoRemaining so as not to alter the gear quantity
                        }
                        else
                        {
                            await objCurrentlyLoadedAmmo.SetQuantityAsync(decQty, token).ConfigureAwait(false);
                            await objSelectedAmmo.SetQuantityAsync(objSelectedAmmo.Quantity - decTopUp, token).ConfigureAwait(false);
                            string strCurrentlyLoadedText = await objCurrentlyLoadedAmmo
                                                                  .GetCurrentDisplayNameAsync(token)
                                                                  .ConfigureAwait(false);
                            string strSelectedText = await objSelectedAmmo
                                                           .GetCurrentDisplayNameAsync(token)
                                                           .ConfigureAwait(false);
                            await treGearView.DoThreadSafeAsync(x =>
                            {
                                // Refresh the Gear tree.
                                TreeNode objSelectedNode = x.FindNode(objCurrentlyLoadedAmmo.InternalId);
                                if (objSelectedNode != null)
                                    objSelectedNode.Text = strCurrentlyLoadedText;
                                objSelectedNode = x.FindNode(objSelectedAmmo.InternalId);
                                if (objSelectedNode != null)
                                    objSelectedNode.Text = strSelectedText;
                            }, token: token).ConfigureAwait(false);
                            GetClip(_intActiveAmmoSlot).Ammo
                                = decQty.ToInt32(); // Bypass AmmoRemaining so as not to alter the gear quantity
                        }

                        return;
                    }

                    if (objSelectedAmmo.Quantity > decQty)
                    {
                        // Duplicate the ammo into a new entry where we can directly deduct from the quantity as we fire
                        Gear objNewSelectedAmmo = new Gear(_objCharacter);
                        try
                        {
                            await objNewSelectedAmmo.CopyAsync(objSelectedAmmo, token).ConfigureAwait(false);
                            await objNewSelectedAmmo.SetQuantityAsync(decQty, token).ConfigureAwait(false);
                            await lstGears.AddAsync(objNewSelectedAmmo, token).ConfigureAwait(false);
                            await objSelectedAmmo.SetQuantityAsync(objSelectedAmmo.Quantity - decQty, token).ConfigureAwait(false);
                            string strId2 = objSelectedAmmo.InternalId;
                            string strText2 = await objSelectedAmmo.GetCurrentDisplayNameAsync(token).ConfigureAwait(false);
                            await treGearView.DoThreadSafeAsync(x =>
                            {
                                // Refresh the Gear tree.
                                TreeNode objSelectedNode = x.FindNode(strId2);
                                if (objSelectedNode != null)
                                    objSelectedNode.Text = strText2;
                            }, token: token).ConfigureAwait(false);
                            objSelectedAmmo = objNewSelectedAmmo;
                        }
                        catch
                        {
                            await objNewSelectedAmmo.DeleteGearAsync(token: CancellationToken.None).ConfigureAwait(false);
                            throw;
                        }
                    }
                    else if (decQty > objSelectedAmmo.Quantity)
                    {
                        decQty = objSelectedAmmo.Quantity;
                    }

                    string strId = objSelectedAmmo.InternalId;
                    string strText = await objSelectedAmmo.GetCurrentDisplayNameAsync(token).ConfigureAwait(false);
                    await treGearView.DoThreadSafeAsync(x =>
                    {
                        // Refresh the Gear tree.
                        TreeNode objSelectedNode = x.FindNode(strId);
                        if (objSelectedNode != null)
                            objSelectedNode.Text = strText;
                    }, token: token).ConfigureAwait(false);
                }
                else
                {
                    objSelectedAmmo = objExternalSource;
                }

                await SetAmmoLoadedAsync(objSelectedAmmo, token).ConfigureAwait(false);
                if (objCurrentlyLoadedAmmo != objSelectedAmmo)
                {
                    if (objCurrentlyLoadedAmmo != null)
                    {
                        string strId = objCurrentlyLoadedAmmo.InternalId;
                        string strText = await objCurrentlyLoadedAmmo.GetCurrentDisplayNameAsync(token)
                            .ConfigureAwait(false);
                        if (objSelectedAmmo != null)
                        {
                            string strId2 = objSelectedAmmo.InternalId;
                            string strText2 = await objSelectedAmmo.GetCurrentDisplayNameAsync(token)
                                .ConfigureAwait(false);
                            await treGearView.DoThreadSafeAsync(x =>
                            {
                                // Refresh the Gear tree.
                                TreeNode objSelectedNode = x.FindNode(strId);
                                if (objSelectedNode != null)
                                    objSelectedNode.Text = strText;
                                objSelectedNode = x.FindNode(strId2);
                                if (objSelectedNode != null)
                                    objSelectedNode.Text = strText2;
                            }, token: token).ConfigureAwait(false);
                        }
                        else
                        {
                            await treGearView.DoThreadSafeAsync(x =>
                            {
                                // Refresh the Gear tree.
                                TreeNode objSelectedNode = x.FindNode(strId);
                                if (objSelectedNode != null)
                                    objSelectedNode.Text = strText;
                            }, token: token).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        string strId = objSelectedAmmo.InternalId;
                        string strText = await objSelectedAmmo.GetCurrentDisplayNameAsync(token)
                            .ConfigureAwait(false);
                        await treGearView.DoThreadSafeAsync(x =>
                        {
                            // Refresh the Gear tree.
                            TreeNode objSelectedNode = x.FindNode(strId);
                            if (objSelectedNode != null)
                                objSelectedNode.Text = strText;
                        }, token: token).ConfigureAwait(false);
                    }
                }

                GetClip(_intActiveAmmoSlot).Ammo
                    = decQty.ToInt32(); // Bypass AmmoRemaining so as not to alter the gear quantity
            }
        }
        public async Task Unload(IAsyncCollection<Gear> lstGears, TreeView treGearView, CancellationToken token = default)
        {
            Clip objClip = GetClip(ActiveAmmoSlot);
            Gear objAmmo = await UnloadGearAsync(lstGears, objClip, token).ConfigureAwait(false);
            if (objAmmo == null)
                return;
            string strText = await objAmmo.GetCurrentDisplayNameAsync(token).ConfigureAwait(false);
            await treGearView.DoThreadSafeAsync(x =>
            {
                // Refresh the Gear tree.
                TreeNode objSelectedNode = x.FindNode(objAmmo.InternalId);
                if (objSelectedNode != null)
                    objSelectedNode.Text = strText;
            }, token: token).ConfigureAwait(false);
        }
        /// <summary>
        /// Add a Weapon to the TreeView.
        /// </summary>
        /// <param name="cmsWeapon">ContextMenuStrip for the Weapon Node.</param>
        /// <param name="cmsWeaponAccessory">ContextMenuStrip for Vehicle Accessory Nodes.</param>
        /// <param name="cmsWeaponAccessoryGear">ContextMenuStrip for Vehicle Weapon Accessory Gear Nodes.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public async Task<TreeNode> CreateTreeNode(ContextMenuStrip cmsWeapon, ContextMenuStrip cmsWeaponAccessory, ContextMenuStrip cmsWeaponAccessoryGear, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if ((Cyberware || Category == "Gear" || Category.StartsWith("Quality", StringComparison.Ordinal) || !string.IsNullOrEmpty(ParentID)) && !string.IsNullOrEmpty(Source)
                && !await (await _objCharacter.GetSettingsAsync(token).ConfigureAwait(false)).BookEnabledAsync(Source, token).ConfigureAwait(false))
                return null;

            TreeNode objNode = new TreeNode
            {
                Name = InternalId,
                Text = await GetCurrentDisplayNameAsync(token).ConfigureAwait(false),
                Tag = this,
                ContextMenuStrip = cmsWeapon,
                ForeColor = await GetPreferredColorAsync(token).ConfigureAwait(false),
                ToolTipText = (await GetNotesAsync(token).ConfigureAwait(false)).WordWrap()
            };

            TreeNodeCollection lstChildNodes = objNode.Nodes;
            // Add Underbarrel Weapons.
            await UnderbarrelWeapons.ForEachAsync(async objUnderbarrelWeapon =>
            {
                TreeNode objLoopNode =
                    await objUnderbarrelWeapon.CreateTreeNode(cmsWeapon, cmsWeaponAccessory, cmsWeaponAccessoryGear,
                        token).ConfigureAwait(false);
                if (objLoopNode != null)
                    lstChildNodes.Add(objLoopNode);
            }, token).ConfigureAwait(false);
            // Add attached Weapon Accessories.
            await WeaponAccessories.ForEachAsync(async objAccessory =>
            {
                TreeNode objLoopNode = await objAccessory.CreateTreeNode(cmsWeaponAccessory, cmsWeaponAccessoryGear, token).ConfigureAwait(false);
                if (objLoopNode != null)
                    lstChildNodes.Add(objLoopNode);
            }, token).ConfigureAwait(false);

            if (lstChildNodes.Count > 0)
                objNode.Expand();

            return objNode;
        }
        public void SetupChildrenWeaponsCollectionChanged(bool blnAdd, TreeView treWeapons, ContextMenuStrip cmsWeapon = null, ContextMenuStrip cmsWeaponAccessory = null, ContextMenuStrip cmsWeaponAccessoryGear = null, AsyncNotifyCollectionChangedEventHandler funcMakeDirty = null)
        {
            if (blnAdd)
            {
                Task FuncUnderbarrelWeaponsBeforeClearToAdd(object x, NotifyCollectionChangedEventArgs y,
                    CancellationToken innerToken = default) =>
                    this.RefreshChildrenWeaponsClearBindings(treWeapons, y, innerToken);

                Task FuncUnderbarrelWeaponsToAdd(object x, NotifyCollectionChangedEventArgs y, CancellationToken innerToken = default) =>
                    this.RefreshChildrenWeapons(treWeapons, cmsWeapon, cmsWeaponAccessory, cmsWeaponAccessoryGear,
                        null, y, funcMakeDirty, token: innerToken);

                Task FuncWeaponAccessoriesBeforeClearToAdd(object x, NotifyCollectionChangedEventArgs y,
                    CancellationToken innerToken = default) =>
                    this.RefreshWeaponAccessoriesClearBindings(treWeapons, y, innerToken);

                Task FuncWeaponAccessoriesToAdd(object x, NotifyCollectionChangedEventArgs y, CancellationToken innerToken = default) =>
                    this.RefreshWeaponAccessories(treWeapons, cmsWeaponAccessory, cmsWeaponAccessoryGear,
                        () => UnderbarrelWeapons.GetCountAsync(innerToken), y, funcMakeDirty, token: innerToken);

                UnderbarrelWeapons.AddTaggedBeforeClearCollectionChanged(treWeapons,
                    FuncUnderbarrelWeaponsBeforeClearToAdd);
                UnderbarrelWeapons.AddTaggedCollectionChanged(treWeapons, FuncUnderbarrelWeaponsToAdd);
                WeaponAccessories.AddTaggedBeforeClearCollectionChanged(treWeapons,
                    FuncWeaponAccessoriesBeforeClearToAdd);
                WeaponAccessories.AddTaggedCollectionChanged(treWeapons, FuncWeaponAccessoriesToAdd);
                if (funcMakeDirty != null)
                {
                    UnderbarrelWeapons.AddTaggedCollectionChanged(treWeapons, funcMakeDirty);
                    WeaponAccessories.AddTaggedCollectionChanged(treWeapons, funcMakeDirty);
                }
                foreach (Weapon objChild in UnderbarrelWeapons)
                {
                    objChild.SetupChildrenWeaponsCollectionChanged(true, treWeapons, cmsWeapon, cmsWeaponAccessory, cmsWeaponAccessoryGear, funcMakeDirty);
                }

                foreach (WeaponAccessory objChild in WeaponAccessories)
                {
                    Task FuncWeaponAccessoryGearBeforeClearToAdd(object x, NotifyCollectionChangedEventArgs y,
                        CancellationToken innerToken = default) =>
                        this.RefreshChildrenGearsClearBindings(treWeapons, y, innerToken);

                    Task FuncWeaponAccessoryGearToAdd(object x, NotifyCollectionChangedEventArgs y,
                        CancellationToken innerToken = default) =>
                        objChild.RefreshChildrenGears(treWeapons, cmsWeaponAccessoryGear, null, null, y, funcMakeDirty,
                            token: innerToken);

                    TaggedObservableCollection<Gear> lstGearChildren = objChild.GearChildren;
                    lstGearChildren.AddTaggedBeforeClearCollectionChanged(treWeapons,
                        FuncWeaponAccessoryGearBeforeClearToAdd);
                    lstGearChildren.AddTaggedCollectionChanged(treWeapons, FuncWeaponAccessoryGearToAdd);
                    if (funcMakeDirty != null)
                        lstGearChildren.AddTaggedCollectionChanged(treWeapons, funcMakeDirty);
                    foreach (Gear objGear in lstGearChildren)
                        objGear.SetupChildrenGearsCollectionChanged(true, treWeapons, cmsWeaponAccessoryGear, null, funcMakeDirty);
                }
            }
            else
            {
                UnderbarrelWeapons.RemoveTaggedAsyncBeforeClearCollectionChanged(treWeapons);
                UnderbarrelWeapons.RemoveTaggedAsyncCollectionChanged(treWeapons);
                WeaponAccessories.RemoveTaggedAsyncBeforeClearCollectionChanged(treWeapons);
                WeaponAccessories.RemoveTaggedAsyncCollectionChanged(treWeapons);
                foreach (Weapon objChild in UnderbarrelWeapons)
                {
                    objChild.SetupChildrenWeaponsCollectionChanged(false, treWeapons);
                }
                foreach (WeaponAccessory objChild in WeaponAccessories)
                {
                    TaggedObservableCollection<Gear> lstGearChildren = objChild.GearChildren;
                    lstGearChildren.RemoveTaggedAsyncBeforeClearCollectionChanged(treWeapons);
                    lstGearChildren.RemoveTaggedAsyncCollectionChanged(treWeapons);
                    foreach (Gear objGear in lstGearChildren)
                        objGear.SetupChildrenGearsCollectionChanged(false, treWeapons);
                }
            }
        }
        public async Task SetupChildrenWeaponsCollectionChangedAsync(bool blnAdd, TreeView treWeapons, ContextMenuStrip cmsWeapon = null, ContextMenuStrip cmsWeaponAccessory = null, ContextMenuStrip cmsWeaponAccessoryGear = null, AsyncNotifyCollectionChangedEventHandler funcMakeDirty = null, CancellationToken token = default)
        {
            if (blnAdd)
            {
                Task FuncUnderbarrelWeaponsBeforeClearToAdd(object x, NotifyCollectionChangedEventArgs y,
                    CancellationToken innerToken = default) =>
                    this.RefreshChildrenWeaponsClearBindings(treWeapons, y, innerToken);

                Task FuncUnderbarrelWeaponsToAdd(object x, NotifyCollectionChangedEventArgs y, CancellationToken innerToken = default) =>
                    this.RefreshChildrenWeapons(treWeapons, cmsWeapon, cmsWeaponAccessory, cmsWeaponAccessoryGear,
                        null, y, funcMakeDirty, token: innerToken);

                Task FuncWeaponAccessoriesBeforeClearToAdd(object x, NotifyCollectionChangedEventArgs y,
                    CancellationToken innerToken = default) =>
                    this.RefreshWeaponAccessoriesClearBindings(treWeapons, y, innerToken);

                Task FuncWeaponAccessoriesToAdd(object x, NotifyCollectionChangedEventArgs y, CancellationToken innerToken = default) =>
                    this.RefreshWeaponAccessories(treWeapons, cmsWeaponAccessory, cmsWeaponAccessoryGear,
                        () => UnderbarrelWeapons.GetCountAsync(innerToken), y, funcMakeDirty, token: innerToken);

                UnderbarrelWeapons.AddTaggedBeforeClearCollectionChanged(treWeapons,
                    FuncUnderbarrelWeaponsBeforeClearToAdd);
                UnderbarrelWeapons.AddTaggedCollectionChanged(treWeapons, FuncUnderbarrelWeaponsToAdd);
                WeaponAccessories.AddTaggedBeforeClearCollectionChanged(treWeapons,
                    FuncWeaponAccessoriesBeforeClearToAdd);
                WeaponAccessories.AddTaggedCollectionChanged(treWeapons, FuncWeaponAccessoriesToAdd);
                if (funcMakeDirty != null)
                {
                    UnderbarrelWeapons.AddTaggedCollectionChanged(treWeapons, funcMakeDirty);
                    WeaponAccessories.AddTaggedCollectionChanged(treWeapons, funcMakeDirty);
                }
                await UnderbarrelWeapons.ForEachWithSideEffectsAsync(
                    objChild => objChild.SetupChildrenWeaponsCollectionChangedAsync(true, treWeapons, cmsWeapon, cmsWeaponAccessory, cmsWeaponAccessoryGear, funcMakeDirty, token),
                    token).ConfigureAwait(false);

                await WeaponAccessories.ForEachWithSideEffectsAsync(async objChild =>
                {
                    Task FuncWeaponAccessoryGearBeforeClearToAdd(object x, NotifyCollectionChangedEventArgs y,
                        CancellationToken innerToken = default) =>
                        this.RefreshChildrenGearsClearBindings(treWeapons, y, innerToken);

                    Task FuncWeaponAccessoryGearToAdd(object x, NotifyCollectionChangedEventArgs y,
                        CancellationToken innerToken = default) =>
                        objChild.RefreshChildrenGears(treWeapons, cmsWeaponAccessoryGear, null, null, y, funcMakeDirty,
                            token: innerToken);

                    TaggedObservableCollection<Gear> lstGearChildren = objChild.GearChildren;
                    lstGearChildren.AddTaggedBeforeClearCollectionChanged(treWeapons,
                        FuncWeaponAccessoryGearBeforeClearToAdd);
                    lstGearChildren.AddTaggedCollectionChanged(treWeapons, FuncWeaponAccessoryGearToAdd);
                    if (funcMakeDirty != null)
                        lstGearChildren.AddTaggedCollectionChanged(treWeapons, funcMakeDirty);
                    await lstGearChildren.ForEachWithSideEffectsAsync(
                        objGear => objGear.SetupChildrenGearsCollectionChangedAsync(true, treWeapons,
                            cmsWeaponAccessoryGear, null, funcMakeDirty, token),
                        token).ConfigureAwait(false);
                }, token).ConfigureAwait(false);
            }
            else
            {
                await UnderbarrelWeapons.RemoveTaggedAsyncBeforeClearCollectionChangedAsync(treWeapons, token).ConfigureAwait(false);
                await UnderbarrelWeapons.RemoveTaggedAsyncCollectionChangedAsync(treWeapons, token).ConfigureAwait(false);
                await WeaponAccessories.RemoveTaggedAsyncBeforeClearCollectionChangedAsync(treWeapons, token).ConfigureAwait(false);
                await WeaponAccessories.RemoveTaggedAsyncCollectionChangedAsync(treWeapons, token).ConfigureAwait(false);
                await UnderbarrelWeapons.ForEachWithSideEffectsAsync(
                    objChild => objChild.SetupChildrenWeaponsCollectionChangedAsync(false, treWeapons, token: token),
                    token).ConfigureAwait(false);
                await WeaponAccessories.ForEachWithSideEffectsAsync(async objChild =>
                {
                    TaggedObservableCollection<Gear> lstGearChildren = objChild.GearChildren;
                    await lstGearChildren.RemoveTaggedAsyncBeforeClearCollectionChangedAsync(treWeapons, token).ConfigureAwait(false);
                    await lstGearChildren.RemoveTaggedAsyncCollectionChangedAsync(treWeapons, token).ConfigureAwait(false);
                    await lstGearChildren.ForEachWithSideEffectsAsync(
                        objGear => objGear.SetupChildrenGearsCollectionChangedAsync(false, treWeapons, token: token),
                        token).ConfigureAwait(false);
                }, token).ConfigureAwait(false);
            }
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
