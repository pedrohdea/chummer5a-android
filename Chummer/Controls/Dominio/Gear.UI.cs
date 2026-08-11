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

// Metade dependente de WinForms de Gear, separada do domínio durante a
// extração do núcleo (DEC-023). A metade de regras vive em Chummer/Backend/Equipment/Gear.cs.
//
// As duas são `partial class Gear`: no projeto legado voltam a ser uma classe
// só, então nenhum chamador precisou mudar. No Chummer.Core, só o domínio existe.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using Chummer.Annotations;
using NLog;

namespace Chummer.Backend.Equipment
{
    public partial class Gear
    {
        public async Task ReaddImprovements(TreeView treGears, StringBuilder sbdOutdatedItems,
                                                 IReadOnlyCollection<string> lstInternalIdFilter,
                                                 Improvement.ImprovementSource eSource
                                                     = Improvement.ImprovementSource.Gear, bool blnStackEquipped = true,
                                                 CancellationToken token = default)
        {
            // We're only re-apply improvements a list of items, not all of them
            if (lstInternalIdFilter?.Contains(InternalId) != false)
            {
                XmlNode objNode = await this.GetNodeAsync(token: token).ConfigureAwait(false);
                if (objNode != null)
                {
                    if (Category == "Stacked Focus")
                    {
                        StackedFocus objStack = await _objCharacter.StackedFoci
                                                                   .FindAsync(x => x.GearId == InternalId, token)
                                                                   .ConfigureAwait(false);
                        if (objStack != null)
                        {
                            await objStack.Gear.ForEachWithSideEffectsAsync(objFociGear =>
                                objFociGear.ReaddImprovements(treGears, sbdOutdatedItems, lstInternalIdFilter,
                                    Improvement.ImprovementSource.StackedFocus,
                                    blnStackEquipped, token), token: token).ConfigureAwait(false);
                        }
                    }

                    Bonus = objNode["bonus"];
                    WirelessBonus = objNode["wirelessbonus"];
                    if (blnStackEquipped && Equipped)
                    {
                        if (Bonus != null)
                        {
                            ImprovementManager.SetForcedValue(Extra, _objCharacter);
                            await ImprovementManager.CreateImprovementsAsync(
                                                        _objCharacter, eSource, InternalId, Bonus, await GetRatingAsync(token).ConfigureAwait(false),
                                                        await GetCurrentDisplayNameShortAsync(token)
                                                            .ConfigureAwait(false), token: token)
                                                    .ConfigureAwait(false);
                            string strSelectedValue = ImprovementManager.GetSelectedValue(_objCharacter);
                            if (!string.IsNullOrEmpty(strSelectedValue))
                            {
                                Extra = strSelectedValue;
                                string strText = await GetCurrentDisplayNameShortAsync(token).ConfigureAwait(false);
                                await treGears.DoThreadSafeAsync(x =>
                                {
                                    TreeNode objGearNode = x.FindNode(InternalId);
                                    if (objGearNode != null)
                                        objGearNode.Text = strText;
                                }, token: token).ConfigureAwait(false);
                            }
                        }

                        if (WirelessOn && WirelessBonus != null)
                        {
                            ImprovementManager.SetForcedValue(Extra, _objCharacter);
                            if (await ImprovementManager.CreateImprovementsAsync(
                                    _objCharacter, eSource, InternalId, WirelessBonus,
                                    await GetRatingAsync(token).ConfigureAwait(false),
                                    await GetCurrentDisplayNameShortAsync(token).ConfigureAwait(false),
                                    token: token).ConfigureAwait(false))
                            {
                                string strSelectedValue = ImprovementManager.GetSelectedValue(_objCharacter);
                                if (!string.IsNullOrEmpty(strSelectedValue))
                                {
                                    Extra = strSelectedValue;
                                    string strText = await GetCurrentDisplayNameShortAsync(token).ConfigureAwait(false);
                                    await treGears.DoThreadSafeAsync(x =>
                                    {
                                        TreeNode objGearNode = x.FindNode(InternalId);
                                        if (objGearNode != null)
                                            objGearNode.Text = strText;
                                    }, token: token).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                }
                else
                {
                    sbdOutdatedItems?.AppendLine(await GetCurrentDisplayNameShortAsync(token).ConfigureAwait(false));
                }
            }

            await Children.ForEachWithSideEffectsAsync(x => x.ReaddImprovements(treGears, sbdOutdatedItems,
                lstInternalIdFilter, eSource, blnStackEquipped,
                token), token).ConfigureAwait(false);
        }
        /// <summary>
        /// Collection of TreeNodes to update when a relevant property is changed
        /// </summary>
        public HashSet<TreeNode> LinkedTreeNodes { get; } = new HashSet<TreeNode>();
        /// <summary>
        /// Build up the Tree for the current piece of Gear and all of its children.
        /// </summary>
        /// <param name="cmsGear">ContextMenuStrip for the Gear to use.</param>
        /// <param name="cmsCustomGear">ContextMenuStrip for the Gear to use if it can be renamed the way Custom Gear can (in Create mode).</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public async Task<TreeNode> CreateTreeNode(ContextMenuStrip cmsGear, ContextMenuStrip cmsCustomGear, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (!string.IsNullOrEmpty(ParentID) && !string.IsNullOrEmpty(Source) &&
                !await (await _objCharacter.GetSettingsAsync(token).ConfigureAwait(false)).BookEnabledAsync(Source, token).ConfigureAwait(false))
                return null;

            TreeNode objNode = new TreeNode
            {
                Name = InternalId,
                Text = await GetCurrentDisplayNameAsync(token).ConfigureAwait(false),
                Tag = this,
                ContextMenuStrip = cmsCustomGear != null && AllowRename ? cmsCustomGear : cmsGear,
                ForeColor = await GetPreferredColorAsync(token).ConfigureAwait(false),
                ToolTipText = (await GetNotesAsync(token).ConfigureAwait(false)).WordWrap()
            };

            await BuildChildrenGearTree(objNode, cmsGear, cmsCustomGear, token).ConfigureAwait(false);

            return objNode;
        }
        /// <summary>
        /// Build up the Tree for the current piece of Gear's children.
        /// </summary>
        /// <param name="objParentNode">Parent node to which to append children gear.</param>
        /// <param name="cmsGear">ContextMenuStrip for the Gear's children to use to use.</param>
        /// <param name="cmsCustomGear">ContextMenuStrip for the Gear's children to use if they can be renamed the way Custom Gear can (in Create mode).</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public async Task BuildChildrenGearTree(TreeNode objParentNode, ContextMenuStrip cmsGear, ContextMenuStrip cmsCustomGear, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (objParentNode == null)
                return;
            bool blnExpandNode = false;
            await Children.ForEachAsync(async objChild =>
            {
                TreeNode objChildNode = await objChild.CreateTreeNode(cmsGear, cmsCustomGear, token).ConfigureAwait(false);
                if (objChildNode != null)
                {
                    objParentNode.Nodes.Add(objChildNode);
                    if (objChild.ParentID != InternalId ||
                        (await this.GetNodeXPathAsync(token: token).ConfigureAwait(false))?.SelectSingleNodeAndCacheExpression("gears/@startcollapsed", token)?.Value !=
                        bool.TrueString)
                        blnExpandNode = true;
                }
            }, token).ConfigureAwait(false);

            if (blnExpandNode)
                objParentNode.Expand();
        }
        public void SetupChildrenGearsCollectionChanged(bool blnAdd, TreeView treGear, ContextMenuStrip cmsGear = null, ContextMenuStrip cmsCustomGear = null, AsyncNotifyCollectionChangedEventHandler funcMakeDirty = null)
        {
            if (blnAdd)
            {
                Task FuncDelegateBeforeClearToAdd(object x, NotifyCollectionChangedEventArgs y,
                    CancellationToken innerToken = default) =>
                    this.RefreshChildrenGearsClearBindings(treGear, y, innerToken);

                Task FuncDelegateToAdd(object x, NotifyCollectionChangedEventArgs y, CancellationToken innerToken = default) =>
                    this.RefreshChildrenGears(treGear, cmsGear, cmsCustomGear, null, y, funcMakeDirty, token: innerToken);

                Children.AddTaggedBeforeClearCollectionChanged(treGear, FuncDelegateBeforeClearToAdd);
                Children.AddTaggedCollectionChanged(treGear, FuncDelegateToAdd);
                if (funcMakeDirty != null)
                    Children.AddTaggedCollectionChanged(treGear, funcMakeDirty);
                foreach (Gear objChild in Children)
                {
                    objChild.SetupChildrenGearsCollectionChanged(true, treGear, cmsGear, cmsCustomGear, funcMakeDirty);
                }
            }
            else
            {
                Children.RemoveTaggedAsyncBeforeClearCollectionChanged(treGear);
                Children.RemoveTaggedAsyncCollectionChanged(treGear);
                foreach (Gear objChild in Children)
                {
                    objChild.SetupChildrenGearsCollectionChanged(false, treGear);
                }
            }
        }
        public async Task SetupChildrenGearsCollectionChangedAsync(bool blnAdd, TreeView treGear, ContextMenuStrip cmsGear = null, ContextMenuStrip cmsCustomGear = null, AsyncNotifyCollectionChangedEventHandler funcMakeDirty = null, CancellationToken token = default)
        {
            if (blnAdd)
            {
                Task FuncDelegateBeforeClearToAdd(object x, NotifyCollectionChangedEventArgs y,
                    CancellationToken innerToken = default) =>
                    this.RefreshChildrenGearsClearBindings(treGear, y, innerToken);

                Task FuncDelegateToAdd(object x, NotifyCollectionChangedEventArgs y, CancellationToken innerToken = default) =>
                    this.RefreshChildrenGears(treGear, cmsGear, cmsCustomGear, null, y, funcMakeDirty, token: innerToken);

                Children.AddTaggedBeforeClearCollectionChanged(treGear, FuncDelegateBeforeClearToAdd);
                Children.AddTaggedCollectionChanged(treGear, FuncDelegateToAdd);
                if (funcMakeDirty != null)
                    Children.AddTaggedCollectionChanged(treGear, funcMakeDirty);
                await Children.ForEachWithSideEffectsAsync(
                    objChild => objChild.SetupChildrenGearsCollectionChangedAsync(true, treGear, cmsGear, cmsCustomGear,
                        funcMakeDirty, token), token).ConfigureAwait(false);
            }
            else
            {
                await Children.RemoveTaggedAsyncBeforeClearCollectionChangedAsync(treGear, token).ConfigureAwait(false);
                await Children.RemoveTaggedAsyncCollectionChangedAsync(treGear, token).ConfigureAwait(false);
                await Children.ForEachWithSideEffectsAsync(
                    objChild => objChild.SetupChildrenGearsCollectionChangedAsync(false, treGear, token: token), token).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// Refreshes a single focus' rating (for changing ratings in create mode)
        /// </summary>
        /// <param name="treFoci">TreeView of foci.</param>
        /// <param name="intNewRating">New rating that the focus is supposed to have.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        /// <returns>True if the new rating complies by focus limits or the gear is not bonded, false otherwise</returns>
        public async Task<bool> RefreshSingleFocusRating(TreeView treFoci, int intNewRating, CancellationToken token = default)
        {
            if (Bonded)
            {
                int intMaxFocusTotal = await (await _objCharacter.GetAttributeAsync("MAG", token: token).ConfigureAwait(false)).GetTotalValueAsync(token).ConfigureAwait(false) * 5;
                if (await (await _objCharacter.GetSettingsAsync(token).ConfigureAwait(false)).GetMysAdeptSecondMAGAttributeAsync(token).ConfigureAwait(false) && await _objCharacter.GetIsMysticAdeptAsync(token).ConfigureAwait(false))
                    intMaxFocusTotal = Math.Min(intMaxFocusTotal, await (await _objCharacter.GetAttributeAsync("MAGAdept", token: token).ConfigureAwait(false)).GetTotalValueAsync(token).ConfigureAwait(false) * 5);

                int intFociTotal = await (await _objCharacter.GetFociAsync(token).ConfigureAwait(false)).SumAsync(x => !ReferenceEquals(x.GearObject, this), x => x.GetRatingAsync(token), token).ConfigureAwait(false);

                if (intFociTotal + intNewRating > intMaxFocusTotal && !await _objCharacter.GetIgnoreRulesAsync(token).ConfigureAwait(false))
                {
                    await Program.ShowScrollableMessageBoxAsync(await LanguageManager.GetStringAsync("Message_FocusMaximumForce", token: token).ConfigureAwait(false),
                        await LanguageManager.GetStringAsync("MessageTitle_FocusMaximum", token: token).ConfigureAwait(false), MessageBoxButtons.OK,
                        MessageBoxIcon.Information, token: token).ConfigureAwait(false);
                    return false;
                }
            }

            await SetRatingAsync(intNewRating, token).ConfigureAwait(false);

            switch (Category)
            {
                case "Foci":
                case "Metamagic Foci":
                    {
                        TreeNode nodFocus = await treFoci.DoThreadSafeFuncAsync(x => x.FindNodeByTag(this), token: token).ConfigureAwait(false);
                        if (nodFocus != null)
                        {
                            string strText = (await GetCurrentDisplayNameAsync(token).ConfigureAwait(false)).Replace(
                                await LanguageManager.GetStringAsync(RatingLabel, token: token).ConfigureAwait(false),
                                await LanguageManager.GetStringAsync("String_Force", token: token).ConfigureAwait(false));
                            await treFoci.DoThreadSafeFuncAsync(() => nodFocus.Text = strText, token: token)
                                         .ConfigureAwait(false);
                        }
                    }
                    break;

                case "Stacked Focus":
                    {
                        ThreadSafeList<StackedFocus> lstStackedFoci
                            = await _objCharacter.GetStackedFociAsync(token).ConfigureAwait(false);
                        for (int i = await lstStackedFoci.GetCountAsync(token).ConfigureAwait(false) - 1; i >= 0; --i)
                        {
                            if (i >= await lstStackedFoci.GetCountAsync(token).ConfigureAwait(false))
                                continue;
                            StackedFocus objStack = await lstStackedFoci.GetValueAtAsync(i, token)
                                                                        .ConfigureAwait(false);
                            if (objStack.GearId != InternalId)
                                continue;
                            TreeNode nodFocus = await treFoci
                                                      .DoThreadSafeFuncAsync(
                                                          x => x.FindNode(objStack.InternalId), token: token)
                                                      .ConfigureAwait(false);
                            if (nodFocus != null)
                            {
                                string strText = (await GetCurrentDisplayNameAsync(token).ConfigureAwait(false)).Replace(
                                    await LanguageManager.GetStringAsync(RatingLabel, token: token).ConfigureAwait(false),
                                    await LanguageManager.GetStringAsync("String_Force", token: token)
                                                         .ConfigureAwait(false));
                                await treFoci.DoThreadSafeFuncAsync(() => nodFocus.Text = strText, token: token)
                                             .ConfigureAwait(false);
                            }

                            break;
                        }
                    }
                    break;
            }

            return true;
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
