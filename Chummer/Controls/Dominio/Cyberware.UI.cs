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

// Metade dependente de WinForms de Cyberware, separada do domínio durante a
// extração do núcleo (DEC-023). A metade de regras vive em Chummer/Backend/Equipment/Cyberware.cs.
//
// As duas são `partial class Cyberware`: no projeto legado voltam a ser uma classe
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
using Chummer.Backend.Attributes;
using Microsoft.VisualStudio.Threading;
using NLog;
using IAsyncDisposable = System.IAsyncDisposable;

namespace Chummer.Backend.Equipment
{
    public partial class Cyberware
    {
        /// <summary>
        /// Build up the Tree for the current piece of Cyberware and all of its children.
        /// </summary>
        public async Task<TreeNode> CreateTreeNode(ContextMenuStrip cmsCyberware, ContextMenuStrip cmsGear, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
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
                    ContextMenuStrip = cmsCyberware,
                    ForeColor = await GetPreferredColorAsync(token).ConfigureAwait(false),
                    ToolTipText = (await GetNotesAsync(token).ConfigureAwait(false)).WordWrap()
                };

                TreeNodeCollection lstChildNodes = objNode.Nodes;
                await (await GetChildrenAsync(token).ConfigureAwait(false)).ForEachAsync(async objChild =>
                {
                    TreeNode objLoopNode = await objChild.CreateTreeNode(cmsCyberware, cmsGear, token).ConfigureAwait(false);
                    if (objLoopNode != null)
                        lstChildNodes.Add(objLoopNode);
                }, token).ConfigureAwait(false);

                await (await GetGearChildrenAsync(token).ConfigureAwait(false)).ForEachAsync(async objGear =>
                {
                    TreeNode objLoopNode = await objGear.CreateTreeNode(cmsGear, null, token).ConfigureAwait(false);
                    if (objLoopNode != null)
                        lstChildNodes.Add(objLoopNode);
                }, token).ConfigureAwait(false);

                if (lstChildNodes.Count > 0)
                    objNode.Expand();

