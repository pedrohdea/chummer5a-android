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

// Metade dependente de WinForms de CharacterCache, separada do domínio durante a
// extração do núcleo (DEC-023). A metade de regras vive em Chummer/Backend/Characters/CharacterCache.cs.
//
// As duas são `partial class CharacterCache`: no projeto legado voltam a ser uma classe
// só, então nenhum chamador precisou mudar. No Chummer.Core, só o domínio existe.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Xml.XPath;
using Newtonsoft.Json;
using static Chummer.EventHandlerExtensions;

namespace Chummer
{
    public sealed partial class CharacterCache
    {
        private Image _imgMugshot;
        private SafeAsyncEventHandler<ValueTuple<KeyEventArgs, TreeNode>> _onMyKeyDown;
        [JsonIgnore]
        [XmlIgnore]
        [IgnoreDataMember]
        public Image Mugshot
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _imgMugshot;
            }
        }
        public async Task<Image> GetMugshotAsync(CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                return _imgMugshot;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }
        [JsonIgnore]
        [XmlIgnore]
        [IgnoreDataMember]
        public SafeAsyncEventHandler<ValueTuple<KeyEventArgs, TreeNode>> OnMyKeyDown
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _onMyKeyDown;
            }
        }
        public async Task OnDefaultKeyDown(object sender, ValueTuple<KeyEventArgs, TreeNode> args, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (args.Item1.KeyCode == Keys.Delete)
            {
                switch (args.Item2.Parent.Tag.ToString())
                {
                    case "Recent":
                        await GlobalSettings.MostRecentlyUsedCharacters.RemoveAsync(await GetFilePathAsync(token).ConfigureAwait(false), token).ConfigureAwait(false);
                        break;

                    case "Favorite":
                        await GlobalSettings.FavoriteCharacters.RemoveAsync(await GetFilePathAsync(token).ConfigureAwait(false), token).ConfigureAwait(false);
                        break;
                }
            }
        }
    }
}
