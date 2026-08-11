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

// Metade dependente de WinForms de Character, separada do domínio durante a extração
// do núcleo (DEC-023).
//
// Contém a região #region UI Methods na íntegra — 'Move TreeNodes' e 'Tab clearing' —
// e os dois SetSourceDetail(Control). Não é regra de Shadowrun: é reconstrução de nós
// de TreeView e limpeza de abas, que moravam dentro do agregado central por herança
// histórica do desenho WinForms.
//
// As duas metades são `partial class Character`: no projeto legado voltam a ser uma
// classe só, então nenhum chamador mudou. No Chummer.Core, só o domínio existe.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using Chummer.Annotations;
using Chummer.Backend.Attributes;
using Chummer.Backend.Enums;
using Chummer.Backend.Equipment;
using Chummer.Backend.Skills;
using Chummer.Backend.Uniques;
using Chummer.Plugins;
using Microsoft.ApplicationInsights;
using Microsoft.IO;
using Newtonsoft.Json;
using NLog;

namespace Chummer
{
    public sealed partial class Character
    {
        #region UI Methods

        #region Move TreeNodes

        /// <summary>
        /// Move a Gear TreeNode after Drag and Drop, changing its parent.
        /// </summary>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="objGearNode">Node of gear to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public void MoveGearParent(TreeNode objDestination, TreeNode objGearNode, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (objGearNode == null || objDestination == null)
                return;
            // The item cannot be dropped onto itself or onto one of its children.
            for (TreeNode objCheckNode = objDestination;
                objCheckNode != null && objCheckNode.Level >= objDestination.Level;
                objCheckNode = objCheckNode.Parent)
                if (objCheckNode == objGearNode)
                    return;
            if (!(objGearNode.Tag is Gear objGear))
                return;

            // Gear cannot be moved to one if its children.
            bool blnAllowMove = true;
            if (objDestination.Level > 0)
            {
                TreeNode objFindNode = objDestination;
                do
                {
                    objFindNode = objFindNode.Parent;
                    if (objFindNode.Tag == objGear)
                    {
                        blnAllowMove = false;
                        break;
                    }
                } while (objFindNode.Level > 0);
            }

            if (!blnAllowMove)
                return;

            using (LockObject.EnterUpgradeableReadLock(token))
            {
                // Remove the Gear from the character.
                if (objGear.Parent is IHasChildren<Gear> parent)
                    parent.Children.Remove(objGear);
                else
                    Gear.Remove(objGear);

                switch (objDestination.Tag)
                {
                    case Location objLocation:
                        // The Gear was moved to a location, so add it to the character instead.
                        objGear.Location = objLocation;
                        objLocation.Children.Add(objGear);
                        Gear.Add(objGear);
                        break;

                    case Gear objParent:
                        // Add the Gear as a child of the destination Node and clear its location.
                        objGear.Location = null;
                        objParent.Children.Add(objGear);
                        break;
                }
            }
        }

        /// <summary>
        /// Move a Gear TreeNode after Drag and Drop, changing its parent.
        /// </summary>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="objGearNode">Node of gear to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public async Task MoveGearParentAsync(TreeNode objDestination, TreeNode objGearNode, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (objGearNode == null || objDestination == null)
                return;
            // The item cannot be dropped onto itself or onto one of its children.
            for (TreeNode objCheckNode = objDestination;
                objCheckNode != null && objCheckNode.Level >= objDestination.Level;
                objCheckNode = objCheckNode.Parent)
                if (objCheckNode == objGearNode)
                    return;
            if (!(objGearNode.Tag is Gear objGear))
                return;

            // Gear cannot be moved to one if its children.
            bool blnAllowMove = true;
            if (objDestination.Level > 0)
            {
                TreeNode objFindNode = objDestination;
                do
                {
                    objFindNode = objFindNode.Parent;
                    if (objFindNode.Tag == objGear)
                    {
                        blnAllowMove = false;
                        break;
                    }
                } while (objFindNode.Level > 0);
            }

            if (!blnAllowMove)
                return;

            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                // Remove the Gear from the character.
                if (objGear.Parent is IHasChildren<Gear> parent)
                    await parent.Children.RemoveAsync(objGear, token).ConfigureAwait(false);
                else
                    await Gear.RemoveAsync(objGear, token).ConfigureAwait(false);

                switch (objDestination.Tag)
                {
                    case Location objLocation:
                        // The Gear was moved to a location, so add it to the character instead.
                        objGear.Location = objLocation;
                        await objLocation.Children.AddAsync(objGear, token).ConfigureAwait(false);
                        await Gear.AddAsync(objGear, token).ConfigureAwait(false);
                        break;

                    case Gear objParent:
                        // Add the Gear as a child of the destination Node and clear its location.
                        objGear.Location = null;
                        await objParent.Children.AddAsync(objGear, token).ConfigureAwait(false);
                        break;
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Move a Gear TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="intNewIndex">Node's new index.</param>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="nodeToMove">Node of gear to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public void MoveGearNode(int intNewIndex, TreeNode objDestination, TreeNode nodeToMove, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (objDestination == null || nodeToMove == null)
                return;
            if (!(nodeToMove.Tag is Gear objGear))
                return;
            TreeNode objNewParent = objDestination;
            while (objNewParent.Level > 0 && !(objNewParent.Tag is Location))
                objNewParent = objNewParent.Parent;

            using (LockObject.EnterUpgradeableReadLock(token))
            {
                switch (objNewParent.Tag)
                {
                    case Location objLocation:
                        if (nodeToMove.TreeView != null)
                            nodeToMove.TreeView.DoThreadSafe(nodeToMove.Remove, token);
                        else
                            nodeToMove.Remove();
                        objGear.Location = objLocation;
                        if (objNewParent.TreeView != null)
                            objNewParent.TreeView.DoThreadSafe(() => objNewParent.Nodes.Insert(0, nodeToMove), token);
                        else
                            objNewParent.Nodes.Insert(0, nodeToMove);
                        break;

                    case string _:
                        objGear.Location = null;
                        intNewIndex = Math.Min(intNewIndex, Gear.Count - 1);
                        Gear.Move(Gear.IndexOf(objGear), intNewIndex);
                        break;
                }
            }
        }

