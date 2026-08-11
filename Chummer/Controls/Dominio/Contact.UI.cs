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

// Metade dependente de WinForms de Contact, separada do domínio durante a
// extração do núcleo (DEC-023). A metade de regras vive em Chummer/Backend/Characters/Contact.cs.
//
// As duas são `partial class Contact`: no projeto legado voltam a ser uma classe
// só, então nenhum chamador precisou mudar. No Chummer.Core, só o domínio existe.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using Chummer.Annotations;
using Chummer.Backend.Enums;
using NLog;

namespace Chummer
{
    public partial class Contact
    {
        private readonly ThreadSafeList<Image> _lstMugshots;
        /// <summary>
        /// Character's portraits encoded using Base64.
        /// </summary>
        public ThreadSafeList<Image> Mugshots
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return LinkedCharacter != null ? LinkedCharacter.Mugshots : _lstMugshots;
            }
        }
        /// <summary>
        /// Character's portraits encoded using Base64.
        /// </summary>
        public async Task<ThreadSafeList<Image>> GetMugshotsAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                Character objLinkedCharacter = await GetLinkedCharacterAsync(token).ConfigureAwait(false);
                if (objLinkedCharacter != null)
                    return await objLinkedCharacter.GetMugshotsAsync(token).ConfigureAwait(false);
                return _lstMugshots;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }
        /// <summary>
        /// Character's main portrait encoded using Base64.
        /// </summary>
        public Image MainMugshot
        {
            get
            {
                using (LockObject.EnterReadLock())
                {
                    if (LinkedCharacter != null)
                        return LinkedCharacter.MainMugshot;
                    if (MainMugshotIndex >= Mugshots.Count || MainMugshotIndex < 0)
                        return null;
                    return Mugshots[MainMugshotIndex];
                }
            }
            set
            {
                if (value == null)
                {
                    MainMugshotIndex = -1;
                    return;
                }

                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (LinkedCharacter != null)
                        LinkedCharacter.MainMugshot = value;
                    else
                    {
                        int intNewMainMugshotIndex = Mugshots.IndexOf(value);
                        if (intNewMainMugshotIndex != -1)
                        {
                            MainMugshotIndex = intNewMainMugshotIndex;
                        }
                        else
                        {
                            using (Mugshots.LockObject.EnterWriteLock())
                            {
                                Mugshots.Add(value);
                                MainMugshotIndex = Mugshots.IndexOf(value);
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Character's main portrait encoded using Base64.
        /// </summary>
        public async Task<Image> GetMainMugshotAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                Character objLinkedCharacter = await GetLinkedCharacterAsync(token).ConfigureAwait(false);
                if (objLinkedCharacter != null)
                    return await objLinkedCharacter.GetMainMugshotAsync(token).ConfigureAwait(false);
                int intIndex = await GetMainMugshotIndexAsync(token).ConfigureAwait(false);
                if (intIndex < 0)
                    return null;
                ThreadSafeList<Image> lstMugshots = await GetMugshotsAsync(token).ConfigureAwait(false);
                if (intIndex >= await lstMugshots.GetCountAsync(token).ConfigureAwait(false))
                    return null;

                return await lstMugshots.GetValueAtAsync(intIndex, token).ConfigureAwait(false);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }
        /// <summary>
        /// Character's main portrait encoded using Base64.
        /// </summary>
        public async Task SetMainMugshotAsync(Image value, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (value == null)
            {
                await SetMainMugshotIndexAsync(-1, token).ConfigureAwait(false);
                return;
            }
            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                Character objLinkedCharacter = await GetLinkedCharacterAsync(token).ConfigureAwait(false);
                if (objLinkedCharacter != null)
                {
                    await objLinkedCharacter.SetMainMugshotAsync(value, token).ConfigureAwait(false);
                }
                else
                {
                    ThreadSafeList<Image> lstMugshots = await GetMugshotsAsync(token).ConfigureAwait(false);
                    int intNewMainMugshotIndex = await lstMugshots.IndexOfAsync(value, token).ConfigureAwait(false);
                    if (intNewMainMugshotIndex != -1)
                    {
                        await SetMainMugshotIndexAsync(intNewMainMugshotIndex, token).ConfigureAwait(false);
                    }
                    else
                    {
                        IAsyncDisposable objLocker2 = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
                        try
                        {
                            token.ThrowIfCancellationRequested();
                            IAsyncDisposable objLocker3 =
                                await lstMugshots.LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
                            try
                            {
                                token.ThrowIfCancellationRequested();
                                await lstMugshots.AddAsync(value, token).ConfigureAwait(false);
                                await SetMainMugshotIndexAsync(await lstMugshots.IndexOfAsync(value, token).ConfigureAwait(false), token).ConfigureAwait(false);
                            }
                            finally
                            {
                                await objLocker3.DisposeAsync().ConfigureAwait(false);
                            }
                        }
                        finally
                        {
                            await objLocker2.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
