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

// Metade dependente de WinForms de Character, separada do domínio durante a
// extração do núcleo (DEC-023). A metade de regras vive em Chummer/Backend/Characters/Character.cs.
//
// As duas são `partial class Character`: no projeto legado voltam a ser uma classe
// só, então nenhum chamador precisou mudar. No Chummer.Core, só o domínio existe.

using System.Windows.Forms;
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
        private static readonly TelemetryClient TelemetryClient = new TelemetryClient();
        /// <summary>
        /// Load the Character from an XML file synchronously.
        /// </summary>
        /// <param name="strFileName">Name of the file to load the character from. Leave empty if the character should be loaded from FileName.</param>
        /// <param name="frmLoadingForm">Instance of frmLoading to use to update with loading progress. <see cref="LoadingBar.PerformStep(string, LoadingBar.ProgressBarTextPatterns)"/> is called <see cref="NumLoadingSections"/> times within this method, so plan accordingly.</param>
        /// <param name="showWarnings">Whether warnings about book content and other character content should be loaded.</param>
        /// <param name="token">Cancellation token to use.</param>
        public bool Load(string strFileName = "", LoadingBar frmLoadingForm = null, bool showWarnings = true, CancellationToken token = default)
        {
            return Utils.SafelyRunSynchronously(() => LoadCoreAsync(true, strFileName, frmLoadingForm, showWarnings, token), token);
        }
        /// <summary>
        /// Load the Character from an XML file asynchronously.
        /// </summary>
        /// <param name="strFileName">Name of the file to load the character from. Leave empty if the character should be loaded from FileName.</param>
        /// <param name="frmLoadingForm">Instance of frmLoading to use to update with loading progress. <see cref="LoadingBar.PerformStepAsync(string, LoadingBar.ProgressBarTextPatterns, CancellationToken)"/> is called <see cref="NumLoadingSections"/> times within this method, so plan accordingly.</param>
        /// <param name="showWarnings">Whether warnings about book content and other character content should be loaded.</param>
        /// <param name="token">Cancellation token to use.</param>
        public Task<bool> LoadAsync(string strFileName = "", LoadingBar frmLoadingForm = null, bool showWarnings = true, CancellationToken token = default)
        {
            return LoadCoreAsync(false, strFileName, frmLoadingForm, showWarnings, token);
        }
        /// <summary>
        /// Load the Character from an XML file.
        /// Uses flag hack method design outlined here to avoid locking:
        /// https://docs.microsoft.com/en-us/archive/msdn-magazine/2015/july/async-programming-brownfield-async-development
        /// </summary>
        /// <param name="blnSync">Flag for whether method should always use synchronous code or not.</param>
        /// <param name="strFileName">Name of the file to load the character from. Leave empty if the character should be loaded from FileName.</param>
        /// <param name="frmLoadingForm">Instance of frmLoading to use to update with loading progress. <see cref="LoadingBar.PerformStep(string, LoadingBar.ProgressBarTextPatterns)"/> or <see cref="LoadingBar.PerformStepAsync(string, LoadingBar.ProgressBarTextPatterns, CancellationToken)"/> is called <see cref="NumLoadingSections"/> times within this method, so plan accordingly.</param>
        /// <param name="showWarnings">Whether warnings about book content and other character content should be loaded.</param>
        /// <param name="token">Cancellation token to use.</param>
        private async Task<bool> LoadCoreAsync(bool blnSync, string strFileName = "", LoadingBar frmLoadingForm = null,
                                               bool showWarnings = true, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(strFileName))
                strFileName = FileName;
            if (!File.Exists(strFileName) || (!strFileName.EndsWith(".chum5", StringComparison.OrdinalIgnoreCase)
                                              && !strFileName.EndsWith(".chum5lz", StringComparison.OrdinalIgnoreCase)))
                return false;

            IDisposable objLocker = null;
            IAsyncDisposable objLockerAsync = null;
            if (blnSync)
                // ReSharper disable once MethodHasAsyncOverload
                objLocker = LockObject.EnterWriteLock(token);
            else
                objLockerAsync = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                if (blnSync)
                    LoadAsDirty = false;
                else
                    await SetLoadAsDirtyAsync(false, token).ConfigureAwait(false);
                using (Activity loadActivity = Timekeeper.StartSyncron("clsCharacter.Load", null,
                                                                             TelemetryOperationType
                                                                                 .DependencyOperation, strFileName))
                {
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        using (Timekeeper.StartSyncron("upload_AI_options", loadActivity))
                        {
                            UploadObjectAsMetric.UploadObject(TelemetryClient, blnSync ? Settings : await GetSettingsAsync(token).ConfigureAwait(false));
                        }

                        XmlDocument objXmlDocument = new XmlDocument { XmlResolver = null };
                        XmlNode objXmlCharacter = null;
                        XPathNavigator xmlCharacterNavigator = null;
                        Quality objLivingPersonaQuality = null;

                        if (frmLoadingForm != null)
                        {
                            if (blnSync)
                                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                frmLoadingForm.PerformStep("XML");
                            else
                                await frmLoadingForm.PerformStepAsync("XML", token: token).ConfigureAwait(false);
                        }

                        using (Timekeeper.StartSyncron("load_xml", loadActivity))
                        {
                            bool blnKeepLoading = blnSync
                                ? LoadSaveFileDocument()
                                : await LoadSaveFileDocumentAsync().ConfigureAwait(false);

                            bool LoadSaveFileDocument()
                            {
                                bool blnErrorCaught = false;
                                do
                                {
                                    try
                                    {
                                        token.ThrowIfCancellationRequested();
                                        if (strFileName.EndsWith(".chum5", StringComparison.OrdinalIgnoreCase))
                                            objXmlDocument.LoadStandardPatient(strFileName, !blnErrorCaught, token: token);
                                        else if (strFileName.EndsWith(".chum5lz", StringComparison.OrdinalIgnoreCase))
                                            objXmlDocument.LoadStandardFromLzmaCompressedPatient(
                                                strFileName, !blnErrorCaught, token: token);
                                        else
                                            throw new InvalidOperationException();
                                        blnErrorCaught = false;
                                    }
                                    catch (XmlException ex)
                                    {
                                        ex = ex.Demystify();
                                        if (ex.Message.HasAnyXmlInvalidUnicodeChars())
                                        {
                                            /*If we found a known control character that's preventing the character from
                                            being loaded (Expected to be notes ingested from PDF mostly) prompt the user whether to use unsafe methods.
                                            If yes, restart the load, explicitly ignoring invalid characters.*/

                                            if (UserInteraction.ShowScrollableMessage(
                                                    LanguageManager.GetString("Message_InvalidTextFound", token: token),
                                                    LanguageManager.GetString(
                                                        "Message_InvalidTextFound_Title", token: token),
                                                    PromptButtons.YesNo, PromptIcon.Warning) ==
                                                PromptResult.No)
                                            {
                                                return false;
                                            }

                                            blnErrorCaught = true;
                                        }
                                        else
                                        {
                                            if (showWarnings)
                                            {
                                                UserInteraction.ShowScrollableMessage(
                                                    string.Format(GlobalSettings.CultureInfo,
                                                                  LanguageManager.GetString(
                                                                      "Message_FailedLoad", token: token),
                                                                  ex.Message),
                                                    string.Format(GlobalSettings.CultureInfo,
                                                                  LanguageManager.GetString(
                                                                      "MessageTitle_FailedLoad", token: token),
                                                                  ex.Message),
                                                    PromptButtons.OK, PromptIcon.Error);
                                            }

                                            return false;
                                        }
                                    }
                                } while (blnErrorCaught);

                                objXmlCharacter = objXmlDocument.SelectSingleNode("/character");
                                xmlCharacterNavigator =
                                    objXmlDocument.GetFastNavigator().SelectSingleNodeAndCacheExpression("/character", token);
                                return true;
                            }

                            async Task<bool> LoadSaveFileDocumentAsync()
                            {
                                bool blnErrorCaught = false;
                                do
                                {
                                    try
                                    {
                                        token.ThrowIfCancellationRequested();
                                        if (strFileName.EndsWith(".chum5", StringComparison.OrdinalIgnoreCase))
                                            await objXmlDocument.LoadStandardPatientAsync(
                                                strFileName, !blnErrorCaught, token: token).ConfigureAwait(false);
                                        else if (strFileName.EndsWith(".chum5lz", StringComparison.OrdinalIgnoreCase))
                                            await objXmlDocument.LoadStandardFromLzmaCompressedPatientAsync(
                                                strFileName, !blnErrorCaught, token: token).ConfigureAwait(false);
                                        else
                                            throw new InvalidOperationException();
                                        blnErrorCaught = false;
                                    }
                                    catch (XmlException ex)
                                    {
                                        ex = ex.Demystify();
                                        if (ex.Message.HasAnyXmlInvalidUnicodeChars())
                                        {
                                            /*If we found a known control character that's preventing the character from
                                            being loaded (Expected to be notes ingested from PDF mostly) prompt the user whether to use unsafe methods.
                                            If yes, restart the load, explicitly ignoring invalid characters.*/

                                            if (await UserInteraction.ShowScrollableMessageAsync(
                                                    await LanguageManager
                                                        .GetStringAsync("Message_InvalidTextFound", token: token)
                                                        .ConfigureAwait(false),
                                                    await LanguageManager
                                                        .GetStringAsync(
                                                            "Message_InvalidTextFound_Title", token: token)
                                                        .ConfigureAwait(false),
                                                    PromptButtons.YesNo, PromptIcon.Warning, token: token).ConfigureAwait(false) ==
                                                PromptResult.No)
                                            {
                                                return false;
                                            }

                                            blnErrorCaught = true;
                                        }
                                        else
                                        {
                                            if (showWarnings)
                                            {
                                                await UserInteraction.ShowScrollableMessageAsync(
                                                    string.Format(GlobalSettings.CultureInfo,
                                                        await LanguageManager
                                                            .GetStringAsync(
                                                                "Message_FailedLoad", token: token)
                                                            .ConfigureAwait(false),
                                                        ex.Message),
                                                    string.Format(GlobalSettings.CultureInfo,
                                                        await LanguageManager
                                                            .GetStringAsync(
                                                                "MessageTitle_FailedLoad", token: token)
                                                            .ConfigureAwait(false),
                                                        ex.Message),
                                                    PromptButtons.OK, PromptIcon.Error, token: token).ConfigureAwait(false);
                                            }

                                            return false;
                                        }
                                    }
                                } while (blnErrorCaught);

                                objXmlCharacter = objXmlDocument.SelectSingleNode("/character");
                                xmlCharacterNavigator
                                    = (await objXmlDocument.GetFastNavigatorAsync(token).ConfigureAwait(false))
                                            .SelectSingleNodeAndCacheExpression("/character", token);
                                return true;
                            }

                            if (!blnKeepLoading || objXmlCharacter == null || xmlCharacterNavigator == null)
                            {
                                return false;
                            }

                            //Timekeeper.Finish("load_xml");
                        }

                        if (blnSync)
                            IsLoading = true;
                        else
                            await SetIsLoadingAsync(true, token).ConfigureAwait(false);

                        try
                        {
                            token.ThrowIfCancellationRequested();
                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("String_Settings", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager.GetStringAsync("String_Settings",
                                                                    token: token)
                                                                .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_misc", loadActivity))
                            {
                                xmlCharacterNavigator.TryGetBoolFieldQuickly("ignorerules", ref _blnIgnoreRules);
                                xmlCharacterNavigator.TryGetBoolFieldQuickly("created", ref _blnCreated);

                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    ResetCharacter(token);
                                else
                                    await ResetCharacterAsync(token).ConfigureAwait(false);

                                // Get the game edition of the file if possible and make sure it's intended to be used with this version of the application.
                                string strGameEdition = string.Empty;
                                if (xmlCharacterNavigator.TryGetStringFieldQuickly("gameedition",
                                        ref strGameEdition) &&
                                    !string.IsNullOrEmpty(strGameEdition) && strGameEdition != "SR5" &&
                                    showWarnings &&
                                    !Utils.IsUnitTest)
                                {
                                    if (blnSync)
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        UserInteraction.ShowScrollableMessage(
                                            // ReSharper disable once MethodHasAsyncOverload
                                            LanguageManager.GetString(
                                                "Message_IncorrectGameVersion_SR4",
                                                token: token),
                                            // ReSharper disable once MethodHasAsyncOverload
                                            LanguageManager.GetString(
                                                "MessageTitle_IncorrectGameVersion",
                                                token: token),
                                            PromptButtons.YesNo, PromptIcon.Error);
                                    else
                                        await UserInteraction.ShowScrollableMessageAsync(
                                            await LanguageManager.GetStringAsync(
                                                "Message_IncorrectGameVersion_SR4",
                                                token: token).ConfigureAwait(false),
                                            await LanguageManager.GetStringAsync(
                                                "MessageTitle_IncorrectGameVersion",
                                                token: token).ConfigureAwait(false),
                                            PromptButtons.YesNo, PromptIcon.Error,
                                            token: token).ConfigureAwait(false);
                                    return false;
                                }

                                string strVersion = string.Empty;
                                //Check to see if the character was created in a version of Chummer later than the currently installed one.
                                if (xmlCharacterNavigator.TryGetStringFieldQuickly("appversion", ref strVersion) &&
                                    !string.IsNullOrEmpty(strVersion))
                                {
                                    strVersion = strVersion.TrimStartOnce("0.");
                                    // Sweep for saves where the saved version includes a long alphanumeric string after the version (because of Application.ProductVersion weirdness)
                                    int intPlusSignIndex = strVersion.IndexOf('+');
                                    if (intPlusSignIndex >= 0)
                                        strVersion = strVersion.Substring(0, intPlusSignIndex);
                                    if (!ValueVersion.TryParse(strVersion, out _verSavedVersion))
                                    {
                                        _verSavedVersion = Utils.IsUnitTest
                                            ? new ValueVersion(int.MaxValue, int.MaxValue, int.MaxValue)
                                            : new ValueVersion();
                                    }
                                    // Check for typo in Corrupter quality and correct it
                                    else if (_verSavedVersion < new ValueVersion(5, 188, 34) && objXmlDocument.InnerXmlContentContains("Corruptor", token))
                                    {
                                        string strNewXml = objXmlDocument.InnerXmlViaPool(token).Replace("Corruptor", "Corrupter");
                                        if (blnSync)
                                            objXmlDocument.LoadXmlStandard(strNewXml, token: token);
                                        else
                                            await objXmlDocument.LoadXmlStandardAsync(strNewXml, token: token).ConfigureAwait(false);
                                        xmlCharacterNavigator =
                                            (blnSync
                                                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                ? objXmlDocument.GetFastNavigator()
                                                : await objXmlDocument.GetFastNavigatorAsync(token)
                                                    .ConfigureAwait(false)
                                            ).SelectSingleNodeAndCacheExpression("/character", token);
                                        if (xmlCharacterNavigator == null)
                                            return false;
                                    }
                                }
#if !DEBUG
                                if (!Utils.IsUnitTest)
                                {
                                    string strMinimumVersion = string.Empty;
                                    // Check to see if a character has a minimum version set where they will not load properly on anything older
                                    if (xmlCharacterNavigator.TryGetStringFieldQuickly(
                                            "minimumappversion", ref strMinimumVersion)
                                        && !string.IsNullOrEmpty(strMinimumVersion))
                                    {
                                        strMinimumVersion = strMinimumVersion.TrimStartOnce("0.");
                                        if (Version.TryParse(strMinimumVersion, out Version objMinimumVersion)
                                            && objMinimumVersion > Utils.CurrentChummerVersion)
                                        {
                                            UserInteraction.ShowMessage(
                                                string.Format(GlobalSettings.CultureInfo,
                                                              blnSync
                                                                  // ReSharper disable once MethodHasAsyncOverload
                                                                  ? LanguageManager.GetString(
                                                                      "Message_OlderThanChummerSaveMinimumVersion", token: token)
                                                                  : await LanguageManager
                                                                          .GetStringAsync(
                                                                              "Message_OlderThanChummerSaveMinimumVersion",
                                                                              token: token).ConfigureAwait(false),
                                                              objMinimumVersion, Utils.CurrentChummerVersion),
                                                blnSync
                                                    // ReSharper disable once MethodHasAsyncOverload
                                                    ? LanguageManager.GetString(
                                                        "MessageTitle_OlderThanChummerSaveMinimumVersion", token: token)
                                                    : await LanguageManager
                                                            .GetStringAsync(
                                                                "MessageTitle_OlderThanChummerSaveMinimumVersion",
                                                                token: token).ConfigureAwait(false),
                                                PromptButtons.OK, PromptIcon.Error);
                                            return false;
                                        }
                                    }

                                    if (_verSavedVersion > Utils.CurrentChummerVersion && PromptResult.Yes
                                        != UserInteraction.ShowMessage(
                                            string.Format(GlobalSettings.CultureInfo,
                                                          blnSync
                                                              // ReSharper disable once MethodHasAsyncOverload
                                                              ? LanguageManager.GetString("Message_OutdatedChummerSave", token: token)
                                                              : await LanguageManager
                                                                      .GetStringAsync(
                                                                          "Message_OutdatedChummerSave", token: token)
                                                                      .ConfigureAwait(false), _verSavedVersion,
                                                          Utils.CurrentChummerVersion),
                                            blnSync
                                                // ReSharper disable once MethodHasAsyncOverload
                                                ? LanguageManager.GetString("MessageTitle_OutdatedChummerSave", token: token)
                                                : await LanguageManager
                                                        .GetStringAsync("MessageTitle_OutdatedChummerSave",
                                                                        token: token).ConfigureAwait(false),
                                            PromptButtons.YesNo, PromptIcon.Warning))
                                    {
                                        return false;
                                    }
                                }
#endif

                                // Get the name of the settings file in use if possible.
                                xmlCharacterNavigator.TryGetStringFieldQuickly("settings", ref _strSettingsKey);

                                int intSettingsHashCode = 0;
                                bool blnHashCodeSuccess
                                    = xmlCharacterNavigator.TryGetInt32FieldQuickly(
                                        "settingshashcode", ref intSettingsHashCode);

                                bool blnSuccess;

                                // Load the character's settings file.
                                string strDummy = string.Empty;
                                if (!xmlCharacterNavigator.TryGetStringFieldQuickly("buildmethod", ref strDummy)
                                    || !Enum.TryParse(strDummy, true, out CharacterBuildMethod eSavedBuildMethod))
                                {
                                    blnSuccess =
                                        (blnSync
                                            ? SettingsManager.LoadedCharacterSettings
                                            : await SettingsManager.GetLoadedCharacterSettingsAsync(token)
                                                .ConfigureAwait(false)).TryGetValue(GlobalSettings.DefaultCharacterSettingDefaultValue,
                                            out CharacterSettings objSettings);
                                    eSavedBuildMethod = blnSuccess
                                        ? objSettings.BuildMethod
                                        : CharacterBuildMethod.Priority;
                                }

                                blnSuccess =
                                    (blnSync
                                        ? SettingsManager.LoadedCharacterSettings
                                        : await SettingsManager.GetLoadedCharacterSettingsAsync(token)
                                            .ConfigureAwait(false)).TryGetValue(GlobalSettings.DefaultCharacterSetting,
                                        out CharacterSettings objDefaultSettings);
                                if (!blnSuccess)
                                {
                                    blnSuccess =
                                        (blnSync
                                            ? SettingsManager.LoadedCharacterSettings
                                            : await SettingsManager.GetLoadedCharacterSettingsAsync(token)
                                                .ConfigureAwait(false)).TryGetValue(GlobalSettings.DefaultCharacterSettingDefaultValue,
                                            out objDefaultSettings);
                                    if (!blnSuccess)
                                    {
                                        objDefaultSettings = blnSync
                                            ? SettingsManager.LoadedCharacterSettings.First().Value
                                            : (await SettingsManager.GetLoadedCharacterSettingsAsync(token)
                                                                    .ConfigureAwait(false)).First().Value;
                                    }
                                }

                                CharacterSettings objProspectiveSettings;
                                bool blnShowSelectBP = false;
                                using (new FetchSafelyFromSafeObjectPool<HashSet<string>>(Utils.StringHashSetPool,
                                           out HashSet<string> setSavedBooks))
                                {
                                    foreach (XPathNavigator xmlBook in xmlCharacterNavigator.SelectAndCacheExpression(
                                                 "sources/source", token))
                                    {
                                        if (!string.IsNullOrEmpty(xmlBook.Value))
                                            setSavedBooks.Add(xmlBook.Value);
                                    }

                                    if (setSavedBooks.Count == 0)
                                        setSavedBooks.AddRange(objDefaultSettings.Books);

                                    XPathNodeIterator xmlCustomDirectoryNames = xmlCharacterNavigator.SelectAndCacheExpression(
                                        "customdatadirectorynames/directoryname", token);
                                    List<string> lstSavedCustomDataDirectoryNames
                                        = new List<string>(xmlCustomDirectoryNames.Count);
                                    foreach (XPathNavigator xmlCustomDataDirectoryName in xmlCustomDirectoryNames)
                                    {
                                        if (!string.IsNullOrEmpty(xmlCustomDataDirectoryName.Value))
                                            lstSavedCustomDataDirectoryNames.Add(xmlCustomDataDirectoryName.Value);
                                    }

                                    decimal decLegacyMaxNuyen = objDefaultSettings.NuyenMaximumBP;
                                    xmlCharacterNavigator.TryGetDecFieldQuickly("maxnuyen", ref decLegacyMaxNuyen);
                                    int intLegacyMaxKarma = objDefaultSettings.BuildKarma;
                                    xmlCharacterNavigator.TryGetInt32FieldQuickly("maxkarma",
                                        ref intLegacyMaxKarma);

                                    // Calculate a score for a character option that roughly coincides with how suitable it is as a
                                    // replacement for the current one the character save contains Settings with a negative score
                                    // should not be considered suitable at all
                                    int CalculateCharacterSettingsMatchScore(CharacterSettings objOptionsToCheck)
                                    {
                                        int intReturn = int.MaxValue;

                                        int intBaseline = objOptionsToCheck.BuiltInOption ? 5 : 4;

                                        if (Created && eSavedBuildMethod != CharacterBuildMethod.LifeModule)
                                        {
                                            if (objOptionsToCheck.BuildMethod != eSavedBuildMethod)
                                            {
                                                if (objOptionsToCheck.BuildMethod.UsesPriorityTables() ==
                                                    eSavedBuildMethod.UsesPriorityTables())
                                                    intReturn -= 2;
                                                else
                                                    intReturn -= 4;
                                            }
                                            if (intLegacyMaxKarma != objOptionsToCheck.BuildKarma)
                                                intReturn -= Math.Min(Math.Abs(intLegacyMaxKarma - objOptionsToCheck.BuildKarma), 2);
                                            if (decLegacyMaxNuyen != objOptionsToCheck.NuyenMaximumBP)
                                                intReturn -= Math.Min(Math.Abs(decLegacyMaxNuyen - objOptionsToCheck.NuyenMaximumBP).StandardRound(), 2);
                                        }
                                        else
                                        {
                                            if (objOptionsToCheck.BuildMethod != eSavedBuildMethod)
                                            {
                                                if (objOptionsToCheck.BuildMethod.UsesPriorityTables() ==
                                                    eSavedBuildMethod.UsesPriorityTables())
                                                {
                                                    intBaseline += 2;
                                                    intReturn -= int.MaxValue / 2;
                                                }
                                                else
                                                {
                                                    intBaseline += 4;
                                                    intReturn -= int.MaxValue;
                                                }
                                            }
                                            intReturn -= ((intLegacyMaxKarma - objOptionsToCheck.BuildKarma)
                                                           .Pow(2)
                                                           + (decLegacyMaxNuyen - objOptionsToCheck.NuyenMaximumBP)
                                                           .Pow(2))
                                                          .FastSqrtAndStandardRound();
                                        }

                                        int intBaselineCustomDataCount
                                            = objOptionsToCheck.EnabledCustomDataDirectoryInfos.Count;
                                        if (intBaselineCustomDataCount == 0)
                                        {
                                            intBaselineCustomDataCount = lstSavedCustomDataDirectoryNames.Count;
                                            if (intBaselineCustomDataCount > 0)
                                            {
                                                intReturn -= intBaselineCustomDataCount.Pow(2) * intBaseline;
                                            }
                                        }
                                        else if (lstSavedCustomDataDirectoryNames.Count == 0)
                                        {
                                            intReturn -= intBaselineCustomDataCount.Pow(2) * intBaseline;
                                        }
                                        else
                                        {
                                            intBaselineCustomDataCount
                                                = Math.Max(lstSavedCustomDataDirectoryNames.Count,
                                                           intBaselineCustomDataCount);
                                            for (int i = 0;
                                                 i < objOptionsToCheck.EnabledCustomDataDirectoryInfos.Count;
                                                 ++i)
                                            {
                                                string strLoopCustomDataName =
                                                    objOptionsToCheck.EnabledCustomDataDirectoryInfos[i].Name;
                                                int intLoopIndex =
                                                    lstSavedCustomDataDirectoryNames.IndexOf(strLoopCustomDataName);
                                                if (intLoopIndex < 0)
                                                    intReturn -= intBaselineCustomDataCount * intBaseline;
                                                else
                                                    intReturn -= Math.Abs(i - intLoopIndex) * intBaseline;
                                            }

                                            int intMismatchCount = lstSavedCustomDataDirectoryNames.Count(x =>
                                                objOptionsToCheck.EnabledCustomDataDirectoryInfos.All(
                                                    y => y.Name != x));
                                            if (intMismatchCount != 0)
                                                intReturn -= intMismatchCount * intBaselineCustomDataCount *
                                                             intBaseline;
                                        }

                                        using (new FetchSafelyFromSafeObjectPool<HashSet<string>>(
                                                   Utils.StringHashSetPool, out HashSet<string> setDummyBooks))
                                        {
                                            setDummyBooks.AddRange(setSavedBooks);
                                            int intExtraBooks = objOptionsToCheck.Books.Count(x => !setDummyBooks.Remove(x));
                                            setDummyBooks.ExceptWith(objOptionsToCheck.Books);
                                            // Missing books are weighted a lot more heavily than extra books
                                            intReturn -= (setDummyBooks.Count * (intBaselineCustomDataCount + byte.MaxValue)
                                                          + intExtraBooks) * intBaseline;
                                        }

                                        return intReturn;
                                    }
                                    // Calculate a score for a character option that roughly coincides with how suitable it is as a
                                    // replacement for the current one the character save contains Settings with a negative score
                                    // should not be considered suitable at all
                                    async Task<int> CalculateCharacterSettingsMatchScoreAsync(CharacterSettings objOptionsToCheck)
                                    {
                                        int intReturn = int.MaxValue;

                                        int intBaseline = objOptionsToCheck.BuiltInOption ? 5 : 4;

                                        if (Created && eSavedBuildMethod != CharacterBuildMethod.LifeModule)
                                        {
                                            if (objOptionsToCheck.BuildMethod != eSavedBuildMethod)
                                            {
                                                if (objOptionsToCheck.BuildMethod.UsesPriorityTables() ==
                                                    eSavedBuildMethod.UsesPriorityTables())
                                                    intReturn -= 2;
                                                else
                                                    intReturn -= 4;
                                            }
                                            if (intLegacyMaxKarma != objOptionsToCheck.BuildKarma)
                                                intReturn -= Math.Min(Math.Abs(intLegacyMaxKarma - objOptionsToCheck.BuildKarma), 2);
                                            if (decLegacyMaxNuyen != objOptionsToCheck.NuyenMaximumBP)
                                                intReturn -= Math.Min(Math.Abs(decLegacyMaxNuyen - objOptionsToCheck.NuyenMaximumBP).StandardRound(), 2);
                                        }
                                        else
                                        {
                                            if (objOptionsToCheck.BuildMethod != eSavedBuildMethod)
                                            {
                                                if (objOptionsToCheck.BuildMethod.UsesPriorityTables() ==
                                                    eSavedBuildMethod.UsesPriorityTables())
                                                {
                                                    intBaseline += 2;
                                                    intReturn -= int.MaxValue / 2;
                                                }
                                                else
                                                {
                                                    intBaseline += 4;
                                                    intReturn -= int.MaxValue;
                                                }
                                            }
                                            intReturn -= ((intLegacyMaxKarma - objOptionsToCheck.BuildKarma)
                                                           .Pow(2)
                                                           + (decLegacyMaxNuyen - objOptionsToCheck.NuyenMaximumBP)
                                                           .Pow(2))
                                                          .FastSqrtAndStandardRound();
                                        }

                                        IReadOnlyList<CustomDataDirectoryInfo> lstOtherEnabledCustomDataDirectoryInfos
                                            = await objOptionsToCheck
                                                    .GetEnabledCustomDataDirectoryInfosAsync(token)
                                                    .ConfigureAwait(false);
                                        int intBaselineCustomDataCount = lstOtherEnabledCustomDataDirectoryInfos.Count;
                                        if (intBaselineCustomDataCount == 0)
                                        {
                                            intBaselineCustomDataCount = lstSavedCustomDataDirectoryNames.Count;
                                            if (intBaselineCustomDataCount > 0)
                                            {
                                                intReturn -= intBaselineCustomDataCount.Pow(2) * intBaseline;
                                            }
                                        }
                                        else if (lstSavedCustomDataDirectoryNames.Count == 0)
                                        {
                                            intReturn -= intBaselineCustomDataCount.Pow(2) * intBaseline;
                                        }
                                        else
                                        {
                                            intBaselineCustomDataCount
                                                = Math.Max(lstSavedCustomDataDirectoryNames.Count,
                                                           intBaselineCustomDataCount);
                                            for (int i = 0;
                                                 i < lstOtherEnabledCustomDataDirectoryInfos.Count;
                                                 ++i)
                                            {
                                                string strLoopCustomDataName =
                                                    lstOtherEnabledCustomDataDirectoryInfos[i].Name;
                                                int intLoopIndex =
                                                    lstSavedCustomDataDirectoryNames.IndexOf(strLoopCustomDataName);
                                                if (intLoopIndex < 0)
                                                    intReturn -= intBaselineCustomDataCount * intBaseline;
                                                else
                                                    intReturn -= Math.Abs(i - intLoopIndex) * intBaseline;
                                            }

                                            int intMismatchCount = lstSavedCustomDataDirectoryNames.Count(x =>
                                                objOptionsToCheck.EnabledCustomDataDirectoryInfos.All(
                                                    y => y.Name != x));
                                            if (intMismatchCount != 0)
                                                intReturn -= intMismatchCount * intBaselineCustomDataCount *
                                                             intBaseline;
                                        }

                                        using (new FetchSafelyFromSafeObjectPool<HashSet<string>>(
                                                   Utils.StringHashSetPool, out HashSet<string> setDummyBooks))
                                        {
                                            setDummyBooks.AddRange(setSavedBooks);
                                            IReadOnlyCollection<string> setOtherBooks
                                                = await objOptionsToCheck.GetBooksAsync(token).ConfigureAwait(false);
                                            int intExtraBooks = setOtherBooks.Count(x => !setDummyBooks.Remove(x));
                                            setDummyBooks.ExceptWith(setOtherBooks);
                                            // Missing books are weighted a lot more heavily than extra books
                                            intReturn -= (setDummyBooks.Count * (intBaselineCustomDataCount + byte.MaxValue)
                                                          + intExtraBooks) * intBaseline;
                                        }

                                        return intReturn;
                                    }

                                    blnSuccess =
                                        (blnSync
                                            ? SettingsManager.LoadedCharacterSettings
                                            : await SettingsManager.GetLoadedCharacterSettingsAsync(token)
                                                .ConfigureAwait(false)).TryGetValue(_strSettingsKey,
                                            out objProspectiveSettings);

                                    if (!blnSuccess && blnHashCodeSuccess)
                                    {
                                        CharacterSettings objHashCodeMatchSettings
                                            = blnSync
                                                ? SettingsManager.LoadedCharacterSettings.FirstOrDefault(
                                                    x => x.Value.GetEquatableHashCode(token) == intSettingsHashCode).Value
                                                : (await (await SettingsManager
                                                                .GetLoadedCharacterSettingsAsync(token)
                                                                .ConfigureAwait(false))
                                                         .FirstOrDefaultAsync(
                                                             async x => await x.Value.GetEquatableHashCodeAsync(token)
                                                                               .ConfigureAwait(false)
                                                                        == intSettingsHashCode, token)
                                                         .ConfigureAwait(false)).Value;
                                        if (objHashCodeMatchSettings?.BuildMethod == eSavedBuildMethod)
                                        {
                                            blnSuccess = true;
                                            objProspectiveSettings = objHashCodeMatchSettings;
                                        }
                                    }

                                    if (!blnSuccess)
                                    {
                                        // Prompt if we want to switch options or leave
                                        if (!Utils.IsUnitTest && showWarnings)
                                        {
                                            if ((blnSync
                                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                    ? UserInteraction.ShowScrollableMessage(
                                                        string.Format(
                                                            GlobalSettings.CultureInfo,
                                                            // ReSharper disable once MethodHasAsyncOverload
                                                            LanguageManager.GetString(
                                                                "Message_CharacterOptions_CannotLoadSetting",
                                                                token: token),
                                                            Path.GetFileNameWithoutExtension(_strSettingsKey)),
                                                        // ReSharper disable once MethodHasAsyncOverload
                                                        LanguageManager.GetString(
                                                            "MessageTitle_CharacterOptions_CannotLoadSetting",
                                                            token: token),
                                                        PromptButtons.YesNo, PromptIcon.Error)
                                                    : await UserInteraction.ShowScrollableMessageAsync(
                                                        string.Format(
                                                            GlobalSettings.CultureInfo,
                                                            await LanguageManager.GetStringAsync(
                                                                "Message_CharacterOptions_CannotLoadSetting",
                                                                token: token).ConfigureAwait(false),
                                                            Path.GetFileNameWithoutExtension(_strSettingsKey)),
                                                        await LanguageManager.GetStringAsync(
                                                            "MessageTitle_CharacterOptions_CannotLoadSetting",
                                                            token: token).ConfigureAwait(false),
                                                        PromptButtons.YesNo, PromptIcon.Error,
                                                        token: token).ConfigureAwait(false)) == PromptResult.No)
                                            {
                                                return false;
                                            }

                                            blnShowSelectBP = true;
                                        }

                                        // Set up interim options for selection by build method
                                        string strReplacementSettingsKey = string.Empty;
                                        int intMostSuitable = int.MinValue;
                                        foreach (KeyValuePair<string, CharacterSettings> kvpLoopOptions in
                                                 SettingsManager.LoadedCharacterSettings)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            int intLoopScore
                                                = blnSync
                                                    ? CalculateCharacterSettingsMatchScore(kvpLoopOptions.Value)
                                                    : await CalculateCharacterSettingsMatchScoreAsync(kvpLoopOptions
                                                        .Value).ConfigureAwait(false);
                                            if (intLoopScore > intMostSuitable)
                                            {
                                                intMostSuitable = intLoopScore;
                                                strReplacementSettingsKey = kvpLoopOptions.Key;
                                            }
                                        }

                                        if (string.IsNullOrEmpty(strReplacementSettingsKey))
                                            blnSuccess = false;
                                        else
                                            blnSuccess =
                                                (blnSync
                                                    ? SettingsManager.LoadedCharacterSettings
                                                    : await SettingsManager.GetLoadedCharacterSettingsAsync(token)
                                                        .ConfigureAwait(false)).TryGetValue(strReplacementSettingsKey,
                                                    out objProspectiveSettings);

                                        if (!blnSuccess)
                                        {
                                            strReplacementSettingsKey
                                                = GlobalSettings.DefaultCharacterSettingDefaultValue;
                                            blnSuccess = (blnSync
                                                ? SettingsManager.LoadedCharacterSettings
                                                : await SettingsManager.GetLoadedCharacterSettingsAsync(token)
                                                    .ConfigureAwait(false)).TryGetValue(
                                                strReplacementSettingsKey, out objProspectiveSettings);
                                            if (!blnSuccess)
                                            {
                                                objProspectiveSettings
                                                    = (blnSync
                                                        ? SettingsManager.LoadedCharacterSettings
                                                        : await SettingsManager.GetLoadedCharacterSettingsAsync(token)
                                                            .ConfigureAwait(false)).FirstOrDefault().Value;
                                                strReplacementSettingsKey = blnSync
                                                    ? objProspectiveSettings.DictionaryKey
                                                    : await objProspectiveSettings.GetDictionaryKeyAsync(token).ConfigureAwait(false);
                                            }
                                        }

                                        _strSettingsKey = strReplacementSettingsKey;
                                        if (blnSync)
                                            LoadAsDirty = true;
                                        else
                                            await SetLoadAsDirtyAsync(true, token).ConfigureAwait(false);
                                    }
                                    else if (!(blnSync ? Created : await GetCreatedAsync(token).ConfigureAwait(false)) && objProspectiveSettings.BuildMethod != eSavedBuildMethod)
                                    {
                                        // Prompt if we want to switch options or leave
                                        if (!Utils.IsUnitTest && showWarnings)
                                        {
                                            if ((blnSync
                                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                    ? UserInteraction.ShowScrollableMessage(
                                                        string.Format(
                                                            GlobalSettings.CultureInfo,
                                                            // ReSharper disable once MethodHasAsyncOverload
                                                            LanguageManager.GetString(
                                                                "Message_CharacterOptions_DesyncBuildMethod",
                                                                token: token),
                                                            Path.GetFileNameWithoutExtension(_strSettingsKey),
                                                            // ReSharper disable once MethodHasAsyncOverload
                                                            LanguageManager.GetString(
                                                                "String_" + objProspectiveSettings
                                                                    .BuildMethod, token: token),
                                                            // ReSharper disable once MethodHasAsyncOverload
                                                            LanguageManager.GetString(
                                                                "String_" + eSavedBuildMethod, token: token)),
                                                        // ReSharper disable once MethodHasAsyncOverload
                                                        LanguageManager.GetString(
                                                            "MessageTitle_CharacterOptions_DesyncBuildMethod",
                                                            token: token),
                                                        PromptButtons.YesNo, PromptIcon.Error)
                                                    : await UserInteraction.ShowScrollableMessageAsync(
                                                        string.Format(
                                                            GlobalSettings.CultureInfo,
                                                            await LanguageManager.GetStringAsync(
                                                                "Message_CharacterOptions_DesyncBuildMethod",
                                                                token: token).ConfigureAwait(false),
                                                            Path.GetFileNameWithoutExtension(_strSettingsKey),
                                                            await LanguageManager.GetStringAsync(
                                                                    "String_" + objProspectiveSettings
                                                                        .BuildMethod, token: token)
                                                                .ConfigureAwait(false),
                                                            await LanguageManager.GetStringAsync(
                                                                    "String_" + eSavedBuildMethod,
                                                                    token: token)
                                                                .ConfigureAwait(false)),
                                                        await LanguageManager.GetStringAsync(
                                                            "MessageTitle_CharacterOptions_DesyncBuildMethod",
                                                            token: token).ConfigureAwait(false),
                                                        PromptButtons.YesNo, PromptIcon.Error,
                                                        token: token).ConfigureAwait(false)) == PromptResult.No)
                                            {
                                                return false;
                                            }

                                            blnShowSelectBP = true;
                                        }

                                        // Set up interim options for selection by build method
                                        string strReplacementSettingsKey = string.Empty;
                                        int intMostSuitable = int.MinValue;
                                        foreach (KeyValuePair<string, CharacterSettings> kvpLoopOptions in SettingsManager.LoadedCharacterSettings)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            int intLoopScore
                                                = blnSync
                                                    ? CalculateCharacterSettingsMatchScore(kvpLoopOptions.Value)
                                                    : await CalculateCharacterSettingsMatchScoreAsync(kvpLoopOptions
                                                        .Value).ConfigureAwait(false);
                                            if (intLoopScore > intMostSuitable)
                                            {
                                                intMostSuitable = intLoopScore;
                                                strReplacementSettingsKey = kvpLoopOptions.Key;
                                            }
                                        }

                                        if (string.IsNullOrEmpty(strReplacementSettingsKey))
                                            blnSuccess = false;
                                        else
                                            blnSuccess = (blnSync
                                                ? SettingsManager.LoadedCharacterSettings
                                                : await SettingsManager.GetLoadedCharacterSettingsAsync(token)
                                                    .ConfigureAwait(false)).TryGetValue(
                                                strReplacementSettingsKey, out objProspectiveSettings);

                                        if (!blnSuccess)
                                        {
                                            strReplacementSettingsKey
                                                = GlobalSettings.DefaultCharacterSettingDefaultValue;
                                            blnSuccess = (blnSync
                                                ? SettingsManager.LoadedCharacterSettings
                                                : await SettingsManager.GetLoadedCharacterSettingsAsync(token)
                                                    .ConfigureAwait(false)).TryGetValue(
                                                strReplacementSettingsKey, out objProspectiveSettings);
                                            if (!blnSuccess)
                                            {
                                                objProspectiveSettings
                                                    = (blnSync
                                                        ? SettingsManager.LoadedCharacterSettings
                                                        : await SettingsManager.GetLoadedCharacterSettingsAsync(token)
                                                            .ConfigureAwait(false)).FirstOrDefault().Value;
                                                strReplacementSettingsKey = blnSync
                                                    ? objProspectiveSettings.DictionaryKey
                                                    : await objProspectiveSettings.GetDictionaryKeyAsync(token).ConfigureAwait(false);
                                            }
                                        }

                                        _strSettingsKey = strReplacementSettingsKey;
                                        if (blnSync)
                                            LoadAsDirty = true;
                                        else
                                            await SetLoadAsDirtyAsync(true, token).ConfigureAwait(false);
                                    }
                                    else if (!Utils.IsUnitTest && showWarnings)
                                    {
                                        // Legacy load stuff
                                        if (setSavedBooks.Count > 0 || lstSavedCustomDataDirectoryNames.Count > 0)
                                        {
                                            // More books is fine, so just test if the stored book list is a subset of the current option's book list
                                            bool blnPromptConfirmSetting =
                                                !setSavedBooks.IsSubsetOf(objProspectiveSettings.Books);
                                            if (!blnPromptConfirmSetting)
                                            {
                                                IReadOnlyList<CustomDataDirectoryInfo> lstProspectiveInfos
                                                    = blnSync
                                                        ? objProspectiveSettings.EnabledCustomDataDirectoryInfos
                                                        : await objProspectiveSettings
                                                            .GetEnabledCustomDataDirectoryInfosAsync(token).ConfigureAwait(false);
                                                // More custom data directories is not fine because additional ones might apply rules that weren't present before, so prompt
                                                blnPromptConfirmSetting = lstSavedCustomDataDirectoryNames.Count !=
                                                                          lstProspectiveInfos.Count;
                                                if (!blnPromptConfirmSetting)
                                                {
                                                    // Check to make sure all the names are the same
                                                    for (int i = 0; i < lstSavedCustomDataDirectoryNames.Count; ++i)
                                                    {
                                                        if (lstSavedCustomDataDirectoryNames[i]
                                                            != lstProspectiveInfos[i].Name)
                                                        {
                                                            blnPromptConfirmSetting = true;
                                                            break;
                                                        }
                                                    }
                                                }
                                            }

                                            if (blnPromptConfirmSetting)
                                            {
                                                PromptResult eShowBPResult = blnSync
                                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                    ? UserInteraction.ShowScrollableMessage(
                                                        string.Format(
                                                            GlobalSettings.CultureInfo,
                                                            // ReSharper disable once MethodHasAsyncOverload
                                                            LanguageManager.GetString(
                                                                "Message_CharacterOptions_DesyncBooksOrCustomData",
                                                                token: token),
                                                            objProspectiveSettings.Name),
                                                        // ReSharper disable once MethodHasAsyncOverload
                                                        LanguageManager.GetString(
                                                            "MessageTitle_CharacterOptions_DesyncBooksOrCustomData",
                                                            token: token),
                                                        PromptButtons.YesNoCancel, PromptIcon.Warning)
                                                    : await UserInteraction.ShowScrollableMessageAsync(
                                                        string.Format(
                                                            GlobalSettings.CultureInfo,
                                                            await LanguageManager.GetStringAsync(
                                                                "Message_CharacterOptions_DesyncBooksOrCustomData",
                                                                token: token).ConfigureAwait(false),
                                                            objProspectiveSettings.Name),
                                                        await LanguageManager.GetStringAsync(
                                                            "MessageTitle_CharacterOptions_DesyncBooksOrCustomData",
                                                            token: token).ConfigureAwait(false),
                                                        PromptButtons.YesNoCancel, PromptIcon.Warning,
                                                        token: token).ConfigureAwait(false);
                                                if (eShowBPResult == PromptResult.Cancel)
                                                {
                                                    return false;
                                                }

                                                blnShowSelectBP = eShowBPResult == PromptResult.Yes;
                                            }
                                        }
                                        else if (blnHashCodeSuccess
                                                 && !objProspectiveSettings.BuiltInOption
                                                 // Need to make sure that the save was made in the same version of Chummer, otherwise we can get a hash code mismatch from settings themselves changing
                                                 && LastSavedVersion == Utils.CurrentChummerVersion
                                                 && (blnSync
                                                     // ReSharper disable once MethodHasAsyncOverload
                                                     ? objProspectiveSettings.GetEquatableHashCode(token)
                                                     : await objProspectiveSettings.GetEquatableHashCodeAsync(
                                                         token).ConfigureAwait(false))
                                                 != intSettingsHashCode)
                                        {
                                            PromptResult eShowBPResult = blnSync
                                                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                ? UserInteraction.ShowScrollableMessage(
                                                    string.Format(
                                                        GlobalSettings.CultureInfo,
                                                        // ReSharper disable once MethodHasAsyncOverload
                                                        LanguageManager.GetString(
                                                            "Message_CharacterOptions_DesyncFromHashCode",
                                                            token: token),
                                                        objProspectiveSettings.Name),
                                                    // ReSharper disable once MethodHasAsyncOverload
                                                    LanguageManager.GetString(
                                                        "MessageTitle_CharacterOptions_DesyncFromHashCode",
                                                        token: token),
                                                    PromptButtons.YesNoCancel, PromptIcon.Warning)
                                                : await UserInteraction.ShowScrollableMessageAsync(
                                                    string.Format(
                                                        GlobalSettings.CultureInfo,
                                                        await LanguageManager.GetStringAsync(
                                                            "Message_CharacterOptions_DesyncFromHashCode",
                                                            token: token).ConfigureAwait(false),
                                                        objProspectiveSettings.Name),
                                                    await LanguageManager.GetStringAsync(
                                                        "MessageTitle_CharacterOptions_DesyncFromHashCode",
                                                        token: token).ConfigureAwait(false),
                                                    PromptButtons.YesNoCancel, PromptIcon.Warning,
                                                    token: token).ConfigureAwait(false);
                                            if (eShowBPResult == PromptResult.Cancel)
                                            {
                                                return false;
                                            }

                                            blnShowSelectBP = eShowBPResult == PromptResult.Yes;
                                        }
                                    }
                                }

                                if (blnSync)
                                    Settings = objProspectiveSettings;
                                else
                                    await SetSettingsAsync(objProspectiveSettings, token).ConfigureAwait(false);

                                if (blnShowSelectBP)
                                {
                                    if (blnSync)
                                    {
                                        LoadAsDirty = true;
                                        // ReSharper disable once MethodHasAsyncOverload
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        using (ThreadSafeForm<SelectBuildMethod> frmPickBP
                                               = ThreadSafeForm<SelectBuildMethod>.Get(
                                                   () => new SelectBuildMethod(this, true)))
                                        {
                                            // ReSharper disable once MethodHasAsyncOverload
                                            if (frmPickBP.ShowDialogSafe(this, token) != DialogResult.OK)
                                            {
                                                return false;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        await SetLoadAsDirtyAsync(true, token).ConfigureAwait(false);
                                        using (ThreadSafeForm<SelectBuildMethod> frmPickBP
                                               = await ThreadSafeForm<SelectBuildMethod>
                                                       .GetAsync(() => new SelectBuildMethod(this, true), token)
                                                       .ConfigureAwait(false))
                                        {
                                            if (await frmPickBP.ShowDialogSafeAsync(this, token).ConfigureAwait(false)
                                                != DialogResult.OK)
                                            {
                                                return false;
                                            }
                                        }
                                    }
                                }

                                if (xmlCharacterNavigator.TryGetDecFieldQuickly("essenceatspecialstart",
                                        ref _decEssenceAtSpecialStart) &&
                                    _decEssenceAtSpecialStart > ESS.MetatypeMaximum)
                                {
                                    // fix to work around a mistake made when saving decimal values in previous versions.
                                    _decEssenceAtSpecialStart /= 10;
                                }

                                xmlCharacterNavigator.TryGetStringFieldQuickly("createdversion",
                                                                               ref _strVersionCreated);

                                // Metatype information.
                                xmlCharacterNavigator.TryGetBoolFieldQuickly("iscritter", ref _blnIsCritter);
                                xmlCharacterNavigator.TryGetStringFieldQuickly("metatype", ref _strMetatype);
                                if (!xmlCharacterNavigator.TryGetGuidFieldQuickly("metatypeid", ref _guiMetatype))
                                {
                                    // ReSharper disable once MethodHasAsyncOverload
                                    XPathNavigator objMetatypeNode = blnSync
                                        ? GetNodeXPath(true, token: token)
                                        : await GetNodeXPathAsync(true, token: token).ConfigureAwait(false);
                                    if (objMetatypeNode == null || !Guid.TryParse(
                                            objMetatypeNode.SelectSingleNodeAndCacheExpression("id", token)?.Value, out _guiMetatype))
                                    {
                                        return false;
                                    }
                                }

                                xmlCharacterNavigator.TryGetStringFieldQuickly("movement", ref _strMovement);

                                xmlCharacterNavigator.TryGetStringFieldQuickly("walk", ref _strWalk);
                                xmlCharacterNavigator.TryGetStringFieldQuickly("run", ref _strRun);
                                xmlCharacterNavigator.TryGetStringFieldQuickly("sprint", ref _strSprint);

                                _strRunAlt = xmlCharacterNavigator.SelectSingleNodeAndCacheExpression("run/@alt", token)
                                                 ?.Value ??
                                             string.Empty;
                                _strWalkAlt = xmlCharacterNavigator.SelectSingleNodeAndCacheExpression("walk/@alt", token)
                                                  ?.Value ??
                                              string.Empty;
                                _strSprintAlt = xmlCharacterNavigator
                                                    .SelectSingleNodeAndCacheExpression("sprint/@alt", token)?.Value ??
                                                string.Empty;

                                xmlCharacterNavigator.TryGetInt32FieldQuickly("initiativedice",
                                                                              ref _intInitiativeDice);
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("metatypebp", ref _intMetatypeBP);
                                xmlCharacterNavigator.TryGetStringFieldQuickly("metavariant", ref _strMetavariant);
                                //Shim for characters created prior to Run Faster Errata
                                if (_strMetavariant == "Cyclopean")
                                    _strMetavariant = "Cyclops";
                                // Legacy shim for characters with no metavariant still saved with "None" as their metavariant
                                else if (_strMetavariant == "None")
                                    _strMetavariant = string.Empty;

                                //Shim for metavariants that were saved with an incorrect metatype string.
                                if (!string.IsNullOrEmpty(_strMetavariant) && _strMetatype == _strMetavariant)
                                {
                                    // ReSharper disable once MethodHasAsyncOverload
                                    _strMetatype = (blnSync
                                            ? GetNodeXPath(true, token: token)
                                            : await GetNodeXPathAsync(true, token: token).ConfigureAwait(false))
                                        ?.SelectSingleNodeAndCacheExpression("name", token)?.Value ?? "Human";
                                }

                                if (string.IsNullOrEmpty(_strMetavariant))
                                {
                                    _guiMetavariant = Guid.Empty;
                                }
                                else if (!xmlCharacterNavigator.TryGetGuidFieldQuickly("metavariantid",
                                        ref _guiMetavariant))
                                {
                                    XPathNavigator objMetavariantNode = blnSync
                                        // ReSharper disable once MethodHasAsyncOverload
                                        ? this.GetNodeXPath(token: token)
                                        : await this.GetNodeXPathAsync(token: token).ConfigureAwait(false);
                                    if (!Guid.TryParse(
                                            objMetavariantNode?.SelectSingleNodeAndCacheExpression("id", token)?.Value,
                                            out _guiMetavariant))
                                        _guiMetavariant = Guid.Empty;
                                }
                                // Empty metavariant GUID takes precedence over non-empty metavariant name
                                else if (_guiMetavariant == Guid.Empty)
                                {
                                    _strMetavariant = string.Empty;
                                }

                                bool blnDoSourceFetch =
                                    !xmlCharacterNavigator.TryGetStringFieldQuickly("source", ref _strSource) ||
                                    string.IsNullOrEmpty(_strSource);
                                // ReSharper disable once ConvertIfToOrExpression
                                if (!xmlCharacterNavigator.TryGetStringFieldQuickly("page", ref _strPage) ||
                                    string.IsNullOrEmpty(_strPage) || _strPage == "0")
                                    blnDoSourceFetch = true;
                                if (blnDoSourceFetch)
                                {
                                    XPathNavigator xmlCharNode
                                        = blnSync
                                            // ReSharper disable once MethodHasAsyncOverload
                                            ? this.GetNodeXPath(token: token)
                                            : await this.GetNodeXPathAsync(token: token).ConfigureAwait(false);
                                    if (xmlCharNode != null)
                                    {
                                        _strSource = xmlCharNode.SelectSingleNodeAndCacheExpression("source", token)?.Value
                                                     ?? _strSource;
                                        _strPage = xmlCharNode.SelectSingleNodeAndCacheExpression("page", token)?.Value
                                                   ?? _strPage;
                                    }
                                }

                                xmlCharacterNavigator.TryGetStringFieldQuickly("metatypecategory",
                                                                               ref _strMetatypeCategory);

                                // General character information.
                                xmlCharacterNavigator.TryGetStringFieldQuickly("name", ref _strName);
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    LoadMugshots(xmlCharacterNavigator, token);
                                else
                                    await LoadMugshotsAsync(xmlCharacterNavigator, token).ConfigureAwait(false);
                                if (!xmlCharacterNavigator.TryGetStringFieldQuickly("gender", ref _strGender))
                                    xmlCharacterNavigator.TryGetStringFieldQuickly("sex", ref _strGender);
                                xmlCharacterNavigator.TryGetStringFieldQuickly("age", ref _strAge);
                                xmlCharacterNavigator.TryGetStringFieldQuickly("eyes", ref _strEyes);
                                xmlCharacterNavigator.TryGetStringFieldQuickly("height", ref _strHeight);
                                xmlCharacterNavigator.TryGetStringFieldQuickly("weight", ref _strWeight);
                                xmlCharacterNavigator.TryGetStringFieldQuickly("skin", ref _strSkin);
                                xmlCharacterNavigator.TryGetStringFieldQuickly("hair", ref _strHair);
                                xmlCharacterNavigator.TryGetMultiLineStringFieldQuickly("description",
                                    ref _strDescription);
                                xmlCharacterNavigator.TryGetMultiLineStringFieldQuickly("background",
                                    ref _strBackground);
                                xmlCharacterNavigator.TryGetMultiLineStringFieldQuickly("concept", ref _strConcept);
                                xmlCharacterNavigator.TryGetMultiLineStringFieldQuickly("notes", ref _strNotes);
                                xmlCharacterNavigator.TryGetStringFieldQuickly("alias", ref _strAlias);
                                xmlCharacterNavigator.TryGetStringFieldQuickly("playername", ref _strPlayerName);
                                xmlCharacterNavigator.TryGetMultiLineStringFieldQuickly("gamenotes",
                                    ref _strGameNotes);
                                if (!xmlCharacterNavigator.TryGetStringFieldQuickly("primaryarm",
                                        ref _strPrimaryArm))
                                    _strPrimaryArm = "Right";

                                xmlCharacterNavigator.TryGetStringFieldQuickly("prioritymetatype",
                                                                               ref _strPriorityMetatype);
                                xmlCharacterNavigator.TryGetStringFieldQuickly("priorityattributes",
                                                                               ref _strPriorityAttributes);
                                xmlCharacterNavigator.TryGetStringFieldQuickly("priorityspecial",
                                                                               ref _strPrioritySpecial);
                                xmlCharacterNavigator.TryGetStringFieldQuickly("priorityskills",
                                                                               ref _strPrioritySkills);
                                xmlCharacterNavigator.TryGetStringFieldQuickly("priorityresources",
                                                                               ref _strPriorityResources);
                                xmlCharacterNavigator.TryGetStringFieldQuickly("prioritytalent",
                                                                               ref _strPriorityTalent);

                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    _lstPrioritySkills.Clear();
                                else
                                    await _lstPrioritySkills.ClearAsync(token).ConfigureAwait(false);
                                foreach (XPathNavigator xmlSkillName in xmlCharacterNavigator.SelectAndCacheExpression("priorityskills/priorityskill", token))
                                {
                                    if (blnSync)
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        _lstPrioritySkills.Add(xmlSkillName.Value);
                                    else
                                        await _lstPrioritySkills.AddAsync(xmlSkillName.Value, token)
                                                                .ConfigureAwait(false);
                                }

                                string strSkill1 = string.Empty;
                                string strSkill2 = string.Empty;
                                if (xmlCharacterNavigator.TryGetStringFieldQuickly("priorityskill1",
                                        ref strSkill1) &&
                                    !string.IsNullOrEmpty(strSkill1))
                                {
                                    if (blnSync)
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        _lstPrioritySkills.Add(strSkill1);
                                    else
                                        await _lstPrioritySkills.AddAsync(strSkill1, token).ConfigureAwait(false);
                                }

                                if (xmlCharacterNavigator.TryGetStringFieldQuickly("priorityskill2",
                                        ref strSkill2) &&
                                    !string.IsNullOrEmpty(strSkill2))
                                {
                                    if (blnSync)
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        _lstPrioritySkills.Add(strSkill2);
                                    else
                                        await _lstPrioritySkills.AddAsync(strSkill2, token).ConfigureAwait(false);
                                }

                                xmlCharacterNavigator.TryGetBoolFieldQuickly("possessed", ref _blnPossessed);

                                xmlCharacterNavigator.TryGetInt32FieldQuickly("contactpoints",
                                                                              ref _intCachedContactPoints);
                                xmlCharacterNavigator.TryGetDecFieldQuickly("basecarrylimit",
                                                                            ref _decCachedBaseCarryLimit);
                                xmlCharacterNavigator.TryGetDecFieldQuickly("baseliftlimit",
                                                                            ref _decCachedBaseLiftLimit);
                                xmlCharacterNavigator.TryGetDecFieldQuickly("totalcarriedweight",
                                                                            ref _decCachedTotalCarriedWeight);
                                xmlCharacterNavigator.TryGetDecFieldQuickly("encumbranceinterval",
                                                                            ref _decCachedEncumbranceInterval);
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("cfplimit", ref _intCFPLimit);
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("ainormalprogramlimit",
                                                                              ref _intAINormalProgramLimit);
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("aiadvancedprogramlimit",
                                                                              ref _intAIAdvancedProgramLimit);
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("currentcounterspellingdice",
                                                                              ref _intCurrentCounterspellingDice);
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("currentliftcarryhits",
                                                                              ref _intCurrentLiftCarryHits);
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("spelllimit", ref _intFreeSpells);
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("karma", ref _intKarma);
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("totalkarma", ref _intTotalKarma);

                                xmlCharacterNavigator.TryGetInt32FieldQuickly("special", ref _intSpecial);
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("totalspecial", ref _intTotalSpecial);
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("totalattributes",
                                                                              ref _intTotalAttributes);
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("edgeused", ref _intEdgeUsed);
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("streetcred", ref _intStreetCred);
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("notoriety", ref _intNotoriety);
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("publicawareness",
                                                                              ref _intPublicAwareness);
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("burntstreetcred",
                                                                              ref _intBurntStreetCred);
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("baseastralreputation",
                                                                              ref _intBaseAstralReputation);
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("basewildreputation",
                                                                              ref _intBaseWildReputation);
                                xmlCharacterNavigator.TryGetDecFieldQuickly("nuyen", ref _decNuyen);
                                xmlCharacterNavigator.TryGetDecFieldQuickly("startingnuyen", ref _decStartingNuyen);
                                xmlCharacterNavigator.TryGetDecFieldQuickly("nuyenbp", ref _decNuyenBP);

                                xmlCharacterNavigator.TryGetBoolFieldQuickly("adept", ref _blnAdeptEnabled);
                                xmlCharacterNavigator.TryGetBoolFieldQuickly("magician", ref _blnMagicianEnabled);
                                xmlCharacterNavigator.TryGetBoolFieldQuickly("technomancer",
                                                                             ref _blnTechnomancerEnabled);
                                xmlCharacterNavigator.TryGetBoolFieldQuickly("ai", ref _blnAdvancedProgramsEnabled);
                                xmlCharacterNavigator.TryGetBoolFieldQuickly("cyberwaredisabled",
                                                                             ref _blnCyberwareDisabled);
                                xmlCharacterNavigator.TryGetBoolFieldQuickly("initiationdisabled",
                                                                             ref _blnInitiationDisabled);
                                xmlCharacterNavigator.TryGetBoolFieldQuickly("critter", ref _blnCritterEnabled);

                                xmlCharacterNavigator.TryGetDecFieldQuickly("prototypetranshuman",
                                                                            ref _decPrototypeTranshuman);
                                xmlCharacterNavigator.TryGetBoolFieldQuickly("magenabled", ref _blnMAGEnabled);
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("initiategrade",
                                                                              ref _intInitiateGrade);
                                xmlCharacterNavigator.TryGetBoolFieldQuickly("resenabled", ref _blnRESEnabled);
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("submersiongrade",
                                                                              ref _intSubmersionGrade);
                                xmlCharacterNavigator.TryGetBoolFieldQuickly("depenabled", ref _blnDEPEnabled);
                                // Legacy shim
                                if (!_blnCreated && !_blnMAGEnabled && !_blnRESEnabled && !_blnDEPEnabled)
                                    _decEssenceAtSpecialStart = decimal.MinValue;
                                xmlCharacterNavigator.TryGetBoolFieldQuickly("groupmember", ref _blnGroupMember);
                                xmlCharacterNavigator.TryGetStringFieldQuickly("groupname", ref _strGroupName);
                                xmlCharacterNavigator.TryGetMultiLineStringFieldQuickly("groupnotes",
                                    ref _strGroupNotes);
                                //end load_char_misc
                            }

                            XmlNodeList objXmlNodeList;
                            XmlNodeList objXmlLocationList;
                            XmlNode xmlRootQualitiesNode;

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("String_MentorSpirit", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager.GetStringAsync("String_MentorSpirit",
                                                                    token: token)
                                                                .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_mentorspirit", loadActivity))
                            {
                                // Improvements.
                                using (objXmlNodeList = objXmlCharacter.SelectNodes("mentorspirits/mentorspirit"))
                                {
                                    foreach (XmlNode objXmlMentor in objXmlNodeList)
                                    {
                                        MentorSpirit objMentor = new MentorSpirit(this, objXmlMentor);
                                        try
                                        {
                                            if (blnSync)
                                            {
                                                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                objMentor.Load(objXmlMentor);
                                                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                _lstMentorSpirits.Add(objMentor);
                                            }
                                            else
                                            {
                                                await objMentor.LoadAsync(objXmlMentor, token).ConfigureAwait(false);
                                                await _lstMentorSpirits.AddAsync(objMentor, token).ConfigureAwait(false);
                                            }
                                        }
                                        catch
                                        {
                                            if (blnSync)
                                                // ReSharper disable once MethodHasAsyncOverload
                                                objMentor.Dispose();
                                            else
                                                await objMentor.DisposeAsync().ConfigureAwait(false);
                                            throw;
                                        }
                                    }
                                }

                                //using finish("load_char_mentorspirit");
                            }

                            List<Improvement> lstCyberadeptSweepGrades =
                                new List<Improvement>(InitiationGrades.Count);

                            // Fastest way to clear is to just create a new bag and then interlock at the end
                            ConcurrentBag<string> lstInternalIdsNeedingReapplyImprovements
                                = new ConcurrentBag<string>();

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("Tab_Improvements", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager.GetStringAsync("Tab_Improvements",
                                                                    token: token)
                                                                .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_imp", loadActivity))
                            {
                                // Improvements.
                                objXmlNodeList = objXmlCharacter.SelectNodes("improvements/improvement");
                                bool blnRemoveImprovements = Utils.IsUnitTest;
                                string strCharacterInnerXml = objXmlCharacter.InnerXmlViaPool(token);
                                int intCharacterInnerXmlLength = strCharacterInnerXml.Length;
                                foreach (XmlNode objXmlImprovement in objXmlNodeList)
                                {
                                    // First check if this is an orphaned improvement
                                    if ((blnRemoveImprovements || showWarnings) &&
                                        objXmlImprovement["custom"]?.InnerTextViaPool(token) != bool.TrueString &&
                                        !string.IsNullOrEmpty(strCharacterInnerXml))
                                    {
                                        string strLoopSourceName = objXmlImprovement["sourcename"]?.InnerTextViaPool(token);
                                        if (!string.IsNullOrEmpty(strLoopSourceName)
                                            && strLoopSourceName.IsGuid())
                                        {
                                            // Specialized version of ContainsAny that has been optimized for this specific case because it's a bottleneck
                                            bool ContainsAnySourceId(string strId)
                                            {
                                                if (strCharacterInnerXml.Length < strId.Length + 13)
                                                    return false;

                                                string strCommonNeedle = "id>" + strId + "</";
                                                string strNeedle1 = "<guid>" + strId + "</guid>";
                                                int intNeedle1Length = strNeedle1.Length;
                                                ReadOnlySpan<char> spnNeedle1 = strNeedle1.AsSpan();
                                                string strNeedle2 = "<metatypeid>" + strId + "</metatypeid>";
                                                int intNeedle2Length = strNeedle2.Length;
                                                ReadOnlySpan<char> spnNeedle2 = strNeedle2.AsSpan();
                                                string strNeedle3 = "<metavariantid>" + strId +
                                                                    "</metavariantid>";
                                                int intNeedle3Length = strNeedle3.Length;
                                                ReadOnlySpan<char> spnNeedle3 = strNeedle3.AsSpan();
                                                for (int intLoopIndex = strCharacterInnerXml.IndexOf(
                                                         strCommonNeedle, 3,
                                                         StringComparison.OrdinalIgnoreCase);
                                                     intLoopIndex >= 3 &&
                                                     intCharacterInnerXmlLength - intLoopIndex + 3 >
                                                     intNeedle1Length;
                                                     intLoopIndex = strCharacterInnerXml.IndexOf(strCommonNeedle,
                                                         intLoopIndex + 1,
                                                         StringComparison.OrdinalIgnoreCase))
                                                {
                                                    if (strCharacterInnerXml.AsSpan(intLoopIndex - 3, intNeedle1Length)
                                                        .Equals(spnNeedle1, StringComparison.OrdinalIgnoreCase))
                                                        return true;
                                                    if (intLoopIndex < 9 ||
                                                        intCharacterInnerXmlLength - intLoopIndex + 9 <
                                                        intNeedle2Length)
                                                        continue;
                                                    if (strCharacterInnerXml.AsSpan(intLoopIndex - 9, intNeedle2Length)
                                                        .Equals(spnNeedle2, StringComparison.OrdinalIgnoreCase))
                                                        return true;
                                                    if (intLoopIndex < 12 ||
                                                        intCharacterInnerXmlLength - intLoopIndex + 12 <
                                                        intNeedle3Length)
                                                        continue;
                                                    if (strCharacterInnerXml.AsSpan(intLoopIndex - 12, intNeedle3Length)
                                                        .Equals(spnNeedle3, StringComparison.OrdinalIgnoreCase))
                                                        return true;
                                                }

                                                return false;
                                            }

                                            if (!ContainsAnySourceId(strLoopSourceName))
                                            {
                                                //Utils.BreakIfDebug();
                                                if (blnRemoveImprovements)
                                                    continue;

                                                if (blnSync)
                                                {
                                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                    if (UserInteraction.ShowScrollableMessage(
                                                            // ReSharper disable once MethodHasAsyncOverload
                                                            LanguageManager.GetString(
                                                                "Message_OrphanedImprovements", token: token),
                                                            // ReSharper disable once MethodHasAsyncOverload
                                                            LanguageManager.GetString(
                                                                "MessageTitle_OrphanedImprovements",
                                                                token: token),
                                                            PromptButtons.YesNo, PromptIcon.Error) ==
                                                        PromptResult.Yes)
                                                    {
                                                        blnRemoveImprovements = true;
                                                        continue;
                                                    }
                                                }
                                                else if (await UserInteraction.ShowScrollableMessageAsync(
                                                             await LanguageManager.GetStringAsync(
                                                                     "Message_OrphanedImprovements",
                                                                     token: token)
                                                                 .ConfigureAwait(false),
                                                             await LanguageManager.GetStringAsync(
                                                                     "MessageTitle_OrphanedImprovements",
                                                                     token: token)
                                                                 .ConfigureAwait(false),
                                                             PromptButtons.YesNo, PromptIcon.Error,
                                                             token: token).ConfigureAwait(false) ==
                                                         PromptResult.Yes)
                                                {
                                                    blnRemoveImprovements = true;
                                                    continue;
                                                }

                                                return false;
                                            }
                                        }
                                    }

                                    string strImprovementSource =
                                        objXmlImprovement["improvementsource"]?.InnerTextViaPool(token);
                                    switch (strImprovementSource)
                                    {
                                        // Do not load condition monitor improvements from older versions of Chummer
                                        case "ConditionMonitor":
                                            continue;
                                        // Load Edge use improvements from older versions of Chummer directly into Character's Edge Use property
                                        case "EdgeUse":
                                            decimal decOldEdgeUsed = 0;
                                            if (objXmlImprovement.TryGetDecFieldQuickly("aug",
                                                    ref decOldEdgeUsed))
                                            {
                                                if (blnSync)
                                                    EdgeUsed = (-decOldEdgeUsed).StandardRound();
                                                else
                                                    await SetEdgeUsedAsync((-decOldEdgeUsed).StandardRound(), token).ConfigureAwait(false);
                                            }
                                            continue;
                                        case nameof(Improvement.ImprovementSource.EssenceLoss):
                                        case nameof(Improvement.ImprovementSource.EssenceLossChargen):
                                            // Do not load essence loss improvements if this character does not have any attributes affected by essence loss
                                            if (_decEssenceAtSpecialStart == decimal.MinValue)
                                                continue;
                                            break;
                                    }

                                    Improvement objImprovement = new Improvement(this);
                                    try
                                    {
                                        token.ThrowIfCancellationRequested();
                                        objImprovement.Load(objXmlImprovement);
                                        // This is initially set to false make sure no property changers are triggered
                                        objImprovement.SetupComplete = true;
                                        if (blnSync)
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            _lstImprovements.Add(objImprovement);
                                        else
                                            await _lstImprovements.AddAsync(objImprovement, token)
                                                .ConfigureAwait(false);

                                        if (objImprovement.ImproveType ==
                                            Improvement.ImprovementType.SkillsoftAccess &&
                                            objImprovement.Value == 0)
                                        {
                                            lstInternalIdsNeedingReapplyImprovements.Add(objImprovement
                                                .SourceName);
                                        }
                                        // Cyberadept fix
                                        else if (LastSavedVersion <= new ValueVersion(5, 212, 78)
                                                 && objImprovement.ImproveSource ==
                                                 Improvement.ImprovementSource.Echo
                                                 && objImprovement.ImproveType ==
                                                 Improvement.ImprovementType.Attribute
                                                 && objImprovement.ImprovedName == "RESBase"
                                                 && objImprovement.Value > 0
                                                 && objImprovement.Value == objImprovement.Augmented)
                                        {
                                            // Cyberadept in these versions was an echo. It is no longer an echo, and so needs a more complicated reapplication
                                            if (blnSync
                                                    ? Settings.SpecialKarmaCostBasedOnShownValue
                                                    : await (await GetSettingsAsync(token).ConfigureAwait(false)).GetSpecialKarmaCostBasedOnShownValueAsync(token).ConfigureAwait(false))
                                            {
                                                if (blnSync)
                                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                    _lstImprovements.Remove(objImprovement);
                                                else
                                                    await _lstImprovements.RemoveAsync(objImprovement, token)
                                                        .ConfigureAwait(false);
                                            }
                                            else
                                                lstCyberadeptSweepGrades.Add(objImprovement);
                                        }
                                    }
                                    catch (ArgumentException)
                                    {
                                        lstInternalIdsNeedingReapplyImprovements.Add(
                                            objXmlImprovement["sourcename"]?.InnerTextViaPool(token));
                                    }
                                }

                                //Timekeeper.Finish("load_char_imp");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("Label_Contacts", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager.GetStringAsync("Label_Contacts",
                                                                    token: token)
                                                                .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_contacts", loadActivity))
                            {
                                // Contacts.
                                foreach (XPathNavigator xmlContact in
                                         xmlCharacterNavigator.SelectAndCacheExpression("contacts/contact", token))
                                {
                                    Contact objContact = new Contact(this);
                                    if (blnSync)
                                    {
                                        try
                                        {
                                            // ReSharper disable once MethodHasAsyncOverload
                                            objContact.Load(xmlContact, token);
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            _lstContacts.Add(objContact);
                                        }
                                        catch
                                        {
                                            try
                                            {
                                                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                _lstContacts.Remove(objContact);
                                            }
                                            catch
                                            {
                                                //swallow this
                                            }
                                            // ReSharper disable once MethodHasAsyncOverload
                                            objContact.Dispose();
                                            throw;
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            await objContact.LoadAsync(xmlContact, token).ConfigureAwait(false);
                                            await _lstContacts.AddAsync(objContact, token).ConfigureAwait(false);
                                        }
                                        catch
                                        {
                                            try
                                            {
                                                await _lstContacts.RemoveAsync(objContact, CancellationToken.None).ConfigureAwait(false);
                                            }
                                            catch
                                            {
                                                // swallow this
                                            }
                                            await objContact.DisposeAsync().ConfigureAwait(false);
                                            throw;
                                        }
                                    }
                                }

                                //Timekeeper.Finish("load_char_contacts");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("String_Qualities", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager.GetStringAsync("String_Qualities",
                                                                    token: token)
                                                                .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_quality", loadActivity))
                            {
                                // Qualities

                                using (objXmlNodeList = objXmlCharacter.SelectNodes("qualities/quality"))
                                {
                                    bool blnHasOldQualities = false;
                                    xmlRootQualitiesNode =
                                        (blnSync
                                            // ReSharper disable once MethodHasAsyncOverload
                                            ? LoadData("qualities.xml", token: token)
                                            : await LoadDataAsync("qualities.xml", token: token).ConfigureAwait(false))
                                        .SelectSingleNode("/chummer/qualities");
                                    foreach (XmlNode objXmlQuality in objXmlNodeList)
                                    {
                                        if (objXmlQuality["name"] != null)
                                        {
                                            if (!(blnSync
                                                    // ReSharper disable once MethodHasAsyncOverload
                                                    ? CorrectedUnleveledQuality(objXmlQuality, xmlRootQualitiesNode, token)
                                                    : await CorrectedUnleveledQualityAsync(objXmlQuality, xmlRootQualitiesNode, token).ConfigureAwait(false)))
                                            {
                                                Quality objQuality = new Quality(this);
                                                try
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                    if (blnSync)
                                                    {
                                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                        objQuality.Load(objXmlQuality);
                                                        // ReSharper disable once MethodHasAsyncOverload
                                                        if (_lstQualities.Any(x => x.InternalId == objQuality.InternalId, token))
                                                            // Corrects an issue arising from older versions of CorrectedUnleveledQuality()
                                                            objQuality.SetGUID(Guid.NewGuid());
                                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                        _lstQualities.Add(objQuality);
                                                        // ReSharper disable once MethodHasAsyncOverload
                                                        if (objQuality.GetNodeXPath(token: token)
                                                                      ?.SelectSingleNodeAndCacheExpression(
                                                                          "bonus/addgear/name", token)
                                                                      ?.Value == "Living Persona")
                                                            objLivingPersonaQuality = objQuality;
                                                    }
                                                    else
                                                    {
                                                        await objQuality.LoadAsync(objXmlQuality, token).ConfigureAwait(false);
                                                        if (await _lstQualities.AnyAsync(x => x.InternalId == objQuality.InternalId, token).ConfigureAwait(false))
                                                            // Corrects an issue arising from older versions of CorrectedUnleveledQuality()
                                                            objQuality.SetGUID(Guid.NewGuid());
                                                        await _lstQualities.AddAsync(objQuality, token)
                                                                           .ConfigureAwait(false);
                                                        XPathNavigator objQualityNode = await objQuality
                                                            .GetNodeXPathAsync(token: token).ConfigureAwait(false);
                                                        if (objQualityNode
                                                                ?.SelectSingleNodeAndCacheExpression(
                                                                    "bonus/addgear/name", token)?.Value ==
                                                            "Living Persona")
                                                            objLivingPersonaQuality = objQuality;
                                                    }

                                                    // Legacy shim
                                                    if (LastSavedVersion <= new ValueVersion(5, 195, 1)
                                                        && (objQuality.Name == "The Artisan's Way"
                                                            || objQuality.Name == "The Artist's Way"
                                                            || objQuality.Name == "The Athlete's Way"
                                                            || objQuality.Name == "The Burnout's Way"
                                                            || objQuality.Name == "The Invisible Way"
                                                            || objQuality.Name == "The Magician's Way"
                                                            || objQuality.Name == "The Speaker's Way"
                                                            || objQuality.Name == "The Warrior's Way")
                                                        && objQuality.Bonus?.HasChildNodes == false)
                                                    {
                                                        if (blnSync)
                                                            // ReSharper disable once MethodHasAsyncOverload
                                                            ImprovementManager.RemoveImprovements(this,
                                                                Improvement.ImprovementSource.Quality,
                                                                objQuality.InternalId, token: token);
                                                        else
                                                            await ImprovementManager.RemoveImprovementsAsync(this,
                                                                Improvement.ImprovementSource.Quality,
                                                                objQuality.InternalId, token).ConfigureAwait(false);
                                                        XmlNode objNode = blnSync
                                                            // ReSharper disable once MethodHasAsyncOverload
                                                            ? objQuality.GetNode(token: token)
                                                            : await objQuality.GetNodeAsync(token: token)
                                                                              .ConfigureAwait(false);
                                                        if (objNode != null)
                                                        {
                                                            objQuality.Bonus = objNode["bonus"];
                                                            if (objQuality.Bonus != null)
                                                            {
                                                                ImprovementManager.SetForcedValue(objQuality.Extra, this);
                                                                if (blnSync)
                                                                    // ReSharper disable once MethodHasAsyncOverload
                                                                    ImprovementManager.CreateImprovements(this,
                                                                        Improvement.ImprovementSource.Quality,
                                                                        objQuality.InternalId, objQuality.Bonus, 1,
                                                                        objQuality.CurrentDisplayNameShort, token: token);
                                                                else
                                                                    await ImprovementManager.CreateImprovementsAsync(
                                                                            this,
                                                                            Improvement.ImprovementSource.Quality,
                                                                            objQuality.InternalId, objQuality.Bonus, 1,
                                                                            await objQuality
                                                                                .GetCurrentDisplayNameShortAsync(token)
                                                                                .ConfigureAwait(false), token: token)
                                                                        .ConfigureAwait(false);
                                                                string strSelectedValue =
                                                                    ImprovementManager.GetSelectedValue(this);
                                                                if (!string.IsNullOrEmpty(strSelectedValue))
                                                                {
                                                                    objQuality.Extra = strSelectedValue;
                                                                }
                                                            }

                                                            objQuality.FirstLevelBonus = objNode["firstlevelbonus"];
                                                            if (objQuality.FirstLevelBonus?.HasChildNodes == true)
                                                            {
                                                                string strCheckExtra = blnSync
                                                                    ? objQuality.Extra
                                                                    : await objQuality.GetExtraAsync(token).ConfigureAwait(false);
                                                                string strCheckSourceName = blnSync
                                                                    ? objQuality.SourceName
                                                                    : await objQuality.GetSourceNameAsync(token).ConfigureAwait(false);
                                                                bool blnDoFirstLevel;
                                                                if (blnSync)
                                                                {
                                                                    // ReSharper disable once MethodHasAsyncOverload
                                                                    blnDoFirstLevel = !Qualities.Any(objCheckQuality =>
                                                                        objCheckQuality != objQuality &&
                                                                        objCheckQuality.SourceID == objQuality.SourceID &&
                                                                        objCheckQuality.Extra == strCheckExtra &&
                                                                        objCheckQuality.SourceName == strCheckSourceName, token);
                                                                }
                                                                else
                                                                {
                                                                    blnDoFirstLevel = !await (await GetQualitiesAsync(token).ConfigureAwait(false)).AnyAsync(async objCheckQuality =>
                                                                        objCheckQuality != objQuality &&
                                                                        objCheckQuality.SourceID == objQuality.SourceID &&
                                                                        await objCheckQuality.GetExtraAsync(token).ConfigureAwait(false) == strCheckExtra &&
                                                                        await objCheckQuality.GetSourceNameAsync(token).ConfigureAwait(false) == strCheckSourceName, token).ConfigureAwait(false);
                                                                }

                                                                if (blnDoFirstLevel)
                                                                {
                                                                    ImprovementManager.SetForcedValue(objQuality.Extra, this);
                                                                    if (blnSync)
                                                                        // ReSharper disable once MethodHasAsyncOverload
                                                                        ImprovementManager.CreateImprovements(this,
                                                                            Improvement.ImprovementSource.Quality,
                                                                            objQuality.InternalId,
                                                                            objQuality.FirstLevelBonus, 1,
                                                                            objQuality.CurrentDisplayNameShort, token: token);
                                                                    else
                                                                        await ImprovementManager
                                                                              .CreateImprovementsAsync(
                                                                                  this,
                                                                                  Improvement.ImprovementSource.Quality,
                                                                                  objQuality.InternalId,
                                                                                  objQuality.FirstLevelBonus, 1,
                                                                                  await objQuality
                                                                                      .GetCurrentDisplayNameShortAsync(
                                                                                          token)
                                                                                      .ConfigureAwait(false),
                                                                                  token: token)
                                                                              .ConfigureAwait(false);
                                                                    string strSelectedValue =
                                                                        ImprovementManager.GetSelectedValue(this);
                                                                    if (!string.IsNullOrEmpty(strSelectedValue))
                                                                    {
                                                                        if (blnSync)
                                                                            objQuality.Extra = strSelectedValue;
                                                                        else
                                                                            await objQuality.SetExtraAsync(strSelectedValue, token).ConfigureAwait(false);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            lstInternalIdsNeedingReapplyImprovements.Add(
                                                                objQuality.InternalId);
                                                        }

                                                        objQuality.NaturalWeaponsNode = objNode["naturalweapons"];
                                                        if (objQuality.NaturalWeaponsNode != null)
                                                        {
                                                            ImprovementManager.SetForcedValue(objQuality.Extra, this);
                                                            if (blnSync)
                                                                // ReSharper disable once MethodHasAsyncOverload
                                                                ImprovementManager.CreateImprovements(this,
                                                                    Improvement.ImprovementSource.Quality,
                                                                    objQuality.InternalId,
                                                                    objQuality.NaturalWeaponsNode, 1,
                                                                    objQuality.CurrentDisplayNameShort, token: token);
                                                            else
                                                                await ImprovementManager.CreateImprovementsAsync(this,
                                                                        Improvement.ImprovementSource.Quality,
                                                                        objQuality.InternalId,
                                                                        objQuality.NaturalWeaponsNode, 1,
                                                                        await objQuality
                                                                              .GetCurrentDisplayNameShortAsync(token)
                                                                              .ConfigureAwait(false), token: token)
                                                                    .ConfigureAwait(false);
                                                            string strSelectedValue =
                                                                ImprovementManager.GetSelectedValue(this);
                                                            if (!string.IsNullOrEmpty(strSelectedValue))
                                                            {
                                                                objQuality.Extra = strSelectedValue;
                                                            }
                                                        }
                                                    }

                                                    if (LastSavedVersion <= new ValueVersion(5, 200, 0)
                                                        && objQuality.Name == "Made Man"
                                                        && objQuality.Bonus["selectcontact"] != null)
                                                    {
                                                        string selectedContactUniqueId = Improvements.FirstOrDefault(
                                                                x =>
                                                                    x.SourceName == objQuality.InternalId &&
                                                                    x.ImproveType == Improvement.ImprovementType
                                                                        .ContactForcedLoyalty)
                                                            ?.ImprovedName;
                                                        if (string.IsNullOrWhiteSpace(selectedContactUniqueId))
                                                        {
                                                            selectedContactUniqueId =
                                                                Contacts.FirstOrDefault(x => x.Name == objQuality.Extra)
                                                                        ?.UniqueId;
                                                        }

                                                        if (string.IsNullOrWhiteSpace(selectedContactUniqueId))
                                                        {
                                                            // Populate the Magician Traditions list.
                                                            using (new FetchSafelyFromSafeObjectPool<List<ListItem>>(
                                                                       Utils.ListItemListPool,
                                                                       out List<ListItem> lstContacts))
                                                            {
                                                                foreach (Contact objContact in Contacts)
                                                                {
                                                                    if (objContact.IsGroup)
                                                                        lstContacts.Add(new ListItem(objContact.Name,
                                                                            objContact.UniqueId));
                                                                }

                                                                if (lstContacts.Count > 1)
                                                                {
                                                                    lstContacts.Sort(CompareListItems.CompareNames);
                                                                }

                                                                if (blnSync)
                                                                {
                                                                    // ReSharper disable once MethodHasAsyncOverload
                                                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                                    using (ThreadSafeForm<SelectItem> frmPickItem
                                                                           = ThreadSafeForm<SelectItem>.Get(
                                                                               () => new SelectItem()))
                                                                    {
                                                                        frmPickItem.MyForm
                                                                            .SetDropdownItemsMode(lstContacts);
                                                                        // ReSharper disable once MethodHasAsyncOverload
                                                                        if (frmPickItem.ShowDialogSafe(this, token)
                                                                            != DialogResult.OK)
                                                                        {
                                                                            return false;
                                                                        }

                                                                        selectedContactUniqueId
                                                                            = frmPickItem.MyForm.SelectedItem;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    using (ThreadSafeForm<SelectItem> frmPickItem
                                                                           = await ThreadSafeForm<SelectItem>
                                                                               .GetAsync(() => new SelectItem(), token)
                                                                               .ConfigureAwait(false))
                                                                    {
                                                                        frmPickItem.MyForm
                                                                            .SetDropdownItemsMode(lstContacts);
                                                                        if (await frmPickItem
                                                                                .ShowDialogSafeAsync(this, token)
                                                                                .ConfigureAwait(false)
                                                                            != DialogResult.OK)
                                                                        {
                                                                            return false;
                                                                        }

                                                                        selectedContactUniqueId
                                                                            = await frmPickItem.MyForm.DoThreadSafeFuncAsync(x => x.SelectedItem, token).ConfigureAwait(false);
                                                                    }
                                                                }
                                                            }
                                                        }

                                                        objQuality.Bonus =
                                                            xmlRootQualitiesNode.SelectSingleNode(
                                                                "quality[name=\"Made Man\"]/bonus");
                                                        objQuality.Extra = string.Empty;
                                                        try
                                                        {
                                                            token.ThrowIfCancellationRequested();
                                                            if (blnSync)
                                                            {
                                                                // ReSharper disable MethodHasAsyncOverload
                                                                ImprovementManager.RemoveImprovements(this,
                                                                    Improvement.ImprovementSource.Quality,
                                                                    objQuality.InternalId, token: token);
                                                                ImprovementManager.CreateImprovement(this, string.Empty,
                                                                    Improvement.ImprovementSource.Quality,
                                                                    objQuality.InternalId,
                                                                    Improvement.ImprovementType.MadeMan,
                                                                    objQuality.CurrentDisplayNameShort, token: token);
                                                                ImprovementManager.CreateImprovement(
                                                                    this, selectedContactUniqueId,
                                                                    Improvement.ImprovementSource.Quality,
                                                                    objQuality.InternalId,
                                                                    Improvement.ImprovementType.AddContact,
                                                                    objQuality.CurrentDisplayNameShort, token: token);
                                                                ImprovementManager.CreateImprovement(
                                                                    this, selectedContactUniqueId,
                                                                    Improvement.ImprovementSource.Quality,
                                                                    objQuality.InternalId,
                                                                    Improvement.ImprovementType.ContactForcedLoyalty,
                                                                    objQuality.CurrentDisplayNameShort, token: token);
                                                                ImprovementManager.CreateImprovement(
                                                                    this, selectedContactUniqueId,
                                                                    Improvement.ImprovementSource.Quality,
                                                                    objQuality.InternalId,
                                                                    Improvement.ImprovementType.ContactForceGroup,
                                                                    objQuality.CurrentDisplayNameShort, token: token);
                                                                ImprovementManager.CreateImprovement(
                                                                    this, selectedContactUniqueId,
                                                                    Improvement.ImprovementSource.Quality,
                                                                    objQuality.InternalId,
                                                                    Improvement.ImprovementType.ContactMakeFree,
                                                                    objQuality.CurrentDisplayNameShort, token: token);
                                                                // ReSharper restore MethodHasAsyncOverload
                                                            }
                                                            else
                                                            {
                                                                await ImprovementManager.RemoveImprovementsAsync(this,
                                                                    Improvement.ImprovementSource.Quality,
                                                                    objQuality.InternalId, token).ConfigureAwait(false);
                                                                await ImprovementManager.CreateImprovementAsync(
                                                                        this, string.Empty,
                                                                        Improvement.ImprovementSource.Quality,
                                                                        objQuality.InternalId,
                                                                        Improvement.ImprovementType.MadeMan,
                                                                        await objQuality
                                                                              .GetCurrentDisplayNameShortAsync(token)
                                                                              .ConfigureAwait(false), token: token)
                                                                    .ConfigureAwait(false);
                                                                await ImprovementManager.CreateImprovementAsync(
                                                                        this, selectedContactUniqueId,
                                                                        Improvement.ImprovementSource.Quality,
                                                                        objQuality.InternalId,
                                                                        Improvement.ImprovementType.AddContact,
                                                                        await objQuality
                                                                              .GetCurrentDisplayNameShortAsync(token)
                                                                              .ConfigureAwait(false), token: token)
                                                                    .ConfigureAwait(false);
                                                                await ImprovementManager.CreateImprovementAsync(
                                                                        this, selectedContactUniqueId,
                                                                        Improvement.ImprovementSource.Quality,
                                                                        objQuality.InternalId,
                                                                        Improvement.ImprovementType
                                                                            .ContactForcedLoyalty,
                                                                        await objQuality
                                                                              .GetCurrentDisplayNameShortAsync(token)
                                                                              .ConfigureAwait(false), token: token)
                                                                    .ConfigureAwait(false);
                                                                await ImprovementManager.CreateImprovementAsync(
                                                                        this, selectedContactUniqueId,
                                                                        Improvement.ImprovementSource.Quality,
                                                                        objQuality.InternalId,
                                                                        Improvement.ImprovementType.ContactForceGroup,
                                                                        await objQuality
                                                                              .GetCurrentDisplayNameShortAsync(token)
                                                                              .ConfigureAwait(false), token: token)
                                                                    .ConfigureAwait(false);
                                                                await ImprovementManager.CreateImprovementAsync(
                                                                        this, selectedContactUniqueId,
                                                                        Improvement.ImprovementSource.Quality,
                                                                        objQuality.InternalId,
                                                                        Improvement.ImprovementType.ContactMakeFree,
                                                                        await objQuality
                                                                              .GetCurrentDisplayNameShortAsync(token)
                                                                              .ConfigureAwait(false), token: token)
                                                                    .ConfigureAwait(false);
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            if (blnSync)
                                                                // ReSharper disable once MethodHasAsyncOverload
                                                                ImprovementManager.Rollback(this, CancellationToken.None);
                                                            else
                                                                await ImprovementManager.RollbackAsync(this, CancellationToken.None).ConfigureAwait(false);
                                                            throw;
                                                        }

                                                        if (blnSync)
                                                            // ReSharper disable once MethodHasAsyncOverload
                                                            ImprovementManager.Commit(this, token);
                                                        else
                                                            await ImprovementManager.CommitAsync(this, token).ConfigureAwait(false);
                                                    }

                                                    if (LastSavedVersion <= new ValueVersion(5, 212, 43)
                                                        && objQuality.Name == "Inspired"
                                                        && objQuality.Source == "SASS"
                                                        && objQuality.Bonus["selectexpertise"] == null)
                                                    {
                                                        // Old handling of SASS' Inspired quality was both hardcoded and wrong
                                                        // Since SASS' Inspired requires the player to choose a specialization, we always need a prompt,
                                                        // so add the quality to the list for processing when the character is opened.
                                                        lstInternalIdsNeedingReapplyImprovements.Add(
                                                            objQuality.InternalId);
                                                    }

                                                    if (LastSavedVersion <= new ValueVersion(5, 212, 56)
                                                        && objQuality.Name == "Chain Breaker"
                                                        && objQuality.Bonus == null)
                                                    {
                                                        // Chain Breaker bonus requires manual selection of two spirit types, so we need a prompt.
                                                        lstInternalIdsNeedingReapplyImprovements.Add(
                                                            objQuality.InternalId);
                                                    }

                                                    if (LastSavedVersion <= new ValueVersion(5, 212, 78)
                                                        && objQuality.Name == "Resonant Stream: Cyberadept"
                                                        && objQuality.Bonus == null)
                                                    {
                                                        objQuality.Bonus =
                                                            xmlRootQualitiesNode.SelectSingleNode(
                                                                "quality[name=\"Resonant Stream: Cyberadept\"]/bonus");
                                                        try
                                                        {
                                                            token.ThrowIfCancellationRequested();
                                                            if (blnSync)
                                                            {
                                                                // ReSharper disable MethodHasAsyncOverload
                                                                ImprovementManager.RemoveImprovements(this,
                                                                    Improvement.ImprovementSource.Quality,
                                                                    objQuality.InternalId, token: token);
                                                                ImprovementManager.CreateImprovement(this, string.Empty,
                                                                    Improvement.ImprovementSource.Quality,
                                                                    objQuality.InternalId,
                                                                    Improvement.ImprovementType.CyberadeptDaemon,
                                                                    objQuality.CurrentDisplayNameShort, token: token);
                                                                // ReSharper restore MethodHasAsyncOverload
                                                            }
                                                            else
                                                            {
                                                                await ImprovementManager.RemoveImprovementsAsync(this,
                                                                        Improvement.ImprovementSource.Quality,
                                                                        objQuality.InternalId, token: token)
                                                                    .ConfigureAwait(false);
                                                                await ImprovementManager.CreateImprovementAsync(
                                                                        this, string.Empty,
                                                                        Improvement.ImprovementSource.Quality,
                                                                        objQuality.InternalId,
                                                                        Improvement.ImprovementType.CyberadeptDaemon,
                                                                        await objQuality
                                                                              .GetCurrentDisplayNameShortAsync(token)
                                                                              .ConfigureAwait(false), token: token)
                                                                    .ConfigureAwait(false);
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            if (blnSync)
                                                                // ReSharper disable once MethodHasAsyncOverload
                                                                ImprovementManager.Rollback(this, CancellationToken.None);
                                                            else
                                                                await ImprovementManager.RollbackAsync(this, CancellationToken.None).ConfigureAwait(false);
                                                            throw;
                                                        }

                                                        if (blnSync)
                                                            // ReSharper disable once MethodHasAsyncOverload
                                                            ImprovementManager.Commit(this, token);
                                                        else
                                                            await ImprovementManager.CommitAsync(this, token).ConfigureAwait(false);
                                                    }
                                                }
                                                catch
                                                {
                                                    if (blnSync)
                                                        // ReSharper disable once MethodHasAsyncOverload
                                                        objQuality.DeleteQuality(token: CancellationToken.None);
                                                    else
                                                        await objQuality.DeleteQualityAsync(token: CancellationToken.None).ConfigureAwait(false);
                                                    throw;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // If the Quality does not have a name tag, it is in the old format. Set the flag to show that old Qualities are in use.
                                            blnHasOldQualities = true;
                                        }
                                    }

                                    // If old Qualities are in use, they need to be converted before loading can continue.
                                    if (blnHasOldQualities)
                                    {
                                        if (blnSync)
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            ConvertOldQualities(objXmlNodeList);
                                        else
                                            await ConvertOldQualitiesAsync(objXmlNodeList, token).ConfigureAwait(false);
                                    }
                                    //Timekeeper.Finish("load_char_quality");
                                }
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("Label_Attributes", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager.GetStringAsync("Label_Attributes",
                                                                    token: token)
                                                                .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_attributes", loadActivity))
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    AttributeSection.Load(objXmlCharacter, token);
                                else
                                    await AttributeSection.LoadAsync(objXmlCharacter, token).ConfigureAwait(false);
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("String_Tradition", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager.GetStringAsync("String_Tradition",
                                                                    token: token)
                                                                .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_misc2", loadActivity))
                            {
                                // Attempt to load the split MAG CharacterAttribute information for Mystic Adepts.
                                if (_blnAdeptEnabled && _blnMagicianEnabled)
                                {
                                    xmlCharacterNavigator.TryGetInt32FieldQuickly("magsplitadept",
                                        ref _intMAGAdept);
                                    xmlCharacterNavigator.TryGetInt32FieldQuickly("magsplitmagician",
                                        ref _intMAGMagician);
                                }

                                // Attempt to load in the character's tradition (or equivalent for Technomancers)
                                string strTemp = string.Empty;
                                if (xmlCharacterNavigator.TryGetStringFieldQuickly("stream", ref strTemp) &&
                                    !string.IsNullOrEmpty(strTemp) && (blnSync ? RESEnabled : await GetRESEnabledAsync(token).ConfigureAwait(false)))
                                {
                                    // Legacy load a Technomancer tradition
                                    XmlNode xmlTraditionListDataNode =
                                        (blnSync
                                            // ReSharper disable once MethodHasAsyncOverload
                                            ? LoadData("streams.xml", token: token)
                                            : await LoadDataAsync("streams.xml", token: token).ConfigureAwait(false))
                                        .SelectSingleNode("/chummer/traditions");
                                    if (xmlTraditionListDataNode != null)
                                    {
                                        XmlNode xmlTraditionDataNode =
                                            xmlTraditionListDataNode.TryGetNodeByNameOrId("tradition", strTemp);
                                        if (xmlTraditionDataNode != null)
                                        {
                                            if (blnSync)
                                            {
                                                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                if (!_objTradition.Create(xmlTraditionDataNode))
                                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                    _objTradition.ResetTradition();
                                            }
                                            else if (!await _objTradition.CreateAsync(xmlTraditionDataNode, token: token).ConfigureAwait(false))
                                                await _objTradition.ResetTraditionAsync(token).ConfigureAwait(false);
                                        }
                                        else
                                        {
                                            xmlTraditionDataNode =
                                                xmlTraditionListDataNode.SelectSingleNode(
                                                    "tradition[name = \"Default\"]");
                                            if (xmlTraditionDataNode != null)
                                            {
                                                if (blnSync)
                                                {
                                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                    if (!_objTradition.Create(xmlTraditionDataNode))
                                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                        _objTradition.ResetTradition();
                                                }
                                                else if (!await _objTradition.CreateAsync(xmlTraditionDataNode, token: token).ConfigureAwait(false))
                                                    await _objTradition.ResetTraditionAsync(token).ConfigureAwait(false);
                                            }
                                            else
                                            {
                                                xmlTraditionDataNode =
                                                    xmlTraditionListDataNode["tradition"];
                                                if (xmlTraditionDataNode != null)
                                                {
                                                    if (blnSync)
                                                    {
                                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                        if (!_objTradition.Create(xmlTraditionDataNode))
                                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                            _objTradition.ResetTradition();
                                                    }
                                                    else if (!await _objTradition.CreateAsync(xmlTraditionDataNode, token: token).ConfigureAwait(false))
                                                        await _objTradition.ResetTraditionAsync(token).ConfigureAwait(false);
                                                }
                                            }
                                        }
                                    }

                                    if (_objTradition.Type != TraditionType.None)
                                    {
                                        if (blnSync)
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            _objTradition.LegacyLoad(xmlCharacterNavigator);
                                        else
                                            await _objTradition.LegacyLoadAsync(xmlCharacterNavigator, token).ConfigureAwait(false);
                                    }
                                }
                                else
                                {
                                    XPathNavigator xpathTraditionNavigator = xmlCharacterNavigator.SelectSingleNodeAndCacheExpression("tradition", token);
                                    // Regular tradition load
                                    if (xpathTraditionNavigator != null)
                                    {
                                        if (xpathTraditionNavigator.SelectSingleNodeAndCacheExpression("guid", token) != null
                                            || xpathTraditionNavigator.SelectSingleNodeAndCacheExpression("id", token) != null)
                                        {
                                            if (blnSync)
                                                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                _objTradition.Load(objXmlCharacter["tradition"]);
                                            else
                                                await _objTradition.LoadAsync(objXmlCharacter["tradition"], token).ConfigureAwait(false);
                                        }
                                        else if (blnSync
                                                     ? MAGEnabled
                                                     : await GetMAGEnabledAsync(token).ConfigureAwait(false))
                                        {
                                            XmlNode xmlTraditionListDataNode =
                                                (blnSync
                                                    // ReSharper disable once MethodHasAsyncOverload
                                                    ? LoadData("traditions.xml", token: token)
                                                    : await LoadDataAsync("traditions.xml", token: token)
                                                        .ConfigureAwait(false))
                                                .SelectSingleNode("/chummer/traditions");
                                            if (xmlTraditionListDataNode != null)
                                            {
                                                xmlCharacterNavigator.TryGetStringFieldQuickly("tradition",
                                                    ref strTemp);
                                                XmlNode xmlTraditionDataNode =
                                                    xmlTraditionListDataNode.TryGetNodeByNameOrId("tradition", strTemp);
                                                if (xmlTraditionDataNode != null)
                                                {
                                                    if (blnSync)
                                                    {
                                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                        if (!_objTradition.Create(xmlTraditionDataNode))
                                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                            _objTradition.ResetTradition();
                                                    }
                                                    else if (!await _objTradition.CreateAsync(xmlTraditionDataNode, token: token).ConfigureAwait(false))
                                                        await _objTradition.ResetTraditionAsync(token).ConfigureAwait(false);
                                                }
                                                else
                                                {
                                                    xmlTraditionDataNode =
                                                        xmlTraditionListDataNode.TryGetNodeByNameOrId("tradition", Tradition.CustomMagicalTraditionGuidString);
                                                    if (xmlTraditionDataNode != null)
                                                    {
                                                        if (blnSync)
                                                        {
                                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                            if (!_objTradition.Create(xmlTraditionDataNode))
                                                                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                                _objTradition.ResetTradition();
                                                        }
                                                        else if (!await _objTradition.CreateAsync(xmlTraditionDataNode, token: token).ConfigureAwait(false))
                                                            await _objTradition.ResetTraditionAsync(token).ConfigureAwait(false);
                                                    }
                                                }
                                            }

                                            if (_objTradition.Type != TraditionType.None)
                                            {
                                                if (blnSync)
                                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                    _objTradition.LegacyLoad(xmlCharacterNavigator);
                                                else
                                                    await _objTradition.LegacyLoadAsync(xmlCharacterNavigator, token).ConfigureAwait(false);
                                            }
                                        }
                                    }
                                    // Not null but doesn't have children -> legacy load a magical tradition
                                    else if (xpathTraditionNavigator != null && (blnSync ? MAGEnabled : await GetMAGEnabledAsync(token).ConfigureAwait(false)))
                                    {
                                        XmlNode xmlTraditionListDataNode =
                                            (blnSync
                                                // ReSharper disable once MethodHasAsyncOverload
                                                ? LoadData("traditions.xml", token: token)
                                                : await LoadDataAsync("traditions.xml", token: token)
                                                    .ConfigureAwait(false))
                                            .SelectSingleNode("/chummer/traditions");
                                        if (xmlTraditionListDataNode != null)
                                        {
                                            xmlCharacterNavigator.TryGetStringFieldQuickly("tradition",
                                                ref strTemp);
                                            XmlNode xmlTraditionDataNode =
                                                xmlTraditionListDataNode.TryGetNodeByNameOrId("tradition", strTemp);
                                            if (xmlTraditionDataNode != null)
                                            {
                                                if (blnSync)
                                                {
                                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                    if (!_objTradition.Create(xmlTraditionDataNode))
                                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                        _objTradition.ResetTradition();
                                                }
                                                else if (!await _objTradition.CreateAsync(xmlTraditionDataNode, token: token).ConfigureAwait(false))
                                                    await _objTradition.ResetTraditionAsync(token).ConfigureAwait(false);
                                            }
                                            else
                                            {
                                                xmlTraditionDataNode =
                                                    xmlTraditionListDataNode.TryGetNodeByNameOrId("tradition", Tradition.CustomMagicalTraditionGuidString);
                                                if (xmlTraditionDataNode != null)
                                                {
                                                    if (blnSync)
                                                    {
                                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                        if (!_objTradition.Create(xmlTraditionDataNode))
                                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                            _objTradition.ResetTradition();
                                                    }
                                                    else if (!await _objTradition.CreateAsync(xmlTraditionDataNode, token: token).ConfigureAwait(false))
                                                        await _objTradition.ResetTraditionAsync(token).ConfigureAwait(false);
                                                }
                                            }
                                        }

                                        if (_objTradition.Type != TraditionType.None)
                                        {
                                            if (blnSync)
                                                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                _objTradition.LegacyLoad(xmlCharacterNavigator);
                                            else
                                                await _objTradition.LegacyLoadAsync(xmlCharacterNavigator, token).ConfigureAwait(false);
                                        }
                                    }
                                }

                                // Attempt to load Condition Monitor Progress.
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("physicalcmfilled",
                                                                              ref _intPhysicalCMFilled);
                                xmlCharacterNavigator.TryGetInt32FieldQuickly("stuncmfilled", ref _intStunCMFilled);

                                xmlCharacterNavigator.TryGetBoolFieldQuickly("psyche", ref _blnPsycheActive);
                                //Timekeeper.Finish("load_char_misc2");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(LanguageManager.GetString("Tab_Skills", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager
                                                                  .GetStringAsync("Tab_Skills", token: token)
                                                                  .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_skills", loadActivity)) //slightly messy
                            {
                                _oldSkillsBackup = objXmlCharacter["skills"]?.Clone();
                                _oldSkillGroupBackup = objXmlCharacter["skillgroups"]?.Clone();

                                XmlElement objSkillNode = objXmlCharacter["newskills"];
                                if (blnSync)
                                {
                                    if (objSkillNode != null)
                                    {
                                        // ReSharper disable once MethodHasAsyncOverload
                                        SkillsSection.Load(objSkillNode, false, loadActivity, token);
                                    }
                                    else
                                    {
                                        // ReSharper disable once MethodHasAsyncOverload
                                        SkillsSection.Load(objXmlCharacter, true, loadActivity, token);
                                    }
                                }
                                else if (objSkillNode != null)
                                {
                                    await SkillsSection.LoadAsync(objSkillNode, false, loadActivity, token)
                                                       .ConfigureAwait(false);
                                }
                                else
                                {
                                    await SkillsSection.LoadAsync(objXmlCharacter, true, loadActivity, token)
                                                       .ConfigureAwait(false);
                                }

                                //Timekeeper.Finish("load_char_skills");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("String_Locations", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager.GetStringAsync("String_Locations",
                                                                    token: token)
                                                                .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_loc", loadActivity))
                            {
                                // Locations.
                                objXmlLocationList = objXmlCharacter.SelectNodes("gearlocations/gearlocation");
                                foreach (XmlNode objXmlLocation in objXmlLocationList)
                                {
                                    Location objLocation = new Location(this, _lstGearLocations);
                                    if (blnSync)
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        objLocation.Load(objXmlLocation);
                                    else
                                        await objLocation.LoadAsync(objXmlLocation, token).ConfigureAwait(false);
                                }

                                objXmlLocationList = objXmlCharacter.SelectNodes("locations/location");
                                foreach (XmlNode objXmlLocation in objXmlLocationList)
                                {
                                    Location objLocation = new Location(this, _lstGearLocations);
                                    if (blnSync)
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        objLocation.Load(objXmlLocation);
                                    else
                                        await objLocation.LoadAsync(objXmlLocation, token).ConfigureAwait(false);
                                }

                                objXmlLocationList = objXmlCharacter.SelectNodes("gearlocations/location");
                                foreach (XmlNode objXmlLocation in objXmlLocationList)
                                {
                                    Location objLocation = new Location(this, _lstGearLocations);
                                    if (blnSync)
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        objLocation.Load(objXmlLocation);
                                    else
                                        await objLocation.LoadAsync(objXmlLocation, token).ConfigureAwait(false);
                                }

                                //Timekeeper.Finish("load_char_loc");
                            }

                            using (Timekeeper.StartSyncron("load_char_abundle", loadActivity))
                            {
                                // Armor Bundles.
                                objXmlLocationList = objXmlCharacter.SelectNodes("armorbundles/armorbundle");
                                foreach (XmlNode objXmlLocation in objXmlLocationList)
                                {
                                    Location objLocation = new Location(this, _lstArmorLocations);
                                    if (blnSync)
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        objLocation.Load(objXmlLocation);
                                    else
                                        await objLocation.LoadAsync(objXmlLocation, token).ConfigureAwait(false);
                                }

                                objXmlLocationList = objXmlCharacter.SelectNodes("armorlocations/armorlocation");
                                foreach (XmlNode objXmlLocation in objXmlLocationList)
                                {
                                    Location objLocation = new Location(this, _lstArmorLocations);
                                    if (blnSync)
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        objLocation.Load(objXmlLocation);
                                    else
                                        await objLocation.LoadAsync(objXmlLocation, token).ConfigureAwait(false);
                                }

                                objXmlLocationList = objXmlCharacter.SelectNodes("armorlocations/location");
                                foreach (XmlNode objXmlLocation in objXmlLocationList)
                                {
                                    Location objLocation = new Location(this, _lstArmorLocations);
                                    if (blnSync)
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        objLocation.Load(objXmlLocation);
                                    else
                                        await objLocation.LoadAsync(objXmlLocation, token).ConfigureAwait(false);
                                }

                                //Timekeeper.Finish("load_char_abundle");
                            }

                            using (Timekeeper.StartSyncron("load_char_vloc", loadActivity))
                            {
                                // Vehicle Locations.
                                XmlNodeList objXmlVehicleLocationList =
                                    objXmlCharacter.SelectNodes("vehiclelocations/vehiclelocation");
                                foreach (XmlNode objXmlLocation in objXmlVehicleLocationList)
                                {
                                    Location objLocation = new Location(this, _lstVehicleLocations);
                                    if (blnSync)
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        objLocation.Load(objXmlLocation);
                                    else
                                        await objLocation.LoadAsync(objXmlLocation, token).ConfigureAwait(false);
                                }

                                objXmlVehicleLocationList =
                                    objXmlCharacter.SelectNodes("vehiclelocations/location");
                                foreach (XmlNode objXmlLocation in objXmlVehicleLocationList)
                                {
                                    Location objLocation = new Location(this, _lstVehicleLocations);
                                    if (blnSync)
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        objLocation.Load(objXmlLocation);
                                    else
                                        await objLocation.LoadAsync(objXmlLocation, token).ConfigureAwait(false);
                                }

                                //Timekeeper.Finish("load_char_vloc");
                            }

                            using (Timekeeper.StartSyncron("load_char_wloc", loadActivity))
                            {
                                // Weapon Locations.
                                XmlNodeList objXmlWeaponLocationList =
                                    objXmlCharacter.SelectNodes("weaponlocations/weaponlocation");
                                foreach (XmlNode objXmlLocation in objXmlWeaponLocationList)
                                {
                                    Location objLocation = new Location(this, _lstWeaponLocations);
                                    if (blnSync)
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        objLocation.Load(objXmlLocation);
                                    else
                                        await objLocation.LoadAsync(objXmlLocation, token).ConfigureAwait(false);
                                }

                                objXmlWeaponLocationList = objXmlCharacter.SelectNodes("weaponlocations/location");
                                foreach (XmlNode objXmlLocation in objXmlWeaponLocationList)
                                {
                                    Location objLocation = new Location(this, _lstWeaponLocations);
                                    if (blnSync)
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        objLocation.Load(objXmlLocation);
                                    else
                                        await objLocation.LoadAsync(objXmlLocation, token).ConfigureAwait(false);
                                }

                                //Timekeeper.Finish("load_char_wloc");
                            }

                            using (Timekeeper.StartSyncron("load_char_sfoci", loadActivity))
                            {
                                // Stacked Foci.
                                objXmlNodeList = objXmlCharacter.SelectNodes("stackedfoci/stackedfocus");
                                foreach (XmlNode objXmlStack in objXmlNodeList)
                                {
                                    StackedFocus objStack = new StackedFocus(this);
                                    if (blnSync)
                                    {
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        objStack.Load(objXmlStack);
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        _lstStackedFoci.Add(objStack);
                                    }
                                    else
                                    {
                                        await objStack.LoadAsync(objXmlStack, token).ConfigureAwait(false);
                                        await _lstStackedFoci.AddAsync(objStack, token).ConfigureAwait(false);
                                    }
                                }

                                //Timekeeper.Finish("load_char_sfoci");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(LanguageManager.GetString("Tab_Armor", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager
                                                                  .GetStringAsync("Tab_Armor", token: token)
                                                                  .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_armor", loadActivity))
                            {
                                // Armor.
                                objXmlNodeList = objXmlCharacter.SelectNodes("armors/armor");
                                foreach (XmlNode objXmlArmor in objXmlNodeList)
                                {
                                    Armor objArmor = new Armor(this);
                                    if (blnSync)
                                    {
                                        try
                                        {
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objArmor.Load(objXmlArmor);
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            _lstArmor.Add(objArmor);
                                        }
                                        catch
                                        {
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objArmor.DeleteArmor();
                                            throw;
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            await objArmor.LoadAsync(objXmlArmor, token: token).ConfigureAwait(false);
                                            await _lstArmor.AddAsync(objArmor, token).ConfigureAwait(false);
                                        }
                                        catch
                                        {
                                            await objArmor.DeleteArmorAsync(token: CancellationToken.None).ConfigureAwait(false);
                                            throw;
                                        }
                                    }
                                }

                                //Timekeeper.Finish("load_char_armor");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(LanguageManager.GetString("Tab_Drugs", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager
                                                                  .GetStringAsync("Tab_Drugs", token: token)
                                                                  .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_drugs", loadActivity))
                            {
                                // Drugs.
                                objXmlNodeList = objXmlCharacter.SelectNodes("drugs/drug");
                                foreach (XmlNode objXmlDrug in objXmlNodeList)
                                {
                                    Drug objDrug = new Drug(this);
                                    if (blnSync)
                                    {
                                        try
                                        {
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objDrug.Load(objXmlDrug);
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            _lstDrugs.Add(objDrug);
                                        }
                                        catch
                                        {
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objDrug.Remove(false);
                                            throw;
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            await objDrug.LoadAsync(objXmlDrug, token).ConfigureAwait(false);
                                            await _lstDrugs.AddAsync(objDrug, token).ConfigureAwait(false);
                                        }
                                        catch
                                        {
                                            await objDrug.RemoveAsync(false, token).ConfigureAwait(false);
                                            throw;
                                        }
                                    }
                                }

                                //Timekeeper.Finish("load_char_drugs");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("Tab_Cyberware", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager
                                                                  .GetStringAsync("Tab_Cyberware", token: token)
                                                                  .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_ware", loadActivity))
                            {
                                // Cyberware/Bioware.
                                objXmlNodeList = objXmlCharacter.SelectNodes("cyberwares/cyberware");
                                // Dictionary for instantly re-applying outdated improvements for 'ware with pair bonuses in legacy shim
                                Dictionary<Cyberware, int> dicPairableCyberwares =
                                    new Dictionary<Cyberware, int>(objXmlNodeList.Count);
                                foreach (XmlNode objXmlCyberware in objXmlNodeList)
                                {
                                    Cyberware objCyberware = new Cyberware(this);
                                    if (blnSync)
                                    {
                                        try
                                        {
                                            // ReSharper disable once MethodHasAsyncOverload
                                            objCyberware.Load(objXmlCyberware, token: token);
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            _lstCyberware.Add(objCyberware);
                                        }
                                        catch
                                        {
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objCyberware.DeleteCyberware();
                                            throw;
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            await objCyberware.LoadAsync(objXmlCyberware, token: token).ConfigureAwait(false);
                                            await _lstCyberware.AddAsync(objCyberware, token).ConfigureAwait(false);
                                        }
                                        catch
                                        {
                                            await objCyberware.DeleteCyberwareAsync(token: CancellationToken.None).ConfigureAwait(false);
                                            throw;
                                        }
                                    }

                                    // Legacy shim #1
                                    if (objCyberware.Name == "Myostatin Inhibitor" &&
                                        LastSavedVersion <= new ValueVersion(5, 195, 1) &&
                                        !(blnSync
                                            // ReSharper disable once MethodHasAsyncOverload
                                            ? Improvements.Any(x =>
                                                                   x.SourceName == objCyberware.InternalId &&
                                                                   x.ImproveType == Improvement.ImprovementType
                                                                       .AttributeKarmaCost, token)
                                            : await Improvements.AnyAsync(x =>
                                                                              x.SourceName == objCyberware.InternalId &&
                                                                              x.ImproveType == Improvement
                                                                                  .ImprovementType
                                                                                  .AttributeKarmaCost, token).ConfigureAwait(false)))
                                    {
                                        XmlNode objNode = blnSync
                                            // ReSharper disable once MethodHasAsyncOverload
                                            ? objCyberware.GetNode(token: token)
                                            : await objCyberware.GetNodeAsync(token: token).ConfigureAwait(false);
                                        if (objNode != null)
                                        {
                                            using (TemporaryStringArray aParams = new TemporaryStringArray(objCyberware.InternalId, objCyberware.InternalId + "Pair"))
                                            {
                                                if (blnSync)
                                                {
                                                    // ReSharper disable once MethodHasAsyncOverload
                                                    ImprovementManager.RemoveImprovements(this, objCyberware.SourceType,
                                                        aParams, token: token);
                                                }
                                                else
                                                {
                                                    await ImprovementManager.RemoveImprovementsAsync(
                                                        this, objCyberware.SourceType,
                                                        aParams, token: token).ConfigureAwait(false);
                                                }
                                            }

                                            objCyberware.Bonus = objNode["bonus"];
                                            objCyberware.WirelessBonus = objNode["wirelessbonus"];
                                            objCyberware.PairBonus = objNode["pairbonus"];
                                            if (!string.IsNullOrEmpty(objCyberware.Forced) &&
                                                objCyberware.Forced != "Right" &&
                                                objCyberware.Forced != "Left")
                                                ImprovementManager.SetForcedValue(objCyberware.Forced, this);
                                            if (objCyberware.Bonus != null)
                                            {
                                                if (blnSync)
                                                    // ReSharper disable once MethodHasAsyncOverload
                                                    ImprovementManager.CreateImprovements(this, objCyberware.SourceType,
                                                        objCyberware.InternalId, objCyberware.Bonus,
                                                        objCyberware.Rating,
                                                        objCyberware.CurrentDisplayNameShort, token: token);
                                                else
                                                    await ImprovementManager.CreateImprovementsAsync(
                                                                                this, objCyberware.SourceType,
                                                                                objCyberware.InternalId,
                                                                                objCyberware.Bonus,
                                                                                await objCyberware.GetRatingAsync(token)
                                                                                    .ConfigureAwait(false),
                                                                                await objCyberware
                                                                                    .GetCurrentDisplayNameShortAsync(
                                                                                        token)
                                                                                    .ConfigureAwait(false),
                                                                                token: token)
                                                                            .ConfigureAwait(false);
                                                string strSelectedValue =
                                                    ImprovementManager.GetSelectedValue(this);
                                                if (!string.IsNullOrEmpty(strSelectedValue))
                                                    objCyberware.Extra = strSelectedValue;
                                            }

                                            if (objCyberware.WirelessOn && objCyberware.WirelessBonus != null)
                                            {
                                                if (blnSync)
                                                    // ReSharper disable once MethodHasAsyncOverload
                                                    ImprovementManager.CreateImprovements(this, objCyberware.SourceType,
                                                        objCyberware.InternalId, objCyberware.WirelessBonus,
                                                        objCyberware.Rating,
                                                        objCyberware.CurrentDisplayNameShort, token: token);
                                                else
                                                    await ImprovementManager.CreateImprovementsAsync(
                                                                                this, objCyberware.SourceType,
                                                                                objCyberware.InternalId,
                                                                                objCyberware.WirelessBonus,
                                                                                await objCyberware.GetRatingAsync(token)
                                                                                    .ConfigureAwait(false),
                                                                                await objCyberware
                                                                                    .GetCurrentDisplayNameShortAsync(
                                                                                        token)
                                                                                    .ConfigureAwait(false),
                                                                                token: token)
                                                                            .ConfigureAwait(false);
                                                string strSelectedValue = ImprovementManager.GetSelectedValue(this);
                                                if (!string.IsNullOrEmpty(strSelectedValue) &&
                                                    string.IsNullOrEmpty(objCyberware.Extra))
                                                    objCyberware.Extra = strSelectedValue;
                                            }

                                            if (!(blnSync ? objCyberware.IsModularCurrentlyEquipped : await objCyberware.GetIsModularCurrentlyEquippedAsync(token).ConfigureAwait(false)))
                                            {
                                                if (blnSync)
                                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                    objCyberware.ChangeModularEquip(false);
                                                else
                                                    await objCyberware.ChangeModularEquipAsync(false, token: token)
                                                                      .ConfigureAwait(false);
                                            }
                                            else if (objCyberware.PairBonus != null)
                                            {
                                                Cyberware objMatchingCyberware =
                                                    dicPairableCyberwares.Keys.FirstOrDefault(
                                                        x =>
                                                            x.Name == objCyberware.Name &&
                                                            x.Extra == objCyberware.Extra);
                                                if (objMatchingCyberware != null)
                                                    ++dicPairableCyberwares[objMatchingCyberware];
                                                else
                                                    dicPairableCyberwares.Add(objCyberware, 1);
                                            }
                                        }
                                        else
                                            lstInternalIdsNeedingReapplyImprovements.Add(objCyberware.InternalId);
                                    }
                                }

                                // Legacy Shim #2 (needed to be separate because we're dealing with PairBonuses here, and we don't know if something needs its PairBonus reapplied until all Cyberwares have been loaded)
                                if (LastSavedVersion <= new ValueVersion(5, 200, 0))
                                {
                                    if (blnSync)
                                    {
                                        // ReSharper disable MethodHasAsyncOverload
                                        Cyberware.ForEach(objCyberware =>
                                        {
                                            if (objCyberware.PairBonus?.HasChildNodes == true &&
                                                !Cyberware.DeepAny(x => x.Children, x =>
                                                {
                                                    if (!objCyberware.IncludePair.Contains(x.Name) ||
                                                        x.Extra != objCyberware.Extra ||
                                                        !x.IsModularCurrentlyEquipped)
                                                        return false;
                                                    string strToMatch = x.InternalId + "Pair";
                                                    return Improvements.Any(y => y.SourceName == strToMatch, token);
                                                }))
                                            {
                                                XmlNode objNode = objCyberware.GetNode(token: token);
                                                if (objNode != null)
                                                {
                                                    using (TemporaryStringArray aParams = new TemporaryStringArray(objCyberware.InternalId, objCyberware.InternalId + "Pair"))
                                                    {
                                                        ImprovementManager.RemoveImprovements(this, objCyberware.SourceType, aParams, token: token);
                                                    }

                                                    objCyberware.Bonus = objNode["bonus"];
                                                    objCyberware.WirelessBonus = objNode["wirelessbonus"];
                                                    objCyberware.PairBonus = objNode["pairbonus"];
                                                    if (!string.IsNullOrEmpty(objCyberware.Forced) &&
                                                        objCyberware.Forced != "Right" &&
                                                        objCyberware.Forced != "Left")
                                                        ImprovementManager.SetForcedValue(objCyberware.Forced, this);
                                                    if (objCyberware.Bonus != null)
                                                    {
                                                        ImprovementManager.CreateImprovements(this,
                                                            objCyberware.SourceType,
                                                            objCyberware.InternalId, objCyberware.Bonus,
                                                            objCyberware.Rating,
                                                            objCyberware.CurrentDisplayNameShort, token: token);
                                                        string strSelectedValue =
                                                            ImprovementManager.GetSelectedValue(this);
                                                        if (!string.IsNullOrEmpty(strSelectedValue))
                                                            objCyberware.Extra = strSelectedValue;
                                                    }

                                                    if (objCyberware.WirelessOn && objCyberware.WirelessBonus != null)
                                                    {
                                                        ImprovementManager.CreateImprovements(this,
                                                            objCyberware.SourceType,
                                                            objCyberware.InternalId, objCyberware.WirelessBonus,
                                                            objCyberware.Rating,
                                                            objCyberware.CurrentDisplayNameShort, token: token);
                                                        string strSelectedValue = ImprovementManager.GetSelectedValue(this);
                                                        if (!string.IsNullOrEmpty(strSelectedValue) &&
                                                            string.IsNullOrEmpty(objCyberware.Extra))
                                                            objCyberware.Extra = strSelectedValue;
                                                    }

                                                    if (!objCyberware.IsModularCurrentlyEquipped)
                                                    {
                                                        objCyberware.ChangeModularEquip(false);
                                                    }
                                                    else if (objCyberware.PairBonus != null)
                                                    {
                                                        Cyberware objMatchingCyberware =
                                                            dicPairableCyberwares.Keys.FirstOrDefault(
                                                                x =>
                                                                    x.Name == objCyberware.Name &&
                                                                    x.Extra == objCyberware.Extra);
                                                        if (objMatchingCyberware != null)
                                                            ++dicPairableCyberwares[objMatchingCyberware];
                                                        else
                                                            dicPairableCyberwares.Add(objCyberware, 1);
                                                    }
                                                }
                                                else
                                                    lstInternalIdsNeedingReapplyImprovements.Add(objCyberware.InternalId);
                                            }
                                        }, token);
                                        // ReSharper restore MethodHasAsyncOverload
                                    }
                                    else
                                    {
                                        await (await GetCyberwareAsync(token).ConfigureAwait(false)).ForEachAsync(async objCyberware =>
                                        {
                                            if (objCyberware.PairBonus?.HasChildNodes == true &&
                                                !await (await GetCyberwareAsync(token).ConfigureAwait(false)).DeepAnyAsync(x => x.GetChildrenAsync(token), async x =>
                                                {
                                                    if (!objCyberware.IncludePair.Contains(x.Name) ||
                                                        x.Extra != objCyberware.Extra ||
                                                        !await x.GetIsModularCurrentlyEquippedAsync(token).ConfigureAwait(false))
                                                        return false;
                                                    string strToMatch = x.InternalId + "Pair";
                                                    return await Improvements.AnyAsync(y => y.SourceName == strToMatch, token).ConfigureAwait(false);
                                                }, token).ConfigureAwait(false))
                                            {
                                                XmlNode objNode = await objCyberware.GetNodeAsync(token: token).ConfigureAwait(false);
                                                if (objNode != null)
                                                {
                                                    using (TemporaryStringArray aParams = new TemporaryStringArray(objCyberware.InternalId, objCyberware.InternalId + "Pair"))
                                                    {
                                                        await ImprovementManager.RemoveImprovementsAsync(this, objCyberware.SourceType, aParams, token: token).ConfigureAwait(false);
                                                    }
                                                    objCyberware.Bonus = objNode["bonus"];
                                                    objCyberware.WirelessBonus = objNode["wirelessbonus"];
                                                    objCyberware.PairBonus = objNode["pairbonus"];
                                                    if (!string.IsNullOrEmpty(objCyberware.Forced) &&
                                                        objCyberware.Forced != "Right" &&
                                                        objCyberware.Forced != "Left")
                                                        ImprovementManager.SetForcedValue(objCyberware.Forced, this);
                                                    if (objCyberware.Bonus != null)
                                                    {
                                                        await ImprovementManager.CreateImprovementsAsync(this,
                                                                objCyberware.SourceType,
                                                                objCyberware.InternalId, objCyberware.Bonus,
                                                                await objCyberware.GetRatingAsync(token)
                                                                    .ConfigureAwait(false),
                                                                await objCyberware
                                                                      .GetCurrentDisplayNameShortAsync(token)
                                                                      .ConfigureAwait(false), token: token)
                                                            .ConfigureAwait(false);
                                                        string strSelectedValue =
                                                            ImprovementManager.GetSelectedValue(this);
                                                        if (!string.IsNullOrEmpty(strSelectedValue))
                                                            objCyberware.Extra = strSelectedValue;
                                                    }

                                                    if (objCyberware.WirelessOn && objCyberware.WirelessBonus != null)
                                                    {
                                                        await ImprovementManager.CreateImprovementsAsync(this,
                                                                objCyberware.SourceType,
                                                                objCyberware.InternalId, objCyberware.WirelessBonus,
                                                                await objCyberware.GetRatingAsync(token)
                                                                    .ConfigureAwait(false),
                                                                await objCyberware
                                                                      .GetCurrentDisplayNameShortAsync(token)
                                                                      .ConfigureAwait(false), token: token)
                                                            .ConfigureAwait(false);
                                                        string strSelectedValue = ImprovementManager.GetSelectedValue(this);
                                                        if (!string.IsNullOrEmpty(strSelectedValue) &&
                                                            string.IsNullOrEmpty(objCyberware.Extra))
                                                            objCyberware.Extra = strSelectedValue;
                                                    }

                                                    if (!await objCyberware.GetIsModularCurrentlyEquippedAsync(token).ConfigureAwait(false))
                                                    {
                                                        await objCyberware.ChangeModularEquipAsync(false, token: token)
                                                                          .ConfigureAwait(false);
                                                    }
                                                    else if (objCyberware.PairBonus != null)
                                                    {
                                                        Cyberware objMatchingCyberware =
                                                            dicPairableCyberwares.Keys.FirstOrDefault(
                                                                x =>
                                                                    x.Name == objCyberware.Name &&
                                                                    x.Extra == objCyberware.Extra);
                                                        if (objMatchingCyberware != null)
                                                            ++dicPairableCyberwares[objMatchingCyberware];
                                                        else
                                                            dicPairableCyberwares.Add(objCyberware, 1);
                                                    }
                                                }
                                                else
                                                    lstInternalIdsNeedingReapplyImprovements.Add(objCyberware.InternalId);
                                            }
                                        }, token).ConfigureAwait(false);
                                    }
                                }

                                // Separate Pass for PairBonuses
                                foreach (KeyValuePair<Cyberware, int> objItem in dicPairableCyberwares)
                                {
                                    Cyberware objCyberware = objItem.Key;
                                    int intCyberwaresCount = objItem.Value;
                                    List<Cyberware> lstPairableCyberwares = blnSync
                                        ? Cyberware.DeepWhere(x => x.Children,
                                                              x => objCyberware.IncludePair.Contains(x.Name) &&
                                                                   x.Extra == objCyberware.Extra &&
                                                                   x.IsModularCurrentlyEquipped, token).ToList()
                                        : await (await GetCyberwareAsync(token).ConfigureAwait(false)).DeepWhereAsync(x => x.GetChildrenAsync(token),
                                                                         async x => objCyberware.IncludePair.Contains(x.Name)
                                                                              && x.Extra == objCyberware.Extra
                                                                              && await x.GetIsModularCurrentlyEquippedAsync(token).ConfigureAwait(false), token).ConfigureAwait(false);
                                    // Need to use slightly different logic if this cyberware has a location (Left or Right) and only pairs with itself because Lefts can only be paired with Rights and Rights only with Lefts
                                    if (!string.IsNullOrEmpty(objCyberware.Location) &&
                                        objCyberware.IncludePair.All(x => x == objCyberware.Name))
                                    {
                                        int intMatchLocationCount = 0;
                                        int intNotMatchLocationCount = 0;
                                        foreach (Cyberware objPairableCyberware in lstPairableCyberwares)
                                        {
                                            if (objPairableCyberware.Location != objCyberware.Location)
                                                ++intNotMatchLocationCount;
                                            else
                                                ++intMatchLocationCount;
                                        }

                                        // Set the count to the total number of cyberwares in matching pairs, which would mean 2x the number of whichever location contains the fewest members (since every single one of theirs would have a pair)
                                        intCyberwaresCount =
                                            Math.Min(intNotMatchLocationCount, intMatchLocationCount) *
                                            2;
                                    }

                                    if (intCyberwaresCount > 0)
                                    {
                                        foreach (Cyberware objLoopCyberware in lstPairableCyberwares)
                                        {
                                            if ((intCyberwaresCount & 1) == 0)
                                            {
                                                if (!string.IsNullOrEmpty(objCyberware.Forced) &&
                                                    objCyberware.Forced != "Right" &&
                                                    objCyberware.Forced != "Left")
                                                    ImprovementManager.SetForcedValue(objCyberware.Forced, this);
                                                if (blnSync)
                                                    // ReSharper disable once MethodHasAsyncOverload
                                                    ImprovementManager.CreateImprovements(this,
                                                        objLoopCyberware.SourceType,
                                                        objLoopCyberware.InternalId + "Pair",
                                                        objLoopCyberware.PairBonus,
                                                        objLoopCyberware.Rating,
                                                        objLoopCyberware.CurrentDisplayNameShort, token: token);
                                                else
                                                    await ImprovementManager.CreateImprovementsAsync(this,
                                                                                objLoopCyberware.SourceType,
                                                                                objLoopCyberware.InternalId + "Pair",
                                                                                objLoopCyberware.PairBonus,
                                                                                await objLoopCyberware
                                                                                    .GetRatingAsync(token)
                                                                                    .ConfigureAwait(false),
                                                                                await objLoopCyberware
                                                                                    .GetCurrentDisplayNameShortAsync(
                                                                                        token)
                                                                                    .ConfigureAwait(false),
                                                                                token: token)
                                                                            .ConfigureAwait(false);
                                                string strSelectedValue = ImprovementManager.GetSelectedValue(this);
                                                if (!string.IsNullOrEmpty(strSelectedValue) &&
                                                    string.IsNullOrEmpty(objCyberware.Extra))
                                                    objCyberware.Extra = strSelectedValue;
                                            }

                                            --intCyberwaresCount;
                                            if (intCyberwaresCount <= 0)
                                                break;
                                        }
                                    }
                                }

                                //Timekeeper.Finish("load_char_ware");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("Label_SelectedSpells", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager.GetStringAsync("Label_SelectedSpells",
                                                                    token: token)
                                                                .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_spells", loadActivity))
                            {
                                // Spells.
                                objXmlNodeList = objXmlCharacter.SelectNodes("spells/spell");
                                foreach (XmlNode objXmlSpell in objXmlNodeList)
                                {
                                    Spell objSpell = new Spell(this);
                                    try
                                    {
                                        if (blnSync)
                                        {
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objSpell.Load(objXmlSpell);
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            _lstSpells.Add(objSpell);
                                        }
                                        else
                                        {
                                            await objSpell.LoadAsync(objXmlSpell, token).ConfigureAwait(false);
                                            await _lstSpells.AddAsync(objSpell, token).ConfigureAwait(false);
                                        }
                                    }
                                    catch
                                    {
                                        if (blnSync)
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objSpell.Remove(false);
                                        else
                                            await objSpell.RemoveAsync(false, CancellationToken.None).ConfigureAwait(false);
                                        throw;
                                    }
                                }
                                //Timekeeper.Finish("load_char_spells");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(LanguageManager.GetString("Tab_Adept", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager
                                                                  .GetStringAsync("Tab_Adept", token: token)
                                                                  .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_powers", loadActivity))
                            {
                                // Powers.
                                objXmlNodeList = objXmlCharacter.SelectNodes("powers/power");
                                if (objXmlNodeList.Count > 0)
                                {
                                    using (new FetchSafelyFromSafeObjectPool<List<ListItem>>(
                                               Utils.ListItemListPool, out List<ListItem> lstPowerOrder))
                                    {
                                        bool blnDoEnhancedAccuracyRefresh =
                                            LastSavedVersion <= new ValueVersion(5, 198, 26);
                                        // Sort the Powers in alphabetical order.
                                        foreach (XmlNode xmlPower in objXmlNodeList)
                                        {
                                            string strGuid = xmlPower["guid"]?.InnerTextViaPool(token);
                                            string strPowerName = xmlPower["name"]?.InnerTextViaPool(token) ?? string.Empty;
                                            if (blnDoEnhancedAccuracyRefresh
                                                && strPowerName == "Enhanced Accuracy (skill)")
                                            {
                                                lstInternalIdsNeedingReapplyImprovements.Add(strGuid);
                                            }

                                            if (!string.IsNullOrEmpty(strGuid))
                                                lstPowerOrder.Add(new ListItem(strGuid,
                                                                               strPowerName
                                                                               + (xmlPower["extra"]?.InnerTextViaPool(token)
                                                                                   ?? string.Empty)));
                                            else
                                            {
                                                Power objPower = new Power(this);
                                                if (blnSync)
                                                {
                                                    try
                                                    {
                                                        // ReSharper disable once MethodHasAsyncOverload
                                                        objPower.Load(xmlPower, token);
                                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                        _lstPowers.Add(objPower);
                                                    }
                                                    catch
                                                    {
                                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                        objPower.DeletePower();
                                                        throw;
                                                    }
                                                }
                                                else
                                                {
                                                    try
                                                    {
                                                        await objPower.LoadAsync(xmlPower, token).ConfigureAwait(false);
                                                        await _lstPowers.AddAsync(objPower, token).ConfigureAwait(false);
                                                    }
                                                    catch
                                                    {
                                                        await objPower.DeletePowerAsync(CancellationToken.None).ConfigureAwait(false);
                                                        throw;
                                                    }
                                                }
                                            }
                                        }

                                        lstPowerOrder.Sort(CompareListItems.CompareNames);

                                        foreach (ListItem objItem in lstPowerOrder)
                                        {
                                            XmlNode objNode =
                                                objXmlCharacter.SelectSingleNode(
                                                    "powers/power[guid = " + objItem.Value.ToString().CleanXPath()
                                                                           + "]");
                                            if (objNode != null)
                                            {
                                                Power objPower = new Power(this);
                                                if (blnSync)
                                                {
                                                    try
                                                    {
                                                        // ReSharper disable once MethodHasAsyncOverload
                                                        objPower.Load(objNode, token);
                                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                        _lstPowers.Add(objPower);
                                                    }
                                                    catch
                                                    {
                                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                        objPower.DeletePower();
                                                        throw;
                                                    }
                                                }
                                                else
                                                {
                                                    try
                                                    {
                                                        await objPower.LoadAsync(objNode, token).ConfigureAwait(false);
                                                        await _lstPowers.AddAsync(objPower, token).ConfigureAwait(false);
                                                    }
                                                    catch
                                                    {
                                                        await objPower.DeletePowerAsync(CancellationToken.None).ConfigureAwait(false);
                                                        throw;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                //Timekeeper.Finish("load_char_powers");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("Label_Spirits", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager
                                                                  .GetStringAsync("Label_Spirits", token: token)
                                                                  .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_spirits", loadActivity))
                            {
                                // Spirits/Sprites.
                                foreach (XPathNavigator xmlSpirit in xmlCharacterNavigator.SelectAndCacheExpression("spirits/spirit", token))
                                {
                                    Spirit objSpirit = new Spirit(this);
                                    if (blnSync)
                                    {
                                        try
                                        {
                                            // ReSharper disable once MethodHasAsyncOverload
                                            objSpirit.Load(xmlSpirit, token);
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            _lstSpirits.Add(objSpirit);
                                        }
                                        catch
                                        {
                                            try
                                            {
                                                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                _lstSpirits.Remove(objSpirit);
                                            }
                                            catch
                                            {
                                                //swallow this
                                            }
                                            // ReSharper disable once MethodHasAsyncOverload
                                            objSpirit.Dispose();
                                            throw;
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            await objSpirit.LoadAsync(xmlSpirit, token).ConfigureAwait(false);
                                            await _lstSpirits.AddAsync(objSpirit, token).ConfigureAwait(false);
                                        }
                                        catch
                                        {
                                            try
                                            {
                                                await _lstSpirits.RemoveAsync(objSpirit, token: token).ConfigureAwait(false);
                                            }
                                            catch
                                            {
                                                //swallow this
                                            }
                                            await objSpirit.DisposeAsync().ConfigureAwait(false);
                                            throw;
                                        }
                                    }
                                }

                                // If we don't have any Fettered spirits, make sure that we
                                if (blnSync)
                                {
                                    // ReSharper disable once MethodHasAsyncOverload
                                    if (!_lstSpirits.Any(s => s.Fettered, token)
                                        // ReSharper disable once MethodHasAsyncOverload
                                        && Improvements.Any(imp => imp.ImproveSource == Improvement.ImprovementSource.SpiritFettering, token))
                                    {
                                        // ReSharper disable once MethodHasAsyncOverload
                                        ImprovementManager.RemoveImprovements(
                                            this, Improvement.ImprovementSource.SpiritFettering, token: token);
                                    }
                                }
                                else
                                {
                                    if (!await _lstSpirits.AnyAsync(s => s.GetFetteredAsync(token), token).ConfigureAwait(false)
                                        && await Improvements
                                                 .AnyAsync(
                                                     imp => imp.ImproveSource
                                                            == Improvement.ImprovementSource.SpiritFettering, token)
                                                 .ConfigureAwait(false))
                                    {
                                        await ImprovementManager.RemoveImprovementsAsync(this,
                                                                    Improvement.ImprovementSource.SpiritFettering,
                                                                    token: token)
                                                                .ConfigureAwait(false);
                                    }
                                }

                                //Timekeeper.Finish("load_char_spirits");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("Label_ComplexForms", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager.GetStringAsync("Label_ComplexForms",
                                                                    token: token)
                                                                .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_complex", loadActivity))
                            {
                                // Complex Forms/Technomancer Programs.
                                objXmlNodeList = objXmlCharacter.SelectNodes("complexforms/complexform");
                                foreach (XmlNode objXmlComplexForm in objXmlNodeList)
                                {
                                    ComplexForm objComplexForm = new ComplexForm(this);
                                    try
                                    {
                                        if (blnSync)
                                        {
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objComplexForm.Load(objXmlComplexForm);
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            _lstComplexForms.Add(objComplexForm);
                                        }
                                        else
                                        {
                                            await objComplexForm.LoadAsync(objXmlComplexForm, token).ConfigureAwait(false);
                                            await _lstComplexForms.AddAsync(objComplexForm, token).ConfigureAwait(false);
                                        }
                                    }
                                    catch
                                    {
                                        if (blnSync)
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objComplexForm.Remove(false);
                                        else
                                            await objComplexForm.RemoveAsync(false, CancellationToken.None).ConfigureAwait(false);
                                        throw;
                                    }
                                }

                                //Timekeeper.Finish("load_char_complex");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("Tab_AdvancedPrograms", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager.GetStringAsync("Tab_AdvancedPrograms",
                                                                    token: token)
                                                                .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_aiprogram", loadActivity))
                            {
                                // Complex Forms/Technomancer Programs.
                                objXmlNodeList = objXmlCharacter.SelectNodes("aiprograms/aiprogram");
                                foreach (XmlNode objXmlProgram in objXmlNodeList)
                                {
                                    AIProgram objProgram = new AIProgram(this);
                                    try
                                    {
                                        if (blnSync)
                                        {
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objProgram.Load(objXmlProgram);
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            _lstAIPrograms.Add(objProgram);
                                        }
                                        else
                                        {
                                            await objProgram.LoadAsync(objXmlProgram, token).ConfigureAwait(false);
                                            await _lstAIPrograms.AddAsync(objProgram, token).ConfigureAwait(false);
                                        }
                                    }
                                    catch
                                    {
                                        if (blnSync)
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objProgram.Remove(false);
                                        else
                                            await objProgram.RemoveAsync(false, CancellationToken.None).ConfigureAwait(false);
                                        throw;
                                    }
                                }

                                //Timekeeper.Finish("load_char_aiprogram");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("Tab_MartialArts", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager.GetStringAsync("Tab_MartialArts",
                                                                    token: token)
                                                                .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_martialarts", loadActivity))
                            {
                                // Martial Arts.
                                objXmlNodeList = objXmlCharacter.SelectNodes("martialarts/martialart");
                                foreach (XmlNode objXmlArt in objXmlNodeList)
                                {
                                    MartialArt objMartialArt = new MartialArt(this);
                                    try
                                    {
                                        if (blnSync)
                                        {
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objMartialArt.Load(objXmlArt);
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            _lstMartialArts.Add(objMartialArt);
                                        }
                                        else
                                        {
                                            await objMartialArt.LoadAsync(objXmlArt, token).ConfigureAwait(false);
                                            await _lstMartialArts.AddAsync(objMartialArt, token).ConfigureAwait(false);
                                        }
                                    }
                                    catch
                                    {
                                        if (blnSync)
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objMartialArt.DeleteMartialArt();
                                        else
                                            await objMartialArt.DeleteMartialArtAsync(token: CancellationToken.None).ConfigureAwait(false);
                                        throw;
                                    }
                                }

                                //Timekeeper.Finish("load_char_marts");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(LanguageManager.GetString("Tab_Limits", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager
                                                                  .GetStringAsync("Tab_Limits", token: token)
                                                                  .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_mod", loadActivity))
                            {
                                // Limit Modifiers.
                                objXmlNodeList = objXmlCharacter.SelectNodes("limitmodifiers/limitmodifier");
                                foreach (XmlNode objXmlLimit in objXmlNodeList)
                                {
                                    LimitModifier objLimitModifier = new LimitModifier(this);
                                    if (blnSync)
                                    {
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        objLimitModifier.Load(objXmlLimit);
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        _lstLimitModifiers.Add(objLimitModifier);
                                    }
                                    else
                                    {
                                        await objLimitModifier.LoadAsync(objXmlLimit, token).ConfigureAwait(false);
                                        await _lstLimitModifiers.AddAsync(objLimitModifier, token).ConfigureAwait(false);
                                    }
                                }

                                //Timekeeper.Finish("load_char_mod");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("String_SelectPACKSKit_Lifestyles", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                        await LanguageManager
                                              .GetStringAsync("String_SelectPACKSKit_Lifestyles", token: token)
                                              .ConfigureAwait(false), token: token).ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_lifestyle", loadActivity))
                            {
                                // Lifestyles.
                                objXmlNodeList = objXmlCharacter.SelectNodes("lifestyles/lifestyle");
                                foreach (XmlNode objXmlLifestyle in objXmlNodeList)
                                {
                                    Lifestyle objLifestyle = new Lifestyle(this);
                                    try
                                    {
                                        if (blnSync)
                                        {
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objLifestyle.Load(objXmlLifestyle);
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            _lstLifestyles.Add(objLifestyle);
                                        }
                                        else
                                        {
                                            await objLifestyle.LoadAsync(objXmlLifestyle, token: token).ConfigureAwait(false);
                                            await _lstLifestyles.AddAsync(objLifestyle, token).ConfigureAwait(false);
                                        }
                                    }
                                    catch
                                    {
                                        if (blnSync)
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objLifestyle.Remove(false);
                                        else
                                            await objLifestyle.RemoveAsync(false, CancellationToken.None).ConfigureAwait(false);
                                        throw;
                                    }
                                }

                                //Timekeeper.Finish("load_char_lifestyle");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(LanguageManager.GetString("Tab_Gear", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager
                                                                  .GetStringAsync("Tab_Gear", token: token)
                                                                  .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_gear", loadActivity))
                            {
                                // <gears>
                                objXmlNodeList = objXmlCharacter.SelectNodes("gears/gear");
                                foreach (XmlNode objXmlGear in objXmlNodeList)
                                {
                                    Gear objGear = new Gear(this);
                                    if (blnSync)
                                    {
                                        try
                                        {
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objGear.Load(objXmlGear);
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            _lstGear.Add(objGear);
                                        }
                                        catch
                                        {
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objGear.DeleteGear();
                                            throw;
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            await objGear.LoadAsync(objXmlGear, token: token).ConfigureAwait(false);
                                            await _lstGear.AddAsync(objGear, token).ConfigureAwait(false);
                                        }
                                        catch
                                        {
                                            await objGear.DeleteGearAsync(token: CancellationToken.None).ConfigureAwait(false);
                                            throw;
                                        }
                                    }
                                }

                                // If the character has a technomancer quality but no Living Persona commlink, its improvements get re-applied immediately
                                if (objLivingPersonaQuality != null && LastSavedVersion <= new ValueVersion(5, 195, 1))
                                {
                                    if (blnSync)
                                        // ReSharper disable once MethodHasAsyncOverload
                                        ImprovementManager.RemoveImprovements(this,
                                                                              Improvement.ImprovementSource.Quality,
                                                                              objLivingPersonaQuality.InternalId, token: token);
                                    else
                                        await ImprovementManager.RemoveImprovementsAsync(this,
                                            Improvement.ImprovementSource.Quality,
                                            objLivingPersonaQuality.InternalId, token: token).ConfigureAwait(false);

                                    XmlNode objNode = blnSync
                                        // ReSharper disable once MethodHasAsyncOverload
                                        ? objLivingPersonaQuality.GetNode(token: token)
                                        : await objLivingPersonaQuality.GetNodeAsync(token: token)
                                                                       .ConfigureAwait(false);
                                    if (objNode != null)
                                    {
                                        objLivingPersonaQuality.Bonus = objNode["bonus"];
                                        if (objLivingPersonaQuality.Bonus != null)
                                        {
                                            ImprovementManager.SetForcedValue(objLivingPersonaQuality.Extra, this);
                                            if (blnSync)
                                                // ReSharper disable once MethodHasAsyncOverload
                                                ImprovementManager.CreateImprovements(this,
                                                    Improvement.ImprovementSource.Quality,
                                                    objLivingPersonaQuality.InternalId,
                                                    objLivingPersonaQuality
                                                        .Bonus, 1,
                                                    objLivingPersonaQuality.CurrentDisplayNameShort, token: token);
                                            else
                                                await ImprovementManager.CreateImprovementsAsync(this,
                                                    Improvement.ImprovementSource.Quality,
                                                    objLivingPersonaQuality.InternalId,
                                                    objLivingPersonaQuality
                                                        .Bonus, 1,
                                                    await objLivingPersonaQuality.GetCurrentDisplayNameShortAsync(token)
                                                        .ConfigureAwait(false), token: token).ConfigureAwait(false);
                                            string strSelectedValue =
                                                ImprovementManager.GetSelectedValue(this);
                                            if (!string.IsNullOrEmpty(strSelectedValue))
                                            {
                                                objLivingPersonaQuality.Extra = strSelectedValue;
                                            }
                                        }

                                        objLivingPersonaQuality.FirstLevelBonus = objNode["firstlevelbonus"];
                                        if (objLivingPersonaQuality.FirstLevelBonus?.HasChildNodes == true)
                                        {
                                            string strCheckExtra = blnSync
                                                ? objLivingPersonaQuality.Extra
                                                : await objLivingPersonaQuality.GetExtraAsync(token).ConfigureAwait(false);
                                            string strCheckSourceName = blnSync
                                                ? objLivingPersonaQuality.SourceName
                                                : await objLivingPersonaQuality.GetSourceNameAsync(token).ConfigureAwait(false);
                                            bool blnDoFirstLevel;
                                            if (blnSync)
                                            {
                                                // ReSharper disable once MethodHasAsyncOverload
                                                blnDoFirstLevel = !Qualities.Any(objCheckQuality =>
                                                    objCheckQuality != objLivingPersonaQuality &&
                                                    objCheckQuality.SourceID == objLivingPersonaQuality.SourceID &&
                                                    objCheckQuality.Extra == strCheckExtra &&
                                                    objCheckQuality.SourceName == strCheckSourceName, token);
                                            }
                                            else
                                            {
                                                blnDoFirstLevel = !await (await GetQualitiesAsync(token).ConfigureAwait(false)).AnyAsync(async objCheckQuality =>
                                                    objCheckQuality != objLivingPersonaQuality &&
                                                    objCheckQuality.SourceID == objLivingPersonaQuality.SourceID &&
                                                    await objCheckQuality.GetExtraAsync(token).ConfigureAwait(false) == strCheckExtra &&
                                                    await objCheckQuality.GetSourceNameAsync(token).ConfigureAwait(false) == strCheckSourceName, token).ConfigureAwait(false);
                                            }

                                            if (blnDoFirstLevel)
                                            {
                                                ImprovementManager.SetForcedValue(objLivingPersonaQuality.Extra, this);
                                                if (blnSync)
                                                    // ReSharper disable once MethodHasAsyncOverload
                                                    ImprovementManager.CreateImprovements(this,
                                                        Improvement.ImprovementSource.Quality,
                                                        objLivingPersonaQuality.InternalId,
                                                        objLivingPersonaQuality
                                                            .FirstLevelBonus, 1,
                                                        objLivingPersonaQuality.CurrentDisplayNameShort, token: token);
                                                else
                                                    await ImprovementManager.CreateImprovementsAsync(this,
                                                                                Improvement.ImprovementSource.Quality,
                                                                                objLivingPersonaQuality.InternalId,
                                                                                objLivingPersonaQuality
                                                                                    .FirstLevelBonus, 1,
                                                                                await objLivingPersonaQuality
                                                                                    .GetCurrentDisplayNameShortAsync(
                                                                                        token)
                                                                                    .ConfigureAwait(false),
                                                                                token: token)
                                                                            .ConfigureAwait(false);
                                                string strSelectedValue =
                                                    ImprovementManager.GetSelectedValue(this);
                                                if (!string.IsNullOrEmpty(strSelectedValue))
                                                {
                                                    if (blnSync)
                                                        objLivingPersonaQuality.Extra = strSelectedValue;
                                                    else
                                                        await objLivingPersonaQuality.SetExtraAsync(strSelectedValue, token).ConfigureAwait(false);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // Failed to re-apply the improvements immediately, so let's just add it for processing when the character is opened
                                        lstInternalIdsNeedingReapplyImprovements.Add(
                                            objLivingPersonaQuality.InternalId);
                                    }

                                    objLivingPersonaQuality.NaturalWeaponsNode = objNode["naturalweapons"];
                                    if (objLivingPersonaQuality.NaturalWeaponsNode != null)
                                    {
                                        ImprovementManager.SetForcedValue(objLivingPersonaQuality.Extra, this);
                                        if (blnSync)
                                            // ReSharper disable once MethodHasAsyncOverload
                                            ImprovementManager.CreateImprovements(this,
                                                Improvement.ImprovementSource.Quality,
                                                objLivingPersonaQuality.InternalId,
                                                objLivingPersonaQuality
                                                    .NaturalWeaponsNode, 1,
                                                objLivingPersonaQuality.CurrentDisplayNameShort, token: token);
                                        else
                                            await ImprovementManager.CreateImprovementsAsync(this,
                                                                        Improvement.ImprovementSource.Quality,
                                                                        objLivingPersonaQuality.InternalId,
                                                                        objLivingPersonaQuality
                                                                            .NaturalWeaponsNode, 1,
                                                                        await objLivingPersonaQuality
                                                                              .GetCurrentDisplayNameShortAsync(token)
                                                                              .ConfigureAwait(false), token: token)
                                                                    .ConfigureAwait(false);
                                        string strSelectedValue =
                                            ImprovementManager.GetSelectedValue(this);
                                        if (!string.IsNullOrEmpty(strSelectedValue))
                                        {
                                            objLivingPersonaQuality.Extra = strSelectedValue;
                                        }
                                    }
                                }

                                //Timekeeper.Finish("load_char_gear");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("Label_Vehicles", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager.GetStringAsync("Label_Vehicles",
                                                                    token: token)
                                                                .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_car", loadActivity))
                            {
                                // Vehicles.
                                objXmlNodeList = objXmlCharacter.SelectNodes("vehicles/vehicle");
                                foreach (XmlNode objXmlVehicle in objXmlNodeList)
                                {
                                    Vehicle objVehicle = new Vehicle(this);
                                    if (blnSync)
                                    {
                                        try
                                        {
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objVehicle.Load(objXmlVehicle);
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            _lstVehicles.Add(objVehicle);
                                        }
                                        catch
                                        {
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objVehicle.DeleteVehicle();
                                            throw;
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            await objVehicle.LoadAsync(objXmlVehicle, token: token).ConfigureAwait(false);
                                            await _lstVehicles.AddAsync(objVehicle, token).ConfigureAwait(false);
                                        }
                                        catch
                                        {
                                            await objVehicle.DeleteVehicleAsync(CancellationToken.None).ConfigureAwait(false);
                                            throw;
                                        }
                                    }
                                }

                                //Timekeeper.Finish("load_char_car");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(LanguageManager.GetString("Tab_Weapons", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager
                                                                  .GetStringAsync("Tab_Weapons", token: token)
                                                                  .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_weapons", loadActivity))
                            {
                                // Weapons.
                                objXmlNodeList = objXmlCharacter.SelectNodes("weapons/weapon");
                                foreach (XmlNode objXmlWeapon in objXmlNodeList)
                                {
                                    Weapon objWeapon = new Weapon(this);
                                    if (blnSync)
                                    {
                                        try
                                        {
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objWeapon.Load(objXmlWeapon);
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            _lstWeapons.Add(objWeapon);
                                        }
                                        catch
                                        {
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objWeapon.DeleteWeapon();
                                            throw;
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            await objWeapon.LoadAsync(objXmlWeapon, token: token).ConfigureAwait(false);
                                            await _lstWeapons.AddAsync(objWeapon, token).ConfigureAwait(false);
                                        }
                                        catch
                                        {
                                            await objWeapon.DeleteWeaponAsync(token: CancellationToken.None).ConfigureAwait(false);
                                            throw;
                                        }
                                    }
                                }

                                //Timekeeper.Finish("load_char_weapons");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("String_Metamagics", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager.GetStringAsync("String_Metamagics",
                                                                    token: token)
                                                                .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_metamagics", loadActivity))
                            {
                                // Metamagics/Echoes.
                                objXmlNodeList = objXmlCharacter.SelectNodes("metamagics/metamagic");
                                foreach (XmlNode objXmlMetamagic in objXmlNodeList)
                                {
                                    Metamagic objMetamagic = new Metamagic(this);
                                    if (blnSync)
                                    {
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        objMetamagic.Load(objXmlMetamagic);
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        _lstMetamagics.Add(objMetamagic);
                                    }
                                    else
                                    {
                                        await objMetamagic.LoadAsync(objXmlMetamagic, token).ConfigureAwait(false);
                                        await _lstMetamagics.AddAsync(objMetamagic, token).ConfigureAwait(false);
                                    }
                                }

                                //Timekeeper.Finish("load_char_mmagic");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(LanguageManager.GetString("String_Arts", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager
                                                                  .GetStringAsync("String_Arts", token: token)
                                                                  .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_arts", loadActivity))
                            {
                                // Arts
                                objXmlNodeList = objXmlCharacter.SelectNodes("arts/art");
                                foreach (XmlNode objXmlArt in objXmlNodeList)
                                {
                                    Art objArt = new Art(this);
                                    if (blnSync)
                                    {
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        objArt.Load(objXmlArt);
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        _lstArts.Add(objArt);
                                    }
                                    else
                                    {
                                        await objArt.LoadAsync(objXmlArt, token).ConfigureAwait(false);
                                        await _lstArts.AddAsync(objArt, token).ConfigureAwait(false);
                                    }
                                }

                                //Timekeeper.Finish("load_char_arts");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("String_Enhancements", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager.GetStringAsync("String_Enhancements",
                                                                    token: token)
                                                                .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_enhancements", loadActivity))
                            {
                                // Enhancements
                                objXmlNodeList = objXmlCharacter.SelectNodes("enhancements/enhancement");
                                foreach (XmlNode objXmlEnhancement in objXmlNodeList)
                                {
                                    Enhancement objEnhancement = new Enhancement(this);
                                    if (blnSync)
                                    {
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        objEnhancement.Load(objXmlEnhancement);
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        _lstEnhancements.Add(objEnhancement);
                                    }
                                    else
                                    {
                                        await objEnhancement.LoadAsync(objXmlEnhancement, token).ConfigureAwait(false);
                                        await _lstEnhancements.AddAsync(objEnhancement, token).ConfigureAwait(false);
                                    }
                                }

                                //Timekeeper.Finish("load_char_ench");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(LanguageManager.GetString("Tab_Critter", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager
                                                                  .GetStringAsync("Tab_Critter", token: token)
                                                                  .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_critterpowers", loadActivity))
                            {
                                // Critter Powers.
                                objXmlNodeList = objXmlCharacter.SelectNodes("critterpowers/critterpower");
                                foreach (XmlNode objXmlPower in objXmlNodeList)
                                {
                                    CritterPower objPower = new CritterPower(this);
                                    if (blnSync)
                                    {
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        objPower.Load(objXmlPower);
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        _lstCritterPowers.Add(objPower);
                                    }
                                    else
                                    {
                                        await objPower.LoadAsync(objXmlPower, token).ConfigureAwait(false);
                                        await _lstCritterPowers.AddAsync(objPower, token).ConfigureAwait(false);
                                    }
                                }

                                //Timekeeper.Finish("load_char_cpow");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("Label_SummaryFoci", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager.GetStringAsync("Label_SummaryFoci",
                                                                    token: token)
                                                                .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_foci", loadActivity))
                            {
                                // Foci.
                                objXmlNodeList = objXmlCharacter.SelectNodes("foci/focus");
                                foreach (XmlNode objXmlFocus in objXmlNodeList)
                                {
                                    Focus objFocus = new Focus(this);
                                    if (blnSync)
                                    {
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        objFocus.Load(objXmlFocus);
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        _lstFoci.Add(objFocus);
                                    }
                                    else
                                    {
                                        await objFocus.LoadAsync(objXmlFocus, token).ConfigureAwait(false);
                                        await _lstFoci.AddAsync(objFocus, token).ConfigureAwait(false);
                                    }
                                }

                                //Timekeeper.Finish("load_char_foci");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("Label_SummaryInitiation", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager
                                                                  .GetStringAsync("Label_SummaryInitiation",
                                                                      token: token)
                                                                  .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_init", loadActivity))
                            {
                                // Initiation Grades.
                                objXmlNodeList = objXmlCharacter.SelectNodes("initiationgrades/initiationgrade");
                                foreach (XmlNode objXmlGrade in objXmlNodeList)
                                {
                                    InitiationGrade objGrade = new InitiationGrade(this);
                                    objGrade.Load(objXmlGrade);
                                    if (blnSync)
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        _lstInitiationGrades.Add(objGrade);
                                    else
                                        await _lstInitiationGrades.AddAsync(objGrade, token).ConfigureAwait(false);
                                }

                                //Timekeeper.Finish("load_char_init");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("String_Expenses", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager.GetStringAsync("String_Expenses",
                                                                    token: token)
                                                                .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            // While expenses are to be saved in create mode due to starting nuyen and starting karma being logged as expense log entries,
                            // they shouldn't get loaded in create mode because they shouldn't be there.
                            if (Created)
                            {
                                using (Timekeeper.StartSyncron("load_char_elog", loadActivity))
                                {
                                    // Expense Log Entries.
                                    XmlNodeList objXmlExpenseList = objXmlCharacter.SelectNodes("expenses/expense");
                                    foreach (XmlNode objXmlExpense in objXmlExpenseList)
                                    {
                                        ExpenseLogEntry objExpenseLogEntry = new ExpenseLogEntry(this);
                                        if (blnSync)
                                        {
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objExpenseLogEntry.Load(objXmlExpense);
                                            _lstExpenseLog.AddWithSort(objExpenseLogEntry, token: token);
                                        }
                                        else
                                        {
                                            await objExpenseLogEntry.LoadAsync(objXmlExpense, token).ConfigureAwait(false);
                                            await _lstExpenseLog.AddWithSortAsync(objExpenseLogEntry, token: token)
                                                                .ConfigureAwait(false);
                                        }
                                    }

                                    //Timekeeper.Finish("load_char_elog");
                                }
                            }
#if DEBUG
                            else
                            {
                                // There shouldn't be any expenses for a character loaded in create mode. This code is to help narrow down issues should expenses somehow be created.
                                XmlNodeList objXmlExpenseList = objXmlCharacter.SelectNodes("expenses/expense");
                                if (objXmlExpenseList?.Count > 0)
                                {
                                    Utils.BreakIfDebug();
                                }
                            }
#endif
                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("Tip_Skill_Sustain", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager.GetStringAsync("Tip_Skill_Sustain",
                                                                    token: token)
                                                                .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            // Need to load these after everything else so that we can properly link them up during loading
                            using (Timekeeper.StartSyncron("load_char_sustainedobjects", loadActivity))
                            {
                                objXmlNodeList = objXmlCharacter.SelectNodes("sustainedobjects/sustainedobject");
                                foreach (XmlNode objXmlSustained in objXmlNodeList)
                                {
                                    SustainedObject objSustained = new SustainedObject(this, objXmlSustained);
                                    if (!objSustained.InternalId.IsEmptyGuid())
                                    {
                                        if (blnSync)
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            _lstSustainedObjects.Add(objSustained);
                                        else
                                            await _lstSustainedObjects.AddAsync(objSustained, token)
                                                .ConfigureAwait(false);
                                    }
                                }
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("Tab_Improvements", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager.GetStringAsync("Tab_Improvements",
                                                                    token: token)
                                                                .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_igroup", loadActivity))
                            {
                                // Improvement Groups.
                                XmlNodeList objXmlGroupList =
                                    objXmlCharacter.SelectNodes("improvementgroups/improvementgroup");
                                if (blnSync)
                                {
                                    foreach (XmlNode objXmlGroup in objXmlGroupList)
                                    {
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        _lstImprovementGroups.Add(objXmlGroup.InnerTextViaPool(token));
                                    }
                                }
                                else
                                {
                                    foreach (XmlNode objXmlGroup in objXmlGroupList)
                                    {
                                        await _lstImprovementGroups.AddAsync(objXmlGroup.InnerTextViaPool(token), token)
                                                                   .ConfigureAwait(false);
                                    }
                                }

                                //Timekeeper.Finish("load_char_igroup");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(LanguageManager.GetString("Tab_Calendar", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager
                                                                  .GetStringAsync("Tab_Calendar", token: token)
                                                                  .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_calendar", loadActivity))
                            {
                                // Calendar.
                                XmlNodeList objXmlWeekList = objXmlCharacter.SelectNodes("calendar/week");
                                foreach (XmlNode objXmlWeek in objXmlWeekList)
                                {
                                    CalendarWeek objWeek = new CalendarWeek();
                                    try
                                    {
                                        token.ThrowIfCancellationRequested();
                                        if (blnSync)
                                        {
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            objWeek.Load(objXmlWeek);
                                            _lstCalendar.AddWithSort(objWeek, (x, y) => y.CompareTo(x), token: token);
                                        }
                                        else
                                        {
                                            await objWeek.LoadAsync(objXmlWeek, token).ConfigureAwait(false);
                                            await _lstCalendar
                                                  .AddWithSortAsync(objWeek, (x, y) => y.CompareTo(x), token: token)
                                                  .ConfigureAwait(false);
                                        }
                                    }
                                    catch
                                    {
                                        if (blnSync)
                                            // ReSharper disable once MethodHasAsyncOverload
                                            objWeek.Dispose();
                                        else
                                            await objWeek.DisposeAsync().ConfigureAwait(false);
                                        throw;
                                    }
                                }

                                //Timekeeper.Finish("load_char_calendar");
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("String_LegacyFixes", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager.GetStringAsync("String_LegacyFixes",
                                                                    token: token)
                                                                .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            using (Timekeeper.StartSyncron("load_char_unarmed", loadActivity))
                            {
                                // Look for the unarmed attack
                                bool blnFoundUnarmed = false;
                                foreach (Weapon objWeapon in _lstWeapons)
                                {
                                    if (objWeapon.Name == "Unarmed Attack")
                                    {
                                        blnFoundUnarmed = true;
                                        break;
                                    }
                                }

                                if (!blnFoundUnarmed)
                                {
                                    // Add the Unarmed Attack Weapon to the character.
                                    XmlDocument objXmlWeaponDoc = blnSync
                                        // ReSharper disable once MethodHasAsyncOverload
                                        ? LoadData("weapons.xml", token: token)
                                        : await LoadDataAsync("weapons.xml", token: token).ConfigureAwait(false);
                                    XmlNode objXmlWeapon =
                                        objXmlWeaponDoc.SelectSingleNode(
                                            "/chummer/weapons/weapon[name = \"Unarmed Attack\"]");
                                    if (objXmlWeapon != null)
                                    {
                                        Weapon objWeapon = new Weapon(this);
                                        try
                                        {
                                            if (blnSync)
                                                // ReSharper disable once MethodHasAsyncOverload
                                                objWeapon.Create(objXmlWeapon, _lstWeapons, token: token);
                                            else
                                                await objWeapon.CreateAsync(objXmlWeapon, _lstWeapons, token: token).ConfigureAwait(false);
                                            objWeapon.IncludedInWeapon = true; // Unarmed attack can never be removed
                                            if (blnSync)
                                                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                _lstWeapons.Add(objWeapon);
                                            else
                                                await _lstWeapons.AddAsync(objWeapon, token).ConfigureAwait(false);
                                        }
                                        catch
                                        {
                                            if (blnSync)
                                                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                objWeapon.DeleteWeapon();
                                            else
                                                await objWeapon.DeleteWeaponAsync(token: CancellationToken.None).ConfigureAwait(false);
                                            throw;
                                        }
                                    }
                                }

                                //Timekeeper.Finish("load_char_unarmed");
                            }

                            using (Timekeeper.StartSyncron("load_char_dwarffix", loadActivity))
                            {
                                // converting from old dwarven resistance to new dwarven resistance
                                if (Metatype.Equals("dwarf", StringComparison.OrdinalIgnoreCase))
                                {
                                    Quality objOldQuality = blnSync
                                        ? Qualities.FirstOrDefault(x =>
                                                                       x.Name.Equals(
                                                                           "Resistance to Pathogens and Toxins",
                                                                           StringComparison.Ordinal))
                                        : await Qualities.FirstOrDefaultAsync(x =>
                                                                                  x.Name.Equals(
                                                                                      "Resistance to Pathogens and Toxins",
                                                                                      StringComparison.Ordinal), token)
                                                         .ConfigureAwait(false);
                                    if (objOldQuality != null)
                                    {
                                        if (blnSync)
                                            // ReSharper disable once MethodHasAsyncOverload
                                            objOldQuality.DeleteQuality(token: token);
                                        else
                                            await objOldQuality.DeleteQualityAsync(token: token).ConfigureAwait(false);

                                        if (blnSync
                                                // ReSharper disable once MethodHasAsyncOverload
                                                ? Qualities.All(x => !x.Name.Equals("Resistance to Pathogens/Toxins", StringComparison.Ordinal)
                                                                     && !x.Name.Equals("Dwarf Resistance", StringComparison.Ordinal), token)
                                                : await Qualities.AllAsync(x => !x.Name.Equals("Resistance to Pathogens/Toxins", StringComparison.Ordinal)
                                                                               && !x.Name.Equals("Dwarf Resistance", StringComparison.Ordinal), token).ConfigureAwait(false))
                                        {
                                            XmlNode objXmlDwarfQuality =
                                                xmlRootQualitiesNode.SelectSingleNode(
                                                    "quality[name = \"Resistance to Pathogens/Toxins\"]") ??
                                                xmlRootQualitiesNode.SelectSingleNode(
                                                    "quality[name = \"Dwarf Resistance\"]");

                                            List<Weapon> lstWeapons = new List<Weapon>(1);
                                            Quality objQuality = new Quality(this);

                                            try
                                            {
                                                token.ThrowIfCancellationRequested();
                                                if (blnSync)
                                                {
                                                    // ReSharper disable once MethodHasAsyncOverload
                                                    objQuality.Create(objXmlDwarfQuality, QualitySource.Metatype,
                                                        lstWeapons, token: token);
                                                    foreach (Weapon objWeapon in lstWeapons)
                                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                        Weapons.Add(objWeapon);
                                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                                    Qualities.Add(objQuality);
                                                }
                                                else
                                                {
                                                    await objQuality.CreateAsync(objXmlDwarfQuality, QualitySource.Metatype,
                                                        lstWeapons, token: token).ConfigureAwait(false);
                                                    foreach (Weapon objWeapon in lstWeapons)
                                                        await Weapons.AddAsync(objWeapon, token).ConfigureAwait(false);
                                                    await Qualities.AddAsync(objQuality, token).ConfigureAwait(false);
                                                }
                                            }
                                            catch
                                            {
                                                if (blnSync)
                                                    // ReSharper disable once MethodHasAsyncOverload
                                                    objQuality.DeleteQuality(token: CancellationToken.None);
                                                else
                                                    await objQuality.DeleteQualityAsync(token: CancellationToken.None).ConfigureAwait(false);
                                                throw;
                                            }
                                        }
                                    }
                                }

                                //Timekeeper.Finish("load_char_dwarffix");
                            }

                            using (Timekeeper.StartSyncron("load_char_cyberadeptfix", loadActivity))
                            {
                                //Sweep through grades if we have any cyberadept improvements that need reassignment
                                if (lstCyberadeptSweepGrades.Count > 0)
                                {
                                    foreach (Improvement objCyberadeptImprovement in lstCyberadeptSweepGrades)
                                    {
                                        InitiationGrade objBestGradeMatch = null;
                                        if (blnSync)
                                        {
                                            // ReSharper disable once MethodHasAsyncOverload
                                            InitiationGrades.ForEach(objInitiationGrade =>
                                            {
                                                if (!objInitiationGrade.Technomancer
                                                    || objInitiationGrade.Grade.DivAwayFromZero(2) >
                                                    objCyberadeptImprovement.Value
                                                    || Metamagics.Any(x => x.Grade == objInitiationGrade.Grade, token)
                                                    || lstCyberadeptSweepGrades.TrueForAll(x =>
                                                            x.ImproveSource != Improvement.ImprovementSource
                                                                .CyberadeptDaemon
                                                            || x.SourceName != objInitiationGrade.InternalId))
                                                    return;
                                                if (objBestGradeMatch == null ||
                                                    objBestGradeMatch.Grade > objInitiationGrade.Grade)
                                                    objBestGradeMatch = objInitiationGrade;
                                            }, token);
                                        }
                                        else
                                        {
                                            await InitiationGrades.ForEachAsync(async objInitiationGrade =>
                                            {
                                                if (!objInitiationGrade.Technomancer
                                                    || objInitiationGrade.Grade.DivAwayFromZero(2) >
                                                    objCyberadeptImprovement.Value
                                                    || await Metamagics.AnyAsync(
                                                        x => x.Grade == objInitiationGrade.Grade, token).ConfigureAwait(false)
                                                    || lstCyberadeptSweepGrades.TrueForAll(x =>
                                                        x.ImproveSource != Improvement.ImprovementSource
                                                            .CyberadeptDaemon
                                                        || x.SourceName != objInitiationGrade.InternalId))
                                                    return;
                                                if (objBestGradeMatch == null ||
                                                    objBestGradeMatch.Grade > objInitiationGrade.Grade)
                                                    objBestGradeMatch = objInitiationGrade;
                                            }, token).ConfigureAwait(false);
                                        }

                                        if (objBestGradeMatch != null)
                                        {
                                            objCyberadeptImprovement.ImproveSource =
                                                Improvement.ImprovementSource.CyberadeptDaemon;
                                            objCyberadeptImprovement.SourceName = objBestGradeMatch.InternalId;
                                        }
                                        else if (blnSync)
                                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                            _lstImprovements.Remove(objCyberadeptImprovement);
                                        else
                                            await _lstImprovements.RemoveAsync(objCyberadeptImprovement, token)
                                                                  .ConfigureAwait(false);
                                    }
                                }

                                //Timekeeper.Finish("load_char_cyberadeptfix");
                            }

                            using (Timekeeper.StartSyncron("load_char_mentorspiritfix", loadActivity))
                            {
                                if (blnSync)
                                {
                                    Quality objMentorQuality = Qualities.FirstOrDefault(q => q.Name == "Mentor Spirit");
                                    // This character doesn't have any improvements tied to a cached Mentor Spirit value, so re-apply the improvement that adds the Mentor spirit
                                    // ReSharper disable once MethodHasAsyncOverload
                                    if (objMentorQuality != null && !Improvements.Any(imp =>
                                                imp.ImproveType == Improvement.ImprovementType.MentorSpirit &&
                                                !string.IsNullOrEmpty(imp.ImprovedName), token))
                                    {
                                        // Selecting bonuses for a mentor spirit mid-load is confusing, so just show the error and let the player manually re-apply
                                        lstInternalIdsNeedingReapplyImprovements.Add(objMentorQuality.InternalId);
                                    }
                                }
                                else
                                {
                                    Quality objMentorQuality = await Qualities.FirstOrDefaultAsync(q => q.Name == "Mentor Spirit", token).ConfigureAwait(false);
                                    // This character doesn't have any improvements tied to a cached Mentor Spirit value, so re-apply the improvement that adds the Mentor spirit
                                    if (objMentorQuality != null && !await Improvements.AnyAsync(imp =>
                                            imp.ImproveType == Improvement.ImprovementType.MentorSpirit &&
                                            !string.IsNullOrEmpty(imp.ImprovedName), token).ConfigureAwait(false))
                                    {
                                        // Selecting bonuses for a mentor spirit mid-load is confusing, so just show the error and let the player manually re-apply
                                        lstInternalIdsNeedingReapplyImprovements.Add(objMentorQuality.InternalId);
                                    }
                                }

                                //Timekeeper.Finish("load_char_mentorspiritfix");
                            }

                            using (Timekeeper.StartSyncron("load_char_startingnuyenfix", loadActivity))
                            {
                                if (blnSync)
                                {
                                    if (!Created)
                                    {
                                        _decNuyenBP = Math.Max(Math.Min(_decNuyenBP, TotalNuyenMaximumBP), 0);
                                    }
                                }
                                else if (!await GetCreatedAsync(token).ConfigureAwait(false))
                                {
                                    _decNuyenBP =
                                        Math.Max(
                                            Math.Min(_decNuyenBP,
                                                await GetTotalNuyenMaximumBPAsync(token).ConfigureAwait(false)), 0);
                                }
                            }

                            // Fix legacy cases where characters have more attribute points assigned than allowed
                            using (Timekeeper.StartSyncron("load_char_badattributesfix", loadActivity))
                            {
                                if (blnSync)
                                {
                                    if (!Created)
                                    {
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        AttributeSection.AllAttributes.ForEach(x => x.DoBaseFix(), token);
                                    }
                                }
                                else if (!await GetCreatedAsync(token).ConfigureAwait(false))
                                {
                                    await AttributeSection.AllAttributes
                                        .ForEachAsync(async x => await x.DoBaseFixAsync(token: token).ConfigureAwait(false),
                                            token).ConfigureAwait(false);
                                }
                            }

                            // Fix skills that shouldn't be allowed to have specializations having them anyway (needed at the last step because improvements and skill groups can affect this)
                            using (Timekeeper.StartSyncron("load_char_badskillspecsfix", loadActivity))
                            {
                                if (blnSync)
                                {
                                    if (!Created)
                                    {
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        SkillsSection.Skills.ForEach(x =>
                                        {
                                            if (x.Specializations.Count > 0 && !x.CanHaveSpecs)
                                                x.Specializations.Clear();
                                        }, token);
                                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                        SkillsSection.KnowledgeSkills.ForEach(x =>
                                        {
                                            if (x.Specializations.Count > 0 && !x.CanHaveSpecs)
                                                x.Specializations.Clear();
                                        }, token);
                                    }
                                }
                                else if (!await GetCreatedAsync(token).ConfigureAwait(false))
                                {
                                    SkillsSection objSkillsSection = await GetSkillsSectionAsync(token).ConfigureAwait(false);
                                    await (await objSkillsSection.GetSkillsAsync(token).ConfigureAwait(false))
                                        .ForEachWithSideEffectsAsync(
                                            async x =>
                                            {
                                                ThreadSafeObservableCollection<SkillSpecialization> lstSpecs =
                                                    await x.GetSpecializationsAsync(token).ConfigureAwait(false);
                                                if (await lstSpecs.GetCountAsync(token).ConfigureAwait(false) > 0 &&
                                                    !await x.GetCanHaveSpecsAsync(token).ConfigureAwait(false))
                                                    await lstSpecs.ClearAsync(token).ConfigureAwait(false);
                                            }, token).ConfigureAwait(false);
                                    await (await objSkillsSection.GetKnowledgeSkillsAsync(token).ConfigureAwait(false))
                                        .ForEachWithSideEffectsAsync(
                                            async x =>
                                            {
                                                ThreadSafeObservableCollection<SkillSpecialization> lstSpecs =
                                                    await x.GetSpecializationsAsync(token).ConfigureAwait(false);
                                                if (await lstSpecs.GetCountAsync(token).ConfigureAwait(false) > 0 &&
                                                    !await x.GetCanHaveSpecsAsync(token).ConfigureAwait(false))
                                                    await lstSpecs.ClearAsync(token).ConfigureAwait(false);
                                            }, token).ConfigureAwait(false);
                                }
                            }

                            if (LastSavedVersion <= new ValueVersion(5, 225, 686))
                            {
                                // Fix legacy cases where characters have dealer connection improvement keys saved in non-English
                                using (Timekeeper.StartSyncron("load_char_nonenglishdealerconnectionfix", loadActivity))
                                {
                                    if (blnSync)
                                    {
                                        // ReSharper disable MethodHasAsyncOverload
                                        List<Improvement> lstDealerConnectionImprovements = ImprovementManager
                                            .GetCachedImprovementListForValueOf(
                                                this, Improvement.ImprovementType.DealerConnection, token: token);
                                        foreach (Improvement objImprovement in lstDealerConnectionImprovements)
                                        {
                                            string strCategory = objImprovement.UniqueName;
                                            if (!string.IsNullOrEmpty(
                                                    LanguageManager.GetString("String_DealerConnection_" + strCategory,
                                                        false, token)))
                                                continue;
                                            if (LanguageManager.GetString("String_DealerConnection_Drones",
                                                    token: token) ==
                                                strCategory)
                                            {
                                                objImprovement.ImprovedName = "Drones";
                                                objImprovement.UniqueName = "Drones";
                                            }
                                            else if (LanguageManager.GetString(
                                                         "String_DealerConnection_Groundcraft", token: token) ==
                                                     strCategory)
                                            {
                                                objImprovement.ImprovedName = "Groundcraft";
                                                objImprovement.UniqueName = "Groundcraft";
                                            }
                                            else if (LanguageManager.GetString(
                                                         "String_DealerConnection_Aircraft", token: token) ==
                                                     strCategory)
                                            {
                                                objImprovement.ImprovedName = "Aircraft";
                                                objImprovement.UniqueName = "Aircraft";
                                            }
                                            else if (LanguageManager.GetString(
                                                         "String_DealerConnection_Watercraft", token: token) ==
                                                     strCategory)
                                            {
                                                objImprovement.ImprovedName = "Watercraft";
                                                objImprovement.UniqueName = "Watercraft";
                                            }
                                        }
                                        // ReSharper enable MethodHasAsyncOverload
                                    }
                                    else
                                    {
                                        List<Improvement> lstDealerConnectionImprovements = await ImprovementManager
                                            .GetCachedImprovementListForValueOfAsync(
                                                this, Improvement.ImprovementType.DealerConnection, token: token)
                                            .ConfigureAwait(false);
                                        foreach (Improvement objImprovement in lstDealerConnectionImprovements)
                                        {
                                            string strCategory = objImprovement.UniqueName;
                                            if (!string.IsNullOrEmpty(
                                                    await LanguageManager.GetStringAsync(
                                                        "String_DealerConnection_" + strCategory,
                                                        false, token).ConfigureAwait(false)))
                                                continue;
                                            if (await LanguageManager
                                                    .GetStringAsync("String_DealerConnection_Drones", token: token)
                                                    .ConfigureAwait(false) ==
                                                strCategory)
                                            {
                                                objImprovement.ImprovedName = "Drones";
                                                objImprovement.UniqueName = "Drones";
                                            }
                                            else if (await LanguageManager
                                                         .GetStringAsync("String_DealerConnection_Groundcraft",
                                                             token: token).ConfigureAwait(false) ==
                                                     strCategory)
                                            {
                                                objImprovement.ImprovedName = "Groundcraft";
                                                objImprovement.UniqueName = "Groundcraft";
                                            }
                                            else if (await LanguageManager
                                                         .GetStringAsync("String_DealerConnection_Aircraft",
                                                             token: token).ConfigureAwait(false) ==
                                                     strCategory)
                                            {
                                                objImprovement.ImprovedName = "Aircraft";
                                                objImprovement.UniqueName = "Aircraft";
                                            }
                                            else if (await LanguageManager
                                                         .GetStringAsync("String_DealerConnection_Watercraft",
                                                             token: token).ConfigureAwait(false) ==
                                                     strCategory)
                                            {
                                                objImprovement.ImprovedName = "Watercraft";
                                                objImprovement.UniqueName = "Watercraft";
                                            }
                                        }
                                    }
                                }
                            }

                            if (frmLoadingForm != null)
                            {
                                if (blnSync)
                                    // ReSharper disable once MethodHasAsyncOverload
                                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                    frmLoadingForm.PerformStep(
                                        LanguageManager.GetString("Tab_Options_Plugins", token: token));
                                else
                                    await frmLoadingForm.PerformStepAsync(
                                                            await LanguageManager.GetStringAsync("Tab_Options_Plugins",
                                                                    token: token)
                                                                .ConfigureAwait(false), token: token)
                                                        .ConfigureAwait(false);
                            }

                            //Plugins
                            using (Timekeeper.StartSyncron("load_char_plugins", loadActivity))
                            {
                                foreach (IPlugin plugin in blnSync
                                             ? Program.PluginLoader.MyActivePlugins
                                             : await Program.PluginLoader.GetMyActivePluginsAsync(token)
                                                 .ConfigureAwait(false))
                                {
                                    foreach (XmlNode objXmlPlugin in objXmlCharacter.SelectNodes("plugins/" +
                                                 plugin.GetPluginAssembly().GetName().Name))
                                    {
                                        plugin.LoadFileElement(this, objXmlPlugin.InnerTextViaPool(token));
                                    }
                                }

                                //Timekeeper.Finish("load_plugins");
                            }

                            ConcurrentBag<string> lstOldIds = Interlocked.Exchange(ref _lstInternalIdsNeedingReapplyImprovements,
                                                 lstInternalIdsNeedingReapplyImprovements);
                            if (lstOldIds != null)
                            {
                                foreach (string strOldId in lstOldIds)
                                    lstInternalIdsNeedingReapplyImprovements.Add(strOldId);
                            }
                        }
                        finally
                        {
                            if (blnSync)
                                IsLoading = false;
                            else
                                await SetIsLoadingAsync(false, token).ConfigureAwait(false);
                        }

                        if (frmLoadingForm != null)
                        {
                            if (blnSync)
                                // ReSharper disable once MethodHasAsyncOverload
                                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                                frmLoadingForm.PerformStep(
                                    LanguageManager.GetString("String_GeneratedImprovements", token: token));
                            else
                                await frmLoadingForm.PerformStepAsync(
                                    await LanguageManager.GetStringAsync("String_GeneratedImprovements", token: token)
                                                         .ConfigureAwait(false), token: token).ConfigureAwait(false);
                        }

                        // Refresh certain improvements
                        using (Timekeeper.StartSyncron("load_char_improvementrefreshers1", loadActivity))
                        {
                            // Process all events related to improvements
                            using (new FetchSafelyFromSafeObjectPool<
                                       Dictionary<INotifyMultiplePropertiesChangedAsync, HashSet<string>>>(
                                       Utils.DictionaryForMultiplePropertyChangedPool,
                                       out Dictionary<INotifyMultiplePropertiesChangedAsync, HashSet<string>>
                                           dicChangedProperties))
                            {
                                try
                                {
                                    token.ThrowIfCancellationRequested();
                                    HashSet<string> setAlwaysChangedProperties = Utils.StringHashSetPool.Get();
                                    dicChangedProperties.Add(this, setAlwaysChangedProperties);
                                    setAlwaysChangedProperties.Add(nameof(BlackMarketDiscount));
                                    setAlwaysChangedProperties.Add(nameof(DealerConnectionDiscount));
                                    setAlwaysChangedProperties.Add(nameof(Essence));
                                    setAlwaysChangedProperties.Add(nameof(WoundModifier));
                                    setAlwaysChangedProperties.Add(nameof(SustainingPenalty));
                                    setAlwaysChangedProperties.Add(nameof(Encumbrance));
                                    setAlwaysChangedProperties.Add(nameof(ArmorEncumbrance));
                                    setAlwaysChangedProperties.Add(nameof(LimbCount)); // Makes sure attributes properly reflect equipped cyberlimbs whose effects aren't handled through improvements

                                    if (blnSync)
                                    {
                                        foreach (Improvement objImprovement in Improvements)
                                        {
                                            if (!objImprovement.Enabled)
                                                continue;
                                            foreach ((INotifyMultiplePropertiesChangedAsync objItemToUpdate,
                                                         string strPropertyToUpdate) in objImprovement
                                                         .GetRelevantPropertyChangers())
                                            {
                                                if (!dicChangedProperties.TryGetValue(
                                                        objItemToUpdate, out HashSet<string> setChangedProperties))
                                                {
                                                    setChangedProperties = Utils.StringHashSetPool.Get();
                                                    dicChangedProperties.Add(objItemToUpdate, setChangedProperties);
                                                }

                                                setChangedProperties.Add(strPropertyToUpdate);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        await Improvements.ForEachAsync(objImprovement =>
                                        {
                                            if (!objImprovement.Enabled)
                                                return;
                                            foreach ((INotifyMultiplePropertiesChangedAsync objItemToUpdate,
                                                         string strPropertyToUpdate) in objImprovement
                                                         .GetRelevantPropertyChangers())
                                            {
                                                if (!dicChangedProperties.TryGetValue(
                                                        objItemToUpdate, out HashSet<string> setChangedProperties))
                                                {
                                                    setChangedProperties = Utils.StringHashSetPool.Get();
                                                    dicChangedProperties.Add(objItemToUpdate, setChangedProperties);
                                                }

                                                setChangedProperties.Add(strPropertyToUpdate);
                                            }
                                        }, token).ConfigureAwait(false);
                                    }

                                    foreach (KeyValuePair<INotifyMultiplePropertiesChangedAsync, HashSet<string>>
                                                 kvpToProcess in
                                             dicChangedProperties)
                                    {
                                        if (blnSync)
                                            kvpToProcess.Key.OnMultiplePropertiesChanged(kvpToProcess.Value);
                                        else
                                            await kvpToProcess.Key
                                                .OnMultiplePropertiesChangedAsync(kvpToProcess.Value, token)
                                                .ConfigureAwait(false);
                                    }
                                }
                                finally
                                {
                                    List<HashSet<string>> lstToReturn = dicChangedProperties.Values.ToList();
                                    for (int i = lstToReturn.Count - 1; i >= 0; --i)
                                    {
                                        HashSet<string> setLoop = lstToReturn[i];
                                        Utils.StringHashSetPool.Return(ref setLoop);
                                    }
                                }
                            }

                            // Curb Mystic Adept power points if the values that were loaded in would be illegal
                            int intMysAdPPs =
                                blnSync ? MysticAdeptPowerPoints : await GetMysticAdeptPowerPointsAsync(token).ConfigureAwait(false);
                            if (intMysAdPPs > 0)
                            {
                                int intMAGTotalValue = blnSync
                                    ? MAG.TotalValue
                                    : await (await GetAttributeAsync("MAG", token: token).ConfigureAwait(false))
                                            .GetTotalValueAsync(token).ConfigureAwait(false);
                                if (intMysAdPPs > intMAGTotalValue)
                                {
                                    if (blnSync)
                                        MysticAdeptPowerPoints = intMAGTotalValue;
                                    else
                                        await SetMysticAdeptPowerPointsAsync(intMAGTotalValue, token).ConfigureAwait(false);
                                }
                            }

                            if (blnSync)
                            {
                                if (!InitiationEnabled || !AddInitiationsAllowed)
                                {
                                    // ReSharper disable once MethodHasAsyncOverload
                                    ClearInitiations(token);
                                }
                            }
                            else if (!await GetInitiationEnabledAsync(token).ConfigureAwait(false) || !await GetAddInitiationsAllowedAsync(token).ConfigureAwait(false))
                                await ClearInitiationsAsync(token).ConfigureAwait(false);

                            // Very rough fix for when Karma values somehow exceed KarmaMaximum after loading in. This shouldn't happen in the first place, but this ad-hoc patch will help fix crashes.
                            if (blnSync)
                            {
                                if (!Created)
                                {
                                    foreach (CharacterAttrib objAttrib in GetAllAttributesForModification(token))
                                    {
                                        while (objAttrib.Base > 0 && objAttrib.KarmaMaximum < 0)
                                        {
                                            --objAttrib.Base;
                                        }

                                        objAttrib.Karma = Math.Min(objAttrib.Karma, objAttrib.KarmaMaximum);
                                    }
                                }
                            }
                            else if (!await GetCreatedAsync(token).ConfigureAwait(false))
                            {
                                foreach (CharacterAttrib objAttrib in await GetAllAttributesForModificationAsync(token).ConfigureAwait(false))
                                {
                                    while (await objAttrib.GetBaseAsync(token).ConfigureAwait(false) > 0 &&
                                           await objAttrib.GetKarmaMaximumAsync(token).ConfigureAwait(false) < 0)
                                    {
                                        await objAttrib.ModifyBaseAsync(-1, token).ConfigureAwait(false);
                                    }

                                    int intKarmaMaximum =
                                        await objAttrib.GetKarmaMaximumAsync(token).ConfigureAwait(false);
                                    if (await objAttrib.GetKarmaAsync(token).ConfigureAwait(false) > intKarmaMaximum)
                                        await objAttrib.SetKarmaAsync(intKarmaMaximum, token).ConfigureAwait(false);
                                }
                            }

                            while (_setPostLoadMethods.TryTake(out Func<CancellationToken, bool> funcToCall))
                            {
                                if (!funcToCall.Invoke(token))
                                    return false;
                            }

                            if (blnSync)
                            {
                                while (_setPostLoadAsyncMethods.TryTake(out Func<CancellationToken, Task<bool>> funcToCall))
                                {
                                    if (!Utils.SafelyRunSynchronously(() => funcToCall.Invoke(token), token))
                                        return false;
                                }
                            }
                            else
                            {
                                while (_setPostLoadAsyncMethods.TryTake(out Func<CancellationToken, Task<bool>> funcToCall))
                                {
                                    if (!await funcToCall.Invoke(token).ConfigureAwait(false))
                                        return false;
                                }
                            }
                            //Timekeeper.Finish("load_char_improvementrefreshers");
                        }

                        //// If the character had old Qualities that were converted, immediately save the file so they are in the new format.
                        //if (blnHasOldQualities)
                        //{
                        //    Timekeeper.Start("load_char_resav");  //Lets not silently save file on load?
                        //    Save();
                        //    Timekeeper.Finish("load_char_resav");
                        //}
                        loadActivity.SetSuccess(true);
                    }
                    catch (Exception e)
                    {
                        e = e.Demystify();
                        loadActivity.SetSuccess(false);
                        Log.Error(e);
                        throw;
                    }
                }

                return true;
            }
            finally
            {
                if (blnSync)
                    // ReSharper disable once MethodHasAsyncOverload
                    objLocker.Dispose();
                else
                    await objLockerAsync.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