        /// <summary>
        /// Move a Gear TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="intNewIndex">Node's new index.</param>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="nodeToMove">Node of gear to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public async Task MoveGearNodeAsync(int intNewIndex, TreeNode objDestination, TreeNode nodeToMove, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (objDestination == null || nodeToMove == null)
                return;
            if (!(nodeToMove.Tag is Gear objGear))
                return;
            TreeNode objNewParent = objDestination;
            while (objNewParent.Level > 0 && !(objNewParent.Tag is Location))
                objNewParent = objNewParent.Parent;

            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                switch (objNewParent.Tag)
                {
                    case Location objLocation:
                        if (nodeToMove.TreeView != null)
                            await nodeToMove.TreeView.DoThreadSafeAsync(nodeToMove.Remove, token).ConfigureAwait(false);
                        else
                            nodeToMove.Remove();
                        objGear.Location = objLocation;
                        if (objNewParent.TreeView != null)
                            await objNewParent.TreeView.DoThreadSafeAsync(() =>
                                objNewParent.Nodes.Insert(0, nodeToMove), token).ConfigureAwait(false);
                        else
                            objNewParent.Nodes.Insert(0, nodeToMove);
                        break;

                    case string _:
                        objGear.Location = null;
                        intNewIndex = Math.Min(intNewIndex, await Gear.GetCountAsync(token).ConfigureAwait(false) - 1);
                        await Gear.MoveAsync(await Gear.IndexOfAsync(objGear, token).ConfigureAwait(false), intNewIndex, token).ConfigureAwait(false);
                        break;
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Move a Gear Location TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="intNewIndex">Node's new index.</param>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="nodOldNode">Node of gear location to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public void MoveGearRoot(int intNewIndex, TreeNode objDestination, TreeNode nodOldNode, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (!(nodOldNode?.Tag is Location objLocation))
                return;
            if (objDestination != null)
            {
                TreeNode objNewParent = objDestination;
                while (objNewParent.Level > 0)
                    objNewParent = objNewParent.Parent;
                intNewIndex = objNewParent.Index;
                if (!(objNewParent.Tag is Location))
                    intNewIndex = 0;
            }
            using (LockObject.EnterUpgradeableReadLock(token))
                GearLocations.Move(GearLocations.IndexOf(objLocation), intNewIndex);
        }

        /// <summary>
        /// Move a Gear Location TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="intNewIndex">Node's new index.</param>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="nodOldNode">Node of gear location to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public async Task MoveGearRootAsync(int intNewIndex, TreeNode objDestination, TreeNode nodOldNode, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (!(nodOldNode?.Tag is Location objLocation))
                return;
            if (objDestination != null)
            {
                TreeNode objNewParent = objDestination;
                while (objNewParent.Level > 0)
                    objNewParent = objNewParent.Parent;
                intNewIndex = objNewParent.Index;
                if (!(objNewParent.Tag is Location))
                    intNewIndex = 0;
            }
            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                await GearLocations.MoveAsync(await GearLocations.IndexOfAsync(objLocation, token).ConfigureAwait(false), intNewIndex, token).ConfigureAwait(false);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Move a Lifestyle TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="intNewIndex">Node's new index.</param>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="nodLifestyleNode">Node of lifestyle to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public void MoveLifestyleNode(int intNewIndex, TreeNode objDestination, TreeNode nodLifestyleNode, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (nodLifestyleNode == null)
                return;
            if (objDestination != null)
            {
                TreeNode objNewParent = objDestination;
                while (objNewParent.Level > 0)
                    objNewParent = objNewParent.Parent;
                intNewIndex = objNewParent.Index;
            }