                return objNode;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }
        public void SetupChildrenCyberwareCollectionChanged(bool blnAdd, TreeView treCyberware,
            ContextMenuStrip cmsCyberware = null, ContextMenuStrip cmsCyberwareGear = null, AsyncNotifyCollectionChangedEventHandler funcMakeDirty = null)
        {
            if (blnAdd)
            {
                Task FuncCyberwareBeforeClearToAdd(object x, NotifyCollectionChangedEventArgs y,
                    CancellationToken innerToken = default) =>
                    this.RefreshChildrenCyberwareClearBindings(treCyberware, y, innerToken);

                Task FuncCyberwareToAdd(object x, NotifyCollectionChangedEventArgs y, CancellationToken innerToken = default) =>
                    this.RefreshChildrenCyberware(treCyberware, cmsCyberware, cmsCyberwareGear, null, y,
                        funcMakeDirty, token: innerToken);

                Task FuncGearBeforeClearToAdd(object x, NotifyCollectionChangedEventArgs y,
                    CancellationToken innerToken = default) =>
                    this.RefreshChildrenGearsClearBindings(treCyberware, y, innerToken);

                Task FuncGearToAdd(object x, NotifyCollectionChangedEventArgs y, CancellationToken innerToken = default) =>
                    this.RefreshChildrenGears(treCyberware, cmsCyberwareGear, null, () => Children.GetCountAsync(innerToken), y,
                        funcMakeDirty, token: innerToken);

                Children.AddTaggedBeforeClearCollectionChanged(treCyberware, FuncCyberwareBeforeClearToAdd);
                Children.AddTaggedCollectionChanged(treCyberware, FuncCyberwareToAdd);
                GearChildren.AddTaggedBeforeClearCollectionChanged(treCyberware, FuncGearBeforeClearToAdd);
                GearChildren.AddTaggedCollectionChanged(treCyberware, FuncGearToAdd);
                if (funcMakeDirty != null)
                {
                    Children.AddTaggedCollectionChanged(treCyberware, funcMakeDirty);
                    GearChildren.AddTaggedCollectionChanged(treCyberware, funcMakeDirty);
                }

                foreach (Cyberware objChild in Children)
                {
                    objChild.SetupChildrenCyberwareCollectionChanged(true, treCyberware, cmsCyberware,
                        cmsCyberwareGear, funcMakeDirty);
                }

                foreach (Gear objGear in GearChildren)
                    objGear.SetupChildrenGearsCollectionChanged(true, treCyberware, cmsCyberwareGear, null,
                        funcMakeDirty);
            }
            else
            {
                Children.RemoveTaggedAsyncBeforeClearCollectionChanged(treCyberware);
                Children.RemoveTaggedAsyncCollectionChanged(treCyberware);
                GearChildren.RemoveTaggedAsyncBeforeClearCollectionChanged(treCyberware);
                GearChildren.RemoveTaggedAsyncCollectionChanged(treCyberware);
                foreach (Cyberware objChild in Children)
                    objChild.SetupChildrenCyberwareCollectionChanged(false, treCyberware);
                foreach (Gear objGear in GearChildren)
                    objGear.SetupChildrenGearsCollectionChanged(false, treCyberware);
            }
        }
        public async Task SetupChildrenCyberwareCollectionChangedAsync(bool blnAdd, TreeView treCyberware,
            ContextMenuStrip cmsCyberware = null, ContextMenuStrip cmsCyberwareGear = null,
            AsyncNotifyCollectionChangedEventHandler funcMakeDirty = null, CancellationToken token = default)
        {
            TaggedObservableCollection<Cyberware> lstChildren = await GetChildrenAsync(token).ConfigureAwait(false);
            TaggedObservableCollection<Gear> lstGearChildren = await GetGearChildrenAsync(token).ConfigureAwait(false);
            if (blnAdd)
            {
                Task FuncCyberwareBeforeClearToAdd(object x, NotifyCollectionChangedEventArgs y,
                    CancellationToken innerToken = default) =>
                    this.RefreshChildrenCyberwareClearBindings(treCyberware, y, innerToken);

                Task FuncCyberwareToAdd(object x, NotifyCollectionChangedEventArgs y, CancellationToken innerToken = default) =>
                    this.RefreshChildrenCyberware(treCyberware, cmsCyberware, cmsCyberwareGear, null, y,
                        funcMakeDirty, token: innerToken);

                Task FuncGearBeforeClearToAdd(object x, NotifyCollectionChangedEventArgs y,
                    CancellationToken innerToken = default) =>
                    this.RefreshChildrenGearsClearBindings(treCyberware, y, innerToken);

                Task FuncGearToAdd(object x, NotifyCollectionChangedEventArgs y, CancellationToken innerToken = default) =>
                    this.RefreshChildrenGears(treCyberware, cmsCyberwareGear, null, () => Children.GetCountAsync(innerToken), y,
                        funcMakeDirty, token: innerToken);

                lstChildren.AddTaggedBeforeClearCollectionChanged(treCyberware, FuncCyberwareBeforeClearToAdd);
                lstChildren.AddTaggedCollectionChanged(treCyberware, FuncCyberwareToAdd);
                lstGearChildren.AddTaggedBeforeClearCollectionChanged(treCyberware, FuncGearBeforeClearToAdd);
                lstGearChildren.AddTaggedCollectionChanged(treCyberware, FuncGearToAdd);
                if (funcMakeDirty != null)
                {
                    lstChildren.AddTaggedCollectionChanged(treCyberware, funcMakeDirty);
                    lstGearChildren.AddTaggedCollectionChanged(treCyberware, funcMakeDirty);
                }

                await lstChildren.ForEachWithSideEffectsAsync(
                    objChild => objChild.SetupChildrenCyberwareCollectionChangedAsync(true, treCyberware, cmsCyberware,
                        cmsCyberwareGear, funcMakeDirty, token), token).ConfigureAwait(false);
                await lstGearChildren.ForEachWithSideEffectsAsync(
                    objChild => objChild.SetupChildrenGearsCollectionChangedAsync(true, treCyberware, cmsCyberwareGear,
                        null, funcMakeDirty, token: token), token).ConfigureAwait(false);
            }
            else
            {
                await lstChildren.RemoveTaggedAsyncBeforeClearCollectionChangedAsync(treCyberware, token).ConfigureAwait(false);
                await lstChildren.RemoveTaggedAsyncCollectionChangedAsync(treCyberware, token).ConfigureAwait(false);
                await lstGearChildren.RemoveTaggedAsyncBeforeClearCollectionChangedAsync(treCyberware, token).ConfigureAwait(false);
                await lstGearChildren.RemoveTaggedAsyncCollectionChangedAsync(treCyberware, token).ConfigureAwait(false);
                await lstChildren.ForEachWithSideEffectsAsync(
                    objChild => objChild.SetupChildrenCyberwareCollectionChangedAsync(false, treCyberware,
                        token: token), token).ConfigureAwait(false);
                await lstGearChildren.ForEachWithSideEffectsAsync(
                    objChild => objChild.SetupChildrenGearsCollectionChangedAsync(false, treCyberware, token: token),
                    token).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// Alias map for SourceDetail control text and tooltip assignation.
        /// </summary>
        /// <param name="sourceControl"></param>
        public void SetSourceDetail(Control sourceControl)
        {
            using (LockObject.EnterReadLock())
            {
                if (_objCachedSourceDetail.Language != GlobalSettings.Language)
                    _objCachedSourceDetail = default;
                SourceDetail.SetControl(sourceControl);
            }
        }
        public async Task SetSourceDetailAsync(Control sourceControl, CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                if (_objCachedSourceDetail.Language != GlobalSettings.Language)
                    _objCachedSourceDetail = default;
                await (await GetSourceDetailAsync(token).ConfigureAwait(false)).SetControlAsync(sourceControl, token).ConfigureAwait(false);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
