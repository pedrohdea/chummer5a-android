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

// Metade dependente de WinForms de Location, separada do domínio durante a
// extração do núcleo (DEC-023). A metade de regras vive em Chummer/Backend/Uniques/Location.cs.
//
// As duas são `partial class Location`: no projeto legado voltam a ser uma classe
// só, então nenhum chamador precisou mudar. No Chummer.Core, só o domínio existe.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace Chummer
{
    public partial class Location
    {
        public async Task<TreeNode> CreateTreeNode(ContextMenuStrip cmsLocation, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                TreeNode objNode = new TreeNode
                {
                    Name = InternalId,
                    Text = await GetCurrentDisplayNameAsync(token).ConfigureAwait(false),
                    Tag = this,
                    ContextMenuStrip = cmsLocation,
                    ForeColor = await GetPreferredColorAsync(token).ConfigureAwait(false),
                    ToolTipText = (await GetNotesAsync(token).ConfigureAwait(false)).WordWrap()
                };

                return objNode;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
