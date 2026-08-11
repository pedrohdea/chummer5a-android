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

// Metade dependente de WinForms de StackedFocus, separada do domínio durante a
// extração do núcleo (DEC-023). A metade de regras vive em Chummer/Backend/Uniques/StackedFocus.cs.
//
// As duas são `partial class StackedFocus`: no projeto legado voltam a ser uma classe
// só, então nenhum chamador precisou mudar. No Chummer.Core, só o domínio existe.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Chummer.Backend.Equipment;

namespace Chummer
{
    public partial class StackedFocus
    {
        public async Task<TreeNode> CreateTreeNode(Gear objGear, ContextMenuStrip cmsStackedFocus, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (objGear == null)
                throw new ArgumentNullException(nameof(objGear));
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                TreeNode objNode = await objGear.CreateTreeNode(cmsStackedFocus, null, token).ConfigureAwait(false);

                objNode.Name = InternalId;
                objNode.Text = await LanguageManager.GetStringAsync("String_StackedFocus", token: token).ConfigureAwait(false)
                               + await LanguageManager.GetStringAsync("String_Colon", token: token).ConfigureAwait(false)
                               + await LanguageManager.GetStringAsync("String_Space", token: token).ConfigureAwait(false)
                               + await GetCurrentDisplayNameAsync(token).ConfigureAwait(false);
                objNode.Tag = this;
                objNode.Checked = Bonded;

                return objNode;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