            if (!(nodLifestyleNode.Tag is Lifestyle objLifestyle))
                return;
            using (LockObject.EnterUpgradeableReadLock(token))
                Lifestyles.Move(Lifestyles.IndexOf(objLifestyle), intNewIndex);
        }

        /// <summary>
        /// Move a Lifestyle TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="intNewIndex">Node's new index.</param>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="nodLifestyleNode">Node of lifestyle to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public async Task MoveLifestyleNodeAsync(int intNewIndex, TreeNode objDestination, TreeNode nodLifestyleNode, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (nodLifestyleNode == null)
                return;
            if (objDestination != null)
            {
                TreeNode objNewParent = objDestination;
                while (objNewParent.Level > 0)
                    objNewParent = objNewParent.Parent;
                intNewIndex = objNewParent.Index;
            }

            if (!(nodLifestyleNode.Tag is Lifestyle objLifestyle))
                return;
            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                await Lifestyles.MoveAsync(await Lifestyles.IndexOfAsync(objLifestyle, token).ConfigureAwait(false), intNewIndex, token).ConfigureAwait(false);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Move an Armor TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="intNewIndex">Node's new index.</param>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="nodeToMove">Node of armor to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public void MoveArmorNode(int intNewIndex, TreeNode objDestination, TreeNode nodeToMove, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (objDestination == null)
                return;
            if (!(nodeToMove?.Tag is Armor objArmor))
                return;
            TreeNode objNewParent = objDestination;
            while (objNewParent.Level > 0 && !(objNewParent.Tag is Location))
                objNewParent = objNewParent.Parent;

            using (LockObject.EnterUpgradeableReadLock(token))
            {
                switch (objNewParent.Tag)
                {
                    case Location objLocation:
                        if (nodeToMove.TreeView != null)
                            nodeToMove.TreeView.DoThreadSafe(nodeToMove.Remove, token);
                        else
                            nodeToMove.Remove();
                        objArmor.Location = objLocation;
                        if (objNewParent.TreeView != null)
                            objNewParent.TreeView.DoThreadSafe(() => objNewParent.Nodes.Insert(0, nodeToMove), token);
                        else
                            objNewParent.Nodes.Insert(0, nodeToMove);
                        break;

                    case string _:
                        objArmor.Location = null;
                        intNewIndex = Math.Min(intNewIndex, Armor.Count - 1);
                        Armor.Move(Armor.IndexOf(objArmor), intNewIndex);
                        break;
                }
            }
        }

        /// <summary>
        /// Move an Armor TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="intNewIndex">Node's new index.</param>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="nodeToMove">Node of armor to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public async Task MoveArmorNodeAsync(int intNewIndex, TreeNode objDestination, TreeNode nodeToMove, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (objDestination == null)
                return;
            if (!(nodeToMove?.Tag is Armor objArmor))
                return;
            TreeNode objNewParent = objDestination;
            while (objNewParent.Level > 0 && !(objNewParent.Tag is Location))
                objNewParent = objNewParent.Parent;

            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                switch (objNewParent.Tag)
                {
                    case Location objLocation:
                        if (nodeToMove.TreeView != null)
                            await nodeToMove.TreeView.DoThreadSafeAsync(nodeToMove.Remove, token).ConfigureAwait(false);
                        else
                            nodeToMove.Remove();
                        objArmor.Location = objLocation;
                        if (objNewParent.TreeView != null)
                            await objNewParent.TreeView.DoThreadSafeAsync(() => objNewParent.Nodes.Insert(0, nodeToMove), token).ConfigureAwait(false);
                        else
                            objNewParent.Nodes.Insert(0, nodeToMove);
                        break;

                    case string _:
                        objArmor.Location = null;
                        intNewIndex = Math.Min(intNewIndex, await Armor.GetCountAsync(token).ConfigureAwait(false) - 1);
                        await Armor.MoveAsync(await Armor.IndexOfAsync(objArmor, token).ConfigureAwait(false), intNewIndex, token).ConfigureAwait(false);
                        break;
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Move an Armor Location TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="intNewIndex">Node's new index.</param>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="nodOldNode">Node of armor location to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public void MoveArmorRoot(int intNewIndex, TreeNode objDestination, TreeNode nodOldNode, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (!(nodOldNode?.Tag is Location objLocation))
                return;
            if (objDestination != null)
            {
                TreeNode objNewParent = objDestination;
                while (objNewParent.Level > 0)
                    objNewParent = objNewParent.Parent;
                intNewIndex = objNewParent.Index;
                if (!(objNewParent.Tag is Location))
                    intNewIndex = 0;
            }
            using (LockObject.EnterUpgradeableReadLock(token))
                ArmorLocations.Move(ArmorLocations.IndexOf(objLocation), intNewIndex);
        }

        /// <summary>
        /// Move an Armor Location TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="intNewIndex">Node's new index.</param>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="nodOldNode">Node of armor location to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public async Task MoveArmorRootAsync(int intNewIndex, TreeNode objDestination, TreeNode nodOldNode, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (!(nodOldNode?.Tag is Location objLocation))
                return;
            if (objDestination != null)
            {
                TreeNode objNewParent = objDestination;
                while (objNewParent.Level > 0)
                    objNewParent = objNewParent.Parent;
                intNewIndex = objNewParent.Index;
                if (!(objNewParent.Tag is Location))
                    intNewIndex = 0;
            }
            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                await ArmorLocations.MoveAsync(await ArmorLocations.IndexOfAsync(objLocation, token).ConfigureAwait(false), intNewIndex, token).ConfigureAwait(false);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Move a Weapon TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="intNewIndex">Node's new index.</param>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="nodeToMove">Node of weapon to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public void MoveWeaponNode(int intNewIndex, TreeNode objDestination, TreeNode nodeToMove, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (objDestination == null)
                return;
            if (!(nodeToMove?.Tag is Weapon objWeapon))
                return;
            TreeNode objNewParent = objDestination;
            while (objNewParent.Level > 0 && !(objNewParent.Tag is Location))
                objNewParent = objNewParent.Parent;

            using (LockObject.EnterUpgradeableReadLock(token))
            {
                switch (objNewParent.Tag)
                {
                    case Location objLocation:
                        if (nodeToMove.TreeView != null)
                            nodeToMove.TreeView.DoThreadSafe(nodeToMove.Remove, token);
                        else
                            nodeToMove.Remove();
                        objWeapon.Location = objLocation;
                        if (objNewParent.TreeView != null)
                            objNewParent.TreeView.DoThreadSafe(() => objNewParent.Nodes.Insert(0, nodeToMove), token);
                        else
                            objNewParent.Nodes.Insert(0, nodeToMove);
                        break;

                    case string _:
                        objWeapon.Location = null;
                        intNewIndex = Math.Min(intNewIndex, Weapons.Count - 1);
                        Weapons.Move(Weapons.IndexOf(objWeapon), intNewIndex);
                        break;
                }
            }
        }

        /// <summary>
        /// Move a Weapon TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="intNewIndex">Node's new index.</param>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="nodeToMove">Node of weapon to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public async Task MoveWeaponNodeAsync(int intNewIndex, TreeNode objDestination, TreeNode nodeToMove, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (objDestination == null)
                return;
            if (!(nodeToMove?.Tag is Weapon objWeapon))
                return;
            TreeNode objNewParent = objDestination;
            while (objNewParent.Level > 0 && !(objNewParent.Tag is Location))
                objNewParent = objNewParent.Parent;

            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                switch (objNewParent.Tag)
                {
                    case Location objLocation:
                        if (nodeToMove.TreeView != null)
                            await nodeToMove.TreeView.DoThreadSafeAsync(nodeToMove.Remove, token).ConfigureAwait(false);
                        else
                            nodeToMove.Remove();
                        objWeapon.Location = objLocation;
                        if (objNewParent.TreeView != null)
                            await objNewParent.TreeView.DoThreadSafeAsync(() => objNewParent.Nodes.Insert(0, nodeToMove), token).ConfigureAwait(false);
                        else
                            objNewParent.Nodes.Insert(0, nodeToMove);
                        break;

                    case string _:
                        objWeapon.Location = null;
                        intNewIndex = Math.Min(intNewIndex, await Weapons.GetCountAsync(token).ConfigureAwait(false) - 1);
                        await Weapons.MoveAsync(await Weapons.IndexOfAsync(objWeapon, token).ConfigureAwait(false), intNewIndex, token).ConfigureAwait(false);
                        break;
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Move a Weapon Location TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="intNewIndex">Node's new index.</param>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="nodOldNode">Node of weapon location to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public void MoveWeaponRoot(int intNewIndex, TreeNode objDestination, TreeNode nodOldNode, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (!(nodOldNode?.Tag is Location objLocation))
                return;
            if (objDestination != null)
            {
                TreeNode objNewParent = objDestination;
                while (objNewParent.Level > 0)
                    objNewParent = objNewParent.Parent;
                intNewIndex = objNewParent.Index;
                if (!(objNewParent.Tag is Location))
                    intNewIndex = 0;
            }
            using (LockObject.EnterUpgradeableReadLock(token))
                WeaponLocations.Move(WeaponLocations.IndexOf(objLocation), intNewIndex);
        }

        /// <summary>
        /// Move a Weapon Location TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="intNewIndex">Node's new index.</param>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="nodOldNode">Node of weapon location to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public async Task MoveWeaponRootAsync(int intNewIndex, TreeNode objDestination, TreeNode nodOldNode, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (!(nodOldNode?.Tag is Location objLocation))
                return;
            if (objDestination != null)
            {
                TreeNode objNewParent = objDestination;
                while (objNewParent.Level > 0)
                    objNewParent = objNewParent.Parent;
                intNewIndex = objNewParent.Index;
                if (!(objNewParent.Tag is Location))
                    intNewIndex = 0;
            }
            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                await WeaponLocations.MoveAsync(await WeaponLocations.IndexOfAsync(objLocation, token).ConfigureAwait(false), intNewIndex, token).ConfigureAwait(false);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Move a Vehicle TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="intNewIndex">Node's new index.</param>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="nodeToMove">Node of vehicle to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public void MoveVehicleNode(int intNewIndex, TreeNode objDestination, TreeNode nodeToMove, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (objDestination == null)
                return;
            if (!(nodeToMove?.Tag is Vehicle objVehicle))
                return;
            TreeNode objNewParent = objDestination;
            while (objNewParent.Level > 0 && !(objNewParent.Tag is Location))
                objNewParent = objNewParent.Parent;

            using (LockObject.EnterUpgradeableReadLock(token))
            {
                switch (objNewParent.Tag)
                {
                    case Location objLocation:
                        if (nodeToMove.TreeView != null)
                            nodeToMove.TreeView.DoThreadSafe(nodeToMove.Remove, token);
                        else
                            nodeToMove.Remove();
                        objVehicle.Location = objLocation;
                        if (objNewParent.TreeView != null)
                            objNewParent.TreeView.DoThreadSafe(() => objNewParent.Nodes.Insert(0, nodeToMove), token);
                        else
                            objNewParent.Nodes.Insert(0, nodeToMove);
                        break;

                    case string _:
                        objVehicle.Location = null;
                        intNewIndex = Math.Min(intNewIndex, Vehicles.Count - 1);
                        Vehicles.Move(Vehicles.IndexOf(objVehicle), intNewIndex);
                        break;
                }
            }
        }

        /// <summary>
        /// Move a Vehicle TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="intNewIndex">Node's new index.</param>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="nodeToMove">Node of vehicle to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public async Task MoveVehicleNodeAsync(int intNewIndex, TreeNode objDestination, TreeNode nodeToMove, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (objDestination == null)
                return;
            if (!(nodeToMove?.Tag is Vehicle objVehicle))
                return;
            TreeNode objNewParent = objDestination;
            while (objNewParent.Level > 0 && !(objNewParent.Tag is Location))
                objNewParent = objNewParent.Parent;

            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                switch (objNewParent.Tag)
                {
                    case Location objLocation:
                        if (nodeToMove.TreeView != null)
                            await nodeToMove.TreeView.DoThreadSafeAsync(nodeToMove.Remove, token).ConfigureAwait(false);
                        else
                            nodeToMove.Remove();
                        objVehicle.Location = objLocation;
                        if (objNewParent.TreeView != null)
                            await objNewParent.TreeView.DoThreadSafeAsync(() => objNewParent.Nodes.Insert(0, nodeToMove), token).ConfigureAwait(false);
                        else
                            objNewParent.Nodes.Insert(0, nodeToMove);
                        break;

                    case string _:
                        objVehicle.Location = null;
                        intNewIndex = Math.Min(intNewIndex, await Vehicles.GetCountAsync(token).ConfigureAwait(false) - 1);
                        await Vehicles.MoveAsync(await Vehicles.IndexOfAsync(objVehicle, token).ConfigureAwait(false), intNewIndex, token).ConfigureAwait(false);
                        break;
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Move a Vehicle Gear TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="nodDestination">Destination Node.</param>
        /// <param name="nodGearNode">Node of gear to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public void MoveVehicleGearParent(TreeNode nodDestination, TreeNode nodGearNode, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (nodDestination == null || nodGearNode == null)
                return;
            // The item cannot be dropped onto itself or onto one of its children.
            for (TreeNode objCheckNode = nodDestination;
                objCheckNode != null && objCheckNode.Level >= nodDestination.Level;
                objCheckNode = objCheckNode.Parent)
                if (objCheckNode == nodGearNode)
                    return;
            if (!(nodGearNode.Tag is IHasInternalId nodeId))
                return;
            using (LockObject.EnterUpgradeableReadLock(token))
            {
                // Locate the currently selected piece of Gear.
                //TODO: Better interface for determining what the parent of a bit of gear is.
                Gear objGear = Vehicles.FindVehicleGear(nodeId.InternalId, out Vehicle objOldVehicle,
                                                        out WeaponAccessory objOldWeaponAccessory,
                                                        out Cyberware objOldCyberware, token);

                if (objGear == null)
                    return;

                using (LockObject.EnterWriteLock(token))
                {
                    if (nodDestination.Tag is Gear objDestinationGear)
                    {
                        // Remove the Gear from the Vehicle.
                        if (objGear.Parent is IHasChildren<Gear> parent)
                            parent.Children.Remove(objGear);
                        else if (objOldCyberware != null)
                            objOldCyberware.GearChildren.Remove(objGear);
                        else if (objOldWeaponAccessory != null)
                            objOldWeaponAccessory.GearChildren.Remove(objGear);
                        else
                            objOldVehicle.GearChildren.Remove(objGear);

                        // Add the Gear to its new parent.
                        objGear.Location = null;
                        objDestinationGear.Children.Add(objGear);
                    }
                    else
                    {
                        // Determine if this is a Location.
                        TreeNode nodVehicleNode = nodDestination;
                        Location objLocation = null;
                        while (nodVehicleNode.Level > 1)
                        {
                            if (objLocation is null && nodVehicleNode.Tag is Location loc)
                            {
                                objLocation = loc;
                            }

                            nodVehicleNode = nodVehicleNode.Parent;
                        }

                        // Determine if this is a Location in the destination Vehicle.
                        if (nodDestination.Tag is Vehicle objNewVehicle)
                        {
                            // Remove the Gear from the Vehicle.
                            if (objGear.Parent is IHasChildren<Gear> parent)
                                parent.Children.Remove(objGear);
                            else if (objOldCyberware != null)
                                objOldCyberware.GearChildren.Remove(objGear);
                            else if (objOldWeaponAccessory != null)
                                objOldWeaponAccessory.GearChildren.Remove(objGear);
                            else
                                objOldVehicle.GearChildren.Remove(objGear);

                            // Add the Gear to the Vehicle and set its Location.
                            objNewVehicle.GearChildren.Add(objGear);
                            objLocation?.Children.Add(objGear);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Move a Vehicle Gear TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="nodDestination">Destination Node.</param>
        /// <param name="nodGearNode">Node of gear to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public async Task MoveVehicleGearParentAsync(TreeNode nodDestination, TreeNode nodGearNode, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (nodDestination == null || nodGearNode == null)
                return;
            // The item cannot be dropped onto itself or onto one of its children.
            for (TreeNode objCheckNode = nodDestination;
                objCheckNode != null && objCheckNode.Level >= nodDestination.Level;
                objCheckNode = objCheckNode.Parent)
            {
                if (objCheckNode == nodGearNode)
                    return;
            }

            if (!(nodGearNode.Tag is IHasInternalId nodeId))
                return;
            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                // Locate the currently selected piece of Gear.
                //TODO: Better interface for determining what the parent of a bit of gear is.
                (Gear objGear, Vehicle objOldVehicle, WeaponAccessory objOldWeaponAccessory, Cyberware objOldCyberware) = await Vehicles.FindVehicleGearAsync(nodeId.InternalId, token: token).ConfigureAwait(false);

                if (objGear == null)
                    return;

                IAsyncDisposable objLocker2 = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
                try
                {
                    token.ThrowIfCancellationRequested();
                    if (nodDestination.Tag is Gear objDestinationGear)
                    {
                        // Remove the Gear from the Vehicle.
                        if (objGear.Parent is IHasChildren<Gear> parent)
                            await parent.Children.RemoveAsync(objGear, token).ConfigureAwait(false);
                        else if (objOldCyberware != null)
                            await objOldCyberware.GearChildren.RemoveAsync(objGear, token).ConfigureAwait(false);
                        else if (objOldWeaponAccessory != null)
                            await objOldWeaponAccessory.GearChildren.RemoveAsync(objGear, token).ConfigureAwait(false);
                        else
                            await objOldVehicle.GearChildren.RemoveAsync(objGear, token).ConfigureAwait(false);

                        // Add the Gear to its new parent.
                        objGear.Location = null;
                        await objDestinationGear.Children.AddAsync(objGear, token).ConfigureAwait(false);
                    }
                    else
                    {
                        // Determine if this is a Location.
                        TreeNode nodVehicleNode = nodDestination;
                        Location objLocation = null;
                        while (nodVehicleNode.Level > 1)
                        {
                            if (objLocation is null && nodVehicleNode.Tag is Location loc)
                            {
                                objLocation = loc;
                            }

                            nodVehicleNode = nodVehicleNode.Parent;
                        }

                        // Determine if this is a Location in the destination Vehicle.
                        if (nodDestination.Tag is Vehicle objNewVehicle)
                        {
                            // Remove the Gear from the Vehicle.
                            if (objGear.Parent is IHasChildren<Gear> parent)
                                await parent.Children.RemoveAsync(objGear, token).ConfigureAwait(false);
                            else if (objOldCyberware != null)
                                await objOldCyberware.GearChildren.RemoveAsync(objGear, token).ConfigureAwait(false);
                            else if (objOldWeaponAccessory != null)
                                await objOldWeaponAccessory.GearChildren.RemoveAsync(objGear, token).ConfigureAwait(false);
                            else
                                await objOldVehicle.GearChildren.RemoveAsync(objGear, token).ConfigureAwait(false);

                            // Add the Gear to the Vehicle and set its Location.
                            await objNewVehicle.GearChildren.AddAsync(objGear, token).ConfigureAwait(false);
                            if (objLocation != null)
                                await objLocation.Children.AddAsync(objGear, token).ConfigureAwait(false);
                        }
                    }
                }
                finally
                {
                    await objLocker2.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Move an Improvement TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="nodOldNode">Node of improvement to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public void MoveImprovementNode(TreeNode objDestination, TreeNode nodOldNode, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (objDestination == null)
                return;
            if (!(nodOldNode?.Tag is Improvement objImprovement))
                return;
            TreeNode objNewParent = objDestination;
            while (objNewParent.Level > 0)
                objNewParent = objNewParent.Parent;
            string strGroup = objNewParent.Tag?.ToString() ?? string.Empty;
            if (!string.IsNullOrEmpty(strGroup) && strGroup != "Node_SelectedImprovements")
                strGroup = objNewParent.Text;
            using (LockObject.EnterWriteLock(token))
            {
                objImprovement.CustomGroup = strGroup;
                Improvements[Improvements.IndexOf(objImprovement)] = objImprovement;
            }
        }

        /// <summary>
        /// Move an Improvement TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="nodOldNode">Node of improvement to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public async Task MoveImprovementNodeAsync(TreeNode objDestination, TreeNode nodOldNode, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (objDestination == null)
                return;
            if (!(nodOldNode?.Tag is Improvement objImprovement))
                return;
            TreeNode objNewParent = objDestination;
            while (objNewParent.Level > 0)
                objNewParent = objNewParent.Parent;
            string strGroup = objNewParent.Tag?.ToString() ?? string.Empty;
            if (!string.IsNullOrEmpty(strGroup) && strGroup != "Node_SelectedImprovements")
                strGroup = objNewParent.Text;
            IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                objImprovement.CustomGroup = strGroup;
                await Improvements.SetValueAtAsync(await Improvements.IndexOfAsync(objImprovement, token).ConfigureAwait(false), objImprovement, token).ConfigureAwait(false);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Move an Improvement Group TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="intNewIndex">Node's new index.</param>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="nodOldNode">Node of improvement group to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public void MoveImprovementRoot(int intNewIndex, TreeNode objDestination, TreeNode nodOldNode, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (nodOldNode == null)
                return;
            string strNewGroup = string.Empty;
            if (objDestination != null)
            {
                TreeNode objNewParent = objDestination;
                while (objNewParent.Level > 0)
                    objNewParent = objNewParent.Parent;
                intNewIndex = objNewParent.Index;
                strNewGroup = objNewParent.Tag?.ToString() ?? string.Empty;
            }

            string strLocation = nodOldNode.Tag.ToString();
            using (LockObject.EnterUpgradeableReadLock(token))
            {
                ThreadSafeObservableCollection<string> lstImprovementGroups = ImprovementGroups;
                if (!lstImprovementGroups.Contains(strNewGroup))
                    intNewIndex = 0;
                lstImprovementGroups.Move(lstImprovementGroups.IndexOf(strLocation), intNewIndex);
            }
        }

        /// <summary>
        /// Move an Improvement Group TreeNode after Drag and Drop.
        /// </summary>
        /// <param name="intNewIndex">Node's new index.</param>
        /// <param name="objDestination">Destination Node.</param>
        /// <param name="nodOldNode">Node of improvement group to move.</param>
        /// <param name="token">CancellationToken to listen to.</param>
        public async Task MoveImprovementRootAsync(int intNewIndex, TreeNode objDestination, TreeNode nodOldNode, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (nodOldNode == null)
                return;
            string strNewGroup = string.Empty;
            if (objDestination != null)
            {
                TreeNode objNewParent = objDestination;
                while (objNewParent.Level > 0)
                    objNewParent = objNewParent.Parent;
                intNewIndex = objNewParent.Index;
                strNewGroup = objNewParent.Tag?.ToString() ?? string.Empty;
            }

            string strLocation = nodOldNode.Tag.ToString();
            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                ThreadSafeObservableCollection<string> lstImprovementGroups = await GetImprovementGroupsAsync(token).ConfigureAwait(false);
                if (!await lstImprovementGroups.ContainsAsync(strNewGroup, token).ConfigureAwait(false))
                    intNewIndex = 0;
                await lstImprovementGroups.MoveAsync(await lstImprovementGroups.IndexOfAsync(strLocation, token).ConfigureAwait(false), intNewIndex, token).ConfigureAwait(false);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        #endregion Move TreeNodes

        #region Tab clearing

        /// <summary>
        /// Clear all Spell tab elements from the character.
        /// </summary>
        public void ClearMagic(bool blnKeepAdeptEligible, CancellationToken token = default)
        {
            using (LockObject.EnterWriteLock(token))
            {
                if (ImprovementManager
                        .GetCachedImprovementListForValueOf(this, Improvement.ImprovementType.FreeSpells, token: token)
                        .Count > 0
                    || ImprovementManager
                        .GetCachedImprovementListForValueOf(this, Improvement.ImprovementType.FreeSpellsATT,
                            token: token).Count > 0
                    || ImprovementManager
                        .GetCachedImprovementListForValueOf(this, Improvement.ImprovementType.FreeSpellsSkill,
                            token: token).Count >
                    0)
                {
                    // Run through all of the Spells and remove their Improvements.
                    if (Spells.All(x =>
                            x.Grade == 0 && (!blnKeepAdeptEligible || x.Category != "Rituals" ||
                                             x.Descriptors.Contains("Spell")), token))
                    {
                        List<string> lstIds = Spells.Select(x => x.InternalId).ToList();
                        ImprovementManager.RemoveImprovements(this, Improvement.ImprovementSource.Spell,
                            lstIds, token: token);
                        Spells.Clear();
                    }
                    else
                    {
                        for (int i = Spells.Count - 1; i >= 0; --i)
                        {
                            if (i < Spells.Count)
                            {
                                Spell objToRemove = Spells[i];
                                if (objToRemove.Grade == 0 &&
                                    (!blnKeepAdeptEligible || objToRemove.Category != "Rituals" ||
                                     objToRemove.Descriptors.Contains("Spell")))
                                {
                                    objToRemove.Remove(false);
                                }
                            }
                        }
                    }
                }

                if (Spirits.All(x => x.EntityType == SpiritType.Spirit, token))
                {
                    Spirits.Clear();
                }
                else
                {
                    for (int i = Spirits.Count - 1; i >= 0; --i)
                    {
                        if (i < Spirits.Count)
                        {
                            Spirit objToRemove = Spirits[i];
                            if (objToRemove.EntityType == SpiritType.Spirit)
                            {
                                Spirits.RemoveAt(i);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clear all Spell tab elements from the character.
        /// </summary>
        public async Task ClearMagicAsync(bool blnKeepAdeptEligible, CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                if ((await ImprovementManager
                        .GetCachedImprovementListForValueOfAsync(this, Improvement.ImprovementType.FreeSpells,
                            token: token).ConfigureAwait(false))
                    .Count > 0
                    || (await ImprovementManager
                        .GetCachedImprovementListForValueOfAsync(this, Improvement.ImprovementType.FreeSpellsATT,
                            token: token).ConfigureAwait(false)).Count > 0
                    || (await ImprovementManager
                        .GetCachedImprovementListForValueOfAsync(this, Improvement.ImprovementType.FreeSpellsSkill,
                            token: token).ConfigureAwait(false)).Count >
                    0)
                {
                    ThreadSafeObservableCollection<Spell> lstSpells =
                        await GetSpellsAsync(token).ConfigureAwait(false);
                    // Run through all of the Spells and remove their Improvements.
                    if (await lstSpells
                            .AllAsync(
                                x => x.Grade == 0 && (!blnKeepAdeptEligible || x.Category != "Rituals" ||
                                                      x.Descriptors.Contains("Spell")), token: token)
                            .ConfigureAwait(false))
                    {
                        List<string> lstIds = new List<string>(await lstSpells.GetCountAsync(token).ConfigureAwait(false));
                        await lstSpells.ForEachAsync(x => lstIds.Add(x.InternalId), token).ConfigureAwait(false);
                        await ImprovementManager.RemoveImprovementsAsync(this, Improvement.ImprovementSource.Spell,
                            lstIds, token: token).ConfigureAwait(false);
                        await lstSpells.ClearAsync(token).ConfigureAwait(false);
                    }
                    else
                    {
                        for (int i = await lstSpells.GetCountAsync(token).ConfigureAwait(false) - 1; i >= 0; --i)
                        {
                            if (i < await lstSpells.GetCountAsync(token).ConfigureAwait(false))
                            {
                                Spell objToRemove = await lstSpells.GetValueAtAsync(i, token).ConfigureAwait(false);
                                if (objToRemove.Grade == 0 &&
                                    (!blnKeepAdeptEligible || objToRemove.Category != "Rituals" ||
                                     objToRemove.Descriptors.Contains("Spell")))
                                {
                                    await objToRemove.RemoveAsync(false, token).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                }

                ThreadSafeObservableCollection<Spirit> lstSpirits =
                    await GetSpiritsAsync(token).ConfigureAwait(false);
                if (await lstSpirits.AllAsync(async x => await x.GetEntityTypeAsync(token).ConfigureAwait(false) == SpiritType.Spirit, token).ConfigureAwait(false))
                {
                    await lstSpirits.ClearAsync(token).ConfigureAwait(false);
                }
                else
                {
                    for (int i = await lstSpirits.GetCountAsync(token).ConfigureAwait(false) - 1; i >= 0; --i)
                    {
                        if (i < await lstSpirits.GetCountAsync(token).ConfigureAwait(false))
                        {
                            Spirit objToRemove = await lstSpirits.GetValueAtAsync(i, token).ConfigureAwait(false);
                            if (await objToRemove.GetEntityTypeAsync(token).ConfigureAwait(false) == SpiritType.Spirit)
                            {
                                await lstSpirits.RemoveAtAsync(i, token).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Clear all Adept tab elements from the character.
        /// </summary>
        public void ClearAdeptPowers(CancellationToken token = default)
        {
            using (LockObject.EnterUpgradeableReadLock(token))
            {
                if (Powers.All(x => x.FreeLevels == 0 && x.FreePoints == 0, token))
                {
                    List<string> lstIds = Powers.Select(x => x.InternalId).ToList();
                    using (LockObject.EnterWriteLock(token))
                    {
                        ImprovementManager.RemoveImprovements(this, Improvement.ImprovementSource.Power, lstIds,
                            token: token);
                        Powers.Clear();
                    }
                }
                else
                {
                    using (LockObject.EnterWriteLock(token))
                    {
                        // Run through all powers and remove the ones not added by improvements or foci
                        for (int i = Powers.Count - 1; i >= 0; --i)
                        {
                            if (i < Powers.Count)
                            {
                                Power objToRemove = Powers[i];
                                if (objToRemove.FreeLevels == 0 && objToRemove.FreePoints == 0)
                                {
                                    // Remove the Improvements created by the Power.
                                    ImprovementManager.RemoveImprovements(this, Improvement.ImprovementSource.Power,
                                        objToRemove.InternalId, token: token);
                                    Powers.RemoveAt(i);
                                }
                                else
                                    objToRemove.Rating = 0;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clear all Adept tab elements from the character.
        /// </summary>
        public async Task ClearAdeptPowersAsync(CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                ThreadSafeBindingList<Power> lstPowers = await GetPowersAsync(token).ConfigureAwait(false);
                if (await lstPowers.AllAsync(async x => await x.GetFreeLevelsAsync(token).ConfigureAwait(false) == 0 && await x.GetFreePointsAsync(token).ConfigureAwait(false) == 0, token).ConfigureAwait(false))
                {
                    List<string> lstIds = new List<string>(await lstPowers.GetCountAsync(token).ConfigureAwait(false));
                    await lstPowers.ForEachAsync(x => lstIds.Add(x.InternalId), token).ConfigureAwait(false);
                    IAsyncDisposable objLocker2 = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        await ImprovementManager.RemoveImprovementsAsync(this, Improvement.ImprovementSource.Power,
                            lstIds,
                            token: token).ConfigureAwait(false);
                        await lstPowers.ClearAsync(token).ConfigureAwait(false);
                    }
                    finally
                    {
                        await objLocker2.DisposeAsync().ConfigureAwait(false);
                    }
                }
                else
                {
                    IAsyncDisposable objLocker2 = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        // Run through all powers and remove the ones not added by improvements or foci
                        for (int i = await lstPowers.GetCountAsync(token).ConfigureAwait(false) - 1; i >= 0; --i)
                        {
                            if (i < await lstPowers.GetCountAsync(token).ConfigureAwait(false))
                            {
                                Power objToRemove = await lstPowers.GetValueAtAsync(i, token).ConfigureAwait(false);
                                if (await objToRemove.GetFreeLevelsAsync(token).ConfigureAwait(false) == 0 &&
                                    await objToRemove.GetFreePointsAsync(token).ConfigureAwait(false) == 0)
                                {
                                    // Remove the Improvements created by the Power.
                                    await ImprovementManager.RemoveImprovementsAsync(this,
                                        Improvement.ImprovementSource.Power,
                                        objToRemove.InternalId, token: token).ConfigureAwait(false);
                                    await lstPowers.RemoveAtAsync(i, token).ConfigureAwait(false);
                                }
                                else
                                    await objToRemove.SetRatingAsync(0, token).ConfigureAwait(false);
                            }
                        }
                    }
                    finally
                    {
                        await objLocker2.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Clear all Technomancer tab elements from the character.
        /// </summary>
        public void ClearResonance(CancellationToken token = default)
        {
            using (LockObject.EnterWriteLock(token))
            {
                // Run through all of the Complex Forms and remove their Improvements.
                for (int i = ComplexForms.Count - 1; i >= 0; --i)
                {
                    if (i < ComplexForms.Count)
                    {
                        ComplexForm objToRemove = ComplexForms[i];
                        if (objToRemove.Grade == 0)
                        {
                            objToRemove.Remove(false);
                        }
                    }
                }

                if (Spirits.All(x => x.EntityType == SpiritType.Sprite, token))
                {
                    Spirits.Clear();
                }
                else
                {
                    for (int i = Spirits.Count - 1; i >= 0; --i)
                    {
                        if (i < Spirits.Count)
                        {
                            Spirit objToRemove = Spirits[i];
                            if (objToRemove.EntityType == SpiritType.Sprite)
                            {
                                Spirits.RemoveAt(i);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clear all Technomancer tab elements from the character.
        /// </summary>
        public async Task ClearResonanceAsync(CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                ThreadSafeObservableCollection<ComplexForm> lstComplexForms = await GetComplexFormsAsync(token).ConfigureAwait(false);
                // Run through all of the Complex Forms and remove their Improvements.
                for (int i = await lstComplexForms.GetCountAsync(token).ConfigureAwait(false) - 1; i >= 0; --i)
                {
                    if (i < await lstComplexForms.GetCountAsync(token).ConfigureAwait(false))
                    {
                        ComplexForm objToRemove =
                            await lstComplexForms.GetValueAtAsync(i, token).ConfigureAwait(false);
                        if (objToRemove.Grade == 0)
                        {
                            // Remove the Improvements created by the Spell.
                            await objToRemove.RemoveAsync(false, token).ConfigureAwait(false);
                        }
                    }
                }

                ThreadSafeObservableCollection<Spirit> lstSpirits = await GetSpiritsAsync(token).ConfigureAwait(false);
                if (await lstSpirits.AllAsync(async x => await x.GetEntityTypeAsync(token).ConfigureAwait(false) == SpiritType.Sprite, token).ConfigureAwait(false))
                {
                    await lstSpirits.ClearAsync(token).ConfigureAwait(false);
                }
                else
                {
                    for (int i = await lstSpirits.GetCountAsync(token).ConfigureAwait(false) - 1; i >= 0; --i)
                    {
                        if (i < await lstSpirits.GetCountAsync(token).ConfigureAwait(false))
                        {
                            Spirit objToRemove = await lstSpirits.GetValueAtAsync(i, token).ConfigureAwait(false);
                            if (await objToRemove.GetEntityTypeAsync(token).ConfigureAwait(false) == SpiritType.Sprite)
                            {
                                await lstSpirits.RemoveAtAsync(i, token).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Clear all Advanced Programs tab elements from the character.
        /// </summary>
        public void ClearAdvancedPrograms(CancellationToken token = default)
        {
            using (LockObject.EnterUpgradeableReadLock(token))
            {
                using (LockObject.EnterWriteLock(token))
                {
                    // Run through all advanced programs and remove the ones not added by improvements
                    for (int i = AIPrograms.Count - 1; i >= 0; --i)
                    {
                        if (i < AIPrograms.Count)
                        {
                            AIProgram objToRemove = AIPrograms[i];
                            if (objToRemove.CanDelete)
                            {
                                objToRemove.Remove(false);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clear all Advanced Programs tab elements from the character.
        /// </summary>
        public async Task ClearAdvancedProgramsAsync(CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                IAsyncDisposable objLocker2 = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
                try
                {
                    token.ThrowIfCancellationRequested();
                    ThreadSafeObservableCollection<AIProgram> lstAIPrograms =
                        await GetAIProgramsAsync(token).ConfigureAwait(false);
                    for (int i = await lstAIPrograms.GetCountAsync(token).ConfigureAwait(false) - 1; i >= 0; --i)
                    {
                        if (i < await lstAIPrograms.GetCountAsync(token).ConfigureAwait(false))
                        {
                            AIProgram objToRemove =
                                await lstAIPrograms.GetValueAtAsync(i, token).ConfigureAwait(false);
                            if (objToRemove.CanDelete)
                            {
                                // Remove the Improvements created by the Program.
                                await objToRemove.RemoveAsync(false, token).ConfigureAwait(false);
                            }
                        }
                    }
                }
                finally
                {
                    await objLocker2.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Clear all cyberware and bioware implanted on the character.
        /// </summary>
        public void ClearCyberwareTab(CancellationToken token = default)
        {
            using (LockObject.EnterUpgradeableReadLock(token))
            {
                string strDisabledSource = string.Empty;
                if (Created)
                {
                    Improvement objDisablingImprovement = ImprovementManager
                        .GetCachedImprovementListForValueOf(
                            this,
                            Improvement.ImprovementType.SpecialTab,
                            "Cyberware", token: token)
                        .Find(x => x.UniqueName == "disabletab");
                    if (objDisablingImprovement != null)
                    {
                        strDisabledSource = LanguageManager.GetString("String_Space", token: token) +
                                            "(" + GetObjectName(objDisablingImprovement, GlobalSettings.Language, token: token) +
                                            ")" +
                                            LanguageManager.GetString("String_Space", token: token);
                    }
                }

                using (LockObject.EnterWriteLock(token))
                {
                    foreach (Cyberware objCyberware in Cyberware
                                 .Where(x => x.SourceID != Backend.Equipment.Cyberware.EssenceHoleGUID
                                             && x.SourceID != Backend.Equipment.Cyberware
                                                 .EssenceAntiHoleGUID && x.IsModularCurrentlyEquipped)
                                 .ToList())
                    {
                        if (!string.IsNullOrEmpty(objCyberware.PlugsIntoModularMount))
                        {
                            if (objCyberware.CanRemoveThroughImprovements)
                            {
                                objCyberware.Parent?.Children.Remove(objCyberware);
                                Cyberware.Add(objCyberware);
                                objCyberware.ChangeModularEquip(false);
                            }
                        }
                        else if (objCyberware.CanRemoveThroughImprovements)
                        {
                            objCyberware.DeleteCyberware();
                            ExpenseLogEntry objExpense = new ExpenseLogEntry(this);
                            string strEntry = LanguageManager.GetString(
                                objCyberware.SourceType == Improvement.ImprovementSource.Cyberware
                                    ? "String_ExpenseSoldCyberware"
                                    : "String_ExpenseSoldBioware", token: token);
                            objExpense.Create(0,
                                strEntry + strDisabledSource
                                         + objCyberware.CurrentDisplayNameShort,
                                ExpenseType.Nuyen, DateTime.Now);
                            ExpenseEntries.AddWithSort(objExpense, token: token);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clear all cyberware and bioware implanted on the character.
        /// </summary>
        public async Task ClearCyberwareTabAsync(CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                string strDisabledSource = string.Empty;
                if (await GetCreatedAsync(token).ConfigureAwait(false))
                {
                    Improvement objDisablingImprovement = (await ImprovementManager
                            .GetCachedImprovementListForValueOfAsync(
                                this,
                                Improvement.ImprovementType.SpecialTab,
                                "Cyberware", token: token).ConfigureAwait(false))
                        .Find(x => x.UniqueName == "disabletab");
                    if (objDisablingImprovement != null)
                    {
                        strDisabledSource = await LanguageManager.GetStringAsync("String_Space", token: token).ConfigureAwait(false) +
                                            "(" + await GetObjectNameAsync(objDisablingImprovement, GlobalSettings.Language, token: token).ConfigureAwait(false) +
                                            ")" +
                                            await LanguageManager.GetStringAsync("String_Space", token: token).ConfigureAwait(false);
                    }
                }

                IAsyncDisposable objLocker2 = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
                try
                {
                    token.ThrowIfCancellationRequested();
                    foreach (Cyberware objCyberware in await Cyberware
                                 .ToListAsync(async x =>
                                 {
                                     Guid guidSourceId = await x.GetSourceIDAsync(token).ConfigureAwait(false);
                                     return guidSourceId != Backend.Equipment.Cyberware.EssenceHoleGUID
                                            && guidSourceId != Backend.Equipment.Cyberware.EssenceAntiHoleGUID
                                            && await x.GetIsModularCurrentlyEquippedAsync(token).ConfigureAwait(false);
                                 }, token: token).ConfigureAwait(false))
                    {
                        if (!string.IsNullOrEmpty(await objCyberware.GetPlugsIntoModularMountAsync(token).ConfigureAwait(false)))
                        {
                            if (await objCyberware.GetCanRemoveThroughImprovementsAsync(token).ConfigureAwait(false))
                            {
                                Cyberware objParent = await objCyberware.GetParentAsync(token).ConfigureAwait(false);
                                if (objParent != null)
                                    await (await objParent.GetChildrenAsync(token).ConfigureAwait(false)).RemoveAsync(objCyberware, token).ConfigureAwait(false);
                                await Cyberware.AddAsync(objCyberware, token).ConfigureAwait(false);
                                await objCyberware.ChangeModularEquipAsync(false, token: token).ConfigureAwait(false);
                            }
                        }
                        else if (await objCyberware.GetCanRemoveThroughImprovementsAsync(token).ConfigureAwait(false))
                        {
                            await objCyberware.DeleteCyberwareAsync(token: token).ConfigureAwait(false);
                            ExpenseLogEntry objExpense = new ExpenseLogEntry(this);
                            string strEntry = await LanguageManager.GetStringAsync(
                                await objCyberware.GetSourceTypeAsync(token).ConfigureAwait(false) == Improvement.ImprovementSource.Cyberware
                                    ? "String_ExpenseSoldCyberware"
                                    : "String_ExpenseSoldBioware", token: token).ConfigureAwait(false);
                            objExpense.Create(0,
                                strEntry + strDisabledSource
                                         + await objCyberware.GetCurrentDisplayNameShortAsync(token).ConfigureAwait(false),
                                ExpenseType.Nuyen, DateTime.Now);
                            await ExpenseEntries.AddWithSortAsync(objExpense, token: token).ConfigureAwait(false);
                        }
                    }
                }
                finally
                {
                    await objLocker2.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Clear all Critter tab elements from the character.
        /// </summary>
        public void ClearCritterPowers(CancellationToken token = default)
        {
            using (LockObject.EnterUpgradeableReadLock(token))
            {
                if (CritterPowers.All(x => x.Grade >= 0, token))
                {
                    List<string> lstIds = CritterPowers.Select(x => x.InternalId).ToList();
                    using (LockObject.EnterWriteLock(token))
                    {
                        ImprovementManager.RemoveImprovements(this, Improvement.ImprovementSource.CritterPower, lstIds, token: token);
                        CritterPowers.Clear();
                    }
                }
                else
                {
                    using (LockObject.EnterWriteLock(token))
                    {
                        for (int i = CritterPowers.Count - 1; i >= 0; --i)
                        {
                            if (i < CritterPowers.Count)
                            {
                                CritterPower objToRemove = CritterPowers[i];
                                if (objToRemove.Grade >= 0)
                                {
                                    // Remove the Improvements created by the Metamagic.
                                    ImprovementManager.RemoveImprovements(this,
                                        Improvement.ImprovementSource.CritterPower,
                                        objToRemove.InternalId, token: token);
                                    CritterPowers.RemoveAt(i);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clear all Critter tab elements from the character.
        /// </summary>
        public async Task ClearCritterPowersAsync(CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                ThreadSafeObservableCollection<CritterPower> lstCritterPowers =
                    await GetCritterPowersAsync(token).ConfigureAwait(false);
                if (await lstCritterPowers.AllAsync(x => x.Grade >= 0, token: token).ConfigureAwait(false))
                {
                    List<string> lstIds = new List<string>(await lstCritterPowers.GetCountAsync(token).ConfigureAwait(false));
                    await lstCritterPowers.ForEachAsync(x => lstIds.Add(x.InternalId), token).ConfigureAwait(false);
                    IAsyncDisposable objLocker2 = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        await ImprovementManager.RemoveImprovementsAsync(this, Improvement.ImprovementSource.CritterPower, lstIds,
                            token: token).ConfigureAwait(false);
                        await lstCritterPowers.ClearAsync(token).ConfigureAwait(false);
                    }
                    finally
                    {
                        await objLocker2.DisposeAsync().ConfigureAwait(false);
                    }
                }
                else
                {
                    IAsyncDisposable objLocker2 = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        for (int i = await lstCritterPowers.GetCountAsync(token).ConfigureAwait(false) - 1; i >= 0; --i)
                        {
                            if (i < await lstCritterPowers.GetCountAsync(token).ConfigureAwait(false))
                            {
                                CritterPower objToRemove =
                                    await lstCritterPowers.GetValueAtAsync(i, token).ConfigureAwait(false);
                                if (objToRemove.Grade >= 0)
                                {
                                    // Remove the Improvements created by the Metamagic.
                                    await ImprovementManager.RemoveImprovementsAsync(this,
                                        Improvement.ImprovementSource.CritterPower,
                                        objToRemove.InternalId, token: token).ConfigureAwait(false);
                                    await lstCritterPowers.RemoveAtAsync(i, token).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                    finally
                    {
                        await objLocker2.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Clear all Initiation tab elements from the character that were not added by improvements.
        /// </summary>
        public void ClearInitiations(CancellationToken token = default)
        {
            using (LockObject.EnterWriteLock(token))
            {
                // Do not update grade numbers until after we're done processing everything
                _blnClearingInitiations = true;
                try
                {
                    token.ThrowIfCancellationRequested();
                    // We need to remove grades that can potentially add stuff that adds grades, so we cannot use foreach
                    for (int i = InitiationGrades.Count - 1; i >= 0; --i)
                    {
                        InitiationGrades[i].Remove(false, false);
                    }
                }
                finally
                {
                    _blnClearingInitiations = false;
                }

                // Now update our grade numbers
                InitiateGrade = 0;
                SubmersionGrade = 0;
            }
        }

        /// <summary>
        /// Clear all Initiation tab elements from the character that were not added by improvements.
        /// </summary>
        public async Task ClearInitiationsAsync(CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                // Do not update grade numbers until after we're done processing everything
                _blnClearingInitiations = true;
                try
                {
                    token.ThrowIfCancellationRequested();
                    // We need to remove grades that can potentially add stuff that adds grades, so we cannot use foreach
                    for (int i = await InitiationGrades.GetCountAsync(token).ConfigureAwait(false) - 1; i >= 0; --i)
                    {
                        await (await InitiationGrades.GetValueAtAsync(i, token).ConfigureAwait(false))
                              .RemoveAsync(false, false, token).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _blnClearingInitiations = false;
                }

                // Now update our grade numbers
                await SetInitiateGradeAsync(0, token).ConfigureAwait(false);
                await SetSubmersionGradeAsync(0, token).ConfigureAwait(false);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        #endregion Tab clearing

        #endregion UI Methods

        /// <summary>
        /// Alias map for SourceDetail control text and tooltip assignation.
        /// </summary>
        /// <param name="sourceControl"></param>
        public void SetSourceDetail(Control sourceControl)
        {
            SourceDetail.SetControl(sourceControl);
        }
        public async Task SetSourceDetailAsync(Control sourceControl, CancellationToken token = default)
        {
            await (await GetSourceDetailAsync(token).ConfigureAwait(false)).SetControlAsync(sourceControl, token).ConfigureAwait(false);
        }
    }
}
